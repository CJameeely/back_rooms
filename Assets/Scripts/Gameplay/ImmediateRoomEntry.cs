// @role Room 1's "already inside at spawn" substitute for RoomEntryTrigger.
// @calls GameState, RoomTimer, ObjectiveToast, Inventory

using System.Collections.Generic;
using UnityEngine;

/// For the one room the player spawns directly inside of at scene load — Room 1. Every other
/// room's "first entry" side effects (checkpoint, timer start, objective toast) come from walking
/// through a RoomEntryTrigger doorway; Room 1 has no doorway to walk through at game start, so
/// this runs the same effects from Start() instead, once, the moment the scene actually loads in
/// Play mode (not at scene-generation time — GameState only means anything at runtime).
public class ImmediateRoomEntry : MonoBehaviour
{
    public int roomNumber;
    public RoomTimer timer;
    [TextArea] public string objectiveText;

    void Start()
    {
        GameState.EnsureStarted();
        GameState.SetCheckpoint(roomNumber, Inventory.Instance != null ? Inventory.Instance.SnapshotIds() : new List<string>());
        timer?.BeginCountdown();
        if (!string.IsNullOrEmpty(objectiveText) && ObjectiveToast.Instance != null)
            ObjectiveToast.Instance.Show(objectiveText);
    }
}
