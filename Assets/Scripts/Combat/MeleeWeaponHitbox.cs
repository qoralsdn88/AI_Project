// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;

// 칼 충돌 범위에서 맞은 대상에게 데미지를 주는 트리거 스크립트입니다.
// 핵심 요약: 플레이어와 장비는 무시하고, 공격 창이 열려 있을 때만 IDamageable에게 맞춥니다.
[RequireComponent(typeof(Collider))]
public class MeleeWeaponHitbox : MonoBehaviour
{
    // 플레이어 최상위 오브젝트입니다.
    // 핵심 요약: 이 아래에 있는 콜라이더는 모두 공격 대상에서 뺍니다.
    [SerializeField] private Transform playerRoot;

    // 공격 타이밍과 데미지 숫자를 맞추는 전투 스크립트입니다.
    // 핵심 요약: combat이 열어준 순간에만 데미지가 나갑니다.
    [SerializeField] private PlayerMeleeCombat combat;

    [Tooltip("트리거 메시지가 환경에 따라 누락될 때 FixedUpdate에서 겹침을 직접 검사합니다.")]
    [SerializeField] private bool usePhysicsOverlapFallback = true;

    [Tooltip("OverlapBox 검사 시 사용하는 버퍼 크기입니다.")]
    [SerializeField] private int overlapBufferSize = 24;

    private Collider _selfCollider;
    private Collider[] _overlapBuffer;

    // 컴포넌트가 붙은 순간 자동으로 실행되는 함수입니다.
    // 핵심 요약: 콜라이더가 트리거인지 한 번 더 확인합니다.
    private void Awake()
    {
        _selfCollider = GetComponent<Collider>();
        if (_selfCollider != null && !_selfCollider.isTrigger) { Debug.LogWarning("[MeleeWeaponHitbox] Collider를 Trigger로 켜주세요."); }
        int cap = Mathf.Clamp(overlapBufferSize, 4, 64);
        _overlapBuffer = new Collider[cap];
    }

    private void Start()
    {
        // Initialize()는 장착 스크립트가 AddComponent 직후에 호출하므로 Start 시점에 참조가 맞는지 확인합니다.
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

    // 외부에서 플레이어 래트와 전투 스크립트를 연결할 때 씁니다.
    // 핵심 요약: 장착 스크립트가 Instantiate 뒤에 호출해 줍니다.
    public void Initialize(Transform root, PlayerMeleeCombat meleeCombat)
    {
        // 플레이어 루트를 저장합니다.
        playerRoot = root;
        // 전투 스크립트를 저장합니다.
        combat = meleeCombat;
    }

    // 다른 콜라이더가 이 트리거 안으로 들어왔을 때 호출됩니다.
    // 핵심 요약: 무시 대상이면 빠져나가고, 아니면 IDamageable을 찾아 맞춥니다.
    private void OnTriggerEnter(Collider other) => TryHitCollider(other);

    // 이미 겹쳐 있던 뒤에 데미지 창만 열리는 경우 OnTriggerEnter가 다시 안 들어올 수 있어 보조로 둡니다.
    private void OnTriggerStay(Collider other) => TryHitCollider(other);

    private void FixedUpdate()
    {
        if (!usePhysicsOverlapFallback || combat == null || _selfCollider == null || !_selfCollider.enabled) { return; }
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
        // 전투 스크립트가 없으면 아무 것도 하지 않습니다.
        if (combat == null) return;
        // 지금은 데미지 창이 아니면 아무 것도 하지 않습니다.
        if (!combat.IsDamageWindowActive) return;
        // 플레이어 쪽은 맞지 않게 무시합니다.
        if (ShouldIgnore(other)) return;

        // 맞은 오브젝트나 부모 중에서 IDamageable을 찾습니다.
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        // 없으면 맞추지 않습니다.
        if (damageable == null) return;
        // 전투 스크립트에 맡겨 중복 타격을 막습니다.
        combat.TryHit(damageable);
    }

    // 이 충돌을 무시해야 하는지 판단하는 함수입니다.
    // 핵심 요약: 플레이어 루트 아래, 장비 태그면 true입니다.
    private bool ShouldIgnore(Collider other)
    {
        // 플레이어 태그면 무시합니다.
        if (other.CompareTag("Player")) return true;
        // 장비 태그면 무시합니다.
        if (other.CompareTag("Equipment")) return true;

        // 플레이어 루트가 정해져 있으면 그 아래는 전부 무시합니다.
        if (playerRoot != null)
        {
            // 부모를 따라 올라가며 플레이어 루트인지 확인합니다.
            Transform t = other.transform;
            // 끝까지 올라가며 확인합니다.
            while (t != null)
            {
                // 같은 오브젝트면 플레이어 몸의 일부로 봅니다.
                if (t == playerRoot) return true;
                // 장비 태그가 붙은 부모도 무시합니다.
                if (t.CompareTag("Equipment")) return true;
                // 한 단계 위 부모로 올라갑니다.
                t = t.parent;
            }
        }

        // 여기까지 왔다면 무시하지 않습니다.
        return false;
    }
}
