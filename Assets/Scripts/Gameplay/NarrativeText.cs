using TMPro;
using UnityEngine;

// Narrative canon from THE LAST LIGHT handoff; puzzle hints remain separate.
// Content is attached to existing interactions so rebuilding scenes preserves it.
public static class NarrativeText
{
    public const string Premise = "3:07 AM. Campus lab. One moment against a wall.\nThen an unfamiliar room, and a note signed R. Shen.";
    public const string Instructions = "Follow the traces of a wanderer who arrived before you.\nEach room loses power while you remain inside. Leave before the last light dies.\n\nWASD  Move    |    Mouse  Look    |    E  Interact\nTab  Recall objective    |    Esc  Close a panel\nKeypad: 0-9 / click, Enter  Confirm";
    public static string Entry(int room, string fallback)
    {
        if (room == 1) return "Four marks, four numbers. The door reads them in its own order.";
        if (room == 2) return "The card you need is somewhere you can't reach yet.";
        return fallback;
    }
    public static void Paper(GameObject panel)
    {
        foreach (var t in panel.GetComponentsInChildren<TMP_Text>(true))
            if (t.name.StartsWith("Note_")) t.gameObject.SetActive(false);
        Label(panel, "TraceHeading", "TORN PAGE / R. SHEN", new Vector2(0, 210), new Vector2(940, 45), 22);
        Label(panel, "TraceBody", "If you're reading this, you noclipped too. Don't panic.\nThe lights will go out. Get out of each room before they do.\n\n- R.S.", new Vector2(0, -100), new Vector2(940, 240), 25);
        Label(panel, "TraceClose", "E / Esc  Put the note down", new Vector2(0, -270), new Vector2(900, 35), 20);
    }
    public static void Calibration(GameObject panel)
    {
        Label(panel, "TraceHeading", "HANDWRITING ON THE CARD", new Vector2(0, 335), new Vector2(1000, 35), 20);
        Label(panel, "TraceBody", "Match the notches. Read the four holes in order.\nThe edges are worn from use.", new Vector2(0, 270), new Vector2(1050, 75), 23);
    }
    static void Label(GameObject panel, string name, string text, Vector2 position, Vector2 size, int fontSize)
    {
        var existing = panel.transform.Find(name);
        var label = existing ? existing.GetComponent<TextMeshProUGUI>() : new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(panel.transform, false);
        label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .5f);
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.84f, .82f, .73f);
        label.raycastTarget = false;
        label.text = text;
    }
}
