using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 보물상자 상호작용 스테이션. 플레이어 범위 + F(Interact) 입력으로 상자 전리품 UI를 엽니다.
/// UI 표시 자체는 Canvas 쪽 <see cref="TreasureChestLootMenuUi"/>가 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public class TreasureChestInteractStation : MonoBehaviour
{
    private static readonly List<TreasureChestInteractStation> RegisteredStations = new List<TreasureChestInteractStation>();
    public static IReadOnlyList<TreasureChestInteractStation> AllStations => RegisteredStations;

    [Header("입력")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField, Min(0.5f)] private float interactTriggerRadius = 2.2f;
    [SerializeField] private Vector3 triggerCenter = new Vector3(0f, 0.8f, 0f);

    [Header("안내 문구 (Canvas HUD에서 사용)")]
    [SerializeField] private string promptMessage = "F:상자 열기";

    private SphereCollider _sphere;
    private readonly HashSet<Transform> _playersInside = new HashSet<Transform>();
    private InputAction _interactAction;
    private bool _interactWired;
    private int _wireAttempts;
    private bool _hasBeenOpened;

    private void OnEnable()
    {
        if (!RegisteredStations.Contains(this)) RegisteredStations.Add(this);
    }

    private void OnDisable()
    {
        RegisteredStations.Remove(this);
    }

    private void Reset()
    {
        EnsureColliderExists();
    }

    private void Awake()
    {
        EnsureColliderExists();
        TryWireInteractAction();
    }

    private void LateUpdate()
    {
        if (!_interactWired && _wireAttempts < 600)
        {
            _wireAttempts++;
            TryWireInteractAction();
        }
    }

    private void OnDestroy()
    {
        UnwireInteractAction();
    }

    public bool ShouldShowInteractPrompt()
    {
        if (_hasBeenOpened) return false;
        if (BlacksmithGameplayLock.IsMenuOpen) return false;
        return _playersInside.Count > 0;
    }

    public string GetInteractPromptText() => promptMessage;
    public void MarkAsOpened() => _hasBeenOpened = true;

    private void EnsureColliderExists()
    {
        _sphere = GetComponent<SphereCollider>();
        if (_sphere == null) _sphere = gameObject.AddComponent<SphereCollider>();
        _sphere.isTrigger = true;
        _sphere.radius = interactTriggerRadius;
        _sphere.center = triggerCenter;
    }

    private void ResolveInputAsset()
    {
        if (inputActionAsset != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        PlayerMeleeCombat melee = PlayerMeleeCombat.Resolve(p.transform);
        if (melee != null) inputActionAsset = melee.InputActionAsset;
    }

    private void TryWireInteractAction()
    {
        if (_interactWired) return;
        ResolveInputAsset();
        if (inputActionAsset == null) return;

        InputActionMap map = inputActionAsset.FindActionMap("Player");
        if (map == null) return;
        _interactAction = map.FindAction("Interact");
        if (_interactAction == null) return;

        _interactAction.started += OnInteractStarted;
        _interactAction.Enable();
        _interactWired = true;
    }

    private void UnwireInteractAction()
    {
        if (!_interactWired || _interactAction == null) return;
        _interactAction.started -= OnInteractStarted;
        _interactAction.Disable();
        _interactWired = false;
        _interactAction = null;
    }

    private void OnInteractStarted(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Started) return;

        TreasureChestLootMenuUi ui = TreasureChestLootMenuUi.EnsureInstance();
        bool inRange = _playersInside.Count > 0;

        if (ui.IsOpen)
        {
            ui.CloseFullUi();
            return;
        }

        if (_hasBeenOpened) return;
        if (BlacksmithGameplayLock.IsMenuOpen) return;
        if (!inRange) return;
        ui.OpenForChest(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayerRoot(other, out Transform playerRoot)) return;
        _playersInside.Add(playerRoot);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetPlayerRoot(other, out Transform playerRoot)) return;
        _playersInside.Remove(playerRoot);
        if (_playersInside.Count == 0)
        {
            TreasureChestLootMenuUi ui = TreasureChestLootMenuUi.EnsureInstance();
            if (ui.IsOpen) ui.CloseFullUi();
        }
    }

    private static bool TryGetPlayerRoot(Collider other, out Transform playerRoot)
    {
        playerRoot = null;
        if (other == null) return false;
        if (other.CompareTag("Player"))
        {
            playerRoot = other.transform;
            return true;
        }

        SimplePlayerHealth health = other.GetComponentInParent<SimplePlayerHealth>();
        if (health == null) return false;
        playerRoot = health.transform;
        return true;
    }
}
