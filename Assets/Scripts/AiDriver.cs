using UnityEngine;

/// <summary>Waypoint AI on the Storm Coast circuit: follows TrackPath samples, rubber-bands to the player.</summary>
[RequireComponent(typeof(Rigidbody))]
public class AiDriver : MonoBehaviour
{
    public float maxSpeed = 30f;
    public float acceleration = 20f;
    public float aggression = 1f;
    public Transform player;
    public TrackPath track;
    public bool racing;

    Rigidbody rb;
    int _indexHint = -1;
    int _lap;
    float _laneOffset;

    public float ProgressMeters { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _laneOffset = (GetInstanceID() % 5 - 2) * 1.8f;
    }

    void FixedUpdate()
    {
        if (track == null || track.Count == 0) return;
        ProgressMeters = track.ProgressOf(transform.position, ref _indexHint, ref _lap);
        if (!racing) return;

        int ahead = (_indexHint + 4) % track.Count;
        Vector3 target = track.PointAt(ahead);
        Vector3 tangent = (track.PointAt(ahead + 1) - track.PointAt(ahead - 1)).normalized;
        Vector3 side = new Vector3(tangent.z, 0f, -tangent.x);
        target += side * _laneOffset;

        Vector3 toTarget = target - transform.position; toTarget.y = 0f;
        float angle = Vector3.SignedAngle(transform.forward, toTarget.normalized, Vector3.up);
        float steer = Mathf.Clamp(angle / 30f, -1f, 1f);

        float rubber = 1f;
        if (player != null && player.TryGetComponent<KartController>(out _))
        {
            // compare race progress through the shared manager if present
            var mgr = FindFirstObjectByType<RaceManager>();
            if (mgr != null) rubber = Mathf.Clamp(1f + (mgr.PlayerProgressMeters - ProgressMeters) * 0.0015f, 0.9f, 1.1f);
        }
        float cap = maxSpeed * aggression * rubber;
        var planar = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (planar.magnitude < cap)
            rb.AddForce(transform.forward * acceleration * rubber, ForceMode.Acceleration);
        if (planar.magnitude > 0.5f)
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, steer * 110f * Time.fixedDeltaTime, 0f));
        var local = transform.InverseTransformDirection(rb.linearVelocity);
        local.x *= 0.82f;
        rb.linearVelocity = transform.TransformDirection(local);
    }
}
