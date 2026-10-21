using System;
using UnityEngine;
using UnityEngine.AI;

public enum KaelAtmosphereState
{
    Active,
    Collapsing,
    Incapacitated
}

// Panelin tek seferlik olayını dinler; tuş okumaz ve geçiş izni üretmez.
public sealed class KaelAtmosphereResponse : MonoBehaviour, IAtmosphereTarget
{
    [SerializeField] private MonoBehaviour atmosphereControl;
    [SerializeField] private Transform visualBody;
    [SerializeField] private CapsuleCollider bodyCollider;
    [SerializeField] private Transform airlockOrigin;
    [SerializeField] private KaelFollowPlayer follower;
    [SerializeField] private MonoBehaviour navigationMotor;
    [SerializeField] private NavMeshAgent navigationAgent;
    [SerializeField] private KaelInteraction dialogueInteraction;
    [SerializeField] private MonoBehaviour dialogueClient;
    [SerializeField, Min(0.1f)] private float collapseDuration = 0.9f;
    [SerializeField] private KaelAtmosphereState currentState;

    public KaelAtmosphereState CurrentState => currentState;
    public bool IsIncapacitated => currentState == KaelAtmosphereState.Incapacitated;
    public bool CanBeAffected => isActiveAndEnabled && currentState == KaelAtmosphereState.Active
        && bodyCollider != null && bodyCollider.enabled && bodyCollider.gameObject.activeInHierarchy;
    public bool IsFullyInsideAirlock => control != null && bodyCollider != null
        && bodyCollider.enabled && control.ContainsEffectBounds(bodyCollider.bounds);
    public string StatusMessage => currentState switch
    {
        KaelAtmosphereState.Collapsing => "Kael solunum yapamıyor…\nGiysinin oksijen sistemi seni koruyor.",
        KaelAtmosphereState.Incapacitated => "Kael etkisiz hale geldi.",
        _ => string.Empty
    };
    public event Action Incapacitated;

    private IAtmosphereControl control;
    private INavigationMotor motor;
    private bool subscribed;
    private float elapsed;
    private Vector3 standingPosition;
    private Quaternion standingRotation;
    private Vector3 fallDirection;
    private Vector3 rotationAxis;
    private float halfHeight;
    private float radius;

    private void Awake()
    {
        control = atmosphereControl as IAtmosphereControl;
        motor = navigationMotor as INavigationMotor;
        currentState = KaelAtmosphereState.Active;
        if (control == null || motor == null || visualBody == null || bodyCollider == null
            || airlockOrigin == null || follower == null || navigationAgent == null
            || dialogueInteraction == null || dialogueClient == null)
        {
            Debug.LogError("Kael atmosfer tepkisinin panel, gövde, hareket veya konuşma bağlantısı eksik.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (control == null)
            control = atmosphereControl as IAtmosphereControl;
        if (control != null && !subscribed)
        {
            control.AtmosphereControlCompleted += HandleAtmosphereCompleted;
            subscribed = true;
        }
    }

    private void HandleAtmosphereCompleted()
    {
        if (!CanBeAffected || !IsFullyInsideAirlock
            || control.CurrentState != AtmosphereControlState.Completed)
            return;

        currentState = KaelAtmosphereState.Collapsing;
        elapsed = 0f;
        standingPosition = visualBody.position;
        standingRotation = visualBody.rotation;
        Bounds bounds = bodyCollider.bounds;
        radius = Mathf.Min(bounds.extents.x, bounds.extents.z);
        halfHeight = Mathf.Max(radius, bounds.extents.y);
        fallDirection = airlockOrigin.position - transform.position;
        fallDirection.y = 0f;
        if (fallDirection.sqrMagnitude < 0.001f)
            fallDirection = transform.right;
        fallDirection.Normalize();
        rotationAxis = Vector3.Cross(Vector3.up, fallDirection).normalized;

        follower.enabled = false;
        motor.Stop();
        navigationMotor.enabled = false;
        navigationAgent.enabled = false;
        dialogueInteraction.enabled = false;
        dialogueClient.StopAllCoroutines();
        dialogueClient.enabled = false;
        Debug.Log("Kael: Atmosfer solunumuma uygun değil; takip ve konuşma durduruldu.", this);
    }

    private void Update()
    {
        if (currentState != KaelAtmosphereState.Collapsing)
            return;
        elapsed += Time.deltaTime;
        float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / collapseDuration));
        float radians = progress * Mathf.PI * 0.5f;
        float segmentHalfLength = halfHeight - radius;
        // Kapsülün alt yüzeyini yerde tutarak yana yatır; gövde zeminin içine düşmez.
        float heightOffset = radius + segmentHalfLength * Mathf.Cos(radians) - halfHeight;
        visualBody.position = standingPosition + fallDirection * (segmentHalfLength * Mathf.Sin(radians))
            + Vector3.up * heightOffset;
        visualBody.rotation = Quaternion.AngleAxis(progress * 90f, rotationAxis) * standingRotation;
        if (elapsed < collapseDuration)
            return;
        currentState = KaelAtmosphereState.Incapacitated;
        Debug.Log("Kael etkisiz hale geldi; gemiye geçiş için gerekli durum hazır.", this);
        Incapacitated?.Invoke();
    }

    private void OnDisable()
    {
        if (control != null && subscribed)
            control.AtmosphereControlCompleted -= HandleAtmosphereCompleted;
        subscribed = false;
    }
}
