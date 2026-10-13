using System.Collections;
using UnityEngine;
using TMPro;

/// Clue 4 (Bar): the switch. E toggles the nearest ceiling light; the clue text next to it
/// fades between near-invisible (light on) and fully visible (light off) over ~0.5s. If
/// RoomTimer later kills this same light on its own, ForceReveal() locks the text visible
/// regardless of switch state, per spec.
public class LightSwitchClue : MonoBehaviour, IInteractable, IClueSource
{
    public ClueSymbol symbol = ClueSymbol.Bar;
    public Light targetLight;
    public Renderer targetPanelRenderer; // the ceiling light's emissive panel mesh
    public TMP_Text label;
    public float fadeDuration = 0.5f;

    public ClueSymbol Symbol => symbol;
    public int Digit { get; private set; }
    public bool IsReadable => _forceRevealed || !_lightOn;

    public string InteractionPrompt => _forceRevealed ? null : (_lightOn ? "Turn off light" : "Turn on light");
    public bool CanInteract => !_forceRevealed;
    public string BlockedReason => null;

    bool _lightOn = true;
    bool _forceRevealed;
    float _baseIntensity;
    Color _baseEmission;
    Material _panelInstance;

    void Start()
    {
        Digit = CodeManager.Instance.GetDigit(symbol);
        if (label != null) label.text = $"{CodeManager.SymbolGlyph(symbol)}  {Digit}";

        _baseIntensity = targetLight != null ? targetLight.intensity : 1f;
        if (targetPanelRenderer != null)
        {
            _panelInstance = targetPanelRenderer.material; // auto-instances a per-renderer copy
            _baseEmission = _panelInstance.GetColor("_EmissionColor");
        }
        ApplyImmediate();
    }

    public void Interact(GameObject interactor)
    {
        if (_forceRevealed) return;
        _lightOn = !_lightOn;
        StopAllCoroutines();
        StartCoroutine(Fade());
    }

    /// Called by RoomTimer when its countdown extinguishes this exact light.
    public void ForceReveal()
    {
        _forceRevealed = true;
        _lightOn = false;
        StopAllCoroutines();
        if (label != null) label.alpha = 1f;
    }

    void ApplyImmediate()
    {
        if (label != null) label.alpha = _lightOn ? 0.05f : 1f;
    }

    IEnumerator Fade()
    {
        float startAlpha = label != null ? label.alpha : 0f;
        float endAlpha = _lightOn ? 0.05f : 1f;
        float startIntensity = targetLight != null ? targetLight.intensity : 0f;
        float endIntensity = _lightOn ? _baseIntensity : 0f;
        Color startEmission = _panelInstance != null ? _panelInstance.GetColor("_EmissionColor") : Color.black;
        Color endEmission = _lightOn ? _baseEmission : Color.black;

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);
            if (label != null) label.alpha = Mathf.Lerp(startAlpha, endAlpha, k);
            if (targetLight != null) targetLight.intensity = Mathf.Lerp(startIntensity, endIntensity, k);
            if (_panelInstance != null) _panelInstance.SetColor("_EmissionColor", Color.Lerp(startEmission, endEmission, k));
            yield return null;
        }
        if (label != null) label.alpha = endAlpha;
        if (targetLight != null) targetLight.intensity = endIntensity;
        if (_panelInstance != null) _panelInstance.SetColor("_EmissionColor", endEmission);
    }
}
