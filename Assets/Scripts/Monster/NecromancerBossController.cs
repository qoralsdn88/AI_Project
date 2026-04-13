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
    [SerializeField] private GameObject skeletonPrefab;
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

    private static readonly int IdleHash = Animator.StringToHash("idle1");
    private static readonly int WalkHash = Animator.StringToHash("walk");
    private static readonly int SpellcastHash = Animator.StringToHash("spellcast1");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _health = GetComponent<SimpleMonsterHealth>();
    }

    private void Start()
    {
        FindPlayerIfMissing();
        EnsureSkeletonPrefab();
        TryPlayState(IdleHash, idleStateName, force: true);
    }

    private void Update()
    {
        if (_health != null && _health.CurrentHealth <= 0) return;
        if (_isCasting) return;

        FindPlayerIfMissing();
        if (_player == null)
        {
            TryPlayState(IdleHash, idleStateName);
            return;
        }

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance > detectRange)
        {
            TryPlayState(IdleHash, idleStateName);
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
            TryPlayState(WalkHash, walkStateName);
            return;
        }

        if (distance > stopRange)
        {
            MoveToward(toPlayer.normalized);
            TryPlayState(WalkHash, walkStateName);
            return;
        }

        TryPlayState(IdleHash, idleStateName);
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
        if (skeletonPrefab == null) return;
        if (_player == null) return;

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        for (int i = 0; i < skeletonSpawnCount; i++)
        {
            float side = (i % 2 == 0) ? -1f : 1f;
            float lane = (i / 2) * 0.6f;
            Vector3 spawnPos = transform.position + forward * (1.6f + lane) + right * side * summonSpread * 0.5f;
            GameObject spawned = Instantiate(skeletonPrefab, spawnPos, Quaternion.identity);

            Vector3 look = _player.position - spawned.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.0001f)
            {
                spawned.transform.rotation = Quaternion.LookRotation(look.normalized);
            }

            MonsterDetectChaseSimple chase = spawned.GetComponent<MonsterDetectChaseSimple>();
            if (chase != null) chase.player = _player;
        }
    }

    private IEnumerator CastAndShootRoutine()
    {
        _isCasting = true;
        TryPlayState(SpellcastHash, spellcastStateName, force: true);
        yield return new WaitForSeconds(Mathf.Max(0.05f, castWindupSeconds));

        if (_player != null)
        {
            Vector3 firePos = transform.position + Vector3.up * 1.3f + transform.forward * 0.9f;
            Vector3 dir = (_player.position + Vector3.up * 1.0f) - firePos;
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f) dir = transform.forward;
            SpawnProjectile(firePos, dir.normalized);
        }

        TryPlayState(IdleHash, idleStateName, force: true);
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

    private void TryPlayState(int hash, string stateName, bool force = false)
    {
        if (_animator == null) return;
        if (hash == 0) return;
        if (!force && _lastPlayedStateHash == hash) return;
        if (!_animator.HasState(0, hash)) return;

        _animator.CrossFade(string.IsNullOrEmpty(stateName) ? hash : Animator.StringToHash(stateName), Mathf.Max(0.02f, crossFadeSeconds), 0, 0f);
        _lastPlayedStateHash = hash;
    }

    private void FindPlayerIfMissing()
    {
        if (_player != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    private void EnsureSkeletonPrefab()
    {
        if (skeletonPrefab != null) return;
        skeletonPrefab = Resources.Load<GameObject>("Monster/Skeleton");
    }

    public void InjectSkeletonPrefab(GameObject prefab)
    {
        if (prefab == null) return;
        skeletonPrefab = prefab;
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
        GameObject skeletonPrefab = LoadSkeletonPrefabForRuntime();
        Animator[] animators = Object.FindObjectsByType<Animator>(FindObjectsSortMode.None);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || animator.runtimeAnimatorController == null) continue;

            string controllerName = animator.runtimeAnimatorController.name;
            if (string.IsNullOrEmpty(controllerName)) continue;
            if (!controllerName.Contains("Necromanser")) continue;

            GameObject root = animator.transform.root.gameObject;
            if (root.GetComponent<NecromancerBossController>() == null)
            {
                root.AddComponent<NecromancerBossController>();
            }

            NecromancerBossController controller = root.GetComponent<NecromancerBossController>();
            if (controller != null && skeletonPrefab != null)
            {
                controller.InjectSkeletonPrefab(skeletonPrefab);
            }

            if (root.GetComponent<SimpleMonsterHealth>() == null)
            {
                root.AddComponent<SimpleMonsterHealth>();
            }
        }
    }

    private static GameObject LoadSkeletonPrefabForRuntime()
    {
        GameObject byResources = Resources.Load<GameObject>("Monster/Skeleton");
        if (byResources != null) return byResources;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Skeleton.prefab");
#else
        return null;
#endif
    }
}
