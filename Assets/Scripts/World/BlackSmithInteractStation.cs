using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 대장장이 오브젝트에 붙입니다. 트리거 범위 판정과 F(Interact) 입력으로 강화 창을 엽니다.
/// UI 렌더링은 Canvas 쪽 HUD 컴포넌트에서 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public class BlackSmithInteractStation : MonoBehaviour
{
    private static readonly List<BlackSmithInteractStation> RegisteredStations = new List<BlackSmithInteractStation>();
    public static IReadOnlyList<BlackSmithInteractStation> AllStations => RegisteredStations;

    [Header("입력")]
    [SerializeField] private InputActionAsset inputActionAsset;
    [SerializeField, Min(0.5f)] private float interactTriggerRadius = 2.4f;
    [SerializeField] private Vector3 triggerCenter = new Vector3(0f, 1.1f, 0f);

    [Header("안내 문구 (Canvas HUD에서 사용)")]
    [SerializeField] private string promptMessage = "F:대장장이에게 말걸기";

    private SphereCollider _sphere;
    private readonly HashSet<Transform> _playersInside = new HashSet<Transform>();
    private InputAction _interactAction;
    private bool _interactWired;
    private int _wireAttempts;

    private void Reset()
    {
        EnsureColliderExists();
    }

    private void OnEnable()
    {
        if (!RegisteredStations.Contains(this)) RegisteredStations.Add(this);
    }

    private void OnDisable()
    {
        RegisteredStations.Remove(this);
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

    /// <summary>HUD 상호작용 안내 표시 여부. 메뉴가 열렸거나 범위 밖이면 false.</summary>
    public bool ShouldShowInteractPrompt()
    {
        if (BlacksmithGameplayLock.IsMenuOpen) return false;
        return _playersInside.Count > 0;
    }

    public string GetInteractPromptText() => promptMessage;

    private void EnsureColliderExists()
    {
        _sphere = GetComponent<SphereCollider>();
        if (_sphere == null)
        {
            _sphere = gameObject.AddComponent<SphereCollider>();
        }

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
        if (melee != null)
        {
            inputActionAsset = melee.InputActionAsset;
        }
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

        BlacksmithUpgradeMenuUi ui = BlacksmithUpgradeMenuUi.EnsureInstance();
        bool inRange = _playersInside.Count > 0;

        if (ui.IsOpen)
        {
            ui.CloseFullUi();
            return;
        }

        if (BlacksmithGameplayLock.IsMenuOpen) return;
        if (!inRange) return;
        ui.OpenPickTypeUi();
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
            BlacksmithUpgradeMenuUi ui = BlacksmithUpgradeMenuUi.EnsureInstance();
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
