using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndMenu : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text subText;

    void Start()
    {
        if (GameResult.Won)
        {
            titleText.text = "YOU FOUND THE WAY OUT";
            int total = Mathf.Max(0, Mathf.RoundToInt(GameResult.ElapsedSeconds));
            subText.text = $"Time: {total / 60:00}:{total % 60:00}";
        }
        else
        {
            titleText.text = "THE LIGHTS WENT OUT";
            subText.text = "";
        }
    }

    public void OnReplayPressed()
    {
        GameResult.StartTime = Time.time;
        SceneManager.LoadScene("WhiteBox_LastLight");
    }

    public void OnTitlePressed()
    {
        SceneManager.LoadScene("Title");
    }
}
