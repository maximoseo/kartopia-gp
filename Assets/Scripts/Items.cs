using UnityEngine;

public class ItemBox : MonoBehaviour
{
    float _respawn;
    MeshRenderer _renderer;
    Collider _collider;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
    }

    void Update()
    {
        transform.Rotate(0f, 90f * Time.deltaTime, 0f);
        if (_respawn > 0f)
        {
            _respawn -= Time.deltaTime;
            if (_respawn <= 0f) SetVisible(true);
        }
    }

    void SetVisible(bool v)
    {
        _renderer.enabled = v;
        _collider.enabled = v;
    }

    public void Collect(KartController kart)
    {
        kart.GiveBoost(1.0f);
        SetVisible(false);
        _respawn = 6f;
    }

    public void CollectAi() { SetVisible(false); _respawn = 6f; }
}

public class BoostPad : MonoBehaviour { }

public class BananaPeel : MonoBehaviour
{
    public void Hit(KartController kart)
    {
        kart.SlowDown(0.45f, 0.5f);
        gameObject.SetActive(false);
    }
}
