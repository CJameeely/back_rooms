using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        foreach (var text in FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
        {
            if (text.name == "Subtitle") { text.text = NarrativeText.Premise; text.fontSize = 22; }
            if (text.name == "Instructions") { text.text = NarrativeText.Instructions; text.fontSize = 23; }
        }
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(Screen.width-165,Screen.height-50,145,30), "Reset Records"))
        { PlayerPrefs.DeleteKey("LastLight.Failures"); PlayerPrefs.DeleteKey("LastLight.Loops"); PlayerPrefs.Save(); }
    }

    public void OnStartPressed()
    {
        GameState.BeginNewGame();
        StoryExperience.IntroPending = true;
        SceneManager.LoadScene("WhiteBox_LastLight");
    }
}
