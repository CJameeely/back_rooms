using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenu : MonoBehaviour
{
    ImmersiveTitle cover;
    void Start()
    {
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
        cover=gameObject.AddComponent<ImmersiveTitle>();cover.Initialize(StartGame);
    }
    public void OnStartPressed()
    {
        if(cover)cover.BeginEnter();else StartGame();
    }
    void StartGame()
    {
        GameState.BeginNewGame();StoryExperience.IntroPending=true;
        SceneManager.LoadScene("WhiteBox_LastLight");
    }
}
