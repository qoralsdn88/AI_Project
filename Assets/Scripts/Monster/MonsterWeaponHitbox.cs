using UnityEngine;

/// <summary>
/// 이 오브젝트는 "실제 무기 메시가 붙은 본"의 자식으로 두는 것이 좋습니다(OrkAssasin 프리팹: Weapons_3, Weapons_4).
/// 손/팔 본만 따라가고 칼 메시는 Weapons 쪽에 붙어 있으면, 히트박스도 그 본 밑에 두어야 공격할 때 함께 움직입니다.
/// MonsterAttackSimple이 공격 시 짧게 피격 창을 열고, 그동안 이 트리거 박스가 플레이어와 겹치면 데미지가 적용됩니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class MonsterWeaponHitbox : MonoBehaviour
{
    [SerializeField] private MonsterAttackSimple attackSource;
    [Tooltip("이 몬스터 루트 이하 콜라이더는 무시(자해 방지)")]
    [SerializeField] private Transform monsterRoot;

    [Header("히트 판정 범위")]
    [Tooltip("무기 메시/자식 렌더러 바운드를 기준으로 박스 크기에 곱합니다. 3~4 권장.")]
    [SerializeField] private float hitSizeMultiplier = 3.5f;
    [Tooltip("바운딩 박스를 못 찾으면 이 로컬 크기(반경 아님, BoxCollider size)를 씁니다.")]
    [SerializeField] private Vector3 fallbackBoxSize = new Vector3(0.6f, 0.15f, 2.2f);

    [Header("검사")]
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private int overlapBufferSize = 16;
    [Tooltip("OverlapBox가 뼈 콜라이더만 잡을 때 피하기 어렵습니다. CharacterController.bounds에 더하는 여유(월드 단위).")]
    [SerializeField] private float chaseTargetBoundsPadding = 0.2f;

    private BoxCollider _box;
    private Collider[] _buffer;
    private int _lastSwingConsumed = -1;

    private void Awake()
    {
        _box = GetComponent<BoxCollider>();
        _box.isTrigger = true;

        if (attackSource == null) { attackSource = GetComponentInParent<MonsterAttackSimple>(true); }
        if (monsterRoot == null && attackSource != null) { monsterRoot = attackSource.transform; }

        ApplyScaledHitSize();

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        int cap = Mathf.Clamp(overlapBufferSize, 4, 64);
        _buffer = new Collider[cap];
    }

    private void ApplyScaledHitSize()
    {
        Bounds combined = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;
        Transform probeRoot = transform.parent != null ? transform.parent : transform;
        var renderers = probeRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] is MeshRenderer || renderers[i] is SkinnedMeshRenderer)
            {
                if (!hasBounds) { combined = renderers[i].bounds; hasBounds = true; }
                else { combined.Encapsulate(renderers[i].bounds); }
            }
        }

        if (hasBounds)
        {
            Vector3 localCenter = transform.InverseTransformPoint(combined.center);
            Vector3 lossy = transform.lossyScale;
            float sx = Mathf.Abs(lossy.x) > 1e-4f ? Mathf.Abs(lossy.x) : 1f;
            float sy = Mathf.Abs(lossy.y) > 1e-4f ? Mathf.Abs(lossy.y) : 1f;
            float sz = Mathf.Abs(lossy.z) > 1e-4f ? Mathf.Abs(lossy.z) : 1f;
            Vector3 worldSize = combined.size;
            Vector3 localSize = new Vector3(worldSize.x / sx, worldSize.y / sy, worldSize.z / sz) * hitSizeMultiplier;
            _box.center = localCenter;
            _box.size = Vector3.Max(localSize, Vector3.one * 0.05f);
        }
        else
        {
            _box.center = Vector3.zero;
            _box.size = fallbackBoxSize * hitSizeMultiplier;
        }
    }

    private void FixedUpdate()
    {
        if (attackSource == null || !_box.enabled) return;
        if (!attackSource.IsWeaponDamageWindowActive) return;

        int swingId = attackSource.WeaponSwingId;
        if (swingId == _lastSwingConsumed) return;

        Physics.SyncTransforms();

        if (TryHitFromPhysicsOverlap(swingId)) return;
        TryHitChaseTargetCharacterController(swingId);
    }

    private bool TryHitFromPhysicsOverlap(int swingId)
    {
        int count = Physics.OverlapBoxNonAlloc(
            _box.bounds.center,
            _box.bounds.extents,
            _buffer,
            _box.transform.rotation,
            hitLayers,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider c = _buffer[i];
            if (c == null || c == _box) continue;
            if (monsterRoot != null && c.transform.IsChildOf(monsterRoot)) continue;

            SimplePlayerHealth hp = SimplePlayerHealth.Resolve(c.transform);
            if (hp == null || hp.IsDead) continue;

            hp.TakeDamage(attackSource.AttackDamage, attackSource.gameObject);
            _lastSwingConsumed = swingId;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 플레이어는 이동용 CharacterController만 두는 경우가 많은데,
    /// CC는 Physics.Overlap 계열에 잡히지 않으므로 bounds 교차로 한 번 더 판정합니다.
    /// </summary>
    private void TryHitChaseTargetCharacterController(int swingId)
    {
        MonsterDetectChaseSimple chase = attackSource.detectChase;
        if (chase == null || chase.player == null) return;

        SimplePlayerHealth hp = SimplePlayerHealth.Resolve(chase.player);
        if (hp == null || hp.IsDead) return;

        CharacterController cc = chase.player.GetComponentInParent<CharacterController>();
        if (cc == null || !cc.enabled) return;

        Bounds playerBounds = cc.bounds;
        if (chaseTargetBoundsPadding > 0f) { playerBounds.Expand(chaseTargetBoundsPadding); }

        if (!_box.bounds.Intersects(playerBounds)) return;

        hp.TakeDamage(attackSource.AttackDamage, attackSource.gameObject);
        _lastSwingConsumed = swingId;
    }
}
