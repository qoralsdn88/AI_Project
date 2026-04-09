using UnityEngine;

/// <summary>
/// 이 오브젝트는 "실제 무기 메시가 붙은 본"의 자식으로 두는 것이 좋습니다(OrkAssasin 프리팹: Weapons_3, Weapons_4).
/// 손/팔 본만 따라가고 칼 메시는 Weapons 쪽에 붙어 있으면, 히트박스도 그 본 밑에 두어야 공격할 때 함께 움직입니다.
/// MonsterAttackSimple이 공격 시 짧게 피격 창을 열고, 그동안 이 트리거 박스가 플레이어와 겹치면 데미지가 적용됩니다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class MonsterWeaponHitbox : MonoBehaviour
{
    private const string LogTag = "[MonsterWeaponHitbox]";

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
    [Tooltip("플레이어가 CharacterController만 있어 트리거를 못 받는 경우에만 켜세요. 켜면 접촉 전 선판정이 날 수 있습니다.")]
    [SerializeField] private bool useCharacterControllerFallback = false;
    [Tooltip("CharacterController 폴백 판정 시 플레이어 바운드에 더하는 여유(월드 단위). 선판정 방지를 위해 기본 0.")]
    [SerializeField] private float chaseTargetBoundsPadding = 0f;
    [Header("디버그")]
    [SerializeField] private bool verboseHitDebugLog = true;
    [Header("피격 히트 스탑")]
    [SerializeField, Min(0f)] private float playerHitStopDuration = 0.05f;
    [SerializeField, Range(0f, 1f)] private float playerHitStopTimeScale = 0f;

    private BoxCollider _box;
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
        if (!useCharacterControllerFallback) return;

        int swingId = attackSource.WeaponSwingId;
        if (swingId == _lastSwingConsumed) return;

        // 우선순위는 실제 트리거 접촉입니다. (OnTriggerEnter/Stay)
        // 플레이어가 CharacterController만 쓰는 경우를 위해서만 폴백을 둡니다.
        TryHitChaseTargetCharacterController(swingId);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitByTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryHitByTrigger(other);
    }

    private void TryHitByTrigger(Collider other)
    {
        if (attackSource == null || !_box.enabled) return;
        if (!attackSource.IsWeaponDamageWindowActive) return;
        if (other == null || other == _box) return;
        if (((1 << other.gameObject.layer) & hitLayers.value) == 0) return;
        if (monsterRoot != null && other.transform.IsChildOf(monsterRoot)) return;

        int swingId = attackSource.WeaponSwingId;
        if (swingId == _lastSwingConsumed) return;

        SimplePlayerHealth hp = SimplePlayerHealth.Resolve(other.transform);
        if (hp == null || hp.IsDead) return;
        TryApplyPlayerHitDamage(hp, swingId);
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

        TryApplyPlayerHitDamage(hp, swingId);
    }

    /// <summary>
    /// 방패 정면 방어에 성공하면 피해 없이 이번 스윙만 소모합니다.
    /// </summary>
    private bool TryApplyPlayerHitDamage(SimplePlayerHealth hp, int swingId)
    {
        GameObject attacker = attackSource != null ? attackSource.gameObject : null;
        if (PlayerShieldBlock.TryBlockHit(hp.transform, attacker))
        {
            if (verboseHitDebugLog)
            {
                Debug.Log($"{LogTag} 가드됨 | player={hp.name} | attacker={(attacker != null ? attacker.name : "null")} | swing={swingId}");
            }
            _lastSwingConsumed = swingId;
            return true;
        }

        if (verboseHitDebugLog)
        {
            Debug.Log($"{LogTag} 피격 적용 | player={hp.name} | attacker={(attacker != null ? attacker.name : "null")} | dmg={attackSource.AttackDamage} | swing={swingId}");
        }
        float duration = playerHitStopDuration > 0f ? playerHitStopDuration : 0.05f;
        HitStopController.Request(duration, playerHitStopTimeScale);
        hp.TakeDamage(attackSource.AttackDamage, attacker);
        _lastSwingConsumed = swingId;
        return true;
    }
}
