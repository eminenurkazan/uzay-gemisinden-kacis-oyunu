using System;
using UnityEngine;

// Kaçış sırasının yetkili durum kaynağıdır; tuş okumaz, arayüz veya oyuncu kontrolü yönetmez.
public sealed class ShipDepartureController : MonoBehaviour, IShipDeparture
{
    [SerializeField] private MonoBehaviour shipAccess;
    [SerializeField] private Transform authorizedPlayer;
    [SerializeField] private Transform departureConsole;
    [SerializeField, Min(0.1f)] private float interactionRange = 1.8f;
    [SerializeField, Min(0.1f)] private float countdownDuration = 3f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 0.8f;
    [SerializeField] private ShipDepartureState currentState;
    [SerializeField] private string lastMessage = string.Empty;

    public ShipDepartureState CurrentState => currentState;
    public float FadeProgress => currentState == ShipDepartureState.Completed ? 1f
        : currentState == ShipDepartureState.Fading ? Mathf.Clamp01(elapsed / fadeDuration) : 0f;
    public string StatusMessage => currentState switch
    {
        ShipDepartureState.Ready => "Giriş kapağı kapalı.\nAcil ayrılma için kokpitin önündeki kumanda konsoluna yaklaş.",
        ShipDepartureState.Countdown => "Acil ayrılma başlatıldı.\n" + Mathf.CeilToInt(Mathf.Max(0f, countdownDuration - elapsed)),
        ShipDepartureState.Fading => "Gemin diğer gemiden ayrılıyor…",
        ShipDepartureState.Completed => "Başarıyla kaçtın.",
        _ => string.IsNullOrEmpty(lastMessage)
            ? "Acil ayrılma için önce gemiye girip giriş kapağını kapat." : lastMessage
    };
    public event Action<ShipDepartureState> StateChanged;

    private IShipAccess access;
    private float elapsed;

    private bool ConditionsMet => shipAccess != null && shipAccess.isActiveAndEnabled && access != null
        && authorizedPlayer != null && authorizedPlayer.gameObject.activeInHierarchy
        && access.IsPlayerInside && access.IsSealed;

    private void Awake()
    {
        access = shipAccess as IShipAccess;
        currentState = ShipDepartureState.Locked;
        if (access == null || authorizedPlayer == null || departureConsole == null)
        {
            Debug.LogError("Acil ayrılmanın gemi erişimi, oyuncu veya konsol bağlantısı eksik.", this);
            enabled = false;
        }
    }

    public bool IsInInteractionArea(Transform actor)
    {
        if (!isActiveAndEnabled || actor == null || actor != authorizedPlayer
            || !actor.gameObject.activeInHierarchy || departureConsole == null
            || access == null || !access.IsPlayerInside)
            return false;
        Vector3 offset = actor.position - departureConsole.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    public bool CanStart(Transform actor) => currentState == ShipDepartureState.Ready
        && ConditionsMet && IsInInteractionArea(actor);

    public bool TryStart(Transform actor)
    {
        if (!CanStart(actor))
            return false;
        elapsed = 0f;
        lastMessage = string.Empty;
        ChangeState(ShipDepartureState.Countdown);
        Debug.Log("Acil ayrılma başlatıldı; oyuncu içeride ve giriş kapağı tamamen kapalı. Geri sayım: "
            + countdownDuration.ToString("0.#") + " saniye.", this);
        return true;
    }

    private void Update()
    {
        if (currentState == ShipDepartureState.Completed)
            return;
        if (currentState == ShipDepartureState.Countdown || currentState == ShipDepartureState.Fading)
        {
            if (!ConditionsMet)
            {
                CancelDeparture();
                return;
            }
            elapsed += Time.deltaTime;
            if (currentState == ShipDepartureState.Countdown && elapsed >= countdownDuration)
            {
                elapsed = 0f;
                ChangeState(ShipDepartureState.Fading);
            }
            else if (currentState == ShipDepartureState.Fading && elapsed >= fadeDuration)
            {
                ChangeState(ShipDepartureState.Completed);
                Debug.Log("Kaçış başarılı; acil ayrılma tamamlandı ve oyun sona erdi.", this);
            }
            return;
        }
        if (ConditionsMet)
        {
            lastMessage = string.Empty;
            ChangeState(ShipDepartureState.Ready);
        }
        else
            ChangeState(ShipDepartureState.Locked);
    }

    private void CancelDeparture()
    {
        elapsed = 0f;
        lastMessage = "Acil ayrılma iptal edildi. Geminin içinde ve giriş kapağı kapalı olmalı.";
        ChangeState(ShipDepartureState.Locked);
        Debug.LogWarning(lastMessage, this);
    }

    private void ChangeState(ShipDepartureState next)
    {
        if (currentState == next)
            return;
        currentState = next;
        StateChanged?.Invoke(next);
    }

    private void OnDisable()
    {
        if (currentState == ShipDepartureState.Countdown || currentState == ShipDepartureState.Fading)
            CancelDeparture();
    }
}
