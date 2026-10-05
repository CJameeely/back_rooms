using UnityEngine;

/// Rotates a world-space clue label to always face the camera (yaw only), so it reads correctly
/// from any approach angle instead of only from one specific direction. This does not defeat
/// occlusion-based hide/reveal puzzles (e.g. the pillar clue): whether the label is visible at
/// all still depends purely on its fixed world position relative to whatever solid geometry
/// might be blocking it, not on which way it's currently facing.
public class Billboard : MonoBehaviour
{
    Transform _cam;

    void LateUpdate()
    {
        if (_cam == null)
        {
            if (Camera.main == null) return;
            _cam = Camera.main.transform;
        }

        Vector3 dir = transform.position - _cam.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}
