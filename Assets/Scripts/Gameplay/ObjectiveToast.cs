using System.Collections;
using TMPro;
using UnityEngine;

/// Top-center "what am I trying to do here" line. Fades in, holds for `holdDuration`, fades out.
/// Tab re-plays whatever the current room's text is (handled by PlayerInteractor, which calls
/// ReplayCurrent()) even after it has already faded out once.
public class ObjectiveToast : MonoBehaviour
{
    public static ObjectiveToast Instance { get; private set; }

    public TMP_Text label;
    public float holdDuration = 5f;
    public float fadeDuration = 0.6f;

    string _currentText = "";
    Coroutine _running;

    void Awake()
    {
        Instance = this;
        if (label != null) label.alpha = 0f;
    }

    /// Shows new text for the room the player just entered, and remembers it as "current" so Tab
    /// can bring it back later.
    public void Show(string text)
    {
        _currentText = text;
        Replay();
    }

    /// Re-shows whatever text is current (Tab), even if nothing changed.
    public void ReplayCurrent() => Replay();

    void Replay()
    {
        if (string.IsNullOrEmpty(_currentText) || label == null) return;
        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(FadeInHoldFadeOut());
    }

    IEnumerator FadeInHoldFadeOut()
    {
        label.text = _currentText;
        yield return Fade(label.alpha, 1f);
        yield return new WaitForSeconds(holdDuration);
        yield return Fade(1f, 0f);
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            label.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        label.alpha = to;
    }
}
