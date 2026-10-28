using UnityEngine;

// Ön koşulları ve gemiye giriş/kapatma durumunu yönetir; tuş okumaz ve kapağı hareket ettirmez.
public sealed class ShipAccessController : MonoBehaviour, IShipAccess
{
    [SerializeField] private MonoBehaviour atmosphereControl;
    [SerializeField] private MonoBehaviour atmosphereTarget;
    [SerializeField] private MonoBehaviour hatchController;
    [SerializeField] private Transform authorizedPlayer;
    [SerializeField] private CharacterController playerBody;
    [SerializeField] private Transform closingPanel;
    [SerializeField] private Vector3 interactionAreaCenter = new Vector3(0f, 1.5f, -3.5f);
    [SerializeField] private Vector3 interactionAreaSize = new Vector3(2.4f, 3f, 3.6f);
    [SerializeField] private Vector3 cabinAreaCenter = new Vector3(0f, 1.5f, 0.5f);
    [SerializeField] private Vector3 cabinAreaSize = new Vector3(5.5f, 3.1f, 5.5f);
    [SerializeField, Min(0.1f)] private float interactionRange = 1.8f;
    [SerializeField, Min(1f)] private float openingTimeout = 8f;
    [SerializeField] private ShipAccessState currentState;
    [SerializeField] private string failureReason = string.Empty;

    public ShipAccessState CurrentState => currentState;
    public bool IsPlayerInside => IsEntirePlayerInside();
    public bool IsSealed => currentState == ShipAccessState.Sealed && PrerequisitesMet
        && HatchAvailable && hatch.IsClosed;
    public string StatusMessage => currentState switch
    {
        ShipAccessState.Locked => "Gemi kapağı kilitli. Önce atmosfer paneliyle Kael'i etkisiz hale getir.",
        ShipAccessState.Ready => "Kael etkisiz hale geldi.\nHava kilidinin sonundaki gemi kapağına yaklaş ve E ile aç.",
        ShipAccessState.Opening => "Gemi kapağı açılıyor…",
        ShipAccessState.Open => "Gemi kapağı açık.\nAçıklıktan geçerek kendi gemine gir.",
        ShipAccessState.Boarded => hatch != null && !hatch.CanCloseSafely
            ? "Kendi gemindesin.\nKapağı kapatmak için açıklıktaki engelin çekilmesini bekle."
            : "Kendi gemine girdin.\nGirişin solundaki iç kapak paneline yaklaş.",
        ShipAccessState.Closing => "Gemi kapağı kapanıyor…\nGeminin içinde kal.",
        ShipAccessState.Reopening => "Kapak güvenlik için yeniden açılıyor.\nTamamen içerideyken panelden tekrar kapatabilirsin.",
        ShipAccessState.Sealed => "Geminin giriş kapağı kapandı.\nKokpitin önündeki acil ayrılma konsoluna ilerle.",
        ShipAccessState.Failed => failureReason,
        _ => string.Empty
    };

    private IAtmosphereControl panel;
    private IAtmosphereTarget target;
    private IClosableDoor hatch;
    private float movementStarted;
    private bool openingRequested;

    private bool PrerequisitesMet => atmosphereControl != null && atmosphereControl.isActiveAndEnabled
        && panel != null && panel.CurrentState == AtmosphereControlState.Completed
        && atmosphereTarget != null && atmosphereTarget.isActiveAndEnabled
        && target != null && target.IsIncapacitated;
    private bool HatchAvailable => hatchController != null && hatchController.isActiveAndEnabled
        && hatch != null && hatch.IsAvailable;

    private void Awake()
    {
        panel = atmosphereControl as IAtmosphereControl;
        target = atmosphereTarget as IAtmosphereTarget;
        hatch = hatchController as IClosableDoor;
        currentState = ShipAccessState.Locked;
        if (panel == null || target == null || hatch == null || authorizedPlayer == null
            || playerBody == null || playerBody.transform != authorizedPlayer || closingPanel == null)
        {
            Fail("Gemi geçişinin panel, Kael, kapak veya oyuncu bağlantısı eksik.");
            enabled = false;
        }
    }

    private bool IsAuthorized(Transform actor) => isActiveAndEnabled && actor != null
        && actor == authorizedPlayer && actor.gameObject.activeInHierarchy;

    private bool IsInOpeningArea(Transform actor)
    {
        if (!IsAuthorized(actor) || hatchController == null)
            return false;
        if (!new Bounds(interactionAreaCenter, interactionAreaSize)
            .Contains(transform.InverseTransformPoint(actor.position)))
            return false;
        return IsNear(actor, hatchController.transform);
    }

    private bool IsInClosingArea(Transform actor) => IsAuthorized(actor) && closingPanel != null
        && IsEntirePlayerInside() && IsNear(actor, closingPanel);

    private bool IsNear(Transform actor, Transform destination)
    {
        Vector3 offset = actor.position - destination.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    public bool IsInInteractionArea(Transform actor) => currentState == ShipAccessState.Boarded
        || currentState == ShipAccessState.Closing || currentState == ShipAccessState.Reopening
        || currentState == ShipAccessState.Sealed
            ? IsInClosingArea(actor) : IsInOpeningArea(actor);

    public bool CanOpenHatch(Transform actor) => IsInOpeningArea(actor) && PrerequisitesMet
        && HatchAvailable && !openingRequested
        && (currentState == ShipAccessState.Ready || currentState == ShipAccessState.Failed);

    public bool TryOpenHatch(Transform actor)
    {
        if (!CanOpenHatch(actor))
            return false;
        if (!hatch.TryOpen(out string reason))
        {
            Fail("Gemi kapağı açılamadı: " + reason);
            return false;
        }
        openingRequested = true;
        movementStarted = Time.time;
        failureReason = string.Empty;
        currentState = ShipAccessState.Opening;
        Debug.Log("Gemi kapağı açılıyor; Kael'in etkisizleşmesi ve atmosfer işlemi doğrulandı.", this);
        return true;
    }

    public bool CanCloseHatch(Transform actor) => currentState == ShipAccessState.Boarded
        && IsInClosingArea(actor) && PrerequisitesMet && HatchAvailable
        && hatch.IsOpen && hatch.CanCloseSafely;

    public bool TryCloseHatch(Transform actor)
    {
        if (!CanCloseHatch(actor))
            return false;
        if (!hatch.TryClose(out string reason))
        {
            Debug.LogWarning("Gemi kapağı kapatılamadı: " + reason, this);
            return false;
        }
        currentState = ShipAccessState.Closing;
        movementStarted = Time.time;
        Debug.Log("Gemi kapağı içerideki panelden kapanıyor; oyuncu tamamen geminin içinde.", this);
        return true;
    }

    private void Update()
    {
        if (!openingRequested)
        {
            if (currentState != ShipAccessState.Failed)
                currentState = PrerequisitesMet ? ShipAccessState.Ready : ShipAccessState.Locked;
            return;
        }
        if (currentState == ShipAccessState.Failed)
            return;
        if (!PrerequisitesMet || !HatchAvailable)
        {
            Fail("Gemi geçişi kullanılamıyor; panel, Kael ve kapak bağlantılarını kontrol et.");
            return;
        }
        if (currentState == ShipAccessState.Sealed)
        {
            if (!hatch.IsClosed)
                Fail("Gemi kapağının kapalı durumu korunamadı.");
            return;
        }
        if (currentState == ShipAccessState.Closing)
        {
            if (!IsEntirePlayerInside())
            {
                Reopen("Oyuncu kapanış sırasında geminin iç alanından çıktı.");
                return;
            }
            if (hatch.IsClosed)
            {
                currentState = ShipAccessState.Sealed;
                Debug.Log("Geminin giriş kapağı tamamen kapandı; kapalı durum doğrulandı.", this);
            }
            else if (!hatch.IsClosing)
                Reopen("Kapak güvenlik nedeniyle kapanmayı durdurdu.");
            else if (Time.time - movementStarted >= openingTimeout)
                Reopen("Kapak kapanışı zaman aşımına uğradı.");
            return;
        }
        if (!hatch.IsOpen)
        {
            if (Time.time - movementStarted >= openingTimeout)
                Fail("Gemi kapağı ayrılan sürede tamamen açılamadı.");
            return;
        }

        bool inside = IsEntirePlayerInside();
        var next = inside ? ShipAccessState.Boarded : ShipAccessState.Open;
        if (currentState == next)
            return;
        bool wasReopening = currentState == ShipAccessState.Reopening;
        currentState = next;
        Debug.Log(wasReopening ? "Gemi kapağı yeniden açıldı; içeriden tekrar kapatılabilir."
            : inside ? "Kendi gemine girdin; oyuncunun gövdesi kokpit alanının içinde."
            : "Gemi kapağı açık; kokpite geçebilirsin.", this);
    }

    private void Reopen(string reason)
    {
        if (!hatch.TryOpen(out string failure))
        {
            Fail("Kapak güvenli biçimde yeniden açılamadı: " + failure);
            return;
        }
        currentState = ShipAccessState.Reopening;
        movementStarted = Time.time;
        Debug.LogWarning(reason + " Kapak yeniden açılıyor.", this);
    }

    private bool IsEntirePlayerInside()
    {
        if (playerBody == null || !playerBody.enabled || !playerBody.gameObject.activeInHierarchy)
            return false;
        var cabin = new Bounds(cabinAreaCenter, cabinAreaSize);
        Bounds body = playerBody.bounds;
        for (int corner = 0; corner < 8; corner++)
        {
            var point = new Vector3((corner & 1) == 0 ? body.min.x : body.max.x,
                (corner & 2) == 0 ? body.min.y : body.max.y,
                (corner & 4) == 0 ? body.min.z : body.max.z);
            if (!cabin.Contains(transform.InverseTransformPoint(point)))
                return false;
        }
        return true;
    }

    private void Fail(string reason)
    {
        if (currentState == ShipAccessState.Failed && failureReason == reason)
            return;
        failureReason = reason;
        currentState = ShipAccessState.Failed;
        Debug.LogWarning(reason, this);
    }
}
