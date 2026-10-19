using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Completes the existing spatial layout. All puzzle interactions run through the normal E raycast.
public class FullCampaign : MonoBehaviour
{
    public static FullCampaign Instance { get; private set; }
    public static string Ending = "";
    public static readonly HashSet<int> FailedRooms = new HashSet<int>();
    public static readonly List<Vector3> LastFootprints = new List<Vector3>();
    public static readonly List<Vector3> LastTouches = new List<Vector3>();
    public static readonly List<Vector3> CurrentFootprints = new List<Vector3>();
    public static readonly List<Vector3> CurrentTouches = new List<Vector3>();
    public bool LampOwned { get; private set; }
    public bool LampOn { get; private set; }
    public bool MasterOff { get; private set; }
    public bool TapeFourHeard { get; private set; }
    public bool DarkEscapeArmed { get; private set; }
    public bool RoomThreeSolved => GameState.IsRoomSolved(3);
    public bool RoomFourSolved => GameState.IsRoomSolved(4);
    public bool ArrowsVisible { get; private set; }
    public bool MirrorVisible { get; private set; }
    public int MirrorStep { get; private set; }
    public readonly bool[] Breakers = new bool[4];
    public readonly int[] Dials = new int[4];
    public TMP_Text arrowText, reflectionText;
    RoomTimer timer3, timer4;
    GameObject gate3, gate4, janitor, mirrorFace, hiddenNames, map, master;
    Light lamp;
    Transform player;
    Vector3 darkOrigin;
    readonly HashSet<int> entered = new HashSet<int>();
    readonly List<TMP_Text> breakerLabels = new List<TMP_Text>();
    readonly List<TMP_Text> dialLabels = new List<TMP_Text>();
    float lastPrint, janitorUntil, nextFinalBlink;
    bool repeatSeen, finishing, namesReward, mapReward;
    Material neutral, metal, paper;
    Camera reflectionCamera;
    RenderTexture mirrorTexture;
    float finalJanitorDistance = 9f;
    int RetryRoom => GameState.FailedRoomNumber;

    public static void ResetRun()
    {
        Ending = ""; FailedRooms.Clear(); LastFootprints.Clear(); LastTouches.Clear();
        CurrentFootprints.Clear(); CurrentTouches.Clear();
    }
    public static void SaveFailure(int room)
    {
        FailedRooms.Add(room);
        LastFootprints.Clear(); LastFootprints.AddRange(CurrentFootprints);
        LastTouches.Clear(); LastTouches.AddRange(CurrentTouches);
        PlayerPrefs.SetInt("LastLight.Failures", PlayerPrefs.GetInt("LastLight.Failures") + 1); PlayerPrefs.Save();
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; }
    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "WhiteBox_LastLight") new GameObject("FullCampaign").AddComponent<FullCampaign>();
    }
    void Awake() { Instance = this; }
    IEnumerator Start()
    {
        yield return null;
        player = FirstPersonController.Instance.transform;
        neutral = Mat(new Color(.46f,.49f,.47f)); metal = Mat(new Color(.18f,.23f,.23f)); paper = Mat(new Color(.72f,.72f,.61f));
        BuildRoomThree(); BuildRoomFour(); BuildExit(); BuildJanitor(); RestoreCheckpoint();
        CurrentFootprints.Clear(); CurrentTouches.Clear();
        foreach (var p in LastFootprints) Box("PreviousFootprint",p + Vector3.up*.012f,new Vector3(.10f,.005f,.23f),metal,false);
        foreach (var p in LastTouches) Box("PreviousTouch",p,new Vector3(.045f,.045f,.015f),paper,false);
    }
    Material Mat(Color c) { var m = new Material(Shader.Find("Universal Render Pipeline/Lit")); m.color = c; m.SetFloat("_Smoothness",.12f); return m; }
    GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat, bool collider = true)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name; o.transform.SetParent(transform);
        o.transform.position = pos; o.transform.localScale = scale; o.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Destroy(o.GetComponent<Collider>()); return o;
    }
    TMP_Text Label(string name, string text, Vector3 pos, float size = .14f, float width = 1.6f)
    {
        var o = new GameObject(name); o.transform.SetParent(transform); o.transform.position = pos;
        var t = o.AddComponent<TextMeshPro>(); t.text = text; t.fontSize = size*10; t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(width,.7f); t.color = new Color(.88f,.91f,.80f); return t;
    }
    CampaignInteractable Target(string name, string action, Vector3 p, Vector3 s, string prompt, int index = 0)
    {
        var go = Box(name,p,s,metal); var target=go.AddComponent<CampaignInteractable>(); target.action=action;target.prompt=prompt;target.index=index;return target;
    }
    void Note(string name, string title, string text, Vector3 p)
    {
        var o=Box(name,p,new Vector3(.3f,.15f,.05f),paper); var story=o.AddComponent<StoryObject>();story.heading=title;story.body=text;
    }
    Transform Room(string token) => GameObject.Find("Rooms").transform.Cast<Transform>().First(t=>t.name.Contains(token));
    RoomTimer Timer(int number, Transform room, float seconds)
    {
        var t=gameObject.AddComponent<RoomTimer>();t.roomNumber=number;t.totalDuration=seconds;
        t.roomLights=room.GetComponentsInChildren<Light>();t.roomPanels=Array.Empty<Renderer>();
        t.flickerWindow=seconds*.3f;t.shutoffWindow=seconds*.05f;return t;
    }
    void HideLabels(Transform room)
    {
        foreach(var t in room.GetComponentsInChildren<TMP_Text>()) if(t.name.StartsWith("Label_"))t.gameObject.SetActive(false);
        foreach(var t in room.GetComponentsInChildren<Transform>()) if(t.name=="Trace_Decal")
            foreach(var c in t.GetComponentsInChildren<Collider>())c.enabled=false;
    }
    void BuildRoomThree()
    {
        var r=Room("R4_PortableLight"); HideLabels(r);HideLabels(Room("R3_PatternAndSlot"));
        timer3=Timer(3,r,270);
        var baseModel=r.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Lamp_PartA");
        var headModel=r.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Lamp_PartB");
        foreach(var t in r.GetComponentsInChildren<Transform>()) if(t.name=="Counter_Drawer")t.gameObject.SetActive(false);
        if(baseModel)baseModel.gameObject.SetActive(false);if(headModel)headModel.gameObject.SetActive(false);
        var a=Target("LampBase","base",new Vector3(10.6f,1.1f,17.5f),new Vector3(.28f,.13f,.24f),"Take inspection lamp base");
        var b=Target("LampHead","head",new Vector3(14.7f,.48f,23.35f),new Vector3(.24f,.18f,.24f),"Take inspection lamp head");
        // Preserve the supplied Tripo visuals at the new functional pickup locations.
        CopyVisual(baseModel,a.transform);CopyVisual(headModel,b.transform);
        Box("WorkbenchLowerShelf",new Vector3(14.7f,.34f,23.35f),new Vector3(.65f,.08f,.5f),neutral);
        Box("FShelf",new Vector3(10.65f,1f,17.5f),new Vector3(.55f,.08f,.6f),neutral);
        Target("LampCradle","cradle",new Vector3(18.4f,1f,25.2f),new Vector3(.75f,.16f,.4f),"Fit lamp parts / take inspection lamp");
        Label("CradleLabel","Hold it low.\nShadows show what light erases.",new Vector3(18.4f,1.45f,25f),.12f);
        Target("BlankPlate","plate",new Vector3(17.35f,1.55f,26.65f),new Vector3(1.1f,.4f,.05f),"Inspect blank plate");
        arrowText=Label("R3Arrows","",new Vector3(17.35f,1.55f,26.61f),.17f,1.35f);arrowText.textWrappingMode=TextWrappingModes.NoWrap;arrowText.gameObject.SetActive(false);
        for(int i=0;i<4;i++)
        {
            var x=16.8f+i*.36f;Target("Breaker"+i,"breaker",new Vector3(x,1.02f,26.55f),new Vector3(.25f,.23f,.14f),"Toggle breaker "+(i+1),i);
            breakerLabels.Add(Label("BreakerState"+i,"DOWN",new Vector3(x,.76f,26.45f),.09f,.36f));
        }
        Target("MainBreaker","submit3",new Vector3(18.45f,1.05f,26.5f),new Vector3(.22f,.45f,.2f),"Submit breaker positions");
        gate3=Box("R3ExitDoor",new Vector3(19,1.1f,24),new Vector3(.16f,2.2f,1.05f),neutral);
        Note("Tape2","TAPE 02 / R. SHEN","The Janitors only move when the lights blink. I counted. Closer every time. Don't let the lights die while you're still inside.",new Vector3(15,1.2f,26.55f));
        hiddenNames=Label("HiddenNames","A. LIN  09/12\nM. CHEN  10/03\nJ. WU  11/19\nR. SHEN  01/07",new Vector3(16,1.45f,21.15f),.12f).gameObject;hiddenNames.transform.Rotate(0,180,0);hiddenNames.SetActive(false);
        if(GameState.IsRoomSolved(3))gate3.SetActive(false);
    }
    void CopyVisual(Transform source,Transform target)
    {
        if(!source)return;
        var child=source.Cast<Transform>().FirstOrDefault(t=>t.name.StartsWith("__Tripo"));if(!child)return;
        var copy=Instantiate(child.gameObject,target);copy.SetActive(true);copy.transform.localPosition=Vector3.zero;
        // Fit model using renderer world bounds so pickup colliders remain simple and reliable.
        var bounds=new Bounds(target.position,Vector3.zero);foreach(var rend in copy.GetComponentsInChildren<Renderer>())bounds.Encapsulate(rend.bounds);
        float max=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));if(max>.001f)copy.transform.localScale*=.32f/max;
        target.GetComponent<Renderer>().enabled=false;
    }
    void BuildRoomFour()
    {
        var r=Room("R6_MirrorAndCentre");HideLabels(r);timer4=Timer(4,r,240);
        foreach(var t in r.GetComponentsInChildren<Transform>()) if(t.name=="Desk" || t.name=="Key")t.gameObject.SetActive(false);
        Box("SleepPod",new Vector3(24, .65f,32.5f),new Vector3(1.3f,1.3f,2.1f),neutral);
        Box("PodWindow",new Vector3(24,1.32f,32.5f),new Vector3(.95f,.025f,1.65f),metal,false);
        var shen=GameObject.CreatePrimitive(PrimitiveType.Capsule);shen.name="Shen";shen.transform.SetParent(transform);shen.transform.position=new Vector3(24,1.47f,32.5f);shen.transform.rotation=Quaternion.Euler(90,0,0);shen.transform.localScale=new Vector3(.35f,.65f,.35f);Destroy(shen.GetComponent<Collider>());
        Target("PodHandle","pod",new Vector3(23.28f,1.12f,32.5f),new Vector3(.14f,.2f,.4f),"Try pod handle");
        Label("PodLabel","SLEEP POD\nOCCUPIED",new Vector3(24,1.8f,31.3f),.13f);
        var oldMirror=r.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Mirror_Panel");if(oldMirror)oldMirror.gameObject.SetActive(false);
        Target("MirrorControl","mirror",new Vector3(22,1.2f,30),new Vector3(.9f,1.1f,.16f),"Rotate steel plate 45 degrees");
        mirrorFace=Box("MirrorFace",new Vector3(22,1.25f,30.45f),new Vector3(.8f,.9f,.015f),metal,false);
        // Render a separate hidden, reverse-written physical clue into the steel plate.
        var clue=Label("HiddenMirrorClue","",new Vector3(200,20,200),.35f,5);
        clue.text=string.Join("   ",GameState.Room4MirrorOrder.Select(CodeManager.SymbolGlyph).ToArray())+"\nREAD LEFT TO RIGHT";
        clue.transform.localScale=new Vector3(-1,1,1);
        reflectionText=clue;
        mirrorTexture=new RenderTexture(768,512,16);
        reflectionCamera=new GameObject("SteelReflectionCamera").AddComponent<Camera>();reflectionCamera.transform.SetParent(transform);
        reflectionCamera.transform.position=new Vector3(200,20,197);reflectionCamera.orthographic=true;reflectionCamera.orthographicSize=1.6f;reflectionCamera.clearFlags=CameraClearFlags.SolidColor;reflectionCamera.backgroundColor=new Color(.025f,.04f,.035f);reflectionCamera.targetTexture=mirrorTexture;
        var reflectMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));reflectMaterial.SetTexture("_BaseMap",mirrorTexture);reflectMaterial.SetTexture("_EmissionMap",mirrorTexture);reflectMaterial.SetColor("_EmissionColor",Color.white);reflectMaterial.EnableKeyword("_EMISSION");reflectMaterial.SetTextureScale("_EmissionMap",new Vector2(-1,1));reflectMaterial.SetTextureOffset("_EmissionMap",new Vector2(1,0));reflectMaterial.SetTextureScale("_BaseMap",new Vector2(-1,1));reflectMaterial.SetTextureOffset("_BaseMap",new Vector2(1,0));mirrorFace.GetComponent<Renderer>().sharedMaterial=reflectMaterial;
        mirrorFace.SetActive(false);
        Label("MirrorNote","E  ROTATE",new Vector3(22,.48f,30.48f),.1f).transform.Rotate(0,180,0);
        Target("MaintenancePanel","panel4",new Vector3(26.4f,1.1f,34.5f),new Vector3(.6f,.55f,.1f),"Open marked maintenance panel");
        for(int i=0;i<4;i++)
        {
            float x=25.35f+i*.36f; var dial=Target("Dial"+i,"dial",new Vector3(x,1.1f,34.55f),new Vector3(.27f,.3f,.16f),"Turn dial "+(i+1),i);dial.gameObject.SetActive(false);
            dialLabels.Add(Label("DialState"+i,"0",new Vector3(x,1.1f,34.44f),.18f,.3f));dialLabels[i].gameObject.SetActive(false);
        }
        Target("DialConfirm","submit4",new Vector3(26.7f,.65f,34.55f),new Vector3(.25f,.18f,.12f),"Confirm dial code").gameObject.SetActive(false);
        gate4=Box("R4ExitDoor",new Vector3(27,1.1f,32),new Vector3(.16f,2.2f,1.05f),neutral);
        Note("Tape3","TAPE 03 / R. SHEN","I know how to leave. I just can't do it. I'm sorry. Maybe you're braver than me.",new Vector3(23,1.2f,34.6f));
        map=Label("HiddenLoopMap","ROOM 1 -> ROOM 2 -> ROOM 3 -> ROOM 4\n  ^___________________________________|\nOUT?",new Vector3(22,1.25f,30.48f),.10f,.9f).gameObject;map.transform.Rotate(0,180,0);map.SetActive(false);
        if(GameState.IsRoomSolved(4))gate4.SetActive(false);
    }
    void BuildExit()
    {
        var final=Room("R7_FinalAlignment");HideLabels(final);
        var seal=final.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Exit_Seal");if(seal)seal.gameObject.SetActive(false);
        Box("ExitFloor",new Vector3(32,-.08f,41),new Vector3(2,.16f,12),neutral);
        Box("ExitWallWest",new Vector3(30.9f,1.35f,41),new Vector3(.2f,2.7f,12),neutral);
        Box("ExitWallEast",new Vector3(33.1f,1.35f,41),new Vector3(.2f,2.7f,12),neutral);
        Box("ExitCeiling",new Vector3(32,2.75f,41),new Vector3(2.2f,.1f,12),neutral);
        for(int i=0;i<4;i++) { var l=new GameObject("ExitLight").AddComponent<Light>();l.transform.SetParent(transform);l.type=LightType.Point;l.transform.position=new Vector3(32,2.3f,36+i*3);l.intensity=.7f;l.range=5;l.color=new Color(.76f,.85f,.88f); }
        Target("LoopDoor","normal",new Vector3(32,1.1f,47),new Vector3(2,2.2f,.15f),"Open ROOM 1");
        Label("RoomOneDoorSign","ROOM 1",new Vector3(32,2.35f,46.85f),.22f);
        Box("ExitStool",new Vector3(32,.225f,40),new Vector3(.6f,.45f,.6f),neutral);
        Target("Tape4","tape4",new Vector3(32,2.64f,40),new Vector3(.35f,.06f,.25f),"Take tape from the light cover");
        master=Target("MasterSwitch","master",new Vector3(32.95f,1.3f,42),new Vector3(.16f,.45f,.35f),"Cut power to the facility").gameObject;master.SetActive(false);
        Label("ExitUncertainty","",new Vector3(32,1.4f,44));
        var source=Room("R5_Aftermath");HideLabels(source);
        Note("AftermathNote","A FINAL TRACE","The door is a loop. Don't stop at the door.",new Vector3(23,1.2f,26.5f));
    }
    void BuildJanitor()
    {
        janitor=new GameObject("The Janitor");janitor.transform.SetParent(transform);
        var white=Mat(new Color(.8f,.83f,.8f));
        var body=GameObject.CreatePrimitive(PrimitiveType.Capsule);body.transform.SetParent(janitor.transform,false);body.transform.localPosition=new Vector3(0,.85f,0);body.transform.localScale=new Vector3(.45f,.65f,.35f);body.GetComponent<Renderer>().sharedMaterial=white;Destroy(body.GetComponent<Collider>());
        var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.transform.SetParent(janitor.transform,false);head.transform.localPosition=new Vector3(0,1.65f,0);head.transform.localScale=Vector3.one*.3f;head.GetComponent<Renderer>().sharedMaterial=white;Destroy(head.GetComponent<Collider>());
        janitor.SetActive(false);
    }
    public void FlashJanitor(RoomTimer timer)
    {
        if(!player || RoomTimer.ReadingPaused || !timer.IsRunning)return;
        float remaining=timer.RemainingSeconds/timer.totalDuration;
        if(remaining>.15f || (remaining>.05f && UnityEngine.Random.value>.4f))return;
        var centre=timer.roomLights.Length>0 ? timer.roomLights.Aggregate(Vector3.zero,(a,l)=>a+l.transform.position)/timer.roomLights.Length : player.position;
        var spots=new[]{new Vector3(-2,0,-2),new Vector3(2,0,2),new Vector3(-2,0,2),new Vector3(2,0,-2),new Vector3(0,0,2),new Vector3(0,0,-2)}.Select(p=>new Vector3(centre.x,0,centre.z)+p).Where(p=>Vector3.Distance(p,player.position)>=3f).OrderBy(p=>Vector3.Distance(p,player.position)).ToArray();
        if(spots.Length==0)return;
        ShowJanitor(remaining>.05f?spots.Last():spots.First(),.12f);
    }
    void ShowJanitor(Vector3 p,float seconds) { if(Vector3.Distance(p,player.position)<3f)return;janitor.transform.position=p;janitor.SetActive(true);janitorUntil=Time.time+seconds; }
    void RestoreCheckpoint()
    {
        if(RetryRoom>0)
        {
            var pos=RetryRoom==1?new Vector3(0,0,6):RetryRoom==2?new Vector3(6,0,8):RetryRoom==3?new Vector3(16,0,22):new Vector3(24,0,30);
            var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.position=pos;cc.enabled=true;
            Inventory.Instance.Clear();foreach(var id in GameState.CheckpointInventory)Inventory.Instance.TryAdd(new InventoryItem(id,id,Color.white));
            LampOwned=Inventory.Instance.HasItem("InspectionLamp");if(LampOwned)CreateLamp();
            Note("RetryNote","A NOTE AT YOUR FEET","I've been here before. Go faster.",pos+new Vector3(.5f,.025f,.5f));
        }
        if(Inventory.Instance.HasItem("LampBase") || LampOwned)GameObject.Find("LampBase")?.SetActive(false);
        if(Inventory.Instance.HasItem("LampHead") || LampOwned)GameObject.Find("LampHead")?.SetActive(false);
    }
    void CreateLamp()
    {
        if(lamp)return;
        var go=new GameObject("InspectionLamp");go.transform.SetParent(FirstPersonController.Instance.Camera.transform,false);go.transform.localPosition=new Vector3(.12f,-.15f,.15f);
        lamp=go.AddComponent<Light>();lamp.type=LightType.Spot;lamp.spotAngle=27;lamp.range=8;lamp.intensity=4;lamp.color=new Color(1,.91f,.7f);lamp.shadows=LightShadows.Soft;lamp.enabled=false;
    }
    bool Grazing(Vector3 pos,Vector3 normal)
    {
        if(!LampOn)return false;var cam=FirstPersonController.Instance.Camera.transform;var d=pos-cam.position;
        return d.magnitude<=1.5f && Mathf.Abs(Vector3.Dot(d.normalized,normal))<=.5f && Vector3.Angle(cam.forward,d)<14;
    }
    void Enter(int number,RoomTimer timer,string objective)
    {
        if(!entered.Add(number))return;
        if(!GameState.IsRoomSolved(number))
        { GameState.SetCheckpoint(number,Inventory.Instance.SnapshotIds());timer.BeginCountdown();ObjectiveToast.Instance?.Show(objective); }
    }
    void Update()
    {
        if(!player || finishing)return;
        if(janitor && (Time.time>janitorUntil || RoomTimer.ReadingPaused))janitor.SetActive(false);
        if(Time.time-lastPrint>1 && !RoomTimer.ReadingPaused) {lastPrint=Time.time;if(CurrentFootprints.Count>=100)CurrentFootprints.RemoveAt(0);CurrentFootprints.Add(new Vector3(player.position.x,0,player.position.z));}
        if(!repeatSeen && Near(new Vector3(8,0,16),2.8f)) {repeatSeen=true;ShowJanitor(new Vector3(8,0,18.4f),.5f);}
        if(Near(new Vector3(16,0,24),2.8f))Enter(3,timer3,"Something is missing from the cradle. The blank plate isn't blank.");
        if(Near(new Vector3(24,0,32),2.8f))Enter(4,timer4,"The obvious lock is not the real one. Some things are only seen in reflection.");
        if(Keyboard.current!=null && Keyboard.current.fKey.wasPressedThisFrame && FirstPersonController.Instance.InputEnabled && LampOwned && !DarkEscapeArmed)
        {
            LampOn=!LampOn;lamp.enabled=LampOn;
            if(MasterOff && !LampOn)ArmDarkEscape();
        }
        ArrowsVisible=Grazing(new Vector3(17.35f,1.55f,26.61f),Vector3.back);
        arrowText.gameObject.SetActive(ArrowsVisible);
        arrowText.text=string.Join("   ",GameState.Room3ArrowUp.Select(up=>up?"▲":"▼").ToArray());
        bool names=Grazing(new Vector3(16,1.45f,21.15f),Vector3.forward);hiddenNames.SetActive(names);
        if(names && !namesReward) {namesReward=true;timer3.AddDiscoveryTime();}
        var view=FirstPersonController.Instance.Camera.transform;
        bool shine=LampOn && Vector3.Distance(view.position,new Vector3(22,1.25f,30))<2.5f && Vector3.Angle(view.forward,new Vector3(22,1.25f,30)-view.position)<18;
        MirrorVisible=shine && MirrorStep==3;mirrorFace.SetActive(MirrorVisible);map.SetActive(shine && MirrorStep==6);
        if(map.activeSelf && !mapReward){mapReward=true;timer4.AddDiscoveryTime();}
        if(MasterOff && !DarkEscapeArmed && Time.time>=nextFinalBlink)
        {nextFinalBlink=Time.time+2;StartCoroutine(FinalBlink());}
        if(DarkEscapeArmed && Vector3.Distance(new Vector3(player.position.x,0,player.position.z),darkOrigin)>=8f)StartCoroutine(TrueEnding());
    }
    bool Near(Vector3 p,float radius) => Mathf.Abs(player.position.x-p.x)<radius && Mathf.Abs(player.position.z-p.z)<radius;
    public bool CanUse(CampaignInteractable t)
    {
        if(t.action=="cradle")return LampOwned || (Inventory.Instance.HasItem("LampBase")&&Inventory.Instance.HasItem("LampHead"));
        if(t.action=="tape4")return player && player.position.y>=.4f && Near(new Vector3(32,0,40),1f);
        if(t.action=="master")return TapeFourHeard && LampOwned;
        return true;
    }
    public void Use(CampaignInteractable target)
    {
        if(!CanUse(target))return;
        CurrentTouches.Add(target.transform.position);
        switch(target.action)
        {
            case "base": case "head":
                string id=target.action=="base"?"LampBase":"LampHead";
                if(Inventory.Instance.TryAdd(new InventoryItem(id,id,Color.gray)))target.gameObject.SetActive(false);break;
            case "cradle":
                if(!LampOwned) {Inventory.Instance.Remove("LampBase");Inventory.Instance.Remove("LampHead");Inventory.Instance.TryAdd(new InventoryItem("InspectionLamp","Inspection lamp",Color.yellow));LampOwned=true;CreateLamp();}
                Inventory.Instance.ShowMessage("Inspection lamp assembled. F toggles the beam.");ObjectiveToast.Instance.Show("F toggles the inspection beam. Hold it nearly parallel to the blank plate.");break;
            case "plate": StoryExperience.Instance.Read("BLANK PLATE","The surface looks blank from here.\nHold the inspection light low, almost parallel to the plate.");break;
            case "breaker": Breakers[target.index]=!Breakers[target.index];breakerLabels[target.index].text=Breakers[target.index]?"UP":"DOWN";break;
            case "submit3":
                if(Breakers.SequenceEqual(GameState.Room3ArrowUp)){GameState.MarkRoomSolved(3);timer3.StopCountdown();gate3.SetActive(false);ObjectiveToast.Instance.Show("The repair circuit holds. Take the lamp with you.");}
                else{Array.Clear(Breakers,0,4);foreach(var t in breakerLabels)t.text="DOWN";timer3.Penalty(10);Inventory.Instance.ShowMessage("Circuit rejected. Breakers reset.");}break;
            case "pod": StoryExperience.Instance.Read("SLEEP POD / OCCUPIED","R. Shen is still inside.\nThe handle will not turn. This lock does not control the exit.");break;
            case "mirror": MirrorStep=(MirrorStep+1)%8;target.transform.localRotation=Quaternion.Euler(0,MirrorStep*45,0);break;
            case "panel4":target.gameObject.SetActive(false);foreach(var t in GetComponentsInChildren<CampaignInteractable>(true))if(t.action=="dial"||t.action=="submit4")t.gameObject.SetActive(true);foreach(var t in dialLabels)t.gameObject.SetActive(true);break;
            case "dial": Dials[target.index]=(Dials[target.index]+1)%10;dialLabels[target.index].text=Dials[target.index].ToString();break;
            case "submit4":
                var answer=GameState.Room4MirrorOrder.Select(s=>GameState.Room1Code[(int)s]).ToArray();
                if(Dials.SequenceEqual(answer)){GameState.MarkRoomSolved(4);timer4.StopCountdown();gate4.SetActive(false);ObjectiveToast.Instance.Show("The door is a loop. Don't stop at the door.");}
                else{Array.Clear(Dials,0,4);foreach(var t in dialLabels)t.text="0";timer4.Penalty(10);Inventory.Instance.ShowMessage("Wrong sequence. Dials reset.");}break;
            case "tape4":TapeFourHeard=true;master.SetActive(true);StoryExperience.Instance.Read("TAPE 04 / R. SHEN","The walls only exist while the lights are on.\nCut the power. Turn off every light. Even the one in your hand.\nWalk into the dark. Don't look back.");break;
            case "master":
                MasterOff=true;foreach(var t in FindObjectsByType<RoomTimer>(FindObjectsSortMode.None))t.StopCountdown();
                foreach(var l in FindObjectsByType<Light>(FindObjectsSortMode.None))if(l!=lamp)l.enabled=false;
                RenderSettings.ambientLight=Color.black;RenderSettings.ambientIntensity=0;RenderSettings.reflectionIntensity=0;
                foreach(var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))foreach(var m in r.materials)if(m.HasProperty("_EmissionColor"))m.SetColor("_EmissionColor",Color.black);
                LampOn=true;lamp.enabled=true;target.gameObject.SetActive(false);break;
            case "normal":StartCoroutine(NormalEnding());break;
        }
    }
    IEnumerator FinalBlink()
    {
        lamp.enabled=false;finalJanitorDistance=Mathf.Max(3.1f,finalJanitorDistance-1);
        ShowJanitor(player.position+player.forward*finalJanitorDistance,.15f);
        yield return new WaitForSeconds(.15f);if(!DarkEscapeArmed)lamp.enabled=true;
    }
    void ArmDarkEscape()
    {
        DarkEscapeArmed=true;LampOn=false;lamp.enabled=false;darkOrigin=new Vector3(player.position.x,0,player.position.z);
        foreach(var c in FindObjectsByType<Collider>(FindObjectsSortMode.None))
        {
            if(c is CharacterController)continue;
            // Keep walkable floors; remove walls, doors, props and triggers.
            if(c.bounds.max.y>.12f)c.enabled=false;
        }
        Box("DarknessFloor",new Vector3(32,-.12f,41),new Vector3(80,.15f,80),metal);
    }
    IEnumerator NormalEnding()
    {
        finishing=true;Ending="normal";PlayerPrefs.SetInt("LastLight.Loops",PlayerPrefs.GetInt("LastLight.Loops")+1);PlayerPrefs.Save();
        StoryExperience.Instance.Read("LEVEL 4.7 / THE CLEAN ROOMS","Wanderers recorded: "+(38+PlayerPrefs.GetInt("LastLight.Loops"))+"\n\nThe same room. The same light.");
        yield return new WaitForSeconds(4);GameState.WinGame();SceneManager.LoadScene("End");
    }
    IEnumerator TrueEnding()
    {
        finishing=true;Ending="true";FirstPersonController.Instance.SetInputEnabled(false);
        // A real separate runtime scene; no need to retain the backrooms geometry behind the ending.
        var campus=SceneManager.CreateScene("CampusLab");SceneManager.SetActiveScene(campus);
        var room=new GameObject("CampusLabRoom");SceneManager.MoveGameObjectToScene(room,campus);
        var oldCamera=FirstPersonController.Instance.Camera;oldCamera.enabled=false;
        var camera=new GameObject("CampusCamera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(camera.gameObject,campus);camera.transform.position=new Vector3(1000,1.6f,1000);
        camera.backgroundColor=new Color(.20f,.15f,.09f);camera.farClipPlane=25;
        var warm=Mat(new Color(.68f,.61f,.46f));
        var wall=Box("CampusWall",new Vector3(1000,1.5f,1004),new Vector3(8,3,.2f),warm);wall.transform.SetParent(room.transform);
        var floor=Box("CampusFloor",new Vector3(1000,-.05f,1001),new Vector3(8,.1f,8),warm);floor.transform.SetParent(room.transform);
        var desk=Box("CampusDesk",new Vector3(1000,.7f,1002),new Vector3(2,.15f,.8f),neutral);desk.transform.SetParent(room.transform);
        var clock=Label("CampusClock","03:08 AM\nCAMPUS LAB",new Vector3(1000,2,1003.8f),.3f,3);clock.transform.SetParent(room.transform);
        var light=new GameObject("CampusWarmLight").AddComponent<Light>();SceneManager.MoveGameObjectToScene(light.gameObject,campus);light.transform.position=new Vector3(1000,2.5f,1001);light.type=LightType.Point;light.range=12;light.intensity=5;light.color=new Color(1,.73f,.4f);
        RenderSettings.ambientLight=new Color(.3f,.23f,.15f);RenderSettings.ambientIntensity=1;
        yield return new WaitForSeconds(4);light.enabled=false;yield return new WaitForSeconds(.12f);light.enabled=true;yield return new WaitForSeconds(1);
        GameState.WinGame();SceneManager.LoadScene("End");
    }
    void OnDestroy() { if(mirrorTexture)mirrorTexture.Release(); }
}
