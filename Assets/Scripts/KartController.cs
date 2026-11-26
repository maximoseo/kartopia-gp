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
    public bool boostDisabled;

    Rigidbody rb;
    float driftCharge;
    float boostTime;
    bool wasDrifting;

    public bool BoostActive => boostTime > 0f;

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

    void Awake() => rb = GetComponent<Rigidbody>();

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
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var pos = Touchscreen.current.primaryTouch.position.ReadValue();
            throttle = 1f;
            steer = Mathf.Clamp((pos.x / Mathf.Max(Screen.width, 1) - 0.5f) * 2.4f, -1f, 1f);
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

        float accel = acceleration + (BoostActive ? boostAccel : 0f);
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
        // grip: kill most lateral slide unless drifting
        var local = transform.InverseTransformDirection(rb.linearVelocity);
        local.x *= drifting ? 0.92f : lateralGripRetention;
        rb.linearVelocity = transform.TransformDirection(local);
    }
}
