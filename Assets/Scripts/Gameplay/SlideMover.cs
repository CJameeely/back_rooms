// @role Generic smooth local-position slide, reused by doors/drawers/cabinets.

using System;
using System.Collections;
using UnityEngine;

/// Generic "smoothly slide this transform's local position to a target over N seconds" helper,
/// reused by the cabinet, the drawer front, and the exit door.
public class SlideMover : MonoBehaviour
{
    Coroutine _running;

    public void SlideTo(Vector3 targetLocalPosition, float duration, Action onComplete = null)
    {
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(Co(targetLocalPosition, duration, onComplete));
    }

    IEnumerator Co(Vector3 target, float duration, Action onComplete)
    {
        Vector3 start = transform.localPosition;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            transform.localPosition = Vector3.Lerp(start, target, k);
            yield return null;
        }
        transform.localPosition = target;
        onComplete?.Invoke();
    }
}
