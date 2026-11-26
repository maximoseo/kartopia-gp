using UnityEngine;

public class WaterFlow : MonoBehaviour
{
    Material _mat;
    void Awake()
    {
        var r = GetComponent<MeshRenderer>();
        if (r != null) _mat = r.material;
    }
    void Update()
    {
        if (_mat != null)
            _mat.mainTextureOffset += new Vector2(Time.deltaTime * 0.008f, Time.deltaTime * 0.005f);
    }
}

public class BirdFlock : MonoBehaviour
{
    public Vector3 center = Vector3.zero;
    public float radius = 75f;
    public float height = 30f;
    public float speed = 0.05f;
    float _angle;
    Transform[] _wings;

    void Start()
    {
        _angle = transform.position.x;
        _wings = GetComponentsInChildren<Transform>();
    }

    void Update()
    {
        _angle += speed * Time.deltaTime;
        var target = center + new Vector3(Mathf.Cos(_angle) * radius, height + Mathf.Sin(Time.time * 0.7f) * 2f, Mathf.Sin(_angle) * radius);
        transform.position = target;
        transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(_angle), 0f, Mathf.Cos(_angle)));
        float flap = Mathf.Sin(Time.time * 9f) * 0.55f;
        for (int i = 1; i < _wings.Length; i++)
        {
            if (_wings[i].name == "WingL") _wings[i].localRotation = Quaternion.Euler(0f, 0f, flap);
            if (_wings[i].name == "WingR") _wings[i].localRotation = Quaternion.Euler(0f, 0f, -flap);
        }
    }
}

public class CrowdBob : MonoBehaviour
{
    float _baseY;
    float _phase;
    void Start()
    {
        _baseY = transform.localPosition.y;
        _phase = (transform.localPosition.x + transform.localPosition.z) * 1.7f;
    }
    void Update()
    {
        var p = transform.localPosition;
        p.y = _baseY + Mathf.Abs(Mathf.Sin(Time.time * 2.4f + _phase)) * 0.09f;
        transform.localPosition = p;
    }
}
