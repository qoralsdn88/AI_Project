// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;
// 코루틴을 쓰기 위해 꼭 필요합니다.
using System.Collections;

// 칼 충돌 범위에서 맞은 대상에게 데미지를 주는 트리거 스크립트입니다.
// 핵심 요약: 플레이어와 장비는 무시하고, PlayerMeleeCombat이 애니메이션 이벤트로 연 피격 창에서만 IDamageable에게 맞춥니다.
[RequireComponent(typeof(Collider))]
public class MeleeWeaponHitbox : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;
    [SerializeField] private PlayerMeleeCombat combat;

    [Tooltip("Begin_Collision 직후 한 프레임 동안 트리거를 껐다 켜서, 이미 겹쳐 있던 적에게도 스윙이 새로 닿은 것처럼 판정하게 합니다.")]
    [SerializeField] private bool refreshTriggerColliderOnStrikeOpen = true;

    [Tooltip("OverlapBox 검사 시 사용하는 버퍼 크기입니다.")]
    [SerializeField] private int overlapBufferSize = 24;

    private Collider _selfCollider;
    private Collider[] _overlapBuffer;
    private Coroutine _colliderRefreshRoutine;

    private void Awake()
    {
        _selfCollider = GetComponent<Collider>();
        if (_selfCollider != null && !_selfCollider.isTrigger) { Debug.LogWarning("[MeleeWeaponHitbox] Collider를 Trigger로 켜주세요."); }
        int cap = Mathf.Clamp(overlapBufferSize, 4, 64);
        _overlapBuffer = new Collider[cap];
    }

    private void Start()
    {
        if (combat == null)
        {
            Debug.LogError($"[MeleeWeaponHitbox] {name}: PlayerMeleeCombat 연결이 없습니다. 검을 붙이는 스크립트의 Initialize(playerRoot, meleeCombat)를 확인하세요.");
        }
        if (GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning(
                $"[MeleeWeaponHitbox] {name}: Rigidbody가 없습니다. 애니메이션으로만 움직이는 트리거는 물리 이벤트가 안 들어올 수 있습니다. " +
                "PlayerEquipmentHolder가 SwordDamageTrigger에 키네마틱 Rigidbody를 붙이는지 확인하거나, 이 오브젝트에 Rigidbody(Is Kinematic)를 추가하세요.");
        }
    }

    private void OnDestroy()
    {
        if (combat != null) { combat.UnregisterWeaponHitbox(this); }
    }

    public void Initialize(Transform root, PlayerMeleeCombat meleeCombat)
    {
        if (combat != null) { combat.UnregisterWeaponHitbox(this); }
        playerRoot = root;
        combat = meleeCombat;
        if (combat != null) { combat.RegisterWeaponHitbox(this); }
    }

    /// <summary>PlayerMeleeCombat의 Begin_Collision에서 호출합니다.</summary>
    public void OnAnimatorStrikeWindowOpened()
    {
        if (!refreshTriggerColliderOnStrikeOpen || _selfCollider == null || !isActiveAndEnabled) { return; }
        if (_colliderRefreshRoutine != null) { StopCoroutine(_colliderRefreshRoutine); }
        _colliderRefreshRoutine = StartCoroutine(CoRefreshTriggerCollider());
    }

    /// <summary>PlayerMeleeCombat의 End_Collision에서 호출됩니다.</summary>
    public void OnAnimatorStrikeWindowClosed() { }

    private IEnumerator CoRefreshTriggerCollider()
    {
        _selfCollider.enabled = false;
        yield return new WaitForFixedUpdate();
        if (_selfCollider != null) { _selfCollider.enabled = true; }
        _colliderRefreshRoutine = null;
    }

    private void FixedUpdate()
    {
        if (combat == null || _selfCollider == null || !_selfCollider.enabled) { return; }
        if (!combat.IsDamageWindowActive) { return; }

        int count = Physics.OverlapBoxNonAlloc(
            _selfCollider.bounds.center,
            _selfCollider.bounds.extents,
            _overlapBuffer,
            _selfCollider.transform.rotation,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider c = _overlapBuffer[i];
            if (c == null || c == _selfCollider) { continue; }
            TryHitCollider(c);
        }
    }

    private void TryHitCollider(Collider other)
    {
        if (combat == null) return;
        if (!combat.IsDamageWindowActive) return;
        if (ShouldIgnore(other)) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;
        combat.TryHit(damageable);
    }

    private bool ShouldIgnore(Collider other)
    {
        if (other.CompareTag("Player")) return true;
        if (other.CompareTag("Equipment")) return true;

        if (playerRoot != null)
        {
            Transform t = other.transform;
            while (t != null)
            {
                if (t == playerRoot) return true;
                if (t.CompareTag("Equipment")) return true;
                t = t.parent;
            }
        }

        return false;
    }
}
