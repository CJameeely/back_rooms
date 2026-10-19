using UnityEngine;

public class CampaignInteractable : MonoBehaviour, IInteractable
{
    public string action;
    public int index;
    public string prompt;
    public string InteractionPrompt => prompt;
    public bool CanInteract => FullCampaign.Instance != null && FullCampaign.Instance.CanUse(this);
    public string BlockedReason => action == "cradle" ? "Find both lamp parts" : action == "tape4" ? "Out of reach" : "Not yet available";
    public void Interact(GameObject player) { if (CanInteract) FullCampaign.Instance.Use(this); }
}
