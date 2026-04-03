using UnityEngine; // 유니티 기본 기능을 쓰기 위해 가져옵니다.

public class SimplePlayerHealth : MonoBehaviour // 플레이어 체력을 관리하는 간단한 스크립트입니다.
{
    public int maxHp = 100; // 플레이어 최대 체력입니다.
    public int currentHp; // 현재 체력을 저장하는 변수입니다.

    void Start() // 게임 시작 시 한 번 실행되는 준비 함수입니다.
    {
        currentHp = maxHp; // 시작할 때 현재 체력을 최대 체력으로 맞춥니다.
    }

    public void TakeDamage(int damage) // 외부에서 데미지를 받았을 때 호출하는 함수입니다.
    {
        currentHp -= damage; // 받은 데미지만큼 현재 체력을 줄입니다.
        if (currentHp < 0) currentHp = 0; // 체력이 음수가 되지 않게 0 아래로는 막습니다.
        Debug.Log("플레이어 현재 체력: " + currentHp); // 체력이 얼마나 남았는지 콘솔에 출력합니다.

        if (currentHp == 0) // 체력이 0이면
        {
            Debug.Log("플레이어가 쓰러졌습니다."); // 사망 상태를 콘솔로 알립니다.
        }
    }
}