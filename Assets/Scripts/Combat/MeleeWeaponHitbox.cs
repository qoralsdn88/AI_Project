using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 무기 트리거. PlayerMeleeCombat의 피격 창에서만 <see cref="IDamageable"/>에게 데미지를 줍니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MeleeWeaponHitbox : MonoBehaviour
{
    [SerializeField] private Transform playerRoot;
    [SerializeField] private PlayerMeleeCombat combat;

    [Tooltip("Begin_Collision 직후 한 프레임 동안 트리거를 껐다 켜서, 이미 겹쳐 있던 적에게도 스윙이 새로 닿은 것처럼 판정하게 합니다.")]
    [SerializeField] private bool refreshTriggerColliderOnStrikeOpen = true;

    private Collider _selfCollider;
    private Coroutine _colliderRefreshRoutine;

    private void Awake()
    {
        _selfCollider = GetComponent<Collider>();
        if (_selfCollider != null && !_selfCollider.isTrigger) { Debug.LogWarning("[MeleeWeaponHitbox] Collider를 Trigger로 켜주세요."); }
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

    // 실제 트리거 접촉 시점에만 타격을 적용해 "닿기 전 히트"를 방지합니다.
    private void OnTriggerEnter(Collider other)
    {
        TryHitCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHitCollider(other);
    }

    private void TryHitCollider(Collider other)
    {
        if (combat == null) return;
        if (!combat.IsDamageWindowActive) return;
        if (ShouldIgnore(other)) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        combat.TryHit(damageable, hitPoint);
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
