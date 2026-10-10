using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Room 1's fail condition. Runs a silent countdown: lights flicker with increasing frequency
/// in the last `flickerWindow` seconds, then shut off one by one over the last `shutoffWindow`
/// seconds, then a full blackout holds for 2s before the player loses. No countdown number is
/// ever shown — the lights themselves are the clock.
public class LightsOutTimer : MonoBehaviour
{
    public bool timerEnabled = true;
    public float totalDuration = 180f;
    public float flickerWindow = 60f;  // starts flickering when this many seconds remain
    public float shutoffWindow = 20f;  // starts sequential shutoff when this many seconds remain
    public float blackoutHold = 2f;

    [Tooltip("Every ceiling light in Room 1, auto-populated by the generator.")]
    public Light[] roomLights = System.Array.Empty<Light>();
    [Tooltip("Same order as roomLights: each light's emissive ceiling panel.")]
    public Renderer[] roomPanels = System.Array.Empty<Renderer>();

    [Tooltip("The specific light the Bar clue's switch controls, so it can be force-revealed if the timer kills it first.")]
    public Light linkedLight;
    public LightSwitchClue linkedSwitchClue;

    public bool HasExpired => timerEnabled && _elapsed >= totalDuration;
    float _elapsed;
    bool _flickerStarted, _shutoffStarted, _blackoutStarted;
    readonly HashSet<Light> _off = new HashSet<Light>();
    List<Light> _shutoffOrder;
    Dictionary<Light, Material> _panelInstances;
    Dictionary<Light, Color> _panelBaseEmission;

    void Start()
    {
        GameResult.StartTime = Time.time;
        GameResult.Won = false;
        GameResult.ElapsedSeconds = 0f;
        if (!timerEnabled) { enabled = false; return; }

        _shutoffOrder = roomLights.OrderBy(_ => Random.value).ToList();
        _panelInstances = new Dictionary<Light, Material>();
        _panelBaseEmission = new Dictionary<Light, Color>();
        for (int i = 0; i < roomLights.Length && i < roomPanels.Length; i++)
        {
            if (roomLights[i] == null || roomPanels[i] == null) continue;
            Material mat = roomPanels[i].material;
            _panelInstances[roomLights[i]] = mat;
            _panelBaseEmission[roomLights[i]] = mat.GetColor("_EmissionColor");
        }
    }

    void Update()
    {
        if (!timerEnabled || _blackoutStarted) return;

        _elapsed += Time.deltaTime;
        float remaining = totalDuration - _elapsed;

        if (remaining <= 0f)
        {
            _blackoutStarted = true;
            StartCoroutine(Blackout());
            return;
        }

        if (remaining <= flickerWindow && !_flickerStarted)
        {
            _flickerStarted = true;
            StartCoroutine(FlickerLoop());
        }

        if (remaining <= shutoffWindow && !_shutoffStarted)
        {
            _shutoffStarted = true;
            StartCoroutine(ShutoffSequence());
        }
    }

    IEnumerator FlickerLoop()
    {
        while (!_shutoffStarted && !_blackoutStarted)
        {
            float remaining = totalDuration - _elapsed;
            // Delay between random blinks shrinks from ~1.2s down to ~0.15s as the flicker
            // window runs out, so flickering visibly speeds up toward the shutoff phase.
            float progress = Mathf.InverseLerp(flickerWindow, shutoffWindow, Mathf.Max(remaining, shutoffWindow));
            float delay = Mathf.Lerp(1.2f, 0.15f, progress);
            yield return new WaitForSeconds(delay);

            if (roomLights.Length == 0) continue;
            Light l = roomLights[Random.Range(0, roomLights.Length)];
            if (l == null || _off.Contains(l)) continue;
            StartCoroutine(QuickBlink(l));
        }
    }

    IEnumerator QuickBlink(Light l)
    {
        l.enabled = false;
        yield return new WaitForSeconds(0.08f);
        if (!_off.Contains(l)) l.enabled = true;
    }

    IEnumerator ShutoffSequence()
    {
        if (_shutoffOrder.Count == 0) yield break;
        float interval = shutoffWindow / _shutoffOrder.Count;
        foreach (Light l in _shutoffOrder)
        {
            if (l == null) continue;
            TurnOff(l);
            yield return new WaitForSeconds(interval);
        }
    }

    void TurnOff(Light l)
    {
        if (_off.Contains(l)) return;
        _off.Add(l);
        l.enabled = false;
        if (_panelInstances.TryGetValue(l, out Material mat))
            mat.SetColor("_EmissionColor", Color.black);

        if (l == linkedLight && linkedSwitchClue != null)
            linkedSwitchClue.ForceReveal();
    }

    IEnumerator Blackout()
    {
        foreach (Light l in roomLights)
            if (l != null) TurnOff(l);

        yield return new WaitForSeconds(blackoutHold);

        GameResult.Won = false;
        GameResult.ElapsedSeconds = Time.time - GameResult.StartTime;
        SceneManager.LoadScene("End");
    }
}
