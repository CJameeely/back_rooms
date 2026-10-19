using UnityEngine;

public class StoryObject : MonoBehaviour, IInteractable
{
    public string heading;
    [TextArea] public string body;
    public int bonusRoom;
    public bool elevated;
    bool rewarded;
    public string InteractionPrompt => elevated ? "Inspect shadow on light cover" : "Read / listen";
    public bool CanInteract
    {
        get
        {
            if (!elevated) return true;
            var player = FirstPersonController.Instance;
            if (!player) return false;
            var delta = player.transform.position - transform.position;
            return player.transform.position.y >= .4f && new Vector2(delta.x, delta.z).magnitude < 1f;
        }
    }
    public string BlockedReason => "Out of reach";
    public void Interact(GameObject interactor)
    {
        if (!CanInteract) return;
        StoryExperience.Instance?.Read(heading, body);
        if (bonusRoom > 0 && !rewarded)
        {
            rewarded = true;
            foreach (var timer in FindObjectsByType<RoomTimer>(FindObjectsSortMode.None))
                if (timer.roomNumber == bonusRoom) timer.AddDiscoveryTime();
        }
    }
}
