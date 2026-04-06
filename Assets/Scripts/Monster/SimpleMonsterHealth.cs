// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;

// 몬스터 체력을 관리하고 플레이어 공격을 받아들이는 간단한 스크립트입니다.
// 핵심 요약: IDamageable을 구현해 칼 충돌 스크립트와 연결됩니다.
public class SimpleMonsterHealth : MonoBehaviour, IDamageable
{
    // 최대 체력 값입니다.
    // 핵심 요약: maxHealth는 시작 체력과 최댓값을 같이 정합니다.
    [SerializeField] private int maxHealth = 30;

    // 지금 남은 체력 값입니다.
    // 핵심 요약: currentHealth가 0이 되면 비활성 처리를 할 수 있습니다.
    private int currentHealth;

    // 준비 단계에서 체력을 채웁니다.
    // 핵심 요약: Awake에서 시작 체력을 maxHealth로 맞춥니다.
    private void Awake()
    {
        // 시작할 때 현재 체력을 최대로 맞춥니다.
        currentHealth = maxHealth;
    }

    // 외부에서 데미지를 줄 때 호출하는 함수입니다.
    // 핵심 요약: attacker는 로그용이며 나중에 통계에도 쓸 수 있습니다.
    public void TakeDamage(int damage, GameObject attacker)
    {
        // 데미지가 0 이하면 아무 일도 하지 않습니다.
        if (damage <= 0) return;

        // 체력을 줄입니다.
        currentHealth -= damage;

        // 디버그용으로 남은 체력을 콘솔에 남깁니다.
        Debug.Log($"[SimpleMonsterHealth] {name} 체력: {currentHealth} / {maxHealth} (공격자: {(attacker != null ? attacker.name : "없음")})");

        // 체력이 0 이하가 되면 비활성 처리로 간단히 죽은 처리를 합니다.
        if (currentHealth <= 0)
        {
            // 오브젝트를 꺼서 씬에서 사라진 것처럼 보이게 합니다.
            gameObject.SetActive(false);
        }
    }
}
