using UnityEngine;
using UnityEngine.AI;

// Düşmüş gövdenin fiziksel sınırını NavMesh'e bildirir; atmosfer veya ajan kararı üretmez.
public sealed class KaelFallenObstacle : MonoBehaviour
{
    [SerializeField] private KaelAtmosphereResponse atmosphereResponse;
    [SerializeField] private CapsuleCollider bodyCollider;
    [SerializeField, Min(0f)] private float clearancePadding = 0.08f;

    private NavMeshObstacle obstacle;

    private void Awake()
    {
        if (atmosphereResponse == null || bodyCollider == null)
        {
            Debug.LogError("Kael'in yerdeki gezinme engeli için atmosfer tepkisi veya gövde bağlantısı eksik.", this);
            enabled = false;
            return;
        }

        var obstacleObject = new GameObject("KaelYerdekiNavMeshEngeli");
        obstacleObject.SetActive(false);
        obstacleObject.transform.SetParent(transform, false);
        obstacle = obstacleObject.AddComponent<NavMeshObstacle>();
        obstacle.enabled = false;
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        obstacle.carvingMoveThreshold = 0.01f;
        obstacleObject.SetActive(true);
    }

    private void OnEnable()
    {
        if (atmosphereResponse == null)
            return;
        atmosphereResponse.Incapacitated += ApplyObstacle;
        if (atmosphereResponse.IsIncapacitated)
            ApplyObstacle();
    }

    private void ApplyObstacle()
    {
        if (obstacle == null || bodyCollider == null || !bodyCollider.enabled)
            return;

        // Son devrilme karesindeki collider sınırı kullanılmalı; ayakta kalmış eski sınır değil.
        Physics.SyncTransforms();
        Bounds bounds = bodyCollider.bounds;
        bounds.Expand(clearancePadding * 2f);
        obstacle.transform.position = bounds.center;
        obstacle.transform.rotation = Quaternion.identity;
        Vector3 scale = obstacle.transform.lossyScale;
        obstacle.center = Vector3.zero;
        obstacle.size = new Vector3(bounds.size.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
            bounds.size.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)),
            bounds.size.z / Mathf.Max(0.001f, Mathf.Abs(scale.z)));
        obstacle.enabled = true;
        Debug.Log("Kael'in yerdeki gövdesi NavMesh engeline dönüştü; merkez: "
            + bounds.center + ", boyut: " + bounds.size, this);
    }

    private void OnDisable()
    {
        if (atmosphereResponse != null)
            atmosphereResponse.Incapacitated -= ApplyObstacle;
        if (obstacle != null)
            obstacle.enabled = false;
    }

    private void OnDestroy()
    {
        if (obstacle != null)
            Destroy(obstacle.gameObject);
    }
}
