using UnityEngine;

// Ön koşulları ve gemiye giriş durumunu yönetir; tuş okumaz ve kapıyı hareket ettirmez.
public sealed class ShipAccessController : MonoBehaviour, IShipAccess
{
    [SerializeField] private MonoBehaviour atmosphereControl;
    [SerializeField] private MonoBehaviour atmosphereTarget;
    [SerializeField] private MonoBehaviour hatchController;
    [SerializeField] private Transform authorizedPlayer;
    [SerializeField] private CharacterController playerBody;
    [SerializeField] private Vector3 interactionAreaCenter = new Vector3(0f, 1.5f, -3.5f);
    [SerializeField] private Vector3 interactionAreaSize = new Vector3(2.4f, 3f, 3.6f);
    [SerializeField] private Vector3 cabinAreaCenter = new Vector3(0f, 1.5f, 0.5f);
    [SerializeField] private Vector3 cabinAreaSize = new Vector3(5.5f, 3.1f, 5.5f);
    [SerializeField, Min(0.1f)] private float interactionRange = 1.8f;
    [SerializeField, Min(1f)] private float openingTimeout = 8f;
    [SerializeField] private ShipAccessState currentState;
    [SerializeField] private string failureReason = string.Empty;

    public ShipAccessState CurrentState => currentState;
    public bool IsPlayerInside => currentState == ShipAccessState.Boarded;
    public string StatusMessage => currentState switch
    {
        ShipAccessState.Locked => "Gemi kapağı kilitli. Önce atmosfer paneliyle Kael'i etkisiz hale getir.",
        ShipAccessState.Ready => "Kael etkisiz hale geldi.\nHava kilidinin sonundaki gemi kapağına yaklaş ve E ile aç.",
        ShipAccessState.Opening => "Gemi kapağı açılıyor…",
        ShipAccessState.Open => "Gemi kapağı açık.\nAçıklıktan geçerek kendi gemine gir.",
        ShipAccessState.Boarded => "Kendi gemine girdin.",
        ShipAccessState.Failed => failureReason,
        _ => string.Empty
    };

    private IAtmosphereControl panel;
    private IAtmosphereTarget target;
    private IOpenableDoor hatch;
    private float openingStarted;
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
        hatch = hatchController as IOpenableDoor;
        currentState = ShipAccessState.Locked;
        if (panel == null || target == null || hatch == null || authorizedPlayer == null
            || playerBody == null || playerBody.transform != authorizedPlayer)
        {
            Fail("Gemi geçişinin panel, Kael, kapak veya oyuncu bağlantısı eksik.");
            enabled = false;
        }
    }

    public bool IsInInteractionArea(Transform actor)
    {
        if (!isActiveAndEnabled || actor == null || actor != authorizedPlayer
            || !actor.gameObject.activeInHierarchy || hatchController == null)
            return false;
        if (!new Bounds(interactionAreaCenter, interactionAreaSize)
            .Contains(transform.InverseTransformPoint(actor.position)))
            return false;
        Vector3 offset = actor.position - hatchController.transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= interactionRange * interactionRange;
    }

    public bool CanOpenHatch(Transform actor) => IsInInteractionArea(actor) && PrerequisitesMet
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
        openingStarted = Time.time;
        failureReason = string.Empty;
        currentState = ShipAccessState.Opening;
        Debug.Log("Gemi kapağı açılıyor; Kael'in etkisizleşmesi ve atmosfer işlemi doğrulandı.", this);
        return true;
    }

    private void Update()
    {
        if (!openingRequested)
        {
            // Eksik bağlantı hatasını Ready durumuyla örtme.
            if (currentState != ShipAccessState.Failed)
                currentState = PrerequisitesMet ? ShipAccessState.Ready : ShipAccessState.Locked;
            return;
        }
        if (!PrerequisitesMet || !HatchAvailable)
        {
            Fail("Gemi geçişi kullanılamıyor; panel, Kael ve kapak bağlantılarını kontrol et.");
            return;
        }
        if (!hatch.IsOpen)
        {
            if (Time.time - openingStarted >= openingTimeout)
                Fail("Gemi kapağı ayrılan sürede tamamen açılamadı.");
            return;
        }

        bool inside = IsEntirePlayerInside();
        var next = inside ? ShipAccessState.Boarded : ShipAccessState.Open;
        if (currentState == next)
            return;
        currentState = next;
        Debug.Log(inside ? "Kendi gemine girdin; oyuncunun gövdesi kokpit alanının içinde."
            : "Gemi kapağı açık; kokpite geçebilirsin.", this);
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
