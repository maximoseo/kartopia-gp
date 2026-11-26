using UnityEngine;

public class ChaseCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 4.2f, -8.5f);
    public float followSharpness = 6f;

    void LateUpdate()
    {
        if (target == null) return;
        var desired = target.position + target.rotation * offset;
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        transform.LookAt(target.position + Vector3.up * 1.2f + target.forward * 4f);
    }
}
