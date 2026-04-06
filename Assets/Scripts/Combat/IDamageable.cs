// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;

// 체력을 깎을 수 있는 대상이 공통으로 가지는 약속(인터페이스)입니다.
// 핵심 요약: 플레이어와 몬스터가 같은 방식으로 맞을 수 있게 이름만 맞춥니다.
public interface IDamageable
{
    // damage만큼 체력을 줄입니다.
    // 핵심 요약: 공격 스크립트는 이 함수만 부르면 됩니다.
    void TakeDamage(int damage, GameObject attacker);
}
