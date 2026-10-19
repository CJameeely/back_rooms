using UnityEngine;
using UnityEngine.SceneManagement;

/// Sits just past a room's (now open) exit door. Walking into it marks that room solved in
/// GameState. Only the final exit corridor's trigger (isFinalExit = true) actually ends the game;
/// every other room's trigger just lets the player continue toward the next one — per the new
/// flow, Room 1's own door no longer wins the game by itself.
[RequireComponent(typeof(Collider))]
public class RoomExitTrigger : MonoBehaviour
{
    [Tooltip("1-4, which room this trigger sits behind.")]
    public int roomNumber;
    [Tooltip("True only for the trigger at the end of the exit corridor, after Room 4.")]
    public bool isFinalExit;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    bool _narrativeShown;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameState.MarkRoomSolved(roomNumber);
        if (roomNumber == 2 && !_narrativeShown && ObjectiveToast.Instance != null)
        {
            _narrativeShown = true;
            ObjectiveToast.Instance.Show("Another room. The same hum. Scratches count rooms someone thought were new.");
        }

        if (isFinalExit)
        {
            GameState.WinGame();
            SceneManager.LoadScene("End");
        }
    }
}
