using UnityEngine;
using UnityEngine.AI;

// Animates visual children from actual navigation velocity; never moves the agent or selects a route.
[DisallowMultipleComponent]
public sealed class LightweightCharacterMotion : MonoBehaviour
{
    [SerializeField] private NavMeshAgent navigationAgent;
    [SerializeField] private Transform[] wheels = new Transform[0];
    [SerializeField] private Transform[] walkingLimbs = new Transform[0];
    [SerializeField, Min(0.01f)] private float wheelRadius = 0.115f;
    private Quaternion[] restingRotations;
    private float phase;
    private float stride;

    private void Awake()
    {
        restingRotations = new Quaternion[walkingLimbs.Length];
        for (int i = 0; i < walkingLimbs.Length; i++)
            restingRotations[i] = walkingLimbs[i] != null ? walkingLimbs[i].localRotation : Quaternion.identity;
    }

    private void LateUpdate()
    {
        Vector3 velocity = navigationAgent != null && navigationAgent.isActiveAndEnabled && navigationAgent.isOnNavMesh
            ? navigationAgent.velocity : Vector3.zero;
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        if (speed > 0.01f)
        {
            float direction = Vector3.Dot(transform.forward, velocity) < 0f ? -1f : 1f;
            float angle = direction * speed * Time.deltaTime / wheelRadius * Mathf.Rad2Deg;
            foreach (var wheel in wheels)
                if (wheel != null)
                    wheel.Rotate(Vector3.right, angle, Space.Self);
            phase = Mathf.Repeat(phase + speed * Time.deltaTime * 6f, Mathf.PI * 2f);
        }
        stride = Mathf.MoveTowards(stride, Mathf.Clamp01(speed / 0.8f), Time.deltaTime * 7f);
        for (int i = 0; i < walkingLimbs.Length; i++)
        {
            if (walkingLimbs[i] == null)
                continue;
            float direction = (i == 0 || i == 3) ? 1f : -1f;
            float degrees = Mathf.Sin(phase) * stride * direction * (i < 2 ? 14f : 19f);
            walkingLimbs[i].localRotation = restingRotations[i] * Quaternion.Euler(degrees, 0f, 0f);
        }
    }
}
