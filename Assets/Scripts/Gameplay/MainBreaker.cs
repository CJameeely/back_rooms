// @role Room 3 — checks all 4 breakers against the nameplate pattern, opens the door or penalizes.
// @calls GameState, RoomTimer, SlideMover
// @tune wrongPenalty:float:0-30:seconds lost per wrong submit

using UnityEngine;

/// The larger main breaker: E checks the 4 small breakers against GameState.Room3ArrowUp. All
/// correct -> status light green, door opens. Any wrong -> all 4 breakers snap back down (no hint
/// which ones were wrong) and a time penalty applies.
public class MainBreaker : MonoBehaviour, IInteractable
{
    public BreakerSwitch[] breakers = new BreakerSwitch[4];
    public SlideMover door;
    public Vector3 doorOpenLocalPosition;
    public Renderer statusLightRenderer;
    public Color deniedColor = new Color(0.9f, 0.15f, 0.1f);
    public Color grantedColor = new Color(0.2f, 0.85f, 0.3f);
    public RoomTimer penaltyTimer;
    public float wrongPenalty = 15f;

    [Tooltip("If GameState already has Room 3 marked solved when the scene loads (Retry Room after a later room failed), snap the door open immediately.")]
    public int roomNumberForFastForward = 3;

    bool _solved;

    public string InteractionPrompt => _solved ? null : "Throw main breaker";
    public bool CanInteract => !_solved;
    public string BlockedReason => null;

    void Start()
    {
        if (statusLightRenderer != null) statusLightRenderer.material.color = deniedColor;
        if (roomNumberForFastForward > 0 && GameState.IsRoomSolved(roomNumberForFastForward))
        {
            _solved = true;
            if (statusLightRenderer != null) statusLightRenderer.material.color = grantedColor;
            if (door != null) door.transform.localPosition = doorOpenLocalPosition;
        }
    }

    public void Interact(GameObject interactor)
    {
        if (_solved) return;

        bool allCorrect = true;
        for (int i = 0; i < breakers.Length && i < GameState.Room3ArrowUp.Length; i++)
        {
            if (breakers[i] == null || breakers[i].IsUp != GameState.Room3ArrowUp[i]) { allCorrect = false; break; }
        }

        if (allCorrect)
        {
            _solved = true;
            if (statusLightRenderer != null) statusLightRenderer.material.color = grantedColor;
            if (door != null) door.SlideTo(doorOpenLocalPosition, 1f);
        }
        else
        {
            foreach (var b in breakers) b?.SetUp(false);
            if (penaltyTimer != null) penaltyTimer.Penalty(wrongPenalty);
        }
    }
}
