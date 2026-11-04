using UnityEngine;
using UnityEngine.AI;

// Serialized agents stay disabled until every surface has registered its baked data.
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public sealed class NavMeshAgentStartup : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float placementTolerance = 0.15f;

    private void Start()
    {
        var agent = GetComponent<NavMeshAgent>();
        if (agent.enabled)
        {
            Debug.LogError(name + ": NavMeshAgent sahnede kapalı kaydedilmelidir; başlangıç sırası güvenli değil.", this);
            return;
        }
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(transform.position, out var hit, placementTolerance, filter))
        {
            Debug.LogError(name + ": Başlangıç konumunda kendi ajan türüne uygun NavMesh bulunamadı. Tür="
                + agent.agentTypeID + ", konum=" + transform.position, this);
            return;
        }
        // Correct only the tiny spawn-to-surface offset, before navigation starts.
        transform.position = hit.position;
        agent.enabled = true;
        if (!agent.isOnNavMesh)
        {
            agent.enabled = false;
            Debug.LogError(name + ": NavMesh doğrulandığı hâlde ajan bağlanamadı. Tür=" + agent.agentTypeID, this);
            return;
        }
        Debug.Log(name + ": NavMesh başlangıcı doğrulandı. Tür=" + agent.agentTypeID, this);
    }
}
