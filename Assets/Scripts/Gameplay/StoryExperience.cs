using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Runtime installation survives editor scene regeneration. Only the playable chapter is populated.
public class StoryExperience : MonoBehaviour
{
    public static StoryExperience Instance { get; private set; }
    public static bool IntroPending;
    public static bool IsReading => Instance && Instance.panel && Instance.panel.activeSelf;
    public const string TapeOne = "Day 31. I scratched my name into the wall yesterday. This morning it was gone. But the card I left on top of the light was still there.\n\nIt doesn't clean what it can't see.";
    GameObject panel;
    TMP_Text heading, body, footer;
    int openedFrame;
    bool intro;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Register() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; }
    static void Loaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "WhiteBox_LastLight" && !FindFirstObjectByType<StoryExperience>())
            new GameObject("StoryExperience").AddComponent<StoryExperience>();
    }
    void Awake() { Instance = this; BuildUI(); }
    IEnumerator Start()
    {
        PlaceObjects();
        // Run after the controller and checkpoint components have initialized.
        yield return null;
        if (IntroPending)
        {
            IntroPending = false;
            yield return Opening();
        }
    }
    void BuildUI()
    {
        var canvas = new GameObject("StoryCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvas.transform.SetParent(transform);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 100;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        panel = new GameObject("StoryPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.025f, .035f, .04f, .99f);
        heading = Text("Heading", 220, 34, 100);
        body = Text("Body", 0, 30, 300);
        footer = Text("Footer", -300, 23, 70);
        panel.SetActive(false);
    }
    TMP_Text Text(string name, float y, int size, float height)
    {
        var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(panel.transform, false);
        label.rectTransform.sizeDelta = new Vector2(1180, height);
        label.rectTransform.anchoredPosition = new Vector2(0, y);
        label.font = TMP_Settings.defaultFontAsset; label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.83f, .87f, .86f);
        label.raycastTarget = false;
        return label;
    }
    public void Read(string title, string text)
    {
        openedFrame = Time.frameCount;
        panel.SetActive(true); heading.text = title; body.text = text;
        footer.text = "E / Esc  Close     |     Room timer paused";
        FirstPersonController.Instance?.SetInputEnabled(false);
    }
    public void Close()
    {
        panel.SetActive(false);
        FirstPersonController.Instance?.SetInputEnabled(true);
    }
    IEnumerator Opening()
    {
        intro = true;
        Read("", ""); footer.text = "Space  Skip";
        const string line = "3:07 AM. Campus lab. You lean against the wall to rest your eyes.";
        float t = 0;
        while (t < 5f)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) { intro = false; Close(); yield break; }
            body.text = line.Substring(0, Mathf.Min(line.Length, (int)(t * 23)));
            t += Time.unscaledDeltaTime; yield return null;
        }
        heading.text = "LEVEL 4.7\nTHE CLEAN ROOMS";
        body.text = "Survival Difficulty: Class 2\nUnsafe / Unsecure / Low entity count\n\nA repeating research facility. No windows. No reliable landmarks.\nTraces of previous wanderers remain in places the light cannot reach.";
        t = 0;
        while (t < 7f)
        {
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) break;
            t += Time.unscaledDeltaTime; yield return null;
        }
        intro = false; Close();
        ObjectiveToast.Instance?.Show(NarrativeText.Entry(1, ""));
    }
    void Update()
    {
        if (!IsReading || intro || Time.frameCount == openedFrame || Keyboard.current == null) return;
        if (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame) Close();
    }
    StoryObject Prop(string name, Vector3 position, Vector3 scale, Color color, string title, string text, int bonus = 0, bool elevated = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(transform);
        go.transform.position = position; go.transform.localScale = scale;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color;
        go.GetComponent<Renderer>().material = mat;
        var obj = go.AddComponent<StoryObject>();
        obj.heading = title; obj.body = text; obj.bonusRoom = bonus; obj.elevated = elevated;
        return obj;
    }
    void PlaceObjects()
    {
        var room = GameObject.Find("Room_02_R1_Displaced");
        Transform desk = null;
        if (room) foreach (var t in room.GetComponentsInChildren<Transform>()) if (t.name == "Desk") { desk = t; break; }
        if (desk)
        {
            var bounds = new Bounds(desk.position, Vector3.zero);
            foreach (var mesh in desk.GetComponentsInChildren<Renderer>()) if (mesh.enabled) bounds.Encapsulate(mesh.bounds);
            var bottle = Prop("Story_AlmondWater", new Vector3(bounds.center.x, bounds.max.y + .13f, bounds.center.z), new Vector3(.10f,.25f,.10f), new Color(.39f,.43f,.36f), "ALMOND WATER", "Half empty. Someone was here.");
            bottle.transform.SetParent(desk, true);
            var note = Prop("Story_UnderDesk", new Vector3(bounds.center.x, bounds.max.y - .12f, bounds.min.z - .035f), new Vector3(.32f,.07f,.025f), new Color(.62f,.60f,.49f), "UNDER THE DESK / R.S.", "Things in the light get reset. Hide what matters in the dark.\n\n- R.S.", 1);
            note.transform.SetParent(desk, true);
        }
        var cabinet = GameObject.Find("Gameplay_SampleCabinet");
        if (cabinet)
        {
            var bounds = new Bounds(cabinet.transform.position, Vector3.zero);
            foreach (var mesh in cabinet.GetComponentsInChildren<Renderer>()) if(mesh.enabled) bounds.Encapsulate(mesh.bounds);
            var p = new Vector3(bounds.center.x, bounds.max.y + .08f, bounds.center.z);
            var tape = Prop("Story_Tape1", p, new Vector3(.32f,.14f,.21f), new Color(.18f,.22f,.23f), "TAPE 01 / R. SHEN", TapeOne);
            for (int i = -1; i <= 1; i += 2)
            {
                var reel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reel.transform.SetParent(tape.transform, false); reel.transform.localPosition = new Vector3(i*.23f,.55f,0);
                reel.transform.localScale = new Vector3(.3f,.08f,.45f);
                Destroy(reel.GetComponent<Collider>());
            }
        }
        Prop("Story_StudentID_L7", new Vector3(6.15f,3.11f,6.15f), new Vector3(.28f,.025f,.18f), new Color(.32f,.34f,.32f), "FADED STUDENT ID", "R. SHEN\nStudent\n\nThe photograph has faded. The name survived above the light.", 2, true);
        Prop("Story_RepeatScratches", new Vector3(5.1f,1.25f,17.8f), new Vector3(.025f,.22f,.45f), new Color(.28f,.30f,.29f), "SCRATCHES IN THE CORNER", "rooms I thought were new\n\n" + (37 + PlayerPrefs.GetInt("LastLight.Failures") + PlayerPrefs.GetInt("LastLight.Loops")) + " scratches");
    }
}
