using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// Win -> "YOU FOUND THE WAY OUT" + total time, with Play Again (starts a brand new game, fresh
/// codes) and Title. Loss -> "THE LIGHTS WENT OUT" + which room, with Retry Room (reloads the
/// level scene but keeps every already-generated code and already-solved room, per GameState) and
/// Title. `retryButton`/`playAgainButton` are toggled based on which outcome this is.
public class EndMenu : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text subText;
    public GameObject retryButton;
    public GameObject playAgainButton;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (GameState.Won)
        {
            titleText.text = FullCampaign.Ending == "true" ? "YOU NOCLIPPED OUT" : "YOU FOUND A DOOR";
            int total = Mathf.Max(0, Mathf.RoundToInt(GameState.ElapsedSeconds));
            subText.text = $"Time: {total / 60:00}:{total % 60:00}" + (FullCampaign.Ending == "normal" ? "\nThe door led back to the beginning." : "\nCampus lab. 3:08 AM.");
            subText.fontSize = 23;
            if (retryButton != null) retryButton.SetActive(false);
            if (playAgainButton != null) playAgainButton.SetActive(true);
        }
        else
        {
            titleText.text = "THE LIGHTS WENT OUT";
            subText.text = $"The hum stops. The same room returns.\nRoom {GameState.FailedRoomNumber}: follow the traces again.";
            subText.fontSize = 23;
            if (retryButton != null) retryButton.SetActive(true);
            if (playAgainButton != null) playAgainButton.SetActive(false);
        }
    }

    /// Loss path: reload the level, keep all codes/progress (GameState untouched).
    public void OnRetryRoomPressed()
    {
        SceneManager.LoadScene("WhiteBox_LastLight");
    }

    /// Win path: a genuinely new game, fresh codes.
    public void OnPlayAgainPressed()
    {
        GameState.BeginNewGame();
        StoryExperience.IntroPending = true;
        SceneManager.LoadScene("WhiteBox_LastLight");
    }

    public void OnTitlePressed()
    {
        SceneManager.LoadScene("Title");
    }
}
