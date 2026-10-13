using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Per-room silent countdown, generalized from the original Room-1-only LightsOutTimer so it can
/// be dropped onto any of the four challenge rooms. Only affects this room's own ceiling lights:
/// lights flicker with increasing frequency in the last `flickerWindow` seconds, then shut off one
/// by one over the last `shutoffWindow` seconds, then a full blackout holds for `blackoutHold`
/// before the player loses. No countdown number is ever shown — the lights themselves are the
/// clock. Does NOT start on its own; call BeginCountdown() once the player actually enters the
/// room (a RoomEntryTrigger normally does this).
public class RoomTimer : MonoBehaviour
{
    [Tooltip("1-4. Used for GameState.Fail() so the End screen can report which room ran out of time.")]
    public int roomNumber;

    public bool timerEnabled = true;
    public float totalDuration = 180f;
    public float flickerWindow = 60f;  // starts flickering when this many seconds remain
    public float shutoffWindow = 20f;  // starts sequential shutoff when this many seconds remain
    public float blackoutHold = 2f;
    [Tooltip("How long a Penalty() dims the room's lights as feedback.")]
    public float penaltyFlashDuration = 0.3f;

    [Tooltip("Every ceiling light in this room, auto-populated by the generator.")]
    public Light[] roomLights = System.Array.Empty<Light>();
    [Tooltip("Same order as roomLights: each light's emissive ceiling panel.")]
    public Renderer[] roomPanels = System.Array.Empty<Renderer>();

    [Tooltip("Room 1 only: the specific light the Bar clue's switch controls, so it can be force-revealed if the timer kills it first.")]
    public Light linkedLight;
    public LightSwitchClue linkedSwitchClue;

    public bool HasExpired => timerEnabled && _elapsed >= totalDuration;
    public bool IsRunning => _started && !_blackoutStarted;

    bool _started;
    float _elapsed;
    bool _flickerStarted, _shutoffStarted, _blackoutStarted;
    readonly HashSet<Light> _off = new HashSet<Light>();
    List<Light> _shutoffOrder;
    Dictionary<Light, Material> _panelInstances;
    Dictionary<Light, Color> _panelBaseEmission;

    public void BeginCountdown()
    {
        if (_started || !timerEnabled) return;
        _started = true;

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

    /// Docks `seconds` off the remaining time (used for wrong-guess penalties) and gives a brief
    /// visual flash so the player feels it even though no number is ever shown.
    public void Penalty(float seconds)
    {
        if (!_started || _blackoutStarted) return;
        _elapsed += seconds;
        StartCoroutine(PenaltyFlash());
    }

    IEnumerator PenaltyFlash()
    {
        foreach (var kv in _panelInstances)
        {
            if (_off.Contains(kv.Key)) continue;
            kv.Value.SetColor("_EmissionColor", _panelBaseEmission[kv.Key] * 0.15f);
        }
        yield return new WaitForSeconds(penaltyFlashDuration);
        foreach (var kv in _panelInstances)
        {
            if (_off.Contains(kv.Key)) continue;
            kv.Value.SetColor("_EmissionColor", _panelBaseEmission[kv.Key]);
        }
    }

    void Update()
    {
        if (!_started || !timerEnabled || _blackoutStarted) return;

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

        GameState.Fail(roomNumber);
        SceneManager.LoadScene("End");
    }
}
