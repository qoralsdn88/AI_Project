using System.Collections;
using UnityEngine;

public class MonsterAttackSimple : MonoBehaviour
{
    [Header("연결 설정")]
    public MonsterDetectChaseSimple detectChase;
    public Animator animator;

    [Header("공격 설정")]
    public int attackDamage = 10;
    public float attackCooldown = 1.2f;
    public string attackTriggerParam = "Attack";

    [Header("무기 충돌 피해")]
    [Tooltip("공격이 나갈 때 이 시간(초) 동안 무기 하이트박스가 플레이어와 겹치면 피해가 들어갑니다. 애니 메시를 맞추려면 길이를 조절하세요.")]
    [SerializeField] private float weaponDamageWindowDuration = 0.35f;
    [Tooltip("공격 시작 후 이 시간(초)만큼 기다렸다가 데미지 창을 엽니다. 0이면 즉시 열립니다.")]
    [SerializeField] private float weaponDamageWindowStartDelay = 0f;

    [Header("공격 모션 여러 개")]
    public bool useAttackVariants = true;
    public string[] attackVariantTriggerParams = new string[] { "Attack1", "Attack2" };
    public bool pickRandomVariant = true;
    [Tooltip("공격 버튼이 눌린 뒤 이 시간(초) 동안은 몬스터를 제자리로 고정합니다.")]
    [SerializeField] private float attackMoveLockDuration = 0.45f;

    private int variantIndex = 0;
    private float attackTimer = 0f;

    [Header("몬스터 피격 연출")]
    [SerializeField] private float attackHitSuppressionDuration = 0.85f;
    private float hitSuppressionEndTime = -999f;

    public bool IsSuppressingHitReaction => Time.time < hitSuppressionEndTime;
    public bool IsAttackMoveLocked => Time.time < attackMoveLockEndTime;

    public int AttackDamage => attackDamage;
    public bool IsWeaponDamageWindowActive { get; private set; }
    public int WeaponSwingId { get; private set; }

    private Coroutine _weaponWindowRoutine;
    private Coroutine _weaponWindowDelayRoutine;
    private float attackMoveLockEndTime = -999f;

    private void Start()
    {
        FindDetectChaseIfMissing();
        FindAnimatorIfMissing();
    }

    private void Update()
    {
        FindDetectChaseIfMissing();
        FindAnimatorIfMissing();
        if (detectChase == null || detectChase.player == null) return;

        UpdateAttackTimer();
        TryAttackIfPossible();
    }

    private void FindDetectChaseIfMissing()
    {
        if (detectChase == null) { detectChase = GetComponent<MonsterDetectChaseSimple>(); }
    }

    private void FindAnimatorIfMissing()
    {
        if (animator == null) { animator = GetComponentInChildren<Animator>(); }
    }

    private void UpdateAttackTimer()
    {
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
    }

    private void TryAttackIfPossible()
    {
        if (!detectChase.IsDetected) return;
        if (!detectChase.IsInAttackRange) return;
        if (attackTimer > 0f) return;

        Vector3 toPlayer = detectChase.player.position - transform.position;
        toPlayer.y = 0f;
        detectChase.FaceDirection(toPlayer.normalized);

        attackTimer = attackCooldown;
        attackMoveLockEndTime = Time.time + Mathf.Max(0f, attackMoveLockDuration);
        hitSuppressionEndTime = Time.time + Mathf.Max(0f, attackHitSuppressionDuration);
        PlayAttackAnimation();
        StartWeaponDamageWindowWithDelay();
    }

    private void StartWeaponDamageWindowWithDelay()
    {
        if (_weaponWindowDelayRoutine != null) { StopCoroutine(_weaponWindowDelayRoutine); }
        _weaponWindowDelayRoutine = StartCoroutine(BeginWeaponDamageWindowAfterDelay());
    }

    private IEnumerator BeginWeaponDamageWindowAfterDelay()
    {
        float delay = Mathf.Max(0f, weaponDamageWindowStartDelay);
        if (delay > 0f) { yield return new WaitForSeconds(delay); }
        _weaponWindowDelayRoutine = null;
        StartWeaponDamageWindowNow();
    }

    private void StartWeaponDamageWindowNow()
    {
        WeaponSwingId++;
        if (_weaponWindowRoutine != null) { StopCoroutine(_weaponWindowRoutine); }
        IsWeaponDamageWindowActive = true;
        _weaponWindowRoutine = StartCoroutine(EndWeaponDamageWindowAfterDelay());
    }

    private IEnumerator EndWeaponDamageWindowAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0.02f, weaponDamageWindowDuration));
        IsWeaponDamageWindowActive = false;
        _weaponWindowRoutine = null;
    }

    private void PlayAttackAnimation()
    {
        if (animator == null) return;
        if (useAttackVariants)
        {
            TryPlayVariantAnimation();
            return;
        }

        if (string.IsNullOrEmpty(attackTriggerParam)) return;
        if (!HasTriggerParameter(attackTriggerParam)) return;
        animator.SetTrigger(attackTriggerParam);
    }

    private void TryPlayVariantAnimation()
    {
        if (attackVariantTriggerParams == null || attackVariantTriggerParams.Length == 0) return;

        int chosenIndex;
        if (pickRandomVariant) { chosenIndex = Random.Range(0, attackVariantTriggerParams.Length); }
        else
        {
            chosenIndex = Mathf.Clamp(variantIndex, 0, attackVariantTriggerParams.Length - 1);
            variantIndex++;
            if (variantIndex >= attackVariantTriggerParams.Length) variantIndex = 0;
        }

        string chosenParam = attackVariantTriggerParams[chosenIndex];
        if (string.IsNullOrEmpty(chosenParam)) return;
        if (!HasTriggerParameter(chosenParam)) return;
        animator.SetTrigger(chosenParam);
    }

    private bool HasTriggerParameter(string paramName)
    {
        if (string.IsNullOrEmpty(paramName)) return false;
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type != AnimatorControllerParameterType.Trigger) continue;
            if (parameters[i].name != paramName) continue;
            return true;
        }

        return false;
    }
}
