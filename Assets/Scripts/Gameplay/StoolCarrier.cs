using UnityEngine;

/// Carries the Room 2 stool: E on it picks it up (hides the real stool, shows a semi-transparent
/// preview that follows the camera's aim point), E again while carrying tries to place it there.
/// Placement is only valid inside this room, within `maxPlaceDistance`, and on a roughly flat,
/// unobstructed surface. PlayerInteractor routes E to TryPlace() first whenever IsCarrying is
/// true, before any normal raycast interaction, matching "看向地面按 E 放下" taking priority over
/// whatever else the player might be looking at while holding it.
public class StoolCarrier : MonoBehaviour
{
    public static StoolCarrier Instance { get; private set; }

    public GameObject stoolVisual;   // the real, physically-collidable stool in the world
    public Transform ghostPreview;   // semi-transparent copy, shown only while carrying
    public Renderer ghostRenderer;
    public Bounds roomFloorBounds;   // Room 2's floor footprint in world space (XZ matters; Y ignored)
    public float maxPlaceDistance = 2f;
    public Color validColor = new Color(0.3f, 0.9f, 0.4f, 0.4f);
    public Color invalidColor = new Color(0.9f, 0.3f, 0.3f, 0.4f);

    public bool IsCarrying { get; private set; }
    /// Set true (via OnTriggerStay) by any DoorwayStoolBlock the player is overlapping this frame
    /// while carrying; PlayerInteractor reads it to show "Put the stool down first" ahead of any
    /// other prompt. Cleared every frame in LateUpdate so it only reflects "right now".
    public bool NearBlockedDoor;

    bool _placementValid;
    Vector3 _pendingPlacePos;
    Camera _cam;

    void Awake() => Instance = this;

    void Start()
    {
        _cam = FirstPersonController.Instance != null ? FirstPersonController.Instance.Camera : null;
        if (ghostPreview != null) ghostPreview.gameObject.SetActive(false);
    }

    public void PickUp()
    {
        if (IsCarrying) return;
        IsCarrying = true;
        if (stoolVisual != null) stoolVisual.SetActive(false);
        if (ghostPreview != null) ghostPreview.gameObject.SetActive(true);
    }

    public bool TryPlace()
    {
        if (!IsCarrying) return false;
        if (!_placementValid) return false;

        IsCarrying = false;
        if (stoolVisual != null)
        {
            stoolVisual.transform.position = _pendingPlacePos;
            stoolVisual.SetActive(true);
        }
        if (ghostPreview != null) ghostPreview.gameObject.SetActive(false);
        return true;
    }

    void Update()
    {
        if (!IsCarrying || _cam == null || ghostPreview == null) return;

        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        bool hit = Physics.Raycast(ray, out RaycastHit info, maxPlaceDistance + 1f, ~0, QueryTriggerInteraction.Ignore);

        if (hit)
        {
            bool flatEnough = Vector3.Angle(info.normal, Vector3.up) < 15f;
            bool closeEnough = info.distance <= maxPlaceDistance;
            bool inRoom = roomFloorBounds.Contains(new Vector3(info.point.x, roomFloorBounds.center.y, info.point.z));
            _placementValid = flatEnough && closeEnough && inRoom;
            _pendingPlacePos = info.point;
            ghostPreview.position = info.point;
        }
        else
        {
            _placementValid = false;
        }

        if (ghostRenderer != null)
            ghostRenderer.material.color = _placementValid ? validColor : invalidColor;
    }

    void LateUpdate() => NearBlockedDoor = false;
}
