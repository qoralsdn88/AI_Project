using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 문 열기(F키/클릭), 몬스터 스폰 신호, 방 자동 잠금과 연동됩니다.
/// </summary>
public class DoorOpenSignal : MonoBehaviour
{
    private static readonly List<DoorOpenSignal> RegisteredDoors = new List<DoorOpenSignal>();

    public static IReadOnlyList<DoorOpenSignal> AllDoors => RegisteredDoors;

    [Header("몬스터 스폰 옵션")]
    [SerializeField] private bool useMonsterSpawn;

    [Header("연결할 방 스폰러")]
    [SerializeField] private RoomMonsterSpawner roomMonsterSpawner;

    [Header("문 애니메이션")]
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private string openTriggerParam = "Open";
    [SerializeField] private bool waitSpawnUntilAnimationEvent = true;

    [Header("방 자동 잠금(닫기)")]
    [SerializeField] private bool allowRoomAutoCloseAndLock = true;
    [SerializeField] private string closeTriggerParam = "Close";
    [SerializeField] private bool requireCloseTriggerInAnimator;

    [Header("F키 열기 설정")]
    [SerializeField] private bool openByFKey = true;
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private bool useTriggerRange = true;
    [SerializeField] private bool useDistanceRangeFallback = true;
    [SerializeField] private float interactDistance = 3f;

    [Header("클릭 열기 설정")]
    [SerializeField] private bool openByMouseClick;
    [SerializeField] private bool canClickOnlyOnce = true;

    [Header("열린 뒤 끄기")]
    [SerializeField] private bool disableThisDoorAfterSignal;

    private bool isDoorLocked;
    private bool hasClicked;
    private bool playerInRange;
    private Transform playerTransform;
    private bool hasOpenRequested;
    private bool hasSpawnSignalSent;

    private void OnEnable()
    {
        if (!RegisteredDoors.Contains(this)) RegisteredDoors.Add(this);
    }

    private void OnDisable()
    {
        RegisteredDoors.Remove(this);
    }

    private void Start()
    {
        if (doorAnimator == null) doorAnimator = GetComponent<Animator>();
        FindPlayerIfMissing();
    }

    private void Update()
    {
        if (isDoorLocked || !openByFKey || hasOpenRequested) return;
        FindPlayerIfMissing();
        if (!IsPlayerInInteractRange()) return;
        if (!Input.GetKeyDown(interactKey)) return;
        OpenDoorNow();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
    }

    private void OnMouseDown()
    {
        if (isDoorLocked || !openByMouseClick) return;
        if (canClickOnlyOnce && hasClicked) return;
        hasClicked = true;
        OpenDoorNow();
    }

    /// <summary>UI 상호작용 안내 표시 여부. 잠금·이미 연 문은 false.</summary>
    public bool ShouldShowInteractPrompt()
    {
        if (!openByFKey || isDoorLocked || hasOpenRequested) return false;
        return IsPlayerInInteractRange();
    }

    public string GetInteractPromptText() => $"{interactKey}: 열기";

    public void CloseAndLockDoor()
    {
        if (!allowRoomAutoCloseAndLock || isDoorLocked) return;
        isDoorLocked = true;

        if (doorAnimator == null || string.IsNullOrEmpty(closeTriggerParam)) return;

        if (HasTriggerParameter(closeTriggerParam))
        {
            doorAnimator.SetTrigger(closeTriggerParam);
        }
        else if (requireCloseTriggerInAnimator)
        {
            Debug.LogWarning(
                "[DoorOpenSignal] Close 트리거가 Animator에 없습니다. closeTriggerParam 이름을 확인하거나 닫기 클립을 추가해주세요.");
        }
    }

    private void OpenDoorNow()
    {
        if (isDoorLocked || hasOpenRequested) return;
        hasOpenRequested = true;

        bool playedAnimation = false;
        if (doorAnimator != null && HasTriggerParameter(openTriggerParam))
        {
            doorAnimator.SetTrigger(openTriggerParam);
            playedAnimation = true;
        }

        if (waitSpawnUntilAnimationEvent && playedAnimation) return;
        DoorOpened();
    }

    public void DoorOpened()
    {
        if (!useMonsterSpawn || hasSpawnSignalSent) return;
        if (roomMonsterSpawner == null)
        {
            Debug.LogWarning(
                "[DoorOpenSignal] useMonsterSpawn이 true인데 roomMonsterSpawner가 비어 있습니다. 스폰 문이면 연결해주세요.");
            return;
        }

        hasSpawnSignalSent = true;
        roomMonsterSpawner.OnDoorOpened();

        if (disableThisDoorAfterSignal) gameObject.SetActive(false);
    }

    private void FindPlayerIfMissing()
    {
        if (playerTransform != null) return;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null) playerTransform = playerObject.transform;
    }

    private bool IsPlayerInInteractRange()
    {
        if (useTriggerRange && playerInRange) return true;
        if (!useDistanceRangeFallback || playerTransform == null) return false;
        return Vector3.Distance(transform.position, playerTransform.position) <= interactDistance;
    }

    private bool HasTriggerParameter(string paramName)
    {
        if (doorAnimator == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter p in doorAnimator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == paramName) return true;
        }

        return false;
    }
}
