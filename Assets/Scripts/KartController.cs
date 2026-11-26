using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Arcade kart: throttle/steer, drift charge -> mini-turbo boost, slipstream-ready.</summary>
[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    public float maxSpeed = 28f;
    public float acceleration = 18f;
    public float turnSpeed = 90f;
    public float driftChargeTime = 0.7f;
    public float boostDuration = 0.85f;
    public float boostAccel = 14f;
    public float lateralGripRetention = 0.75f;
    public AudioClip engineClip;
    AudioSource _engine;
    ParticleSystem _smoke;
    public bool boostDisabled;

    Rigidbody rb;
    Transform _visual;
    float driftCharge;
    float boostTime;
    bool wasDrifting;

    public bool BoostActive => boostTime > 0f;
    public bool Slipstreaming { get; private set; }
    float _slowUntil;
    AiDriver[] _opponents;

    public void ApplySlowZone() => _slowUntil = Time.time + 0.25f;

    bool driftingNow() => _visual != null && driftCharge > 0.02f;

    float _shieldUntil;
    GameObject _shieldBubble;
    public bool ShieldActive => Time.time < _shieldUntil;

    public void GiveShield(float seconds = 4f)
    {
        _shieldUntil = Time.time + seconds;
        if (_shieldBubble == null)
        {
            _shieldBubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _shieldBubble.name = "ShieldBubble";
            _shieldBubble.transform.SetParent(transform);
            _shieldBubble.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            _shieldBubble.transform.localScale = Vector3.one * 2.6f;
            var mr = _shieldBubble.GetComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mr.material.SetColor("_BaseColor", new Color(0.4f, 0.85f, 1f, 0.28f));
            mr.material.SetFloat("_Surface", 1f);
            _shieldBubble.GetComponent<Collider>().enabled = false;
        }
        _shieldBubble.SetActive(true);
    }

    public void GiveBoost(float seconds = 0.9f) => boostTime = Mathf.Max(boostTime, seconds);

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<ItemBox>(out var box)) { box.Collect(this); return; }
        if (other.TryGetComponent<BoostPad>(out _)) GiveBoost(0.7f);
        if (other.TryGetComponent<BananaPeel>(out var peel)) { peel.Hit(this); }
    }

    public void SlowDown(float factor, float seconds)
    {
        rb.linearVelocity *= factor;
        boostTime = 0f;
    }
    public float ChargeRatio => Mathf.Clamp01(driftCharge / driftChargeTime);

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        _visual = transform.Find("KartModel");
        if (engineClip != null)
        {
            _engine = gameObject.AddComponent<AudioSource>();
            _engine.clip = engineClip; _engine.loop = true; _engine.volume = 0.0f;
            _engine.Play();
        }
        var smokeObj = new GameObject("DriftSmoke");
        smokeObj.transform.SetParent(transform);
        smokeObj.transform.localPosition = new Vector3(0f, 0.25f, -1.0f);
        _smoke = smokeObj.AddComponent<ParticleSystem>();
        var main = _smoke.main;
        main.startLifetime = 0.55f; main.startSpeed = 1.2f; main.startSize = 0.55f;
        main.startColor = new Color(1f, 1f, 1f, 0.5f); main.maxParticles = 90;
        var emission = _smoke.emission; emission.rateOverTime = 0f;
        var shape = _smoke.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.4f;
        var smokeRenderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        smokeRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        _smoke.Stop();
    }

    void FixedUpdate()
    {
        float throttle = 0f, steer = 0f;
        bool drift = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttle -= 0.6f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
            drift = kb.spaceKey.isPressed;
        }
        if (TouchControls.Gas) throttle = 1f;
        if (TouchControls.Brake) throttle = -0.6f;
        if (TouchControls.Left) steer -= 1f;
        if (TouchControls.Right) steer += 1f;
        if (TouchControls.Drift) drift = true;
        steer = Mathf.Clamp(steer, -1f, 1f);

        if (_shieldBubble != null && _shieldBubble.activeSelf && !ShieldActive)
            _shieldBubble.SetActive(false);
        if (_engine != null)
        {
            float sr = Mathf.Clamp01(Mathf.Abs(Vector3.Dot(rb.linearVelocity, transform.forward)) / Mathf.Max(maxSpeed, 1f));
            _engine.pitch = 0.75f + sr * 0.95f;
            _engine.volume = 0.12f + Mathf.Abs(throttle) * 0.2f;
        }
        if (_smoke != null)
        {
            var emission = _smoke.emission;
            bool emit = driftingNow();
            emission.rateOverTime = emit ? 42f : 0f;
            if (emit && !_smoke.isPlaying) _smoke.Play();
            if (!emit && _smoke.isPlaying) { /* let particles fade */ }
        }
        var fwdSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
        bool drifting = drift && fwdSpeed > 8f;
        if (drifting && !boostDisabled) driftCharge += Time.fixedDeltaTime;
        else if (wasDrifting)
        {
            if (driftCharge >= driftChargeTime) boostTime = boostDuration;
            driftCharge = 0f;
        }
        wasDrifting = drifting;
        if (boostTime > 0f) boostTime -= Time.fixedDeltaTime;

        // slipstream: tuck behind an opponent for extra pace
        Slipstreaming = false;
        if (_opponents == null) _opponents = FindObjectsByType<AiDriver>(FindObjectsSortMode.None);
        foreach (var ai in _opponents)
        {
            if (ai == null) continue;
            var rel = ai.transform.position - transform.position;
            float ahead = Vector3.Dot(rel, transform.forward);
            float lateral = Vector3.Dot(rel, transform.right);
            if (ahead > 2.5f && ahead < 11f && Mathf.Abs(lateral) < 2.1f && fwdSpeed > 9f)
            { Slipstreaming = true; break; }
        }
        float accel = acceleration + (BoostActive ? boostAccel : 0f) + (Slipstreaming ? 7f : 0f);
        if (Time.time < _slowUntil) accel *= 0.45f;
        if (throttle != 0f)
            rb.AddForce(transform.forward * (throttle * accel), ForceMode.Acceleration);

        var planar = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float cap = maxSpeed * (BoostActive ? 1.15f : 1f);
        if (planar.magnitude > cap)
        {
            planar = planar.normalized * cap;
            rb.linearVelocity = new Vector3(planar.x, rb.linearVelocity.y, planar.z);
        }

        if (Mathf.Abs(fwdSpeed) > 0.5f)
        {
            float dir = Mathf.Sign(fwdSpeed);
            float turn = steer * turnSpeed * (drifting ? 1.45f : 1f) * dir;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn * Time.fixedDeltaTime, 0f));
        }
        if (_visual != null)
        {
            float roll = -steer * (drifting ? 9f : 4f);
            float yaw = steer * (drifting ? 12f : 5f);
            _visual.localRotation = Quaternion.Slerp(_visual.localRotation,
                Quaternion.Euler(0f, 180f + yaw, roll), 1f - Mathf.Exp(-10f * Time.fixedDeltaTime));
        }
        // grip: kill most lateral slide unless drifting
        var local = transform.InverseTransformDirection(rb.linearVelocity);
        local.x *= drifting ? 0.92f : lateralGripRetention;
        rb.linearVelocity = transform.TransformDirection(local);
    }
}
