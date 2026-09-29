using UnityEngine;

// Drives a camera smoothly through a fixed list of waypoints, one per room, for recording a
// walkthrough of the whole level. Pauses briefly at each waypoint and sweeps the view left/right
// so every room reads clearly on camera before moving on.
[RequireComponent(typeof(Camera))]
public class CameraTourDriver : MonoBehaviour
{
    [Tooltip("One waypoint per room, in visiting order.")]
    public Transform[] waypoints;

    [Header("Move")]
    public float moveSpeed = 2.5f;
    public float turnSpeed = 120f;

    [Header("Look around at each room")]
    public float dwellTime = 2.5f;
    public float lookSweepDegrees = 35f;
    public float lookSweepSpeed = 40f;

    [Tooltip("Loop back to the first waypoint after reaching the last one.")]
    public bool loop = false;

    [Tooltip("Any GameObject with this name is deactivated on Awake so it can't fight this camera for control (e.g. FlyCamera).")]
    public string[] disableGameObjectsNamed = { "FlyCamera" };

    int _index = 0;
    float _dwellTimer = 0f;
    bool _dwelling = false;
    Quaternion _dwellBaseRotation;
    Quaternion _travelRotation;

    void Awake()
    {
        foreach (string name in disableGameObjectsNamed)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) go.SetActive(false);
        }
    }

    void Start()
    {
        if (waypoints != null && waypoints.Length > 0)
        {
            transform.position = waypoints[0].position;
            _travelRotation = transform.rotation;
            if (waypoints.Length > 1)
                _travelRotation = Quaternion.LookRotation((waypoints[1].position - waypoints[0].position).normalized, Vector3.up);
            transform.rotation = _travelRotation;
        }
        _dwelling = true;
        _dwellBaseRotation = transform.rotation;
    }

    void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (_dwelling)
        {
            _dwellTimer += Time.deltaTime;
            float t = _dwellTimer / Mathf.Max(0.01f, dwellTime);
            float yaw = Mathf.Sin(t * Mathf.PI * 2f * (lookSweepSpeed / 90f)) * lookSweepDegrees;
            transform.rotation = _dwellBaseRotation * Quaternion.Euler(0, yaw, 0);

            if (_dwellTimer >= dwellTime)
            {
                _dwelling = false;
                _dwellTimer = 0f;
                transform.rotation = _dwellBaseRotation;
            }
            return;
        }

        Transform target = waypoints[_index];
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist > 0.05f)
        {
            _travelRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, _travelRotation, turnSpeed * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
        }
        else
        {
            bool isLast = _index >= waypoints.Length - 1;
            if (isLast && !loop)
                return; // tour finished, hold position

            _index = (_index + 1) % waypoints.Length;
            _dwelling = true;
            _dwellTimer = 0f;
            _dwellBaseRotation = transform.rotation;
        }
    }
}
