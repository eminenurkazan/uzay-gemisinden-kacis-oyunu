using UnityEngine;

public interface IObstacleSensor
{
    string ObstacleName { get; }
    bool Scan(Vector3 direction, float distanceToNextCorner);
}

public sealed class ObstacleSensor : MonoBehaviour, IObstacleSensor
{
    [SerializeField, Min(0.1f)] private float detectionRange = 0.9f;
    [SerializeField, Min(0f)] private float originHeight = 0.4f;
    [SerializeField, Min(0f)] private float halfWidth = 0.24f;
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private bool obstacleDetected;
    [SerializeField] private float obstacleDistance;
    [SerializeField] private float obstacleAngle;
    [SerializeField] private string obstacleName;

    public string ObstacleName => obstacleName;

    private readonly RaycastHit[] hits = new RaycastHit[32];
    private Vector3 lastDirection;
    private float lastRange;

    public bool Scan(Vector3 direction, float distanceToNextCorner)
    {
        obstacleDetected = false;
        obstacleDistance = float.PositiveInfinity;
        obstacleAngle = 0f;
        obstacleName = string.Empty;
        direction.y = 0f;
        lastRange = Mathf.Min(detectionRange, Mathf.Max(0f, distanceToNextCorner));
        lastDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
        if (!isActiveAndEnabled || lastDirection == Vector3.zero || lastRange < 0.01f)
            return false;

        Vector3 side = Vector3.Cross(Vector3.up, lastDirection);
        for (int i = -1; i <= 1; i++)
        {
            Vector3 origin = transform.position + Vector3.up * originHeight + side * (i * halfWidth);
            int count = Physics.RaycastNonAlloc(origin, lastDirection, hits, lastRange,
                obstacleLayers, QueryTriggerInteraction.Ignore);
            for (int j = 0; j < count; j++)
            {
                var hit = hits[j];
                // Kendi görsel gövdesi engel değildir.
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;
                if (hit.distance >= obstacleDistance)
                    continue;
                obstacleDetected = true;
                obstacleDistance = hit.distance;
                Vector3 offset = hit.point - transform.position;
                offset.y = 0f;
                obstacleAngle = Vector3.SignedAngle(transform.forward, offset, Vector3.up);
                obstacleName = hit.collider.name;
            }
            Debug.DrawRay(origin, lastDirection * lastRange, obstacleDetected ? Color.red : Color.green);
        }
        return obstacleDetected;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 direction = lastDirection == Vector3.zero ? transform.forward : lastDirection;
        float range = lastRange > 0f ? lastRange : detectionRange;
        Vector3 side = Vector3.Cross(Vector3.up, direction);
        Gizmos.color = obstacleDetected ? Color.red : Color.green;
        for (int i = -1; i <= 1; i++)
        {
            Vector3 origin = transform.position + Vector3.up * originHeight + side * (i * halfWidth);
            Gizmos.DrawLine(origin, origin + direction * range);
        }
    }
}
