using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnStartPressed()
    {
        GameState.BeginNewGame();
        SceneManager.LoadScene("WhiteBox_LastLight");
    }
}
