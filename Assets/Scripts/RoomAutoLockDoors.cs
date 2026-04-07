using System.Collections; // 코루틴(지연 실행)을 쓰기 위해 가져옵니다.
using UnityEngine; // Unity 기본 기능을 쓰기 위해 가져옵니다.

public class RoomAutoLockDoors : MonoBehaviour // 플레이어가 방 구역에 들어오면 문을 닫고 잠그는 스크립트입니다.
{ // 이 클래스의 시작 중괄호입니다.
    [Header("잠글 문들")] // 인스펙터에서 문 목록을 보기 좋게 묶습니다.
    [SerializeField] private DoorOpenSignal[] doorsToCloseAndLock; // 닫고 잠글 문 스크립트들을 넣는 배열입니다.
    [Header("동작 설정")] // 인스펙터에서 동작 옵션을 묶습니다.
    [SerializeField] private float lockDelaySeconds = 0f; // 방에 들어온 뒤 몇 초 뒤에 잠길지 정합니다.
    [SerializeField] private bool lockOnlyOnce = true; // true면 한 방에 한 번만 잠금 처리를 합니다.
    private bool lockProcessStarted = false; // 잠금 예약이나 실행이 이미 시작됐는지 저장합니다.
    private void OnTriggerEnter(Collider other) // 다른 콜라이더가 이 방 트리거 안으로 들어오면 호출됩니다.
    { // 플레이어 진입 처리 블록입니다.
        if (!other.CompareTag("Player")) return; // Player 태그가 아니면 무시합니다.
        if (lockOnlyOnce && lockProcessStarted) return; // 한 번만 잠그는 모드에서 이미 시작했으면 무시합니다.
        if (lockOnlyOnce) lockProcessStarted = true; // 한 번만 모드일 때만 중복 진입을 막기 위해 true로 둡니다.
        if (lockDelaySeconds <= 0f) // 지연이 0이면
        { // 바로 잠금 처리 블록입니다.
            ApplyLockToAllDoors(); // 즉시 모든 문에 닫기·잠금을 적용합니다.
            return; // 여기서 종료합니다.
        } // 지연 없음 블록의 끝 중괄호입니다.
        StartCoroutine(LockAfterDelayCoroutine()); // 지연 시간이 있으면 코루틴으로 나중에 잠급니다.
    } // OnTriggerEnter 블록의 끝 중괄호입니다.
    private IEnumerator LockAfterDelayCoroutine() // 일정 시간 기다렸다가 잠그는 코루틴입니다.
    { // 지연 잠금 블록입니다.
        yield return new WaitForSeconds(lockDelaySeconds); // 설정한 초만큼 기다립니다.
        ApplyLockToAllDoors(); // 기다린 뒤 모든 문에 닫기·잠금을 적용합니다.
    } // LockAfterDelayCoroutine 블록의 끝 중괄호입니다.
    private void ApplyLockToAllDoors() // 배열에 넣은 문들에게 순서대로 닫기·잠금을 요청합니다.
    { // 문 순회 처리 블록입니다.
        if (doorsToCloseAndLock == null) return; // 배열이 없으면 할 일이 없어서 종료합니다.
        for (int i = 0; i < doorsToCloseAndLock.Length; i++) // 문 개수만큼 반복합니다.
        { // 한 문씩 처리하는 블록입니다.
            if (doorsToCloseAndLock[i] == null) continue; // 빈 칸은 건너뜁니다.
            doorsToCloseAndLock[i].CloseAndLockDoor(); // 닫기·잠금 함수를 호출합니다.
        } // 반복 블록의 끝 중괄호입니다.
    } // ApplyLockToAllDoors 블록의 끝 중괄호입니다.
} // RoomAutoLockDoors 클래스의 끝 중괄호입니다.
