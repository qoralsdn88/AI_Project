// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;

// 이 스크립트를 붙이면 Animator가 같이 있어야 합니다.
[RequireComponent(typeof(Animator))]

// 캐릭터 이동 방향을 보고 Animator에 Idle/앞/뒤/좌/우 값을 넣는 스크립트입니다.
public class PlayerDirectionalAnimationController : MonoBehaviour
{
    // Animator에서 사용할 정수 파라미터 이름입니다.
    // 핵심 요약: Animator Controller의 `MoveState`와 이름을 똑같이 맞춰야 합니다.
    [SerializeField] private string moveStateParameter = "MoveState";

    // 이 값보다 느리면 Idle로 바꾸는 속도 기준입니다.
    // 핵심 요약: 너무 작게 두면 미세 움직임에도 걷기 애니메이션이 나옵니다.
    [SerializeField] private float minSpeedToWalk = 0.05f;

    // 현재 오브젝트의 Animator 컴포넌트를 저장합니다.
    // 핵심 요약: Animator에 파라미터를 SetInteger로 넣기 위해 필요합니다.
    private Animator animator;

    // 현재 오브젝트의 CharacterController를 저장합니다.
    // 핵심 요약: velocity를 읽어 실제 이동 방향을 계산하기 위해 필요합니다.
    private CharacterController characterController;

    // 공격 중일 때는 걷기 상태를 덮어쓰지 않기 위해 참조합니다.
    // 핵심 요약: 비어 있으면 자동으로 같은 오브젝트에서 찾습니다.
    [SerializeField] private PlayerMeleeCombat meleeCombat;

    [SerializeField] private SimplePlayerHealth playerHealth;

    // 오브젝트가 준비되자마자 실행되는 함수입니다.
    // 핵심 요약: Awake에서 컴포넌트를 찾아서 Update에서 안전하게 쓰게 합니다.
    private void Awake()
    {
        // Animator 컴포넌트를 가져옵니다.
        animator = GetComponent<Animator>(); // Animator가 없으면 여기서 null일 수 있습니다.
        // CharacterController 컴포넌트를 가져옵니다.
        characterController = GetComponent<CharacterController>(); // PlayerSimpleMover에서 쓰는 것과 같은 컨트롤러를 기대합니다.
        // 전투 스크립트가 비어 있으면 같은 오브젝트에서 찾습니다.
        if (meleeCombat == null) { meleeCombat = PlayerMeleeCombat.Resolve(transform); }
        if (playerHealth == null) { playerHealth = SimplePlayerHealth.Resolve(transform); }

        // Animator가 없으면 에러를 남깁니다.
        if (animator == null) { Debug.LogError("[PlayerDirectionalAnimationController] Animator가 없습니다. Player에 Animator 컴포넌트를 추가해주세요."); } // 찾기 실패 시 바로 원인을 알려줍니다.
        // CharacterController가 없으면 에러를 남깁니다.
        if (characterController == null) { Debug.LogError("[PlayerDirectionalAnimationController] CharacterController가 없습니다. Player에 CharacterController를 추가해주세요."); } // 이동 방향 계산에 필요합니다.
    }

    // 매 프레임마다 실행되는 함수입니다.
    // 핵심 요약: 현재 속도를 보고 MoveState를 Idle/앞/뒤/좌/우로 결정합니다.
    private void Update()
    {
        // Animator나 CharacterController가 없으면 더 할 수 없으니 중단합니다.
        if (animator == null || characterController == null) { return; } // 방어 코드입니다.

        // 사망 후에는 MoveState를 건드리지 않습니다. Dead 상태 전환 직후 파라미터가 경쟁하는 것을 막습니다.
        if (playerHealth != null && playerHealth.IsDead) { return; }

        // 공격 중에는 걷기 상태를 고정하지 않으면 Combo 중 Idle을 잠깐 지날 때
        // 예전 MoveState(예: 전진)로 바로 튕겨 콤보 트리거와 경쟁할 수 있습니다.
        if (meleeCombat != null && meleeCombat.IsAttacking)
        {
            animator.SetInteger(moveStateParameter, 0);
            return;
        }

        // 피격·사망 연출 중에는 이동 블렌드 트리가 피격 모션과 싸우지 않게 Idle(0)로 고정합니다.
        if (playerHealth != null && playerHealth.IsActionLocked)
        {
            animator.SetInteger(moveStateParameter, 0);
            return;
        }

        // CharacterController가 가진 현재 속도를 가져옵니다.
        Vector3 worldVelocity = characterController.velocity; // 월드 방향 속도입니다.

        // 위아래(y축) 속도는 애니메이션 방향 판단에서 빼고 평면만 씁니다.
        worldVelocity.y = 0f; // 낙하 중에도 걷기 방향만 판단하지 않게 합니다.

        // 평면 속도의 크기를 계산합니다.
        float speed = worldVelocity.magnitude; // 속도가 얼마나 빠른지 숫자로 봅니다.

        // 속도가 너무 작으면 Idle입니다.
        if (speed < minSpeedToWalk)
        {
            // 0은 Idle 상태로 맞춥니다.
            animator.SetInteger(moveStateParameter, 0); // Animator의 Idle 상태 조건에 써야 합니다.
            // Idle이면 더 계산할 필요가 없으니 끝냅니다.
            return; // 다음 프레임으로 넘깁니다.
        }

        // 캐릭터 기준(로컬)으로 속도 방향을 바꿉니다.
        // 핵심 요약: 로컬 X는 좌/우, 로컬 Z는 앞/뒤가 됩니다.
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity); // 몸이 향하는 방향 기준으로 바꿉니다.

        // 로컬 X축 값(좌/우)을 꺼냅니다.
        float localX = localVelocity.x; // 양수면 오른쪽, 음수면 왼쪽입니다.
        // 로컬 Z축 값(앞/뒤)을 꺼냅니다.
        float localZ = localVelocity.z; // 양수면 앞, 음수면 뒤입니다.

        // X 방향 움직임이 Z 방향보다 더 크면 좌/우로 보고,
        // 아니면 앞/뒤로 봅니다.
        int moveState;
        if (Mathf.Abs(localX) > Mathf.Abs(localZ))
        {
            // 오른쪽으로 움직이면 4를 줍니다.
            moveState = localX > 0f ? 4 : 3; // 4=Right, 3=Left 입니다.
        }
        else
        {
            // 앞으로 움직이면 1, 뒤로 움직이면 2입니다.
            moveState = localZ > 0f ? 1 : 2; // 1=Forward, 2=Backward 입니다.
        }

        // Animator에 지금 상태 값을 넣습니다.
        animator.SetInteger(moveStateParameter, moveState); // Animator이 이 값으로 상태를 바꿉니다.
    }
}

