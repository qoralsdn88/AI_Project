using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 방어 입력 + “정면에서 온 공격만” 막는 각도 판정을 모읍니다.
/// 몬스터 쪽은 <see cref="MonsterWeaponHitbox"/>에서 피해를 주기 전에 여기만 호출하면 됩니다.
/// </summary>
public class PlayerShieldBlock : MonoBehaviour
{
    private const string LogTag = "[PlayerBlock]";

    [SerializeField] private InputActionAsset inputActionAsset;
    [Tooltip("비우면 이 오브젝트 기준 forward(대개 마우스로 도는 몸통 방향)로 정면을 잡습니다.")]
    [SerializeField] private Transform facingReference;

    [Header("방어 각도")]
    [Tooltip("플레이어 정면 기준 ±이 각도(도) 안에서 오면 ‘정면 맞음’으로 봅니다. 45~70 정도가 무난합니다.")]
    [SerializeField] private float blockArcHalfAngleDegrees = 55f;

    [Header("연결 (비우면 자동 검색)")]
    [SerializeField] private SimplePlayerHealth playerHealth;
    [SerializeField] private PlayerMeleeCombat meleeCombat;
    [SerializeField] private Animator animator;

    [Header("방어 성공 연출")]
    [Tooltip("Animator Trigger 이름. 있으면 우선 사용합니다.")]
    [SerializeField] private string blockImpactTrigger = "ShieldImpact";
    [Tooltip("베이스 레이어 상태 이름. Trigger가 없을 때 CrossFade로 재생합니다.")]
    [SerializeField] private string blockImpactStateName = "ShieldImpact";
    [SerializeField] private float blockImpactCrossFadeDuration = 0.05f;

    [Header("디버그")]
    [SerializeField] private bool verboseDebugLog = true;
    [SerializeField] private float debugLogInterval = 0.25f;
    
    private InputAction _blockAction;
    private float _nextDebugLogTime;

    /// <summary>
    /// 오른쪽 마우스(방어키) 유지 여부를 외부(애니메이션 등)에서 읽을 때 사용합니다.
    /// </summary>
    public bool IsBlockInputHeld => IsBlockButtonHeld();

    public static PlayerShieldBlock Resolve(Transform t) => TransformHierarchy.FindComponent<PlayerShieldBlock>(t);

    /// <summary>
    /// 몬스터 히트박스에서 호출: 정면 가드에 성공하면 true입니다(실제 피해량은 호출 측에서 감소 적용).
    /// </summary>
    public static bool TryBlockHit(Transform playerTransform, GameObject attacker)
    {
        PlayerShieldBlock block = Resolve(playerTransform);
        if (block == null) return false;
        return block.EvaluateBlock(attacker);
    }

    private void Awake()
    {
        if (facingReference == null) { facingReference = transform; }
        if (playerHealth == null) { playerHealth = SimplePlayerHealth.Resolve(transform); }
        if (meleeCombat == null) { meleeCombat = PlayerMeleeCombat.Resolve(transform); }
        if (animator == null) { animator = GetComponentInChildren<Animator>(true); }

        if (inputActionAsset == null)
        {
            Debug.LogWarning($"{LogTag} Input Action Asset이 비었습니다. Block 입력 체크는 입력 폴백으로 처리합니다.");
            return;
        }

        InputActionMap map = inputActionAsset.FindActionMap("Player");
        if (map == null)
        {
            Debug.LogError($"{LogTag} Player 액션 맵을 찾지 못했습니다.");
            return;
        }

        _blockAction = map.FindAction("Block");
        if (_blockAction == null)
        {
            Debug.LogWarning($"{LogTag} Block 액션을 찾지 못했습니다. 입력 폴백(마우스 우클릭)으로 처리합니다.");
        }
    }

    private void OnEnable()
    {
        _blockAction?.Enable();
    }

    private void OnDisable()
    {
        _blockAction?.Disable();
    }

    private bool EvaluateBlock(GameObject attacker)
    {
        if (playerHealth == null) { playerHealth = SimplePlayerHealth.Resolve(transform); }
        if (playerHealth != null && playerHealth.IsDead)
        {
            DebugBlockFail("플레이어 사망 상태");
            return false;
        }
        if (playerHealth != null && playerHealth.IsActionLocked)
        {
            DebugBlockFail("플레이어 액션 락 상태");
            return false;
        }
        if (!IsBlockButtonHeld())
        {
            DebugBlockFail("방어 키(우클릭) 미입력");
            return false;
        }
        if (attacker == null)
        {
            DebugBlockFail("attacker가 null");
            return false;
        }

        if (meleeCombat == null) { meleeCombat = PlayerMeleeCombat.Resolve(transform); }
        if (meleeCombat != null && meleeCombat.IsAttacking)
        {
            DebugBlockFail("공격 중이라 방어 불가");
            return false;
        }

        if (!IsAttackerInFrontBlockArc(attacker.transform))
        {
            DebugBlockFail($"정면 아님: 공격자 {attacker.name}");
            return false;
        }

        PlayBlockImpactAnimation();
        Debug.Log($"{LogTag} 방어 성공 — {attacker.name}의 정면 공격을 막음");
        return true;
    }

    private bool IsBlockButtonHeld()
    {
        if (_blockAction != null)
        {
            return _blockAction.IsPressed();
        }

        // 입력 액션이 비어 있어도 동작하도록 우클릭 직접 읽기 폴백.
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.isPressed) { return true; }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButton(1)) { return true; }
#endif
        return false;
    }

    /// <summary>
    /// 몬스터가 플레이어 정면 부채꼴 안에 있는지 봅니다. Y는 무시해 카메라 기울기에 덜 흔들리게 합니다.
    /// </summary>
    private bool IsAttackerInFrontBlockArc(Transform attackerTransform)
    {
        Vector3 origin = facingReference.position;
        Vector3 toAttacker = attackerTransform.position - origin;
        toAttacker.y = 0f;
        float distSq = toAttacker.sqrMagnitude;
        if (distSq < 0.0001f) return true;

        toAttacker /= Mathf.Sqrt(distSq);

        Vector3 forward = facingReference.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return false;
        forward.Normalize();

        float dot = Vector3.Dot(forward, toAttacker);
        float threshold = Mathf.Cos(Mathf.Clamp(blockArcHalfAngleDegrees, 5f, 89f) * Mathf.Deg2Rad);
        if (verboseDebugLog && Time.time >= _nextDebugLogTime)
        {
            _nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugLogInterval);
            float angle = Vector3.Angle(forward, toAttacker);
            Debug.Log($"{LogTag} 각도 검사 | angle={angle:F1} | half={blockArcHalfAngleDegrees:F1} | dot={dot:F3} | threshold={threshold:F3}");
        }
        return dot >= threshold;
    }

    private void DebugBlockFail(string reason)
    {
        if (!verboseDebugLog) return;
        if (Time.time < _nextDebugLogTime) return;
        _nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugLogInterval);
        Debug.Log($"{LogTag} 방어 실패 — {reason}");
    }

    private void PlayBlockImpactAnimation()
    {
        if (animator == null) return;

        if (HasAnimatorTrigger(blockImpactTrigger))
        {
            animator.ResetTrigger(blockImpactTrigger);
            animator.SetTrigger(blockImpactTrigger);
            return;
        }

        if (!string.IsNullOrEmpty(blockImpactStateName))
        {
            int stateHash = Animator.StringToHash(blockImpactStateName);
            if (animator.HasState(0, stateHash))
            {
                animator.CrossFade(stateHash, Mathf.Max(0.01f, blockImpactCrossFadeDuration), 0, 0f);
                return;
            }
        }

        Debug.LogWarning(
            $"{LogTag} 방어 성공 애니 재생 실패. Animator에 Trigger '{blockImpactTrigger}' 또는 상태 '{blockImpactStateName}'를 추가해주세요.");
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
