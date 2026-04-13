using System.Collections;
using UnityEngine;

/// <summary>
/// 네크로맨서 보스 전용 AI:
/// - 평소 Idle
/// - 플레이어 탐지 시 스켈레톤 2마리 소환(20초 쿨)
/// - Spellcast 후 구체 투사체 발사(10초 쿨, 20 데미지)
/// - 이동 시 Walk, 정지 시 Idle 애니 전환
/// </summary>
public class NecromancerBossController : MonoBehaviour
{
    [Header("탐지/이동")]
    [SerializeField] private float detectRange = 16f;
    [SerializeField] private float chaseRange = 13f;
    [SerializeField] private float stopRange = 9f;
    [SerializeField] private float moveSpeed = 2.2f;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("스킬 쿨타임")]
    [SerializeField] private float summonCooldown = 20f;
    [SerializeField] private float projectileCooldown = 10f;

    [Header("스킬 설정")]
    [Tooltip("네크로맨서가 소환할 몬스터 프리팹입니다. 반드시 인스펙터에서 직접 할당하세요.")]
    public GameObject summonMonsterPrefab;
    [SerializeField] private int skeletonSpawnCount = 2;
    [SerializeField] private float summonSpread = 1.6f;
    [SerializeField] private float castWindupSeconds = 0.6f;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private int projectileDamage = 20;
    [SerializeField] private float projectileLifetime = 5f;

    [Header("애니메이션 상태 이름")]
    [SerializeField] private string idleStateName = "idle1";
    [SerializeField] private string walkStateName = "walk";
    [SerializeField] private string spellcastStateName = "spellcast1";
    [SerializeField] private float crossFadeSeconds = 0.1f;

    private Animator _animator;
    private Transform _player;
    private SimpleMonsterHealth _health;
    private float _nextSummonTime;
    private float _nextProjectileTime;
    private bool _isCasting;
    private bool _hasSeenPlayer;
    private int _lastPlayedStateHash;
    private int _resolvedIdleHash;
    private int _resolvedWalkHash;
    private int _resolvedSpellcastHash;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<SimpleMonsterHealth>();
    }

    private void Start()
    {
        FindPlayerIfMissing();
        ValidateSummonPrefab();
        ResolveAnimationStates();
        TryPlayState(_resolvedIdleHash, force: true);
    }

    private void Update()
    {
        if (_health != null && _health.CurrentHealth <= 0) return;
        if (_isCasting) return;

        FindPlayerIfMissing();
        if (_player == null)
        {
            TryPlayState(_resolvedIdleHash);
            return;
        }

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > detectRange)
        {
            TryPlayState(_resolvedIdleHash);
            return;
        }

        if (!_hasSeenPlayer)
        {
            _hasSeenPlayer = true;
            _nextSummonTime = Time.time;
            _nextProjectileTime = Time.time;
        }

        FacePlayer(toPlayer);
        HandleMovement(distance, toPlayer);
        TryCastSkills();
    }

    private void HandleMovement(float distance, Vector3 toPlayer)
    {
        if (distance > chaseRange)
        {
            MoveToward(toPlayer.normalized);
            TryPlayState(_resolvedWalkHash);
            return;
        }

        if (distance > stopRange)
        {
            MoveToward(toPlayer.normalized);
            TryPlayState(_resolvedWalkHash);
            return;
        }

        TryPlayState(_resolvedIdleHash);
    }

    private void MoveToward(Vector3 direction)
    {
        transform.position += direction * moveSpeed * Time.deltaTime;
    }

    private void FacePlayer(Vector3 toPlayer)
    {
        if (toPlayer.sqrMagnitude <= 0.0001f) return;
        Quaternion target = Quaternion.LookRotation(toPlayer.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, rotateSpeed * Time.deltaTime);
    }

    private void TryCastSkills()
    {
        if (_player == null) return;

        if (Time.time >= _nextSummonTime)
        {
            SummonSkeletonsTowardPlayer();
            _nextSummonTime = Time.time + Mathf.Max(1f, summonCooldown);
        }

        if (Time.time >= _nextProjectileTime)
        {
            StartCoroutine(CastAndShootRoutine());
            _nextProjectileTime = Time.time + Mathf.Max(1f, projectileCooldown);
        }
    }

    private void SummonSkeletonsTowardPlayer()
    {
        if (summonMonsterPrefab == null) return;
        if (_player == null) return;

        Vector3 forward = _player.position - transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = transform.forward;
            forward.y = 0f;
        }
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        for (int i = 0; i < skeletonSpawnCount; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float lane = (i / 2) * 0.6f;
            Vector3 spawnPos = transform.position + forward * (1.6f + lane) + right * side * summonSpread * 0.5f;
            GameObject spawned = Instantiate(summonMonsterPrefab, spawnPos, Quaternion.identity);

            Vector3 look = _player.position - spawned.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
            {
                spawned.transform.rotation = Quaternion.LookRotation(look.normalized);
            }

            MonsterDetectChaseSimple chase = spawned.GetComponent<MonsterDetectChaseSimple>();
            if (chase != null) chase.BeginImmediateChase(_player);
        }
    }

    private IEnumerator CastAndShootRoutine()
    {
        _isCasting = true;
        TryPlayState(_resolvedSpellcastHash, force: true);
        yield return new WaitForSeconds(Mathf.Max(0.05f, castWindupSeconds));

        if (_player != null)
        {
            Vector3 firePos = transform.position + Vector3.up * 1.3f + transform.forward * 0.9f;
            Vector3 dir = (_player.position + Vector3.up * 1.0f) - firePos;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f) dir = transform.forward;
            SpawnProjectile(firePos, dir.normalized);
        }

        TryPlayState(_resolvedIdleHash, force: true);
        _isCasting = false;
    }

    private void SpawnProjectile(Vector3 position, Vector3 direction)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "NecromancerProjectile";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * 0.35f;

        Collider col = sphere.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = sphere.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = true;

        NecromancerProjectile projectile = sphere.AddComponent<NecromancerProjectile>();
        projectile.Initialize(gameObject, direction, projectileSpeed, projectileDamage, projectileLifetime);
    }

    private void TryPlayState(int hash, bool force = false)
    {
        if (_animator == null) return;
        if (hash == 0) return;
        if (!force && _lastPlayedStateHash == hash) return;

        _animator.CrossFade(hash, Mathf.Max(0.02f, crossFadeSeconds), 0, 0f);
        _lastPlayedStateHash = hash;
    }

    private void ResolveAnimationStates()
    {
        _resolvedIdleHash = ResolveStateHash(idleStateName, "idle1", "idle2");
        _resolvedWalkHash = ResolveStateHash(walkStateName, "walk", "walk 0");
        _resolvedSpellcastHash = ResolveStateHash(spellcastStateName, "spellcast1", "atack1", "atack2");
    }

    private int ResolveStateHash(string primaryName, params string[] fallbackNames)
    {
        if (_animator == null) return 0;

        if (TryGetStateHash(primaryName, out int primaryHash))
        {
            return primaryHash;
        }

        for (int i = 0; i < fallbackNames.Length; i++)
        {
            if (TryGetStateHash(fallbackNames[i], out int fallbackHash))
            {
                return fallbackHash;
            }
        }

        return 0;
    }

    private bool TryGetStateHash(string stateName, out int hash)
    {
        hash = 0;
        if (string.IsNullOrEmpty(stateName)) return false;

        int candidate = Animator.StringToHash(stateName);
        if (!_animator.HasState(0, candidate)) return false;

        hash = candidate;
        return true;
    }

    private void FindPlayerIfMissing()
    {
        if (_player != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void ValidateSummonPrefab()
    {
        if (summonMonsterPrefab != null) return;
        Debug.LogWarning("[NecromancerBossController] summonMonsterPrefab이 비어 있어 소환 스킬이 비활성화됩니다. 네크로맨서 컴포넌트에 소환 프리팹을 직접 연결하세요.");
    }

    public void InjectSummonMonsterPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        summonMonsterPrefab = prefab;
    }

    // 이전 메서드명을 쓰는 코드와 호환되도록 유지합니다.
    public void InjectSkeletonPrefab(GameObject prefab)
    {
        InjectSummonMonsterPrefab(prefab);
    }
}

/// <summary>
/// 씬에 네크로맨서 프리팹을 두면 런타임에 보스 로직을 자동으로 붙입니다.
/// 사용자가 인스펙터에서 수동 연결하지 않아도 기본 동작이 되도록 하는 보조 초기화기입니다.
/// </summary>
public static class NecromancerBossRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachBossControllerToNecromancers()
    {
        Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || animator.runtimeAnimatorController == null) continue;

            string controllerName = animator.runtimeAnimatorController.name;
            if (string.IsNullOrEmpty(controllerName)) continue;
            if (!controllerName.Contains("Necromanser") && !controllerName.Contains("Necromancer")) continue;

            GameObject root = animator.transform.root.gameObject;
            if (root.GetComponent<NecromancerBossController>() == null)
            {
                root.AddComponent<NecromancerBossController>();
            }

            NecromancerBossController controller = root.GetComponent<NecromancerBossController>();
            if (controller != null && controller.summonMonsterPrefab == null)
                Debug.LogWarning($"[NecromancerBossRuntimeBootstrap] {root.name}의 NecromancerBossController에 summonMonsterPrefab이 비어 있습니다.");

            if (root.GetComponent<SimpleMonsterHealth>() == null)
            {
                root.AddComponent<SimpleMonsterHealth>();
            }

            EnsureDamageCollider(root);
        }
    }

    private static void EnsureDamageCollider(GameObject root)
    {
        if (root == null) return;
        if (root.GetComponent<Collider>() != null) return;

        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.isTrigger = false;
        capsule.radius = 0.45f;
        capsule.height = 1.8f;
        capsule.center = new Vector3(0f, 0.9f, 0f);
    }
}
