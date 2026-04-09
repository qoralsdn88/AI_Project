using UnityEngine;

/// <summary>
/// 방어 모션 재생 중에만 방패의 월드 회전을 고정해 손 본 회전 꼬임을 막습니다.
/// </summary>
public class ShieldDefenseRotationLock : MonoBehaviour
{
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private string[] defenseStateNames = { "ShieldImpact", "Block", "Guard" };

    private Quaternion _lockedWorldRotation;
    [Header("비방어 시 장착 유지")]
    [SerializeField] private bool keepAttachmentPoseOutsideDefense = true;

    private Vector3 _cachedLocalPosition;
    private Quaternion _cachedLocalRotation;
    private bool _wasLocking;
    private Transform _parent;
    private Vector3 _lockStartParentWorldPosition;
    private Vector3 _lockStartShieldWorldPosition;

    public void Initialize(Animator animatorRef, int layerIndex, string[] stateNames)
    {
        targetAnimator = animatorRef;
        animatorLayer = Mathf.Max(0, layerIndex);

        if (stateNames != null && stateNames.Length > 0)
        {
            defenseStateNames = stateNames;
        }
    }

    private void Awake()
    {
        _parent = transform.parent;
        _cachedLocalPosition = transform.localPosition;
        _cachedLocalRotation = transform.localRotation;
        _lockedWorldRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (targetAnimator == null) { return; }
        bool isDefense = IsInDefenseState();

        if (isDefense)
        {
            if (!_wasLocking)
            {
                // 방어 모션 진입 시점의 월드 회전을 잠금값으로 사용합니다.
                _lockedWorldRotation = transform.rotation;
                _lockStartShieldWorldPosition = transform.position;
                _lockStartParentWorldPosition = _parent != null ? _parent.position : transform.position;
            }

            // 손 본 회전은 무시하고, 손 본의 "위치 변화량"만 따라갑니다.
            // 이렇게 하면 방어 중 월드 회전 고정 상태에서도 공중으로 튀는 궤도 이동을 막을 수 있습니다.
            if (_parent != null)
            {
                Vector3 parentDelta = _parent.position - _lockStartParentWorldPosition;
                transform.position = _lockStartShieldWorldPosition + parentDelta;
                transform.rotation = _lockedWorldRotation;
            }
            else
            {
                transform.rotation = _lockedWorldRotation;
            }
            _wasLocking = true;
            return;
        }

        if (_wasLocking)
        {
            // 방어 종료 시 회전만 원래 장착 회전으로 복원합니다.
            transform.localPosition = _cachedLocalPosition;
            transform.localRotation = _cachedLocalRotation;
            _wasLocking = false;
        }

        if (keepAttachmentPoseOutsideDefense)
        {
            // 피격/타 애니메이션/물리 간섭으로 장착 오프셋이 변해도 기본 장착값으로 복원합니다.
            transform.localPosition = _cachedLocalPosition;
            transform.localRotation = _cachedLocalRotation;
        }
    }

    private bool IsInDefenseState()
    {
        if (defenseStateNames == null || defenseStateNames.Length == 0) { return false; }

        AnimatorStateInfo current = targetAnimator.GetCurrentAnimatorStateInfo(animatorLayer);
        if (IsDefenseState(current)) { return true; }

        if (targetAnimator.IsInTransition(animatorLayer))
        {
            AnimatorStateInfo next = targetAnimator.GetNextAnimatorStateInfo(animatorLayer);
            if (IsDefenseState(next)) { return true; }
        }

        return false;
    }

    private bool IsDefenseState(AnimatorStateInfo stateInfo)
    {
        for (int i = 0; i < defenseStateNames.Length; i++)
        {
            string name = defenseStateNames[i];
            if (string.IsNullOrWhiteSpace(name)) { continue; }
            if (stateInfo.IsName(name)) { return true; }
        }

        return false;
    }
}
