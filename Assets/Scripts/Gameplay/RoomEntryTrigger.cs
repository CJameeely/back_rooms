// @role First-entry doorway trigger — starts the room timer, sets the save checkpoint, shows the objective toast.
// @calls GameState, RoomTimer, ObjectiveToast, Inventory

using UnityEngine;

/// Sits in a room's entrance doorway. The player's FIRST pass through it: records a save
/// checkpoint (this room + current inventory), starts that room's RoomTimer (if it has one), and
/// shows the room's objective toast. Leaving and re-entering does nothing further — per spec,
/// leaving a room never pauses its timer, and the checkpoint is only ever the most recent
/// *first* entry, not refreshed on every lap through the doorway.
[RequireComponent(typeof(Collider))]
public class RoomEntryTrigger : MonoBehaviour
{
    [Tooltip("1-4. Rooms without a number (Start, F, the exit corridor) just pass 0 and skip the checkpoint.")]
    public int roomNumber;
    public RoomTimer timer; // null for rooms with no countdown (F, the exit corridor)
    [TextArea] public string objectiveText;

    bool _entered;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_entered || !other.CompareTag("Player")) return;
        _entered = true;

        if (roomNumber > 0)
            GameState.SetCheckpoint(roomNumber, Inventory.Instance != null ? Inventory.Instance.SnapshotIds() : new System.Collections.Generic.List<string>());

        timer?.BeginCountdown();

        if (!string.IsNullOrEmpty(objectiveText) && ObjectiveToast.Instance != null)
            ObjectiveToast.Instance.Show(objectiveText);
    }
}
