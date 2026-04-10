// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using System.Collections;
using UnityEngine;

// 몬스터 체력을 관리하고 플레이어 공격을 받아들이는 간단한 스크립트입니다.
// 핵심 요약: IDamageable을 구현해 칼 충돌 스크립트와 연결됩니다.
public class SimpleMonsterHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 30;

    [Header("피격 표현")]
    [SerializeField] private Animator animator;
    [Tooltip("Monster_Base 등 Animator에 추가한 피격용 Trigger 이름.")]
    [SerializeField] private string getHitTriggerParameter = "GetHit";
    [SerializeField] private MonsterAttackSimple attackBehaviour;
    [Tooltip("피격(또는 사망) 애니가 재생된 뒤, 이 시간(실시간 초) 후에 히트 스탑이 걸립니다.")]
    [SerializeField, Min(0f)] private float hitStopDelayAfterHitReactSeconds = 0.1f;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.05f;
    [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0f;

    [Header("사망")]
    [Tooltip("Animator의 Dead 트리거 이름(Monster_Base 기본값: Dead).")]
    [SerializeField] private string deathTriggerParameter = "Dead";
    [Tooltip("Animator 상태 이름. FBX 클립 이름이 Dead이면 보통 상태 이름도 Dead입니다.")]
    [SerializeField] private string deathStateName = "Dead";
    [Tooltip("죽는 애니가 끝난 뒤 씬에서 제거하기까지 대기(초).")]
    [SerializeField] private float removeAfterDeathAnimationSeconds = 3f;
    [Tooltip("사망 애니가 끝났는지 판별할 때 사용. 너무 짧으면 마지막 자세 전에 넘어갈 수 있습니다.")]
    [SerializeField] private float deathAnimFinishedNormalizedTime = 0.98f;
    [Tooltip("애니가 안 넘어가도 이 시간(초)이 지나면 제거 대기 단계로 진행합니다.")]
    [SerializeField] private float deathAnimSafetyTimeout = 12f;

    private int currentHealth;
    private bool isDeathStarted;
    private MonsterOrcAssassinStealthSimple stealthSkill; // 오크 어쌔신 은신 스킬이 있으면 여기에 캐시합니다.

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        if (animator == null) { animator = GetComponentInChildren<Animator>(); }
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

        DisableCombatAndMovement();
        DisableDamageColliders();

        StartCoroutine(DeathSequenceRoutine());
    }

    private void DisableCombatAndMovement()
    {
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

        float start = Time.time;
        bool finishedByTime = false;

        while (Time.time - start < deathAnimSafetyTimeout)
        {
            if (animator == null || string.IsNullOrEmpty(deathStateName))
            {
                finishedByTime = true;
                break;
            }

            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(deathStateName) && info.normalizedTime >= deathAnimFinishedNormalizedTime)
            {
                finishedByTime = true;
                break;
            }

            yield return null;
        }

        if (!finishedByTime && animator != null)
        {
            Debug.LogWarning($"[SimpleMonsterHealth] {name}: 사망 상태({deathStateName})가 {deathAnimSafetyTimeout}초 안에 끝나지 않아 안전 타임아웃으로 넘어갑니다. Animator에 Dead 트리거/상태가 있는지 확인하세요.");
        }

        if (removeAfterDeathAnimationSeconds > 0f)
        {
            yield return new WaitForSeconds(removeAfterDeathAnimationSeconds);
        }

        Destroy(gameObject);
    }

    private void PlayDeathAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(deathTriggerParameter)) return;
        if (!HasTriggerParameter(deathTriggerParameter))
        {
            Debug.LogWarning($"[SimpleMonsterHealth] {name}: Animator에 Trigger '{deathTriggerParameter}'가 없습니다. Monster_Base.controller에 Dead 트리거를 추가했는지 확인하세요.");
            return;
        }

        animator.ResetTrigger(deathTriggerParameter);
        animator.SetTrigger(deathTriggerParameter);
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
        if (animator == null || string.IsNullOrEmpty(getHitTriggerParameter)) return;
        if (!HasTriggerParameter(getHitTriggerParameter)) return;
        animator.ResetTrigger(getHitTriggerParameter);
        animator.SetTrigger(getHitTriggerParameter);
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
}
