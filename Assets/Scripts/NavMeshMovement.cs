using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class NavMeshMovement : MonoBehaviour, INavigationMotor
{
    [SerializeField, Min(0.05f)] private float destinationTolerance = 0.35f;
    [SerializeField, Min(1f)] private float stuckTimeout = 6f;
    [SerializeField, Min(1f)] private float travelTimeout = 45f;

    public NavigationStatus Status { get; private set; } = NavigationStatus.Idle;
    public string FailureReason { get; private set; } = string.Empty;

    private NavMeshAgent agent;
    private Vector3 lastProgressPosition;
    private float lastProgressTime;
    private float movementStarted;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public bool TryMoveTo(Vector3 destination)
    {
        FailureReason = string.Empty;
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (!isActiveAndEnabled || !agent.isActiveAndEnabled)
            return Fail("Robotun hareket bileşeni etkin değil.");

        var filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (!agent.isOnNavMesh)
        {
            if (!NavMesh.SamplePosition(transform.position, out var start, 0.5f, filter)
                || !agent.Warp(start.position))
                return Fail("Robot yürünebilir NavMesh alanında değil.");
        }
        if (!NavMesh.SamplePosition(destination, out var goal, destinationTolerance, filter))
            return Fail("Panelin önünde yürünebilir hedef bulunamadı.");

        var path = new NavMeshPath();
        if (!agent.CalculatePath(goal.position, path) || path.status != NavMeshPathStatus.PathComplete)
            return Fail("Panele ulaşan kesintisiz rota bulunamadı.");
        agent.isStopped = false;
        if (!agent.SetPath(path))
            return Fail("Hesaplanan rota robota atanamadı.");

        movementStarted = lastProgressTime = Time.time;
        lastProgressPosition = transform.position;
        Status = NavigationStatus.Moving;
        return true;
    }

    public void Tick()
    {
        if (Status != NavigationStatus.Moving)
            return;
        if (!isActiveAndEnabled || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh)
        {
            Fail("Robot hareket sırasında NavMesh bağlantısını kaybetti.");
            return;
        }
        if (Time.time - movementStarted >= travelTimeout)
        {
            Fail("Robot ayrılan sürede panele ulaşamadı.");
            return;
        }
        if (agent.pathPending)
            return;
        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Fail("Panel rotası artık tamamlanabilir durumda değil.");
            return;
        }
        if (!float.IsInfinity(agent.remainingDistance)
            && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            HaltAgent();
            Status = NavigationStatus.Arrived;
            return;
        }

        if ((transform.position - lastProgressPosition).sqrMagnitude >= 0.02f * 0.02f)
        {
            lastProgressPosition = transform.position;
            lastProgressTime = Time.time;
        }
        else if (Time.time - lastProgressTime >= stuckTimeout)
            Fail("Robot ilerleyemedi; yol veya fiziksel engeller kontrol edilmeli.");
    }

    public void Stop()
    {
        HaltAgent();
        Status = NavigationStatus.Idle;
    }

    private bool Fail(string reason)
    {
        HaltAgent();
        FailureReason = reason;
        Status = NavigationStatus.Failed;
        return false;
    }

    private void HaltAgent()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    private void OnDisable()
    {
        Stop();
    }
}
