#if UNITY_EDITOR || RECORDING_CAPTURE
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

/// Opt-in capture harness. Runs only in a recording player launched with
/// --record-playthrough <mp4>. Gameplay uses queued keyboard/mouse input and real raycasts;
/// the password is read only from the four discovered HUD clues. No win/lose state is set.
public sealed class PlaythroughRecorder : MonoBehaviour
{
    const int Fps = 30;
    Keyboard keyboard;
    Mouse mouse;
    Key[] held = Array.Empty<Key>();
    Vector2 lookDelta;
    Process encoder;
    Stream video, audio;
    int audioRate;
    string output, caption = "", stage = "initializing";
    bool recording;
    int frames;
    Vector3 room;
    string[] digits;
    string[] firstRoomDigits;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "--record-playthrough");
        if (index < 0 || index + 1 >= args.Length) return;
        var go = new GameObject("RecordingOnly_InputReplay");
        DontDestroyOnLoad(go);
        go.AddComponent<PlaythroughRecorder>().output = args[index + 1];
    }

    IEnumerator Start()
    {
        Application.logMessageReceived += OnLog;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Environment.GetCommandLineArgs().Contains("--chapter-check") ? -1 : Fps;
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        keyboard = InputSystem.AddDevice<Keyboard>("ReplayKeyboard");
        mouse = InputSystem.AddDevice<Mouse>("ReplayMouse");
        InputSystem.onBeforeUpdate += FeedInput;
        yield return new WaitForSeconds(2);
        var audioConfig=AudioSettings.GetConfiguration();audioConfig.speakerMode=AudioSpeakerMode.Stereo;AudioSettings.Reset(audioConfig);
        audioRate=AudioSettings.outputSampleRate;
        Time.captureFramerate = Fps;
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        encoder = new Process { StartInfo = new ProcessStartInfo {
            FileName = "/opt/homebrew/bin/ffmpeg",
            Arguments = "-y -hide_banner -loglevel error -f rawvideo -pixel_format rgba -video_size 1280x720 -framerate 30 -i pipe:0 -vf vflip -c:v libx264 -preset fast -crf 20 -pix_fmt yuv420p -movflags +faststart \"" + output + "\"",
            UseShellExecute = false, RedirectStandardInput = true, CreateNoWindow = true
        }};
        encoder.Start();
        video = encoder.StandardInput.BaseStream;
        audio=File.Create(output+".f32le");
        if(!AudioRenderer.Start())throw new Exception("Audio capture could not start");
        recording = true;
        StartCoroutine(Capture());
        yield return Walkthrough();
        caption = "";
        yield return Hold(2);
        recording = false;
        yield return null;
        video.Close();audio.Close();AudioRenderer.Stop();
        encoder.WaitForExit();
        File.WriteAllText(output + ".json", JsonUtility.ToJson(new CaptureResult {
            success = encoder.ExitCode == 0, frames = frames, seconds = frames / (float)Fps,
            method = "Automated keyboard/mouse input replay; native Unity framebuffer and game audio; normal game rules", stage = stage, audioSampleRate=audioRate, audioChannels=2
        }, true));
        Debug.Log("RECORDING_COMPLETE " + output);
        Application.Quit(encoder.ExitCode);
    }

    [Serializable] class CaptureResult { public bool success; public int frames; public float seconds; public string method; public string stage; public int audioSampleRate; public int audioChannels; }

    void FeedInput()
    {
        if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        if(!keyboard.enabled)InputSystem.EnableDevice(keyboard);
        if(!mouse.enabled)InputSystem.EnableDevice(mouse);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(held));
        InputSystem.QueueStateEvent(mouse, new MouseState { delta = lookDelta, position = new Vector2(640, 360) });
        keyboard.MakeCurrent();
        mouse.MakeCurrent();
    }

    IEnumerator Capture()
    {
        while (recording)
        {
            yield return new WaitForEndOfFrame();
            if (!recording) break;
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            if (tex.width != 1280 || tex.height != 720 || tex.format != TextureFormat.RGBA32)
                throw new Exception($"Unexpected framebuffer: {tex.width}x{tex.height} {tex.format}");
            byte[] bytes = tex.GetRawTextureData<byte>().ToArray();
            video.Write(bytes, 0, bytes.Length);
            Destroy(tex);
            int samples=AudioRenderer.GetSampleCountForCaptureFrame();
            using(var buffer=new Unity.Collections.NativeArray<float>(samples*2,Unity.Collections.Allocator.Temp))
            {
                if(!AudioRenderer.Render(buffer))throw new Exception("Audio frame capture failed");
                var values=buffer.ToArray();var pcm=new byte[values.Length*4];Buffer.BlockCopy(values,0,pcm,0,pcm.Length);audio.Write(pcm,0,pcm.Length);
            }
            frames++;
        }
    }

    IEnumerator Walkthrough()
    {
        if(Environment.GetCommandLineArgs().Contains("--chapter-check"))
        {
            Stage("development_fixture", "DEVELOPMENT FIXTURE / NOT A SUBMISSION RECORDING");
            GameState.BeginNewGame();GameState.MarkRoomSolved(1);GameState.MarkRoomSolved(2);
            GameState.SetCheckpoint(3,new System.Collections.Generic.List<string>());GameState.FailedRoomNumber=3;
            SceneManager.LoadScene("WhiteBox_LastLight");yield return Hold(3);
            firstRoomDigits=GameState.Room1Code.Select(i=>i.ToString()).ToArray();
            room=new Vector3(16,0,24);yield return Move(0,-4.6f);
            room=new Vector3(16,0,16);yield return Move(0,0);yield return Move(-4.6f,0);
            yield return LaterRooms();yield break;
        }
        Stage("title", "THE LAST LIGHT  |  CJ Evan");
        yield return Hold(5);
        Button("StartButton").onClick.Invoke();
        yield return Hold(14);
        var player = FirstPersonController.Instance;
        room = player.transform.position + new Vector3(0, 0, 2);
        room.y = 0;
        Stage("explore", "WASD to move  |  Mouse to look  |  E to interact");
        yield return Hold(3);
        yield return Move(0, 0.4f);
        yield return Move(2.1f, 0.8f);
        var cabinet = FindObjectOfType<MovableCabinetClue>();
        Stage("cabinet", "01  /  Move the cabinet to uncover the circle clue");
        yield return Aim(cabinet.transform.position + Vector3.up * 0.4f);
        yield return Interact<MovableCabinetClue>();
        yield return Hold(2);
        yield return Move(2f, 1.5f);
        yield return Aim(cabinet.revealTarget.transform.position);
        yield return Hold(3);
        RequireClue(0);

        Stage("pillar", "02  /  Look behind the pillar");
        yield return Move(0f, 1.3f);
        yield return Move(-1f, 1.3f);
        yield return Move(-1f, 2.62f);
        yield return Move(-2f, 2.62f);
        var pillar = GameObject.Find("Gameplay_PillarClue");
        yield return Aim(pillar.transform.position);
        yield return Hold(4);
        RequireClue(2);

        Stage("switch", "03  /  Turn off the light to reveal the bar clue");
        yield return Move(-1f, 2.62f);
        yield return Move(-1f, 0.6f);
        yield return Move(-1.9f, -0.6f);
        var lightSwitch = FindObjectOfType<LightSwitchClue>();
        yield return Aim(lightSwitch.transform.Find("Mesh").position);
        yield return Interact<LightSwitchClue>();
        yield return Hold(1);
        yield return Aim(lightSwitch.label.transform.position);
        yield return Hold(4);
        RequireClue(3);

        Stage("drawer", "04  /  Open the drawer and read the note");
        yield return Move(-0.8f, 0.5f);
        yield return Move(1.8f, 0.1f);
        var drawer = FindObjectOfType<DrawerClue>();
        yield return Aim(drawer.transform.position);
        yield return Interact<DrawerClue>();
        yield return Hold(1);
        yield return Aim(drawer.paperObject.transform.GetChild(0).position);
        yield return Interact<PaperCloseupClue>();
        yield return Hold(4);
        if (!PaperCloseupUI.Instance.IsOpen) throw new Exception("Note was not opened by E input");
        RequireClue(1);
        yield return Press(Key.Escape);
        yield return Hold(1);

        Stage("keypad", "Match the digits in this order: circle, triangle, square, bar");
        digits = ClueTracker.Instance.slots.Select(t => t.text.Trim().Split(' ').Last()).ToArray();
        firstRoomDigits = (string[])digits.Clone();
        yield return Move(2.1f, -0.5f);
        var keypad = GameObject.Find("Gameplay_Keypad").GetComponent<KeypadController>();
        yield return Aim(keypad.transform.position);
        yield return Interact<KeypadController>();
        yield return Hold(3);
        if (!keypad.ui.IsOpen) throw new Exception("Keypad was not opened by E input");
        Key[] numberKeys = { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };
        foreach (string digit in digits)
        {
            yield return Press(numberKeys[int.Parse(digit)]);
            yield return Hold(0.65f);
        }
        yield return Hold(1.5f);
        yield return Press(Key.Enter);
        yield return Hold(2);
        if (!keypad.ui.Solved) throw new Exception("Recorded digits did not solve keypad");
        Stage("escape", "Door unlocked. Walk through the exit.");
        yield return Move(2.1f, 0f);
        yield return Move(4.6f, 0f);
        yield return Hold(1);
        if (!GameState.IsRoomSolved(1)) throw new Exception("Room 1 exit was not completed");
        Stage("room2_entry", "Room 1 complete. Enter the sample storage room.");
        room = GameObject.Find("Rooms").transform.Cast<Transform>().First(t=>t.name.Contains("R2_LockedCounter")).position;
        yield return Move(-1.6f,0);
        yield return Hold(2);
        Stage("stool", "Move the stool underneath the ceiling access panel");
        yield return Move(-1.6f,-.9f);
        var stool=GameObject.Find("Gameplay_Stool");
        yield return Aim(stool.transform.position+Vector3.up*.25f);
        yield return Interact<StoolPickupTarget>();
        if(!StoolCarrier.Instance.IsCarrying) throw new Exception("Stool pickup failed");
        yield return Move(0,-.9f);
        yield return Aim(room);
        yield return Hold(1);
        yield return Press(Key.E);
        yield return Hold(1);
        if(StoolCarrier.Instance.IsCarrying)throw new Exception("Stool placement failed");
        Stage("ceiling_card", "Step onto the stool and open the ceiling panel");
        yield return Move(0,0);
        var panel=FindObjectOfType<CeilingCardPanel>();
        yield return Aim(panel.cover.position + new Vector3(.2f,0,.2f));
        yield return Interact<CeilingCardPanel>();
        yield return Hold(2);
        if(!Inventory.Instance.HasItem("AccessCard"))throw new Exception("Access card not collected");
        Stage("reader", "Use the access card to unlock the cabinet");
        yield return Move(1.4f,-.8f);
        yield return Move(1.4f,-2.1f);
        var reader=FindObjectOfType<CardReader>();
        yield return Aim(reader.transform.position);
        yield return Interact<CardReader>();
        yield return Hold(2);
        Stage("calibration", "Collect the calibration card from the cabinet");
        yield return Move(.1f,-1.2f);
        var card=GameObject.Find("Gameplay_CalibrationCard");
        yield return Aim(card.transform.position);
        yield return Interact<InventoryPickup>();
        yield return Hold(2);
        if(!Inventory.Instance.HasItem("CalibrationCard"))throw new Exception("Calibration card not collected");
        Stage("tape_one", "R. Shen's first recording: traces survive outside the light");
        yield return Aim(GameObject.Find("Story_Tape1").transform.position);
        yield return Interact<StoryObject>();yield return Hold(9);yield return Press(Key.Escape);
        Stage("matrix", "Rotate the card so its notch aligns with the display");
        yield return Move(-1.2f,-.8f);
        yield return Move(-1.2f,1.45f);
        var screen=FindObjectOfType<DataMatrixScreen>();
        yield return Aim(screen.transform.position);
        yield return Interact<DataMatrixScreen>();
        yield return Hold(2);
        var align=CardAlignmentUI.Instance;
        if(!align.IsOpen)throw new Exception("Alignment screen not open");
        yield return Press(Key.R);yield return Hold(1);
        yield return Press(Key.R);yield return Hold(1);
        yield return Press(Key.E);yield return Hold(3);
        if(!align.Aligned)throw new Exception("Card failed to align");
        digits=align.holeCellIndex.Select(i=>align.matrixCells[i].text.Trim()).ToArray();
        Stage("revealed_code", "Read the four revealed digits in pinhole order");
        yield return Hold(4);yield return Press(Key.Escape);yield return Hold(1);
        Stage("room2_keypad", "Enter the code revealed by the calibration card");
        yield return Move(.5f,1.4f);yield return Move(1.2f,2.2f);
        var keypad2=GameObject.Find("Gameplay_Keypad2").GetComponent<KeypadController>();
        yield return Aim(keypad2.transform.position);yield return Interact<KeypadController>();
        yield return Hold(2);
        foreach(var digit in digits){yield return Press(numberKeys[int.Parse(digit)]);yield return Hold(.65f);}
        yield return Hold(1);yield return Press(Key.Enter);yield return Hold(2);
        if(!keypad2.ui.Solved)throw new Exception("Room 2 keypad not solved");
        Stage("room2_exit", "Room 2 unlocked. Continue through the doorway.");
        yield return Move(0,1.5f);yield return Move(0,4.7f);yield return Hold(2);
        if(!GameState.IsRoomSolved(2))throw new Exception("Room 2 exit did not register");
        yield return LaterRooms();

    }

    IEnumerator ReadStory(string name, float seconds = 7)
    {
        yield return Aim(GameObject.Find(name).transform.position);
        yield return Interact<StoryObject>();yield return Hold(seconds);yield return Press(Key.Escape);yield return Hold(.4f);
    }
    IEnumerator Use(string name)
    {
        yield return Aim(GameObject.Find(name).transform.position);
        yield return Interact<CampaignInteractable>();yield return Hold(.5f);
    }
    IEnumerator LaterRooms()
    {
        room=new Vector3(8,0,16);
        Stage("repeat", "An empty repeat room. A lamp base was left on the shelf.");
        yield return Move(0,-1.5f);yield return Move(.7f,1.2f);yield return Move(1.6f,1.4f);
        yield return Use("LampBase");
        yield return Move(1.5f,0);yield return Move(4.5f,0);
        room=new Vector3(16,0,16);yield return Move(-1.8f,0);yield return Move(0,0);yield return Move(0,4.6f);
        room=new Vector3(16,0,24);
        Stage("lamp_head", "Room 3 / Find the missing lamp head beneath the workbench");
        yield return Move(0,-.6f);yield return Move(-.3f,-.65f);yield return Use("LampHead");
        yield return Move(0,0);yield return Move(1.2f,.6f);
        Stage("assembly", "Fit both parts into the cradle. F controls the inspection beam.");
        yield return Use("LampCradle");yield return Press(Key.F);yield return Hold(2);
        if(!FullCampaign.Instance.LampOn)throw new Exception("Inspection lamp did not turn on");
        yield return Move(-.8f,1.3f);yield return ReadStory("Tape2",8);
        Stage("side_light", "Hold the beam nearly parallel to the blank plate");
        yield return Move(.15f,2.15f);
        yield return Aim(new Vector3(17.35f,1.55f,26.61f));yield return Hold(5);
        if(!FullCampaign.Instance.ArrowsVisible)throw new Exception("Side light did not reveal arrows");
        var arrows=FullCampaign.Instance.arrowText.text.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
        if(arrows.Length!=4)throw new Exception("Unreadable breaker clue");
        Stage("breakers", "Set the four breakers to match the revealed arrows");
        yield return Move(1.2f,1.5f);
        for(int i=0;i<4;i++)if(arrows[i]=="▲")yield return Use("Breaker"+i);
        yield return Use("MainBreaker");yield return Hold(2);
        if(!GameState.IsRoomSolved(3))throw new Exception("Room 3 failed to unlock");
        Stage("aftermath", "The circuit holds. Carry the light into the next room.");
        yield return Move(1.2f,0);yield return Move(4.6f,0);
        room=new Vector3(24,0,24);yield return Move(-1.6f,0);yield return Move(-1f,1.3f);yield return ReadStory("AftermathNote",5);
        yield return Move(0,1.3f);yield return Move(0,4.6f);
        room=new Vector3(24,0,32);
        Stage("sleep_pod", "Room 4 / R. Shen found a place to stay, not a way out");
        yield return Move(-.9f,-1.8f);yield return Move(-1.4f,-.2f);yield return Use("PodHandle");yield return Hold(7);yield return Press(Key.Escape);
        yield return Move(-1.3f,1.5f);yield return ReadStory("Tape3",8);
        Stage("reflection", "Rotate the steel plate, then illuminate it with the inspection lamp");
        yield return Move(-1.3f,-1.1f);yield return Move(-2f,-.8f);
        for(int i=0;i<3;i++)yield return Use("MirrorControl");
        yield return Aim(new Vector3(22,1.25f,30.45f));yield return Hold(5);
        if(!FullCampaign.Instance.MirrorVisible)throw new Exception("Mirror clue not visible");
        string reflected=FullCampaign.Instance.reflectionText.text.Split('\n')[0];
        var glyphs=reflected.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
        if(glyphs.Length!=4)throw new Exception("Mirror sequence unreadable");
        var code=glyphs.Select(g=>int.Parse(firstRoomDigits[Enumerable.Range(0,4).First(i=>CodeManager.SymbolGlyph((ClueSymbol)i)==g)])).ToArray();
        Stage("dials", "Use the mirror's symbol order with the digits collected in Room 1");
        yield return Move(-1.2f,-1.8f);yield return Move(1.3f,-1.8f);yield return Move(1.5f,1.7f);
        yield return Use("MaintenancePanel");
        for(int i=0;i<4;i++) {yield return Aim(GameObject.Find("Dial"+i).transform.position);for(int j=0;j<code[i];j++){yield return Press(Key.E);yield return Hold(.25f);}}
        yield return Use("DialConfirm");yield return Hold(3);
        if(!GameState.IsRoomSolved(4))throw new Exception("Room 4 did not unlock");
        Stage("final_corridor", "Beyond the last lock, another door marked ROOM 1");
        yield return Move(1.5f,0);yield return Move(4.6f,0);
        room=new Vector3(32,0,32);yield return Move(-1.8f,0);yield return Move(0,0);yield return Move(0,4.5f);
        room=new Vector3(32,0,40);yield return Move(0,-1);yield return Move(0,0);
        Stage("tape_four", "The final tape was hidden above the light");
        yield return Aim(GameObject.Find("Tape4").transform.position+new Vector3(.12f,0,0));yield return Interact<CampaignInteractable>();yield return Hold(12);yield return Press(Key.Escape);
        if(!FullCampaign.Instance.TapeFourHeard)throw new Exception("Tape 4 not read");
        yield return Move(0,1.2f);
        if(Environment.GetCommandLineArgs().Contains("--normal-ending"))
        {
            Stage("normal_ending", "Choose the door marked ROOM 1");yield return Move(0,5.7f);yield return Use("LoopDoor");yield return Hold(6);
        }
        else
        {
            Stage("master_switch", "Cut the building's power. Only the inspection lamp remains.");yield return Use("MasterSwitch");yield return Hold(7);
            Stage("last_light", "Turn off the last light. Step into the dark.");yield return Press(Key.F);yield return Hold(2);
            if(!FullCampaign.Instance.DarkEscapeArmed)throw new Exception("Last light did not release the walls");
            // Actual movement; the game's ending triggers after walking eight metres in darkness.
            yield return Aim(FirstPersonController.Instance.Camera.transform.position+Vector3.forward*10);
            held=new[]{Key.W};yield return new WaitForSeconds(3);held=Array.Empty<Key>();lookDelta=Vector2.zero;
            yield return Hold(7);
        }
        if(SceneManager.GetActiveScene().name!="End" || !GameState.Won)throw new Exception("Ending did not complete");
        Stage("complete",FullCampaign.Ending=="true"?"YOU NOCLIPPED OUT  /  Complete playthrough":"YOU FOUND A DOOR  /  The loop continues");yield return Hold(6);
    }

    static Button Button(string name) => GameObject.Find(name).GetComponent<Button>();
    void Stage(string name, string text) { stage = name; caption = text; Debug.Log($"CAPTURE_STAGE {name} frame={frames}"); ScreenCapture.CaptureScreenshot(output+"."+name+".png"); }
    IEnumerator Hold(float seconds) { held = Array.Empty<Key>(); lookDelta = Vector2.zero; yield return new WaitForSeconds(seconds); }
    IEnumerator Press(Key key) { held = new[] { key }; yield return null; yield return null; held = Array.Empty<Key>(); yield return null; yield return null; }

    IEnumerator Interact<T>() where T : Component
    {
        var cam = FirstPersonController.Instance.Camera;
        if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, 2f, ~0, QueryTriggerInteraction.Collide)
            || hit.collider.GetComponentInParent<T>() == null)
            throw new Exception("Interaction ray missed " + typeof(T).Name + "; hit=" + (hit.collider == null ? "none" : hit.collider.name) + "; pos=" + cam.transform.position);
        yield return Hold(1);
        yield return Press(Key.E);
    }

    void RequireClue(int slot)
    {
        string value = ClueTracker.Instance.slots[slot].text;
        if (!char.IsDigit(value.Last())) throw new Exception("Clue was not discovered: " + slot + " " + value);
        Debug.Log("DISCOVERED_CLUE " + slot + " " + value);
    }

    IEnumerator Aim(Vector3 target)
    {
        held = Array.Empty<Key>();
        for (int n = 0; n < Fps * 8; n++)
        {
            if (SetLook(target) < 0.3f) { lookDelta = Vector2.zero; yield return null; yield break; }
            yield return null;
        }
        throw new Exception("Camera aim timed out at " + stage);
    }

    float SetLook(Vector3 target)
    {
        var player = FirstPersonController.Instance;
        var cam = player.Camera.transform;
        Vector3 dir = target - cam.position;
        float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude) * Mathf.Rad2Deg;
        float dy = Mathf.DeltaAngle(player.transform.eulerAngles.y, yaw);
        float dp = Mathf.DeltaAngle(cam.localEulerAngles.x, pitch);
        lookDelta = new Vector2(Mathf.Clamp(dy, -2f, 2f), -Mathf.Clamp(dp, -2f, 2f)) / player.mouseSensitivity;
        return Mathf.Max(Mathf.Abs(dy), Mathf.Abs(dp));
    }

    IEnumerator Move(float x, float z, bool exit = false)
    {
        Vector3 target = room + new Vector3(x, 0, z);
        for (int n = 0; n < Fps * 12; n++)
        {
            if (exit && SceneManager.GetActiveScene().name == "End") { held = Array.Empty<Key>(); lookDelta = Vector2.zero; yield break; }
            var player = FirstPersonController.Instance;
            Vector3 delta = target - player.transform.position;
            delta.y = 0;
            if (delta.magnitude < 0.13f) { yield return Hold(0.25f); yield break; }
            float angle = SetLook(target + Vector3.up * player.Camera.transform.position.y);
            held = angle < 8f ? new[] { Key.W } : Array.Empty<Key>();
            yield return null;
        }
        throw new Exception("Movement blocked at " + stage + "; target=" + target + "; position=" + FirstPersonController.Instance.transform.position + "; input="+FirstPersonController.Instance.InputEnabled+" enabled="+keyboard.enabled+" W="+keyboard.wKey.isPressed);
    }

    void OnGUI()
    {
        if (!recording) return;
        var small = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleRight };
        small.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        GUI.Label(new Rect(Screen.width - 355, 14, 330, 24), Environment.GetCommandLineArgs().Contains("--chapter-check") ? "DEVELOPMENT FIXTURE / NOT SUBMISSION" : "GAMEPLAY CAPTURE  /  AUTOMATED INPUT", small);
        if (string.IsNullOrEmpty(caption)) return;
        var style = new GUIStyle(GUI.skin.box) { fontSize = 22, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(15,15,8,8) };
        GUI.Box(new Rect(200, Screen.height - 125, Screen.width - 240, 52), caption, style);
    }

    void OnLog(string message,string stack,LogType type)
    {
        if(type!=LogType.Exception && type!=LogType.Error)return;
        if(!recording)return;
        recording=false;
        File.WriteAllText(output+".failure.txt",stage+"\n"+message+"\n"+stack);
        video?.Close();
        Application.Quit(1);
    }
    void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        InputSystem.onBeforeUpdate -= FeedInput;
        if (video != null) video.Dispose();
        audio?.Dispose();AudioRenderer.Stop();
        if (keyboard != null) InputSystem.RemoveDevice(keyboard);
        if (mouse != null) InputSystem.RemoveDevice(mouse);
        Time.captureFramerate = 0;
    }
}
#endif
