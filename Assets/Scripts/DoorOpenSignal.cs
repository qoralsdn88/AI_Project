using UnityEngine; // Unity 기본 기능을 쓰기 위해 가져옵니다.
public class DoorOpenSignal : MonoBehaviour // 문 열기 입력과 스폰 신호를 함께 처리하는 스크립트입니다.
{ // 이 클래스의 시작 중괄호입니다.
    [Header("몬스터 스폰 옵션")] // 인스펙터에서 몬스터 스폰 사용 여부를 묶습니다.
    [SerializeField] private bool useMonsterSpawn = false; // true인 문만 몬스터 스폰 신호를 사용합니다.
    [Header("연결할 방 스폰러")] // 인스펙터에서 연결 값을 보기 좋게 묶습니다.
    [SerializeField] private RoomMonsterSpawner roomMonsterSpawner; // 문이 연결할 방 스폰 스크립트입니다.
    [Header("문 애니메이션")] // 인스펙터에서 문 애니메이션 관련 값을 묶습니다.
    [SerializeField] private Animator doorAnimator; // 문 오브젝트의 애니메이터를 연결하는 변수입니다.
    [SerializeField] private string openTriggerParam = "Open"; // 문 열기 트리거 이름을 저장하는 변수입니다.
    [SerializeField] private bool waitSpawnUntilAnimationEvent = true; // true면 애니메이션 이벤트에서 DoorOpened를 따로 호출할 때 스폰합니다.
    [Header("방 자동 잠금(닫기)")] // 인스펙터에서 방에 들어왔을 때 닫기 관련 값을 묶습니다.
    [SerializeField] private bool allowRoomAutoCloseAndLock = true; // false면 RoomAutoLockDoors가 이 문을 닫지 못하게 막습니다.
    [SerializeField] private string closeTriggerParam = "Close"; // 문 닫기 애니메이션에 쓸 트리거 이름을 저장합니다.
    [SerializeField] private bool requireCloseTriggerInAnimator = false; // true면 Close 트리거가 없을 때 경고를 남깁니다.
    private bool isDoorLocked = false; // true면 F키·클릭으로 다시 열 수 없습니다.
    [Header("F키 열기 설정")] // 인스펙터에서 F키 입력 관련 옵션을 묶습니다.
    [SerializeField] private bool openByFKey = true; // true면 플레이어가 범위 안에서 F를 눌렀을 때 문을 엽니다.
    [SerializeField] private KeyCode interactKey = KeyCode.F; // 상호작용 키를 F로 저장합니다.
    [SerializeField] private bool useTriggerRange = true; // true면 트리거 범위 안에 들어왔을 때만 F키를 받습니다.
    [SerializeField] private bool useDistanceRangeFallback = true; // true면 트리거가 안 잡혀도 거리로 한 번 더 검사합니다.
    [SerializeField] private float interactDistance = 3f; // 거리 검사 모드에서 문과 플레이어 사이 허용 거리를 정합니다.
    [Header("클릭 열기 설정")] // 인스펙터에서 클릭 입력 관련 옵션을 묶습니다.
    [SerializeField] private bool openByMouseClick = false; // true면 마우스 클릭으로도 문을 열 수 있게 합니다.
    [SerializeField] private bool canClickOnlyOnce = true; // true면 클릭은 한 번만 처리합니다.
    [Header("열린 뒤 끄기")] // 인스펙터에서 문 오브젝트 끄기 옵션을 묶습니다.
    [SerializeField] private bool disableThisDoorAfterSignal = false; // true면 스폰 신호 후 문 오브젝트를 끕니다.
    private bool hasClicked = false; // 클릭이 이미 처리되었는지 저장합니다.
    private bool playerInRange = false; // 플레이어가 문 앞 범위 안에 있는지 저장합니다.
    private Transform playerTransform; // Player 태그 오브젝트의 위치를 저장해서 거리 계산에 사용합니다.
    private bool hasOpenRequested = false; // 문 열기 요청이 이미 들어갔는지 저장합니다.
    private bool hasSpawnSignalSent = false; // 스폰 신호를 이미 보냈는지 저장합니다.
    private void Start() // 게임 시작 시 한 번 실행되는 함수입니다.
    { // 시작 준비 블록입니다.
        if (doorAnimator == null) doorAnimator = GetComponent<Animator>(); // 애니메이터 연결이 비어 있으면 같은 오브젝트에서 자동으로 찾습니다.
        FindPlayerIfMissing(); // 시작 시 플레이어를 한 번 찾아서 거리 검사 준비를 합니다.
    } // Start 블록의 끝 중괄호입니다.
    private void Update() // 매 프레임마다 F키 입력을 확인하는 함수입니다.
    { // 입력 감지 블록입니다.
        if (isDoorLocked) return; // 문이 잠긴 상태면 더 이상 열기 입력을 받지 않습니다.
        if (!openByFKey) return; // F키 열기 기능을 끈 상태면 아무 작업도 하지 않습니다.
        if (hasOpenRequested) return; // 이미 문 열기를 요청했으면 중복 입력을 막습니다.
        FindPlayerIfMissing(); // 플레이어 연결이 비어 있을 수 있으니 매 프레임 가볍게 다시 찾습니다.
        if (!IsPlayerInInteractRange()) return; // 트리거 또는 거리 조건을 만족하지 않으면 입력을 무시합니다.
        if (!Input.GetKeyDown(interactKey)) return; // 이번 프레임에 F키를 누르지 않았으면 종료합니다.
        OpenDoorNow(); // 조건이 모두 맞으면 문 열기 요청을 실행합니다.
    } // Update 블록의 끝 중괄호입니다.
    private void OnTriggerEnter(Collider other) // 다른 콜라이더가 문의 트리거 범위에 들어오면 호출됩니다.
    { // 범위 진입 감지 블록입니다.
        if (!other.CompareTag("Player")) return; // Player 태그가 아니면 무시합니다.
        playerInRange = true; // Player가 범위 안으로 들어왔으니 true로 바꿉니다.
    } // OnTriggerEnter 블록의 끝 중괄호입니다.
    private void OnTriggerExit(Collider other) // 다른 콜라이더가 문의 트리거 범위에서 나가면 호출됩니다.
    { // 범위 이탈 감지 블록입니다.
        if (!other.CompareTag("Player")) return; // Player 태그가 아니면 무시합니다.
        playerInRange = false; // Player가 범위 밖으로 나갔으니 false로 바꿉니다.
    } // OnTriggerExit 블록의 끝 중괄호입니다.
    private void FindPlayerIfMissing() // Player 태그 오브젝트를 찾는 함수입니다.
    { // 플레이어 자동 찾기 블록입니다.
        if (playerTransform != null) return; // 이미 찾은 상태면 다시 찾지 않고 끝냅니다.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Player 태그 오브젝트를 씬에서 찾습니다.
        if (playerObject == null) return; // 아직 못 찾았으면 다음 프레임에 다시 시도합니다.
        playerTransform = playerObject.transform; // 찾았으면 Transform을 저장해서 거리 계산에 사용합니다.
    } // FindPlayerIfMissing 블록의 끝 중괄호입니다.
    private bool IsPlayerInInteractRange() // F키 입력을 받을 수 있는 거리/범위인지 판단하는 함수입니다.
    { // 상호작용 범위 판단 블록입니다.
        if (useTriggerRange && playerInRange) return true; // 트리거 모드이고 트리거 안이라면 바로 true를 돌려줍니다.
        if (!useDistanceRangeFallback) return false; // 거리 백업 모드를 끄면 여기서 false를 돌려줍니다.
        if (playerTransform == null) return false; // 플레이어 위치를 모르면 거리 계산을 할 수 없어서 false입니다.
        float distance = Vector3.Distance(transform.position, playerTransform.position); // 문과 플레이어 사이 거리를 계산합니다.
        return distance <= interactDistance; // 계산된 거리가 설정 거리 이하면 상호작용 가능으로 true를 돌려줍니다.
    } // IsPlayerInInteractRange 블록의 끝 중괄호입니다.
    private void OnMouseDown() // 문 오브젝트를 마우스로 눌렀을 때 Unity가 자동 호출하는 함수입니다.
    { // 클릭 입력 처리 블록입니다.
        if (isDoorLocked) return; // 문이 잠긴 상태면 클릭으로도 열 수 없습니다.
        if (!openByMouseClick) return; // 클릭 열기 기능을 끈 상태면 아무 작업도 하지 않습니다.
        if (canClickOnlyOnce && hasClicked) return; // 클릭 1회 제한이 켜져 있고 이미 눌렀으면 무시합니다.
        hasClicked = true; // 클릭이 처리되었다고 기록합니다.
        OpenDoorNow(); // 클릭도 같은 문 열기 흐름으로 연결합니다.
    } // OnMouseDown 블록의 끝 중괄호입니다.
    public void CloseAndLockDoor() // RoomAutoLockDoors 같은 방 스크립트에서 호출하는 닫기·잠금 함수입니다.
    { // 방 진입 시 문을 닫고 열기를 막는 블록입니다.
        if (!allowRoomAutoCloseAndLock) return; // 이 문이 방 자동 잠금 대상이 아니면 아무것도 하지 않습니다.
        if (isDoorLocked) return; // 이미 잠긴 문이면 중복 처리를 하지 않습니다.
        isDoorLocked = true; // 먼저 잠궈서 닫는 동안에도 F키로 열리지 않게 합니다.
        if (doorAnimator != null && !string.IsNullOrEmpty(closeTriggerParam)) // 애니메이터와 닫기 트리거 이름이 있으면
        { // 닫기 트리거 실행 시도 블록입니다.
            if (HasTriggerParameter(closeTriggerParam)) // Animator에 Close 트리거가 실제로 있을 때만
            { // 트리거가 있을 때만 실행하는 블록입니다.
                doorAnimator.SetTrigger(closeTriggerParam); // 닫기 애니메이션 트리거를 실행합니다.
            } // 트리거 있음 블록의 끝 중괄호입니다.
            else // Close 트리거가 없을 때
            { // 트리거 없음 처리 블록입니다.
                if (requireCloseTriggerInAnimator) // 경고를 켜 둔 경우에만
                { // 개발자에게 Animator 설정 문제를 알리는 블록입니다.
                    Debug.LogWarning("[DoorOpenSignal] Close 트리거가 Animator에 없습니다. closeTriggerParam 이름을 확인하거나 닫기 클립을 추가해주세요."); // 설정 누락을 알려줍니다.
                } // 경고 블록의 끝 중괄호입니다.
            } // 트리거 없음 블록의 끝 중괄호입니다.
        } // 닫기 트리거 실행 시도 블록의 끝 중괄호입니다.
    } // CloseAndLockDoor 블록의 끝 중괄호입니다.
    private void OpenDoorNow() // 실제 문 열기 요청을 한 번만 처리하는 함수입니다.
    { // 문 열기 처리 블록입니다.
        if (isDoorLocked) return; // 잠긴 문은 열 수 없습니다.
        if (hasOpenRequested) return; // 이미 요청된 상태면 중복 실행을 막습니다.
        hasOpenRequested = true; // 지금부터 문 열기 요청이 처리되었다고 기록합니다.
        bool playedAnimation = false; // 애니메이션이 실제로 실행됐는지 저장하는 변수입니다.
        if (doorAnimator != null && HasTriggerParameter(openTriggerParam)) // 애니메이터가 있고 트리거 이름도 맞으면
        { // 애니메이션 트리거 실행 블록입니다.
            doorAnimator.SetTrigger(openTriggerParam); // 문 열기 트리거를 실행해서 애니메이션을 시작합니다.
            playedAnimation = true; // 애니메이션 실행 성공으로 표시합니다.
        } // 애니메이션 트리거 실행 블록의 끝 중괄호입니다.
        if (waitSpawnUntilAnimationEvent && playedAnimation) return; // 애니메이션 끝 이벤트를 기다리는 모드면 여기서 종료합니다.
        DoorOpened(); // 즉시 스폰 모드이거나 애니메이션이 없으면 바로 스폰 신호를 보냅니다.
    } // OpenDoorNow 블록의 끝 중괄호입니다.
    public void DoorOpened() // 애니메이션 이벤트나 즉시 처리에서 호출되는 스폰 신호 함수입니다.
    { // 스폰 신호 처리 블록입니다.
        if (!useMonsterSpawn) return; // 이 문이 스폰 문이 아니면 여기서 끝내고 스폰을 하지 않습니다.
        if (hasSpawnSignalSent) return; // 이미 스폰 신호를 보냈으면 중복 실행을 막습니다.
        if (roomMonsterSpawner == null) // 연결할 방 스폰러가 없으면
        { // 연결 누락 처리 블록입니다.
            Debug.LogWarning("[DoorOpenSignal] useMonsterSpawn이 true인데 roomMonsterSpawner가 비어 있습니다. 스폰 문이면 연결해주세요."); // 스폰 문 설정일 때만 연결 누락 원인을 알려줍니다.
            return; // 스폰 호출 대상이 없으니 종료합니다.
        } // 연결 누락 처리 블록의 끝 중괄호입니다.
        hasSpawnSignalSent = true; // 실제 스폰 신호를 보내기 직전에 완료 표시를 기록합니다.
        roomMonsterSpawner.OnDoorOpened(); // 방 스폰러의 스폰 시작 함수에 문 열림을 전달합니다.
        if (!disableThisDoorAfterSignal) return; // 문 오브젝트를 끄지 않을 설정이면 여기서 끝냅니다.
        gameObject.SetActive(false); // 문 오브젝트를 비활성화해서 추가 입력을 막습니다.
    } // DoorOpened 블록의 끝 중괄호입니다.
    private bool HasTriggerParameter(string paramName) // 애니메이터에 해당 트리거가 있는지 확인하는 함수입니다.
    { // 트리거 확인 블록입니다.
        if (doorAnimator == null) return false; // 애니메이터가 없으면 false를 돌려줍니다.
        if (string.IsNullOrEmpty(paramName)) return false; // 트리거 이름이 비어 있으면 false를 돌려줍니다.
        AnimatorControllerParameter[] parameters = doorAnimator.parameters; // 애니메이터 파라미터 목록을 가져옵니다.
        for (int i = 0; i < parameters.Length; i++) // 파라미터 목록을 앞에서부터 검사합니다.
        { // 반복 검사 블록입니다.
            if (parameters[i].type != AnimatorControllerParameterType.Trigger) continue; // 트리거 타입이 아니면 건너뜁니다.
            if (parameters[i].name != paramName) continue; // 이름이 다르면 건너뜁니다.
            return true; // 이름과 타입이 모두 맞으면 true를 돌려줍니다.
        } // 반복 검사 블록의 끝 중괄호입니다.
        return false; // 끝까지 못 찾으면 false를 돌려줍니다.
    } // HasTriggerParameter 블록의 끝 중괄호입니다.
} // DoorOpenSignal 클래스의 끝 중괄호입니다.
