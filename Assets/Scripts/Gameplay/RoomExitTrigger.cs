// @role Marks a room solved on exit; the final one ends the game in victory.
// @calls GameState

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

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        GameState.MarkRoomSolved(roomNumber);

        if (isFinalExit)
        {
            GameState.WinGame();
            SceneManager.LoadScene("End");
        }
    }
}
