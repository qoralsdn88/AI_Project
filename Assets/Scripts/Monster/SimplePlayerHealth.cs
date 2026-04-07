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
    [SerializeField] private float respawnDelaySeconds = 5f;
    [SerializeField] private float hitCrossFadeDuration = 0.1f;
    [Tooltip("사망 클립으로 강제 전환할 때 사용합니다. 트리거만 쓰면 공격 Any State 전환과 겹쳐 Dead로 안 들어갈 때가 있습니다.")]
    [SerializeField] private float deathCrossFadeDuration = 0.12f;

    [Header("연결 (비우면 자동 검색)")]
    [SerializeField] private PlayerMeleeCombat meleeCombat;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private float _actionLockUntilTime;
    private bool _isDead;
    private Coroutine _respawnRoutine;

    public bool IsDead => _isDead;
    public bool IsActionLocked => _isDead || Time.time < _actionLockUntilTime;

    /// <summary>
    /// <paramref name="t"/>가 가리키는 오브젝트, 그 부모, 그 자식에서 체력 컴포넌트를 찾습니다.
    /// (예: Monster 쪽에 연결한 player가 모델 자식이고 체력은 루트에만 있을 때.)
    /// </summary>
    public static SimplePlayerHealth Resolve(Transform t)
    {
        if (t == null) return null;
        if (t.TryGetComponent(out SimplePlayerHealth direct)) return direct;
        SimplePlayerHealth p = t.GetComponentInParent<SimplePlayerHealth>(true);
        if (p != null) return p;
        return t.GetComponentInChildren<SimplePlayerHealth>(true);
    }

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
        TakeDamage(damage, null);
    }

    public void TakeDamage(int damage, GameObject attacker)
    {
        if (_isDead) return;

        int previousHp = currentHp;
        int applied = Mathf.Max(0, damage);
        currentHp = Mathf.Max(0, currentHp - applied);

        Debug.Log(
            $"{LogTag} 피해 {applied} | 이전 체력 {previousHp} → 현재 {currentHp} / 최대 {maxHp}" +
            (attacker != null ? $" | 공격자: {attacker.name}" : string.Empty));

        if (currentHp <= 0)
        {
            Debug.Log($"{LogTag} 사망 처리 시작 (HP 0)");
            EnterDeath();
            return;
        }

        ApplyHitReaction();
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
