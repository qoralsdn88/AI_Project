using UnityEngine; // Unity 기본 기능을 쓰기 위해 가져옵니다.
public class DoorOpenSignal : MonoBehaviour // 문이 열렸을 때 방 스폰러에게 신호를 보내는 역할을 맡습니다.
{ // 이 클래스의 시작 중괄호입니다.
    [Header("연결할 방 스폰러")] // 인스펙터에서 연결 값을 보기 좋게 묶습니다.
    [SerializeField] private RoomMonsterSpawner roomMonsterSpawner; // 문이 연결할 방 스폰 스크립트입니다.
    [Header("열린 뒤 끄기")] // 인스펙터에서 문 오픈 후 처리 옵션을 묶습니다.
    [SerializeField] private bool disableThisDoorAfterSignal = false; // true면 신호 후 문 오브젝트를 꺼서 중복 호출을 줄입니다.
    [Header("클릭 열기 설정")] // 인스펙터에서 클릭 입력 관련 옵션을 묶습니다.
    [SerializeField] private bool openByMouseClick = true; // true면 문을 마우스로 눌렀을 때 DoorOpened를 자동 호출합니다.
    [SerializeField] private bool canClickOnlyOnce = true; // true면 한 번 클릭 후 다시 클릭해도 더 이상 실행하지 않습니다.
    private bool hasClicked = false; // 이미 클릭 처리했는지 기록해서 중복 실행을 막습니다.

    private void OnMouseDown() // 문 오브젝트를 마우스로 눌렀을 때 Unity가 자동 호출하는 함수입니다.
    { // 클릭으로 열기 입력을 처리하는 블록입니다.
        if (!openByMouseClick) return; // 클릭 열기 기능을 끈 상태면 아무 작업도 하지 않습니다.
        if (canClickOnlyOnce && hasClicked) return; // 한 번만 클릭 허용이면 두 번째부터는 무시합니다.
        hasClicked = true; // 클릭이 처리되었다고 표시해 중복 실행을 막습니다.
        DoorOpened(); // 기존 문 열림 함수를 호출해서 같은 흐름으로 스폰 신호를 보냅니다.
    } // OnMouseDown 블록이 끝납니다.

    public void DoorOpened() // 애니메이션 이벤트에서 이 이름으로 호출할 수 있는 함수입니다.
    { // 문 열림 신호를 처리하는 블록입니다.
        if (roomMonsterSpawner == null) // 연결할 방 스폰러가 없으면
        { // 스폰러가 비어 있을 때 경고를 찍고 끝내는 블록입니다.
            Debug.LogWarning("[DoorOpenSignal] roomMonsterSpawner가 비어 있습니다. DoorOpenSignal에서 연결해주세요."); // 연결 누락을 알려줍니다.
            return; // 스폰을 호출할 수 없으니 종료합니다.
        } // 스폰러 비어 있음 블록이 끝납니다.
        roomMonsterSpawner.OnDoorOpened(); // 방 스폰러의 스폰 시작 함수에 문 열림을 전달합니다.
        if (disableThisDoorAfterSignal) // 신호를 보낸 뒤 문을 끌지 정하는 옵션입니다.
        { // 문 오브젝트를 꺼서 중복 호출 가능성을 줄이는 블록입니다.
            gameObject.SetActive(false); // 문 오브젝트를 비활성화합니다.
        } // 문 비활성화 블록이 끝납니다.
    } // DoorOpened 블록이 끝납니다.
} // DoorOpenSignal 클래스의 끝 중괄호입니다.
