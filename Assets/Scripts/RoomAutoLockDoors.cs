using System.Collections;
using UnityEngine;

/// <summary>
/// 플레이어가 방 트리거에 진입하면 연결된 문들을 닫고 잠급니다.
/// </summary>
public class RoomAutoLockDoors : MonoBehaviour
{
    [Header("잠글 문들")]
    [SerializeField] private DoorOpenSignal[] doorsToCloseAndLock;

    [Header("동작 설정")]
    [SerializeField] private float lockDelaySeconds;
    [SerializeField] private bool lockOnlyOnce = true;

    private bool lockProcessStarted;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (lockOnlyOnce && lockProcessStarted) return;
        if (lockOnlyOnce) lockProcessStarted = true;

        if (lockDelaySeconds <= 0f)
        {
            ApplyLockToAllDoors();
            return;
        }

        StartCoroutine(LockAfterDelayCoroutine());
    }

    private IEnumerator LockAfterDelayCoroutine()
    {
        yield return new WaitForSeconds(lockDelaySeconds);
        ApplyLockToAllDoors();
    }

    private void ApplyLockToAllDoors()
    {
        if (doorsToCloseAndLock == null) return;
        for (int i = 0; i < doorsToCloseAndLock.Length; i++)
        {
            if (doorsToCloseAndLock[i] != null) doorsToCloseAndLock[i].CloseAndLockDoor();
        }
    }
}
