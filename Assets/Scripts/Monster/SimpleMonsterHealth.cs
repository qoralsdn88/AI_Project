// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// 몬스터 체력을 관리하고 플레이어 공격을 받아들이는 간단한 스크립트입니다.
// 핵심 요약: IDamageable을 구현해 칼 충돌 스크립트와 연결됩니다.
public class SimpleMonsterHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 30;

    [Header("네크로맨서 자동 기본값")]
    [Tooltip("Necromanser/Necromancer Animator를 감지하면 아래 기본값(HP 10, getgit, death)을 자동 적용합니다.")]
    [SerializeField] private bool autoApplyNecromancerDefaults = true;
    [SerializeField] private int necromancerMaxHealth = 10;
    [SerializeField] private string necromancerHitStateName = "getgit";
    [SerializeField] private string necromancerDeathStateName = "Dead";

    [Header("피격 표현")]
    [SerializeField] private Animator animator;
    [Tooltip("Monster_Base 등 Animator에 추가한 피격용 Trigger 이름.")]
    [SerializeField] private string getHitTriggerParameter = "GetHit";
    [Tooltip("피격 상태 이름. 트리거가 없거나 전환이 막히면 이 상태로 직접 크로스페이드합니다.")]
    [SerializeField] private string getHitStateName = "GetHit";
    [SerializeField] private MonsterAttackSimple attackBehaviour;
    [Tooltip("피격(또는 사망) 애니가 재생된 뒤, 이 시간(실시간 초) 후에 히트 스탑이 걸립니다.")]
    [SerializeField, Min(0f)] private float hitStopDelayAfterHitReactSeconds = 0.1f;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.05f;
    [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0f;

    [Header("사망")]
    [Tooltip("Animator의 Dead Bool 파라미터 이름(Monster_Base 기본값: Dead).")]
    [FormerlySerializedAs("deathTriggerParameter")]
    [SerializeField] private string deathBoolParameter = "Dead";
    [Tooltip("Animator 상태 이름. FBX 클립 이름이 Dead이면 보통 상태 이름도 Dead입니다.")]
    [SerializeField] private string deathStateName = "Dead";
    [Tooltip("죽는 애니가 끝난 뒤 씬에서 제거하기까지 대기(초).")]
    [SerializeField] private float removeAfterDeathAnimationSeconds = 3f;
    [Tooltip("사망 애니가 끝났는지 판별할 때 사용. 너무 짧으면 마지막 자세 전에 넘어갈 수 있습니다.")]
    [SerializeField] private float deathAnimFinishedNormalizedTime = 0.98f;
    [Tooltip("애니가 안 넘어가도 이 시간(초)이 지나면 제거 대기 단계로 진행합니다.")]
    [SerializeField] private float deathAnimSafetyTimeout = 12f;
    [Tooltip("사망 시작 후 제거될 때까지 death 상태를 강제로 유지합니다.")]
    [SerializeField] private bool lockToDeathStateUntilDestroyed = true;
    [Tooltip("사망 상태 재진입 강제 시도 간격(초). 너무 짧으면 Animator 요청이 과도해집니다.")]
    [SerializeField, Min(0.05f)] private float deathReapplyIntervalSeconds = 0.2f;

    private int currentHealth;
    private bool isDeathStarted;
    private int deathStateHash;
    private float nextDeathReapplyRealtime;
    private MonsterOrcAssassinStealthSimple stealthSkill; // 오크 어쌔신 은신 스킬이 있으면 여기에 캐시합니다.

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        if (animator == null) { animator = GetComponentInChildren<Animator>(); }
        ApplyNecromancerDefaultsIfNeeded();
        currentHealth = maxHealth;
        if (attackBehaviour == null) { TryGetComponent(out attackBehaviour); }
        TryGetComponent(out stealthSkill); // 은신 스킬 컴포넌트가 있으면 한 번만 찾아 둡니다.
    }

    public void TakeDamage(int damage, GameObject attacker, Vector3 hitPoint)
    {
        if (isDeathStarted) return;
        if (damage <= 0) return;

        Vector3 vfxPos = HitPoint.IsUnspecified(hitPoint) ? transform.position + Vector3.up * 1f : hitPoint;
        HitImpactVfx.PlayAt(vfxPos, attacker);

        currentHealth -= damage;
        float hitStopLen = hitStopDuration > 0f ? hitStopDuration : 0.05f;

        Debug.Log($"[SimpleMonsterHealth] {name} 체력: {currentHealth} / {maxHealth} (공격자: {(attacker != null ? attacker.name : "없음")})");

        if (IsDamageFromPlayer(attacker))
        {
            if (stealthSkill != null) { stealthSkill.NotifyHitByPlayer(); } // 플레이어에게 맞으면 은신을 즉시 풉니다.
            string hitNote;
            if (currentHealth <= 0) hitNote = " | 사망 처리";
            else if (ShouldPlayHitReaction()) hitNote = " | 피격 반응 재생";
            else hitNote = " | 피격 반응 생략(몬스터 공격 중)";
            Debug.Log($"[SimpleMonsterHealth] 플레이어 공격 적중: {name} | 데미지 {damage} | 남은 HP {currentHealth}/{maxHealth}{hitNote}");
        }

        if (currentHealth <= 0)
        {
            StartDeathIfNeeded();
            HitStopController.RequestAfterRealtimeDelay(hitStopDelayAfterHitReactSeconds, hitStopLen, hitStopTimeScale);
            return;
        }

        if (ShouldPlayHitReaction())
        {
            PlayHitReaction();
        }

        HitStopController.RequestAfterRealtimeDelay(hitStopDelayAfterHitReactSeconds, hitStopLen, hitStopTimeScale);
    }

    private void StartDeathIfNeeded()
    {
        if (isDeathStarted) return;
        isDeathStarted = true;
        deathStateHash = ResolveDeathStateHash();
        nextDeathReapplyRealtime = 0f;

        DisableCombatAndMovement();
        DisableDamageColliders();

        StartCoroutine(DeathSequenceRoutine());
    }

    private void DisableCombatAndMovement()
    {
        if (TryGetComponent(out MonsterOrcAssassinStealthSimple stealth))
        {
            // 은신 스크립트가 OnDisable에서 추격/공격을 복구할 수 있어 먼저 끕니다.
            stealth.enabled = false;
        }

        if (TryGetComponent(out NecromancerBossController necromancerBoss))
        {
            necromancerBoss.enabled = false;
        }

        if (attackBehaviour != null) attackBehaviour.enabled = false;

        if (TryGetComponent(out MonsterDetectChaseSimple chase))
        {
            chase.enabled = false;
        }
    }

    private void DisableDamageColliders()
    {
        foreach (Collider c in GetComponentsInChildren<Collider>(includeInactive: true))
        {
            c.enabled = false;
        }
    }

    private IEnumerator DeathSequenceRoutine()
    {
        PlayDeathAnimation();

        yield return null;

        float startedRealtime = Time.realtimeSinceStartup;
        float safetyUntilRealtime = startedRealtime + Mathf.Max(0f, deathAnimSafetyTimeout);
        bool deathAnimReachedEnd = false;
        bool warnedTimeout = false;

        while (Time.realtimeSinceStartup < safetyUntilRealtime)
        {
            if (animator != null && !string.IsNullOrEmpty(deathStateName))
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                bool inDeathState = IsInDeathState(info);

                if (inDeathState && info.normalizedTime >= deathAnimFinishedNormalizedTime)
                {
                    deathAnimReachedEnd = true;
                    break;
                }

                // 사망 상태 진입이 늦어지면 간격을 두고 재시도해 요청 폭주를 방지합니다.
                if (lockToDeathStateUntilDestroyed && !inDeathState && Time.realtimeSinceStartup >= nextDeathReapplyRealtime)
                {
                    TryForceEnterDeathState();
                    nextDeathReapplyRealtime = Time.realtimeSinceStartup + Mathf.Max(0.05f, deathReapplyIntervalSeconds);
                }

                if (!warnedTimeout && Time.realtimeSinceStartup >= safetyUntilRealtime)
                {
                    warnedTimeout = true;
                    Debug.LogWarning($"[SimpleMonsterHealth] {name}: 사망 상태({deathStateName})를 확인하는 중 안전 타임아웃({deathAnimSafetyTimeout}초)에 도달했습니다. death 상태를 다시 고정합니다.");
                }
            }

            yield return null;
        }

        if (!deathAnimReachedEnd && !warnedTimeout)
        {
            Debug.LogWarning($"[SimpleMonsterHealth] {name}: 사망 애니 끝(normalizedTime {deathAnimFinishedNormalizedTime})까지 도달하지 못했습니다. 안전 제거를 진행합니다.");
        }

        float holdPoseSeconds = Mathf.Max(0f, removeAfterDeathAnimationSeconds);
        if (holdPoseSeconds > 0f)
        {
            yield return new WaitForSecondsRealtime(holdPoseSeconds);
        }

        if (animator != null)
        {
            // 사망 포즈를 고정해 Idle 등 다른 상태로 되돌아가지 않게 막습니다.
            animator.enabled = false;
        }

        Destroy(gameObject);
    }

    private void PlayDeathAnimation()
    {
        if (animator == null) return;

        // 공격/피격 트리거가 남아 있으면 Any State 전이가 반복될 수 있어 사망 시작 시 모두 정리합니다.
        ResetAllAnimatorTriggers();

        if (!string.IsNullOrEmpty(deathBoolParameter) && HasBoolParameter(deathBoolParameter))
        {
            animator.SetBool(deathBoolParameter, true);
        }

        if (TryForceEnterDeathState()) return;

        Debug.LogWarning($"[SimpleMonsterHealth] {name}: 사망 애니 재생 실패(Bool='{deathBoolParameter}', State='{deathStateName}'). Animator 설정을 확인하세요.");
    }

    private bool TryForceEnterDeathState()
    {
        if (animator == null) return false;
        if (deathStateHash == 0) return false;
        if (!animator.HasState(0, deathStateHash)) return false;

        animator.CrossFade(deathStateHash, 0.02f, 0, 0f);
        return true;
    }

    private int ResolveDeathStateHash()
    {
        if (animator == null) return 0;
        if (string.IsNullOrEmpty(deathStateName)) return 0;

        int hash = Animator.StringToHash(deathStateName);
        if (!animator.HasState(0, hash)) return 0;
        return hash;
    }

    private bool IsInDeathState(AnimatorStateInfo info)
    {
        if (string.IsNullOrEmpty(deathStateName)) return false;
        return info.IsName(deathStateName) || info.IsName($"Base Layer.{deathStateName}");
    }

    private void ResetAllAnimatorTriggers()
    {
        if (animator == null) return;
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter p = parameters[i];
            if (p.type != AnimatorControllerParameterType.Trigger) continue;
            animator.ResetTrigger(p.name);
        }
    }

    private static bool IsDamageFromPlayer(GameObject attacker)
    {
        if (attacker == null) return false;
        if (attacker.CompareTag("Player")) return true;
        for (Transform t = attacker.transform.parent; t != null; t = t.parent)
        {
            if (t.CompareTag("Player")) return true;
        }
        return false;
    }

    private bool ShouldPlayHitReaction()
    {
        if (attackBehaviour != null && attackBehaviour.IsSuppressingHitReaction) return false;
        return true;
    }

    private void PlayHitReaction()
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(getHitTriggerParameter) && HasTriggerParameter(getHitTriggerParameter))
        {
            animator.ResetTrigger(getHitTriggerParameter);
            animator.SetTrigger(getHitTriggerParameter);
            return;
        }

        if (!string.IsNullOrEmpty(getHitStateName))
        {
            int hitHash = Animator.StringToHash(getHitStateName);
            if (animator.HasState(0, hitHash))
            {
                animator.CrossFade(hitHash, 0.06f, 0, 0f);
            }
        }
    }

    private bool HasTriggerParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName) return true;
        }
        return false;
    }

    private bool HasBoolParameter(string paramName)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName) return true;
        }
        return false;
    }

    private void ApplyNecromancerDefaultsIfNeeded()
    {
        if (!autoApplyNecromancerDefaults || animator == null || animator.runtimeAnimatorController == null) return;

        string controllerName = animator.runtimeAnimatorController.name;
        if (string.IsNullOrEmpty(controllerName)) return;

        bool isNecromancerController =
            controllerName.Contains("Necromanser") ||
            controllerName.Contains("Necromancer");
        if (!isNecromancerController) return;

        maxHealth = Mathf.Max(1, necromancerMaxHealth);
        getHitStateName = string.IsNullOrEmpty(necromancerHitStateName) ? getHitStateName : necromancerHitStateName;
        deathStateName = string.IsNullOrEmpty(necromancerDeathStateName) ? deathStateName : necromancerDeathStateName;
    }
}
