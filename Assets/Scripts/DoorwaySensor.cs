using UnityEngine;

public interface IDoorwaySensor
{
    bool IsClear();
    string BlockingObjectName { get; }
}

// Açıklığı sabit kapı çerçevesinden ölçer; yukarı hareket eden kapağı takip etmez.
public sealed class DoorwaySensor : MonoBehaviour, IDoorwaySensor
{
    [SerializeField] private Transform frameOrigin;
    [SerializeField] private Transform ignoredDoorRoot;
    [SerializeField] private Vector3 localCenter = new Vector3(0f, 1.5f, 2.5f);
    [SerializeField] private Vector3 localSize = new Vector3(2.3f, 2.8f, 1f);
    [SerializeField] private LayerMask blockingLayers = ~0;
    [SerializeField] private string blockingObjectName = string.Empty;

    private readonly Collider[] hits = new Collider[32];
    public string BlockingObjectName => blockingObjectName;

    public bool IsClear()
    {
        blockingObjectName = string.Empty;
        if (!isActiveAndEnabled || frameOrigin == null || ignoredDoorRoot == null)
        {
            blockingObjectName = "Kapı açıklığı sensörü kullanılamıyor.";
            return false;
        }
        Vector3 scale = frameOrigin.lossyScale;
        var halfExtents = new Vector3(Mathf.Abs(localSize.x * scale.x) * 0.5f,
            Mathf.Abs(localSize.y * scale.y) * 0.5f, Mathf.Abs(localSize.z * scale.z) * 0.5f);
        Physics.SyncTransforms();
        int count = Physics.OverlapBoxNonAlloc(frameOrigin.TransformPoint(localCenter), halfExtents,
            hits, frameOrigin.rotation, blockingLayers, QueryTriggerInteraction.Ignore);
        if (count == hits.Length)
        {
            blockingObjectName = "Kapı açıklığının boşluğu güvenli biçimde doğrulanamadı.";
            return false;
        }
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit == null || hit.transform.IsChildOf(ignoredDoorRoot))
                continue;
            blockingObjectName = hit.name;
            return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (frameOrigin == null)
            return;
        Gizmos.matrix = frameOrigin.localToWorldMatrix;
        Gizmos.color = string.IsNullOrEmpty(blockingObjectName) ? Color.green : Color.red;
        Gizmos.DrawWireCube(localCenter, localSize);
    }
}
