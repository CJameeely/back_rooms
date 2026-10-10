using UnityEngine;
using UnityEngine.SceneManagement;

/// Sits in the doorway behind the exit door. Walking into it (once the door is open) wins.
[RequireComponent(typeof(Collider))]
public class WinTrigger : MonoBehaviour
{
    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (KeypadUI.Instance == null || !KeypadUI.Instance.Solved) return;
        var timer = FindFirstObjectByType<LightsOutTimer>();
        if (timer != null && timer.HasExpired) return;
        GameResult.Won = true;
        GameResult.ElapsedSeconds = Time.time - GameResult.StartTime;
        SceneManager.LoadScene("End");
    }
}
