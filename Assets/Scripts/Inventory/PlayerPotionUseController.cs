using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 숫자 1 키로 인벤토리 포션을 사용하고, 손에 프리팹을 쥔 채 Drinking 애니메이션을 재생합니다.
/// </summary>
public class PlayerPotionUseController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private PlayerPotionInventory inventory;
    [SerializeField] private SimplePlayerHealth playerHealth;

    [Header("애니메이션")]
    [SerializeField] private string drinkTriggerParameter = "DrinkPotion";
    [SerializeField] private string drinkStateName = "DrinkPotionState";
    [SerializeField] private int drinkAnimatorLayer = 0;
    [SerializeField] private float drinkDuration = 1.8f;

    [Header("손 장착 오프셋")]
    [SerializeField] private Vector3 leftHandLocalPosition = new Vector3(-0.0062f, 0.0515f, 0.0128f);
    [SerializeField] private Vector3 leftHandLocalEuler = new Vector3(0f, -3f, 0f);
    [SerializeField] private Vector3 leftHandLocalScale = new Vector3(0.88f, 0.88f, 0.88f);

    [Header("병목 기준 보정")]
    [Tooltip("포션 프리팹 원점에서 병목까지의 로컬 오프셋. 이 값을 기준으로 손가락 사이에 병목이 오도록 보정합니다.")]
    [SerializeField] private Vector3 bottleNeckLocalOffset = new Vector3(0f, 0.075f, 0f);
    [Tooltip("병목 위치를 손가락 기준점에 미세하게 튜닝하기 위한 월드 오프셋.")]
    [SerializeField] private Vector3 fingerAnchorWorldOffset = Vector3.zero;

    private Transform _leftHand;
    private Transform _leftIndexDistal;
    private bool _isDrinking;
    private GameObject _holdInstance;
    private readonly System.Collections.Generic.List<GameObject> _temporarilyHiddenEquipments = new System.Collections.Generic.List<GameObject>();
    public bool IsDrinking => _isDrinking;

    private void Awake()
    {
        if (targetAnimator == null) targetAnimator = GetComponentInChildren<Animator>();
        if (inventory == null) inventory = PlayerPotionInventory.Resolve(transform);
        if (inventory == null) inventory = gameObject.AddComponent<PlayerPotionInventory>();
        if (playerHealth == null) playerHealth = SimplePlayerHealth.Resolve(transform);
        if (targetAnimator != null)
        {
            _leftHand = targetAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            _leftIndexDistal = targetAnimator.GetBoneTransform(HumanBodyBones.LeftIndexDistal);
        }
    }

    private void Update()
    {
        if (_isDrinking) return;
        if (BlacksmithGameplayLock.IsMenuOpen) return;
        if (!ReadUseKeyDown()) return;

        if (inventory == null && (inventory = PlayerPotionInventory.Resolve(transform)) == null) return;
        if (!inventory.TryConsume(out PlayerPotionInventory.PotionItem item)) return;
        StartCoroutine(DrinkRoutine(item));
    }

    private IEnumerator DrinkRoutine(PlayerPotionInventory.PotionItem item)
    {
        _isDrinking = true;
        HideEquipmentsDuringDrink();
        AttachPotionToHand(item.holdPrefab);
        PlayDrinkAnimation();
        yield return WaitUntilDrinkAnimationFinished();
        ApplyPotionHeal();
        CleanupHoldItem();
        RestoreEquipmentsAfterDrink();
        _isDrinking = false;
    }

    private void ApplyPotionHeal()
    {
        if (playerHealth == null)
        {
            playerHealth = SimplePlayerHealth.Resolve(transform);
        }

        if (playerHealth == null) return;
        playerHealth.HealToFull();
    }

    private void AttachPotionToHand(GameObject holdPrefab)
    {
        if (_leftHand == null || holdPrefab == null) return;
        _holdInstance = Instantiate(holdPrefab, _leftHand);
        _holdInstance.transform.localPosition = leftHandLocalPosition;
        _holdInstance.transform.localRotation = Quaternion.Euler(leftHandLocalEuler);
        _holdInstance.transform.localScale = leftHandLocalScale;

        // 병목이 손 기준점이 되도록 원점 차이를 역보정합니다.
        Vector3 neckCompensation = _holdInstance.transform.localRotation * Vector3.Scale(bottleNeckLocalOffset, _holdInstance.transform.localScale);
        _holdInstance.transform.localPosition -= neckCompensation;
        AlignPotionToFingerAtDrinkStart();
    }

    private void AlignPotionToFingerAtDrinkStart()
    {
        if (_holdInstance == null) return;

        Transform hold = _holdInstance.transform;
        Vector3 euler = hold.eulerAngles;
        hold.rotation = Quaternion.Euler(0f, euler.y, 0f);

        Transform fingerAnchor = _leftIndexDistal != null ? _leftIndexDistal : _leftHand;
        if (fingerAnchor == null) return;

        Vector3 neckWorldPosition = hold.TransformPoint(bottleNeckLocalOffset);
        Vector3 targetWorldPosition = fingerAnchor.position + fingerAnchorWorldOffset;
        hold.position += (targetWorldPosition - neckWorldPosition);
    }

    private void PlayDrinkAnimation()
    {
        if (targetAnimator == null) return;
        if (HasTriggerParameter(drinkTriggerParameter))
        {
            targetAnimator.ResetTrigger(drinkTriggerParameter);
            targetAnimator.SetTrigger(drinkTriggerParameter);
        }
    }

    private bool HasTriggerParameter(string paramName)
    {
        if (targetAnimator == null || string.IsNullOrEmpty(paramName)) return false;
        for (int i = 0; i < targetAnimator.parameters.Length; i++)
        {
            AnimatorControllerParameter p = targetAnimator.parameters[i];
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName) return true;
        }

        return false;
    }

    private void CleanupHoldItem()
    {
        if (_holdInstance != null) Destroy(_holdInstance);
        _holdInstance = null;
    }

    private void HideEquipmentsDuringDrink()
    {
        _temporarilyHiddenEquipments.Clear();
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            Transform t = all[i];
            if (!t.CompareTag("Equipment")) continue;
            if (!t.gameObject.activeSelf) continue;
            t.gameObject.SetActive(false);
            _temporarilyHiddenEquipments.Add(t.gameObject);
        }
    }

    private void RestoreEquipmentsAfterDrink()
    {
        for (int i = 0; i < _temporarilyHiddenEquipments.Count; i++)
        {
            GameObject go = _temporarilyHiddenEquipments[i];
            if (go != null) go.SetActive(true);
        }

        _temporarilyHiddenEquipments.Clear();
    }

    private IEnumerator WaitUntilDrinkAnimationFinished()
    {
        if (targetAnimator == null || string.IsNullOrEmpty(drinkStateName))
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, drinkDuration));
            yield break;
        }

        float enterTimeout = 0.8f;
        float t = 0f;
        while (t < enterTimeout && !IsAnimatorInDrinkState())
        {
            t += Time.deltaTime;
            yield return null;
        }

        if (!IsAnimatorInDrinkState())
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, drinkDuration));
            yield break;
        }

        float safetyCap = Mathf.Max(0.5f, drinkDuration + 2f);
        t = 0f;
        while (t < safetyCap && IsAnimatorInDrinkState())
        {
            t += Time.deltaTime;
            yield return null;
        }
    }

    private bool IsAnimatorInDrinkState()
    {
        if (targetAnimator == null) return false;
        AnimatorStateInfo cur = targetAnimator.GetCurrentAnimatorStateInfo(drinkAnimatorLayer);
        if (cur.IsName(drinkStateName)) return true;
        if (targetAnimator.IsInTransition(drinkAnimatorLayer))
        {
            AnimatorStateInfo next = targetAnimator.GetNextAnimatorStateInfo(drinkAnimatorLayer);
            if (next.IsName(drinkStateName)) return true;
        }

        return false;
    }

    private static bool ReadUseKeyDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Alpha1)) return true;
#endif
        return false;
    }
}
