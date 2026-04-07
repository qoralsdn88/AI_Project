// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;
// Input System 에셋을 쓰기 위해 꼭 필요합니다.
using UnityEngine.InputSystem;
// 코루틴을 쓰기 위해 꼭 필요합니다.
using System.Collections;
// 해시셋을 쓰기 위해 꼭 필요합니다.
using System.Collections.Generic;

// 마우스 왼쪽(입력 에셋의 Attack)으로 콤보 공격을 실행하는 스크립트입니다.
// 핵심 요약: ComboIndex와 Attack 트리거로 1·2·3타를 이어갑니다.
public class PlayerMeleeCombat : MonoBehaviour
{
    // 프로젝트에 있는 InputSystem_Actions 에셋을 넣습니다.
    // 핵심 요약: Player 맵의 Attack 액션을 읽습니다.
    [SerializeField] private InputActionAsset inputActionAsset;

    // 같은 오브젝트 트리 안에서 Animator를 찾습니다.
    // 핵심 요약: Humanoid 손 위치와 공격 애니메이션을 같이 씁니다.
    [SerializeField] private Animator animator;

    // Animator의 콤보 단계를 넣는 정수 파라미터 이름입니다.
    // 핵심 요약: 1, 2, 3 값으로 Sword_Combo 클립을 고릅니다.
    [SerializeField] private string comboIndexParameter = "ComboIndex";

    // Animator의 공격 시작용 트리거 이름입니다.
    // 핵심 요약: 누를 때마다 이 트리거를 켭니다.
    [SerializeField] private string attackTriggerParameter = "Attack";

    // 이번 스윙에서 맞춘 대상을 중복으로 맞추지 않기 위한 모음입니다.
    // 핵심 요약: IDamageable이 붙은 오브젝트 ID를 저장합니다.
    private readonly HashSet<int> _hitInstanceIds = new HashSet<int>();

    // 한 타가 시작한 뒤 다음 콤보로 이어질 수 있는 최대 대기 시간입니다.
    // 핵심 요약: 이 시간을 넘기면 다음 공격은 1타부터 다시옵니다.
    [SerializeField] private float comboChainResetTime = 0.9f;

    // 마지막으로 공격 입력을 처리한 시간입니다.
    // 핵심 요약: comboChainResetTime 비교에 씁니다.
    private float _lastAttackInputTime = -999f;

    // 지금 실행 중인 공격 코루틴이 있는지 표시합니다.
    // 핵심 요약: true면 아직 이 타가 끝나지 않은 것입니다.
    private bool _isAttacking;

    // 공격 중에 쌓인 “다음 타로 이어가기” 요청 수입니다.
    // 핵심 요약: 1타만 연타해도 2·3타로 이어지려면 최대 2번까지면 충분합니다.
    private int _queuedChainSteps;

    // 한 번의 공격 코루틴 안에서만 쌓을 연타 버퍼 상한입니다(2 = 1→2→3용).
    // 핵심 요약: 숫자를 크게 두면 연타가 ‘남아’ 제자리에서 여러 번 콤보가 도는 원인이 됩니다.
    [SerializeField] private int maxQueuedChainSteps = 2;

    // 지금 타격 판정이 열려 있는지 표시합니다.
    // 핵심 요약: MeleeWeaponHitbox가 이 값을 보고 데미지를 줍니다.
    private bool _damageWindowActive;
    // 현재 타의 "실제 타격 구간"이 끝났는지 표시합니다.
    // 핵심 요약: true가 되면 모션이 남아 있어도 다음 콤보로 자연스럽게 넘어갈 수 있습니다.
    private bool _attackHitPhaseEnded;

    [Header("타격 타이밍 (애니메이션 이벤트)")]
    [Tooltip("켜두면 클립의 Begin_Collision / End_Collision 이벤트가 피격 창을 열고 닫습니다. Animator와 같은 오브젝트(또는 자식)에 이 스크립트가 있어야 이벤트가 호출됩니다.")]
    [SerializeField] private bool useAnimationEventsForDamageWindow = true;

    // 한 번에 줄 데미지 크기입니다.
    // 핵심 요약: TryHit에서 그대로 전달합니다.
    [SerializeField] private int attackDamage = 12;

    [Header("타격 타이밍 (타이머 폴백)")]
    [Tooltip("useAnimationEventsForDamageWindow가 꺼져 있을 때만 사용합니다.")]
    [SerializeField] private float[] damageWindowStartTimes = { 0.18f, 0.2f, 0.22f };

    [Tooltip("useAnimationEventsForDamageWindow가 꺼져 있을 때만 사용합니다.")]
    [SerializeField] private float[] damageWindowLengths = { 0.12f, 0.14f, 0.16f };

    // 각 콤보가 끝날 때까지 기다리는 최소 시간입니다.
    // 핵심 요약: Animator 상태 대기 실패 시 폴백으로 씁니다.
    [SerializeField] private float[] attackTotalDurations = { 0.55f, 0.6f, 0.75f };

    // 공격이 재생되는 Animator 레이어 인덱스입니다.
    [SerializeField] private int attackAnimatorLayer = 0;

    // 각 콤보 Animator 상태 이름입니다(MaceController 등과 동일하게 맞춤).
    [SerializeField] private string combo1StateName = "Combo1";
    [SerializeField] private string combo2StateName = "Combo2";
    [SerializeField] private string combo3StateName = "Combo3";

    // Player 맵에서 Attack 액션을 저장합니다.
    // 핵심 요약: OnEnable에서 구독합니다.
    private InputAction _attackAction;

    // 지금 몇 번째 콤보인지 저장합니다.
    // 핵심 요약: 1, 2, 3 사이를 순환합니다.
    private int _comboStep;

    // SwordDamageTrigger 등이 스트라이크가 열릴 때 물리를 새로 고칩니다.
    private readonly List<MeleeWeaponHitbox> _registeredWeaponHitboxes = new List<MeleeWeaponHitbox>(4);

    // 외부에서 “지금 공격 중인지”를 읽습니다.
    // 핵심 요약: 걷기 애니메이션을 잠시 멈출 때 씁니다.
    public bool IsAttacking => _isAttacking;

    // 외부에서 “지금 데미지 창인지”를 읽습니다.
    // 핵심 요약: MeleeWeaponHitbox가 매 프레임 판단합니다.
    public bool IsDamageWindowActive => _damageWindowActive;

    private SimplePlayerHealth _playerHealth;

    public static PlayerMeleeCombat Resolve(Transform t)
    {
        if (t == null) return null;
        if (t.TryGetComponent(out PlayerMeleeCombat c)) return c;
        c = t.GetComponentInParent<PlayerMeleeCombat>(true);
        if (c != null) return c;
        return t.GetComponentInChildren<PlayerMeleeCombat>(true);
    }

    // 피격·사망 시 공격을 즉시 중단합니다. StopAllCoroutines()는 코루틴의 finally를 호출하지 않으므로 여기서 상태를 정리합니다.
    public void InterruptAttack()
    {
        StopAllCoroutines();
        _queuedChainSteps = 0;
        _isAttacking = false;
        _damageWindowActive = false;
        NotifyWeaponHitboxesStrikeClosed();
        if (animator != null) { animator.ResetTrigger(attackTriggerParameter); }
    }

    /// <summary>
    /// 검 트리거가 붙을 때 호출합니다. PlayerEquipmentHolder가 자동으로 연결합니다.
    /// </summary>
    public void RegisterWeaponHitbox(MeleeWeaponHitbox hitbox)
    {
        if (hitbox == null) return;
        if (!_registeredWeaponHitboxes.Contains(hitbox)) { _registeredWeaponHitboxes.Add(hitbox); }
    }

    /// <summary>
    /// 검 트리거가 없어질 때 호출합니다.
    /// </summary>
    public void UnregisterWeaponHitbox(MeleeWeaponHitbox hitbox)
    {
        if (hitbox == null) return;
        _registeredWeaponHitboxes.Remove(hitbox);
    }

    // ─── 애니메이션 이벤트 (FBX 클립에 넣은 함수 이름과 정확히 같아야 합니다) ───

    /// <summary>콤보 구간이 시작될 때(선택). 이어지기/이펙트용으로 써도 됩니다.</summary>
    public void Begin_Combo()
    {
        if (!_isAttacking) return;
    }

    /// <summary>이벤트 시점부터 칼날 피격을 허용합니다. 클립에 반드시 넣으세요.</summary>
    public void Begin_Collision()
    {
        if (!_isAttacking) return;
        if (useAnimationEventsForDamageWindow)
        {
            _hitInstanceIds.Clear();
            _damageWindowActive = true;
            NotifyWeaponHitboxesStrikeOpened();
        }
    }

    /// <summary>이벤트 시점에서 칼날 피격을 끕니다. Begin_Collision과 쌍으로 넣으세요.</summary>
    public void End_Collision()
    {
        if (!useAnimationEventsForDamageWindow) return;
        _damageWindowActive = false;
        _attackHitPhaseEnded = true;
        NotifyWeaponHitboxesStrikeClosed();
    }

    /// <summary>콤보 한 동작이 끝날 때(선택).</summary>
    public void End_Combo()
    {
        if (!_isAttacking) return;
    }

    /// <summary>루트 모션·이동 잠금 등에 쓰는 훅(선택).</summary>
    public void Begin_DoAction() { }

    /// <summary>루트 모션·이동 잠금 등에 쓰는 훅(선택).</summary>
    public void End_DoAction() { }

    private void NotifyWeaponHitboxesStrikeOpened()
    {
        for (int i = 0; i < _registeredWeaponHitboxes.Count; i++)
        {
            if (_registeredWeaponHitboxes[i] != null) { _registeredWeaponHitboxes[i].OnAnimatorStrikeWindowOpened(); }
        }
    }

    private void NotifyWeaponHitboxesStrikeClosed()
    {
        for (int i = 0; i < _registeredWeaponHitboxes.Count; i++)
        {
            if (_registeredWeaponHitboxes[i] != null) { _registeredWeaponHitboxes[i].OnAnimatorStrikeWindowClosed(); }
        }
    }

    // 준비 단계에서 Animator를 찾고 입력을 연결합니다.
    // 핵심 요약: Animator가 비어 있으면 자식에서 찾습니다.
    private void Awake()
    {
        // Animator가 비어 있으면 자식에서 찾습니다.
        if (animator == null) { animator = GetComponentInChildren<Animator>(); }
        // 그래도 없으면 에러를 남깁니다.
        if (animator == null) { Debug.LogError("[PlayerMeleeCombat] Animator를 찾지 못했습니다."); }
        else if (useAnimationEventsForDamageWindow && animator.gameObject != gameObject)
        {
            Debug.LogWarning(
                "[PlayerMeleeCombat] Animator가 이 GameObject가 아닙니다. FBX 애니메이션 이벤트(Begin_Collision 등)는 보통 Animator가 붙은 오브젝트의 스크립트만 호출합니다. " +
                "이 스크립트를 Animator와 같은 오브젝트로 옮기거나, Animator를 이 오브젝트로 맞추세요.");
        }

        _playerHealth = SimplePlayerHealth.Resolve(transform);

        // 입력 에셋이 없으면 에러를 남깁니다.
        if (inputActionAsset == null) { Debug.LogError("[PlayerMeleeCombat] Input Action Asset이 비어 있습니다. InputSystem_Actions를 넣어주세요."); return; }

        // Player 맵을 찾습니다.
        InputActionMap map = inputActionAsset.FindActionMap("Player");
        // 맵이 없으면 에러를 남깁니다.
        if (map == null) { Debug.LogError("[PlayerMeleeCombat] Player 액션 맵을 찾지 못했습니다."); return; }

        // Attack 액션을 찾습니다.
        _attackAction = map.FindAction("Attack");
        // 액션이 없으면 에러를 남깁니다.
        if (_attackAction == null) { Debug.LogError("[PlayerMeleeCombat] Attack 액션을 찾지 못했습니다."); }
    }

    // 오브젝트가 켜질 때 입력 구독을 시작합니다.
    // 핵심 요약: performed 이벤트에 연결합니다.
    private void OnEnable()
    {
        // 액션이 없으면 종료합니다.
        if (_attackAction == null) return;
        // performed는 장치에 따라 누르고 있는 동안 반복될 수 있어 started(맨 처음 눌림)만 씁니다.
        _attackAction.started += OnAttackStarted;
        // 액션을 활성화합니다.
        _attackAction.Enable();
    }

    // 오브젝트가 꺼질 때 입력을 해제합니다.
    // 핵심 요약: 메모리 누수를 막기 위해 반드시 풉니다.
    private void OnDisable()
    {
        // 액션이 없으면 종료합니다.
        if (_attackAction == null) return;
        // 연결을 해제합니다.
        _attackAction.started -= OnAttackStarted;
        // 액션을 끕니다.
        _attackAction.Disable();
    }

    // Attack이 눌린 순간(에지) 한 번만 호출됩니다.
    // 핵심 요약: performed로 두면 같은 입력이 프레임마다 들어와 큐가 불어날 수 있습니다.
    private void OnAttackStarted(InputAction.CallbackContext ctx)
    {
        if (ctx.phase != InputActionPhase.Started) { return; }

        if (_playerHealth == null) { _playerHealth = SimplePlayerHealth.Resolve(transform); }
        if (_playerHealth != null && _playerHealth.IsActionLocked) { return; }

        // 이미 공격이 돌고 있으면 다음 1타분만 버퍼합니다(최대 maxQueuedChainSteps).
        if (_isAttacking)
        {
            _lastAttackInputTime = Time.time;
            int cap = Mathf.Clamp(maxQueuedChainSteps, 1, 3);
            _queuedChainSteps = Mathf.Min(_queuedChainSteps + 1, cap);
            return;
        }

        // 핵심: 콤보 리셋은 "대기 중일 때 새로 공격을 시작할 때"만 검사합니다.
        // while 루프 안에서 같은 식으로 리셋하면 1타 애니가 comboChainResetTime보다 길 때
        // 2타로 넘어가기 전에 단계가 0으로 돌아가 제자리에서 또 1타만 나갑니다.
        if (Time.time - _lastAttackInputTime > comboChainResetTime) { _comboStep = 0; }
        _lastAttackInputTime = Time.time;
        _queuedChainSteps = 0;
        StartCoroutine(AttackRoutine());
    }

    // 공격(연콤보 포함) 전체를 한 코루틴에서 처리합니다.
    // 핵심 요약: 타마다 _isAttacking을 풀면 입력·코루틴이 겹쳐 1타 중복·2타 스킵이 납니다.
    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;
        try
        {
            while (true)
            {
                _comboStep++;
                if (_comboStep > 3) { _comboStep = 1; }

                int idx = _comboStep - 1;
                float startT = SafePick(damageWindowStartTimes, idx, 0.2f);
                float lenT = SafePick(damageWindowLengths, idx, 0.14f);

                _hitInstanceIds.Clear();
                _damageWindowActive = false;
                _attackHitPhaseEnded = false;

                if (animator != null)
                {
                    animator.ResetTrigger(attackTriggerParameter);
                    animator.SetInteger(comboIndexParameter, _comboStep);
                    animator.SetTrigger(attackTriggerParameter);
                }

                if (useAnimationEventsForDamageWindow)
                {
                    // 피격 창은 Begin_Collision / End_Collision 애니메이션 이벤트가 열고 닫습니다.
                }
                else
                {
                    yield return new WaitForSeconds(startT);
                    _damageWindowActive = true;
                    yield return new WaitForSeconds(lenT);
                    _damageWindowActive = false;
                    _attackHitPhaseEnded = true;
                }

                if (_comboStep < 3)
                {
                    // 1~2타는 "모션 끝"까지 강제 대기하지 않고,
                    // 공격 판정이 끝난 뒤 버퍼 입력이 있으면 바로 다음 콤보로 넘어갑니다.
                    yield return WaitUntilComboAdvanceWindowOpen();
                }
                else
                {
                    // 마지막 타는 자연스럽게 끝까지 재생합니다.
                    yield return WaitUntilComboAnimatorStateExited();
                }

                if (_comboStep >= 3)
                {
                    _queuedChainSteps = 0;
                    break;
                }

                if (_queuedChainSteps <= 0) { break; }

                _queuedChainSteps--;
            }
        }
        finally
        {
            // 코루틴이 끝난 뒤 남은 버퍼로 또 공격이 이어지지 않게 비웁니다.
            _queuedChainSteps = 0;
            _isAttacking = false;
            _damageWindowActive = false;
            NotifyWeaponHitboxesStrikeClosed();
            if (animator != null)
            {
                animator.ResetTrigger(attackTriggerParameter);
            }
        }
    }

    private string GetComboStateName(int step)
    {
        if (step == 1) return combo1StateName;
        if (step == 2) return combo2StateName;
        if (step == 3) return combo3StateName;
        return combo1StateName;
    }

    private bool IsAnimatorInState(int layer, string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) { return false; }
        AnimatorStateInfo cur = animator.GetCurrentAnimatorStateInfo(layer);
        if (cur.IsName(stateName)) { return true; }
        if (animator.IsInTransition(layer))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layer);
            if (next.IsName(stateName)) { return true; }
        }
        return false;
    }

    // 현재 콤보 상태(Combo1~3)에 들어갔다가 빠져나올 때까지 기다립니다.
    private IEnumerator WaitUntilComboAnimatorStateExited()
    {
        if (animator == null)
        {
            float d = SafePick(attackTotalDurations, _comboStep - 1, 0.6f);
            yield return new WaitForSeconds(d);
            yield break;
        }

        int layer = attackAnimatorLayer;
        string stateName = GetComboStateName(_comboStep);
        float fallbackDur = SafePick(attackTotalDurations, _comboStep - 1, 0.6f);

        float enterTimeout = 0.5f;
        float t = 0f;
        while (t < enterTimeout && !IsAnimatorInState(layer, stateName))
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!IsAnimatorInState(layer, stateName))
        {
            yield return new WaitForSeconds(fallbackDur);
            yield break;
        }

        float lingerCap = fallbackDur + 2.5f;
        t = 0f;
        while (t < lingerCap && IsAnimatorInState(layer, stateName))
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    // 현재 콤보 상태에 들어간 뒤,
    // 1) 상태가 끝나거나
    // 2) 타격 구간이 끝났고 버퍼 입력이 있으면
    // 둘 중 먼저 만족하는 시점까지 기다립니다.
    private IEnumerator WaitUntilComboAdvanceWindowOpen()
    {
        if (animator == null)
        {
            float d = SafePick(attackTotalDurations, _comboStep - 1, 0.6f);
            yield return new WaitForSeconds(d);
            yield break;
        }

        int layer = attackAnimatorLayer;
        string stateName = GetComboStateName(_comboStep);
        float fallbackDur = SafePick(attackTotalDurations, _comboStep - 1, 0.6f);

        float enterTimeout = 0.5f;
        float t = 0f;
        while (t < enterTimeout && !IsAnimatorInState(layer, stateName))
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!IsAnimatorInState(layer, stateName))
        {
            yield return new WaitForSeconds(fallbackDur);
            yield break;
        }

        float lingerCap = fallbackDur + 2.5f;
        t = 0f;
        while (t < lingerCap && IsAnimatorInState(layer, stateName))
        {
            // 공격 판정이 끝났고 다음 타 입력이 버퍼되어 있으면
            // 현재 모션이 끝나기 전이라도 다음 콤보로 넘어갑니다.
            if (_attackHitPhaseEnded && _queuedChainSteps > 0)
            {
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }
    }

    // 배열에서 안전하게 값을 꺼내는 도우미 함수입니다.
    // 핵심 요약: 배열 길이가 짧으면 기본값을 씁니다.
    private static float SafePick(float[] arr, int index, float fallback)
    {
        // 배열이 비었으면 기본값을 씁니다.
        if (arr == null || arr.Length == 0) return fallback;
        // 인덱스가 범위를 벗어나면 마지막 값을 씁니다.
        if (index < 0 || index >= arr.Length) return arr[arr.Length - 1];
        // 정상 인덱스면 그대로 반환합니다.
        return arr[index];
    }

    // 칼 충돌에서 호출해 데미지를 주는 함수입니다.
    // 핵심 요약: 같은 스윙에서 같은 적은 한 번만 맞습니다.
    public void TryHit(IDamageable target)
    {
        // 대상이 없으면 종료합니다.
        if (target == null) return;
        // MonoBehaviour로 바꿔서 인스턴스 ID를 구합니다.
        Component c = target as Component;
        // 컴포넌트가 없으면 종료합니다.
        if (c == null) return;

        // 게임오브젝트 고유 번호를 가져옵니다.
        int id = c.gameObject.GetInstanceID();
        // 이미 맞춘 적이면 종료합니다.
        if (_hitInstanceIds.Contains(id)) return;
        // 목록에 넣습니다.
        _hitInstanceIds.Add(id);
        // 데미지를 줍니다.
        target.TakeDamage(attackDamage, gameObject);
    }
}
