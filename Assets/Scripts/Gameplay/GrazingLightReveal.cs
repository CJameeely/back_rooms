// @role Reveals a surface only under a close, shallow-angle light graze (Room 3's nameplate).
// @calls InspectionLamp
// @tune maxDistance:float:0.5-3:required lamp distance (m)
// @tune maxGrazeAngle:float:10-45:allowed angle from parallel-to-surface (deg)

using TMPro;
using UnityEngine;

/// Room 3's nameplate: looks completely blank under the room's own ceiling lights no matter how
/// bright or dim they are. Only reveals its embossed arrows when ALL of these hold at once:
/// the inspection lamp is on, it's within `maxDistance` of this surface, and its beam is grazing
/// the surface at a shallow angle (within `maxGrazeAngle` of parallel-to-the-surface — aimed
/// straight on gives the worst, 90-degree-from-grazing angle and reveals nothing). Clarity fades
/// in smoothly as the angle gets closer to a perfect graze, rather than snapping on at the
/// threshold.
public class GrazingLightReveal : MonoBehaviour
{
    [Tooltip("The arrow labels this reveals (alpha-faded, not SetActive, so clarity can read as a smooth fade).")]
    public TMP_Text[] revealedLabels = System.Array.Empty<TMP_Text>();
    public float maxDistance = 1.5f;
    public float maxGrazeAngle = 30f;

    void Update()
    {
        float clarity = ComputeClarity();
        foreach (var label in revealedLabels)
            if (label != null) label.alpha = clarity;
    }

    float ComputeClarity()
    {
        var lamp = InspectionLamp.Instance;
        if (lamp == null || !lamp.IsOn || lamp.spotLight == null) return 0f;

        Transform lampT = lamp.spotLight.transform;
        float dist = Vector3.Distance(lampT.position, transform.position);
        if (dist > maxDistance) return 0f;

        // Angle between the beam and this surface's own normal: 90 degrees means the beam runs
        // exactly along the surface (a perfect graze); 0 or 180 means aimed straight at/away from it.
        float angleToNormal = Vector3.Angle(lampT.forward, transform.forward);
        float angleFromGraze = Mathf.Abs(90f - angleToNormal);
        if (angleFromGraze > maxGrazeAngle) return 0f;

        return 1f - (angleFromGraze / maxGrazeAngle);
    }
}
