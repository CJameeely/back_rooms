// @role Room 2 — blocks/warns the player from carrying the stool out through a doorway.
// @calls StoolCarrier

using UnityEngine;

/// Sits in a doorway the player shouldn't be able to carry the stool through. `sense` (a trigger
/// collider spanning the opening) flags StoolCarrier.NearBlockedDoor while the player is in it and
/// carrying, which PlayerInteractor turns into the "Put the stool down first" prompt. `solidBlock`
/// (a non-trigger collider filling the same opening, disabled by default) is switched on whenever
/// the player is carrying, so a CharacterController physically can't walk through — the message
/// alone wouldn't actually stop anyone.
public class DoorwayStoolBlock : MonoBehaviour
{
    public Collider solidBlock;

    void Update()
    {
        if (solidBlock == null) return;
        solidBlock.enabled = StoolCarrier.Instance != null && StoolCarrier.Instance.IsCarrying;
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player") || StoolCarrier.Instance == null || !StoolCarrier.Instance.IsCarrying) return;
        StoolCarrier.Instance.NearBlockedDoor = true;
    }
}
