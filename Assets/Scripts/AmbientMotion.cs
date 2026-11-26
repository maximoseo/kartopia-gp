using UnityEngine;

public class CloudDrift : MonoBehaviour
{
    public float speed = 1.1f;
    void Update()
    {
        transform.position += Vector3.right * (speed * Time.deltaTime);
        if (transform.position.x > 150f) transform.position -= Vector3.right * 300f;
    }
}

public class FlagWave : MonoBehaviour
{
    float _phase;
    void Start() => _phase = transform.position.x * 0.37f;
    void Update()
    {
        var e = transform.localEulerAngles;
        e.y = Mathf.Sin(Time.time * 3.2f + _phase) * 14f;
        e.z = Mathf.Sin(Time.time * 4.1f + _phase) * 6f;
        transform.localEulerAngles = e;
    }
}
