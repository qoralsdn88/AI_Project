using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어 체력·피격 연출·사망·리스폰을 한 컴포넌트에서 처리합니다.
/// 몬스터가 붙잡는 Transform이 자식일 때도 찾을 수 있도록 <see cref="Resolve"/>를 사용하세요.
/// </summary>
public class SimplePlayerHealth : MonoBehaviour
{
    private const string LogTag = "[PlayerHP]";

    [Header("체력")]
    public int maxHp = 100;
    public int currentHp;

    [Header("애니메이터")]
    [Tooltip("비우면 자식에서 Animator를 찾습니다.")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitReactTrigger = "HitReact";
    [SerializeField] private string deathTrigger = "Dead";
    [SerializeField] private string hitStateName = "Sword_Impact";
    [SerializeField] private string deathStateName = "Dead";

    [Header("피격·사망 타이밍")]
    [SerializeField] private float hitStunDuration = 0.5f;
    [Tooltip("피격이 적용된 뒤 이 시간 동안은 추가 피격을 무시합니다.")]
    [SerializeField, Min(0f)] private float hitInvulnerabilitySeconds = 1f;
    [SerializeField] private float respawnDelaySeconds = 5f;
    [SerializeField] private float hitCrossFadeDuration = 0.1f;
    [Tooltip("피격(또는 사망) 애니가 재생된 뒤, 이 시간(실시간 초) 후에 히트 스탑이 걸립니다.")]
    [SerializeField, Min(0f)] private float hitStopDelayAfterHitReactSeconds = 0.1f;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.05f;
    [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0f;
    [Tooltip("사망 클립으로 강제 전환할 때 사용합니다. 트리거만 쓰면 공격 Any State 전환과 겹쳐 Dead로 안 들어갈 때가 있습니다.")]
    [SerializeField] private float deathCrossFadeDuration = 0.12f;

    [Header("연결 (비우면 자동 검색)")]
    [SerializeField] private PlayerMeleeCombat meleeCombat;

    [Header("Animator — 사망 시 정리")]
    [Tooltip("방어 중 사망 시 IsBlockHeld 등이 Dead 전환과 경쟁하지 않게 끕니다. PlayerDirectionalAnimationController 기본값과 맞춥니다.")]
    [SerializeField] private string blockHeldParameter = "IsBlockHeld";
    [SerializeField] private string unarmedMovingParameter = "IsUnarmedMoving";
    [SerializeField] private string moveStateParameter = "MoveState";
    [Tooltip("가드 성공 직후 사망 시 남은 ShieldImpact 트리거가 Dead를 덮지 않게 리셋합니다.")]
    [SerializeField] private string shieldImpactTrigger = "ShieldImpact";

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private float _actionLockUntilTime;
    private float _nextDamageAllowedTime;
    private bool _isDead;
    private Coroutine _respawnRoutine;

    public bool IsDead => _isDead;
    public bool IsActionLocked => _isDead || Time.time < _actionLockUntilTime;

    /// <summary>
    /// <paramref name="t"/>가 가리키는 오브젝트, 그 부모, 그 자식에서 체력 컴포넌트를 찾습니다.
    /// (예: Monster 쪽에 연결한 player가 모델 자식이고 체력은 루트에만 있을 때.)
    /// </summary>
    public static SimplePlayerHealth Resolve(Transform t) => TransformHierarchy.FindComponent<SimplePlayerHealth>(t);

    private void Awake()
    {
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;

        if (animator == null) { animator = GetComponentInChildren<Animator>(true); }
        if (meleeCombat == null) { meleeCombat = PlayerMeleeCombat.Resolve(transform); }
    }

    private void Start()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, null, HitPoint.Unspecified);
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        TakeDamage(damage, attacker, HitPoint.Unspecified);
    }

    public void TakeDamage(int damage, GameObject attacker, Vector3 hitPoint)
    {
        if (_isDead) return;
        if (Time.time < _nextDamageAllowedTime) return;

        int previousHp = currentHp;
        int applied = Mathf.Max(0, damage);
        if (applied > 0)
        {
            Vector3 vfxPos = HitPoint.IsUnspecified(hitPoint) ? transform.position + Vector3.up * 1f : hitPoint;
            HitImpactVfx.PlayAt(vfxPos, attacker);
        }

        currentHp = Mathf.Max(0, currentHp - applied);
        if (applied > 0)
        {
            _nextDamageAllowedTime = Time.time + hitInvulnerabilitySeconds;
        }
        float hitStopLen = applied > 0 ? (hitStopDuration > 0f ? hitStopDuration : 0.05f) : 0f;

        Debug.Log(
            $"{LogTag} 피해 {applied} | 이전 체력 {previousHp} → 현재 {currentHp} / 최대 {maxHp}" +
            (attacker != null ? $" | 공격자: {attacker.name}" : string.Empty));

        if (currentHp <= 0)
        {
            Debug.Log($"{LogTag} 사망 처리 시작 (HP 0)");
            EnterDeath();
            if (hitStopLen > 0f)
            {
                HitStopController.RequestAfterRealtimeDelay(hitStopDelayAfterHitReactSeconds, hitStopLen, hitStopTimeScale);
            }

            return;
        }

        ApplyHitReaction();
        if (hitStopLen > 0f)
        {
            HitStopController.RequestAfterRealtimeDelay(hitStopDelayAfterHitReactSeconds, hitStopLen, hitStopTimeScale);
        }
    }

    private void ApplyHitReaction()
    {
        InterruptMelee();
        PlayHitAnimation();
        _actionLockUntilTime = Mathf.Max(_actionLockUntilTime, Time.time + Mathf.Max(0.02f, hitStunDuration));
    }

    /// <summary>
    /// 트리거(Any State)만 쓰면 Attack·콤보 전환과 같은 프레임에 경쟁할 수 있어,
    /// 레이어 0에 Dead 상태가 있으면 CrossFade로 먼저 강제 전환합니다.
    /// </summary>
    /// <summary>
    /// 방어 유지 bool·이동 블렌드·가드 임팩트 트리거가 Dead CrossFade/트리거와 동시에 켜져 있으면
    /// 베이스 레이어가 Block 쪽에 머물러 사망 모션이 안 나올 수 있어, 사망 직전에 정리합니다.
    /// </summary>
    private void ClearAnimatorStateCompetingWithDeath()
    {
        if (animator == null) return;

        TrySetAnimatorBool(blockHeldParameter, false);
        TrySetAnimatorBool(unarmedMovingParameter, false);
        TrySetAnimatorInt(moveStateParameter, 0);
        TryResetAnimatorTrigger(shieldImpactTrigger);
    }

    private void TrySetAnimatorBool(string paramName, bool value)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == paramName)
            {
                animator.SetBool(paramName, value);
                return;
            }
        }
    }

    private void TrySetAnimatorInt(string paramName, int value)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Int && p.name == paramName)
            {
                animator.SetInteger(paramName, value);
                return;
            }
        }
    }

    private void TryResetAnimatorTrigger(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName)
            {
                animator.ResetTrigger(paramName);
                return;
            }
        }
    }

    private void PlayDeathAnimation()
    {
        if (animator == null) return;

        animator.ResetTrigger(hitReactTrigger);
        float fade = Mathf.Max(0.02f, deathCrossFadeDuration);

        if (!string.IsNullOrEmpty(deathStateName))
        {
            int deadHash = Animator.StringToHash(deathStateName);
            if (animator.HasState(0, deadHash))
            {
                animator.CrossFade(deadHash, fade, 0, 0f);
                return;
            }
        }

        if (HasAnimatorTrigger(deathTrigger))
        {
            animator.ResetTrigger(deathTrigger);
            animator.SetTrigger(deathTrigger);
            return;
        }

        Debug.LogWarning(
            $"{LogTag} 사망 애니 실패. Controller 베이스 레이어에 상태 이름이 정확히 '{deathStateName}' 인지, " +
            $"또는 Trigger '{deathTrigger}' 가 있는지 확인하세요.");
    }

    private void PlayHitAnimation()
    {
        if (animator == null) return;

        if (HasAnimatorTrigger(hitReactTrigger))
        {
            animator.ResetTrigger(hitReactTrigger);
            animator.SetTrigger(hitReactTrigger);
            return;
        }

        if (!string.IsNullOrEmpty(hitStateName) && animator.HasState(0, Animator.StringToHash(hitStateName)))
        {
            animator.CrossFade(hitStateName, hitCrossFadeDuration, 0, 0f);
            return;
        }

        Debug.LogWarning($"{LogTag} 피격 애니 실패. Animator에 HitReact / Sword_Impact를 추가했는지 확인하세요.");
    }

    private void EnterDeath()
    {
        currentHp = 0;
        _isDead = true;
        InterruptMelee();
        _actionLockUntilTime = float.MaxValue;

        ClearAnimatorStateCompetingWithDeath();
        PlayDeathAnimation();

        if (_respawnRoutine != null) { StopCoroutine(_respawnRoutine); }
        _respawnRoutine = StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, respawnDelaySeconds));
        RespawnAtStart();
        _respawnRoutine = null;
    }

    private void RespawnAtStart()
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
            cc.enabled = true;
        }
        else
        {
            transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        }

        currentHp = maxHp;
        _isDead = false;
        _actionLockUntilTime = 0f;
        _nextDamageAllowedTime = 0f;

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        Debug.Log($"{LogTag} 리스폰 완료 — 위치·체력 {currentHp}/{maxHp}");
    }

    private void InterruptMelee()
    {
        if (meleeCombat == null) { meleeCombat = PlayerMeleeCombat.Resolve(transform); }
        if (meleeCombat != null) { meleeCombat.InterruptAttack(); }
    }

    public void End_Damaged()
    {
        if (_isDead) return;
        _actionLockUntilTime = Time.time;
    }

    private bool HasAnimatorTrigger(string paramName)
    {
        if (animator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName) return true;
        }

        return false;
    }
}
