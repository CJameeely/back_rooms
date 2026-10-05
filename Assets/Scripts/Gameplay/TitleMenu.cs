using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    public void OnStartPressed()
    {
        GameResult.StartTime = Time.time;
        SceneManager.LoadScene("WhiteBox_LastLight");
    }
}
