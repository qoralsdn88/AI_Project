using UnityEngine; // 유니티 기본 기능을 쓰기 위해 가져옵니다.

public class MonsterAttackSimple : MonoBehaviour // 몬스터의 공격만 담당하는 스크립트입니다.
{
    [Header("연결 설정")] // 인스펙터에서 연결할 대상을 보기 쉽게 묶는 제목입니다.
    public MonsterDetectChaseSimple detectChase; // 탐지/추격 스크립트를 참조해서 공격 조건을 가져옵니다.
    public Animator animator; // 몬스터 애니메이터를 연결하는 변수입니다.

    [Header("공격 설정")] // 인스펙터에서 공격 관련 값을 모아 보여주는 제목입니다.
    public int attackDamage = 10; // 공격이 성공했을 때 플레이어에게 주는 체력 감소량입니다.
    public float attackCooldown = 1.2f; // 한 번 공격한 뒤 다음 공격까지 기다리는 시간입니다.
    public string attackTriggerParam = "Attack"; // (기본) 공격 재생에 사용할 애니메이터 trigger 파라미터 이름입니다.

    [Header("공격 모션 여러 개")] // 공격 애니메이션을 여러 개 섞어서 쓰는 설정을 모읍니다.
    public bool useAttackVariants = true; // 여러 공격 모션을 쓸지 여부입니다.
    public string[] attackVariantTriggerParams = new string[] { "Attack1", "Attack2" }; // Animator에 만들어둔 trigger 이름들입니다.
    public bool pickRandomVariant = true; // true면 랜덤, false면 목록 순서대로 공격합니다.

    private int variantIndex = 0; // 순서대로 공격할 때 다음으로 쓸 인덱스를 기억합니다.

    private float attackTimer = 0f; // 공격 대기 시간을 계산하는 내부 변수이며 0 이하면 공격 가능 상태입니다.

    void Start() // 게임 시작 시 한 번 실행되는 준비 함수입니다.
    {
        FindDetectChaseIfMissing(); // detectChase가 비어 있으면 같은 오브젝트에서 자동으로 찾아 연결합니다.
        FindAnimatorIfMissing(); // animator가 비어 있으면 같은 오브젝트에서 자동으로 찾아 연결합니다.
    }

    void Update() // 매 프레임마다 실행되며 공격 가능 여부를 판단합니다.
    {
        FindDetectChaseIfMissing(); // 연결이 비어 있을 때 다시 찾아 연결합니다.
        FindAnimatorIfMissing(); // 애니메이터 연결이 비어 있을 때 다시 찾아 연결합니다.
        if (detectChase == null) return; // 탐지/추격 스크립트가 없으면 아무 행동도 하지 않고 종료합니다.
        if (detectChase.player == null) return; // 플레이어를 못 찾은 상태면 아무 행동도 하지 않고 종료합니다.

        UpdateAttackTimer(); // 공격 대기 시간을 매 프레임 줄여 다음 공격 가능 시점을 관리합니다.
        TryAttackIfPossible(); // 탐지 상태와 거리 상태를 확인해 공격을 시도합니다.
    }

    private void FindDetectChaseIfMissing() // detectChase 변수가 비어 있을 때만 같은 오브젝트에서 찾아 넣는 함수입니다.
    {
        if (detectChase != null) return; // 이미 연결되어 있으면 다시 찾지 않고 종료합니다.
        detectChase = GetComponent<MonsterDetectChaseSimple>(); // 같은 오브젝트의 탐지/추격 스크립트를 찾아 저장합니다.
    }

    private void FindAnimatorIfMissing() // animator 변수가 비어 있을 때만 같은 오브젝트에서 찾아 넣는 함수입니다.
    {
        if (animator != null) return; // 이미 연결되어 있으면 다시 찾지 않고 종료합니다.
        animator = GetComponentInChildren<Animator>(); // 자식 오브젝트까지 포함해서 애니메이터를 찾아 저장합니다.
    }

    private void UpdateAttackTimer() // 공격 대기 시간을 매 프레임 줄여주는 함수입니다.
    {
        if (attackTimer > 0f) attackTimer -= Time.deltaTime; // 대기 시간이 남아 있으면 경과 시간만큼 줄입니다.
    }

    private void TryAttackIfPossible() // 공격 조건을 만족할 때만 공격을 실행하는 함수입니다.
    {
        if (!detectChase.IsDetected) return; // 플레이어를 아직 탐지하지 못했으면 공격하지 않습니다.
        if (!detectChase.IsInAttackRange) return; // 플레이어가 공격 거리 밖이면 공격하지 않습니다.
        if (attackTimer > 0f) return; // 아직 공격 대기 시간이 남아 있으면 이번 프레임 공격하지 않습니다.

        Vector3 toPlayer = detectChase.player.position - transform.position; // 몬스터에서 플레이어로 향하는 방향 벡터를 구합니다.
        toPlayer.y = 0f; // 위아래 차이는 무시해서 바닥 기준으로만 회전하게 만듭니다.
        detectChase.FaceDirection(toPlayer.normalized); // 공격 전에 플레이어를 바라보도록 회전을 맞춥니다.

        attackTimer = attackCooldown; // 지금 공격했으니 다음 공격까지 기다리는 시간을 다시 채웁니다.
        PlayAttackAnimation(); // 공격 타이밍에 공격 애니메이션을 재생합니다.

        SimplePlayerHealth hp = detectChase.player.GetComponent<SimplePlayerHealth>(); // 플레이어 체력 스크립트를 가져옵니다.
        if (hp != null) hp.TakeDamage(attackDamage); // 체력 스크립트가 있으면 설정한 데미지만큼 체력을 깎습니다.

        Debug.Log("몬스터가 플레이어를 공격했습니다."); // 콘솔에 공격 발생 로그를 출력해 확인을 쉽게 합니다.
    }

    private void PlayAttackAnimation() // 공격 애니메이션 트리거를 실행하는 함수입니다.
    {
        if (animator == null) return; // 애니메이터가 없으면 애니메이션 처리를 하지 않고 종료합니다.
        if (useAttackVariants) // 여러 공격 모션을 쓰는 경우입니다.
        {
            TryPlayVariantAnimation(); // 변형 모션 중 하나를 골라 트리거합니다.
            return; // 기본 단일 공격 로직은 여기서 생략합니다.
        }

        // 여러 모션을 안 쓰고 기본 1개만 쓰는 경우입니다.
        if (string.IsNullOrEmpty(attackTriggerParam)) return; // 파라미터 이름이 비어 있으면 실행하지 않고 종료합니다.
        if (!HasTriggerParameter(attackTriggerParam)) return; // trigger 파라미터가 없으면 실행하지 않고 종료합니다.
        animator.SetTrigger(attackTriggerParam); // 설정한 trigger 파라미터를 실행해 공격 애니메이션을 재생합니다.
    }

    private void TryPlayVariantAnimation() // 공격 모션 여러 개 중 하나를 골라 트리거하는 함수입니다.
    {
        if (attackVariantTriggerParams == null) return; // 목록이 없으면 실행하지 않습니다.
        if (attackVariantTriggerParams.Length == 0) return; // 비어 있으면 실행하지 않습니다.

        int chosenIndex = 0; // 이번에 고를 트리거 인덱스를 저장합니다.
        if (pickRandomVariant) // 랜덤으로 고르는 경우입니다.
        {
            chosenIndex = Random.Range(0, attackVariantTriggerParams.Length); // 0부터 목록 마지막까지 중 랜덤을 고릅니다.
        }
        else // 순서대로 고르는 경우입니다.
        {
            chosenIndex = Mathf.Clamp(variantIndex, 0, attackVariantTriggerParams.Length - 1); // 안전장치로 범위를 맞춥니다.
            variantIndex++; // 다음 번을 위해 인덱스를 증가시킵니다.
            if (variantIndex >= attackVariantTriggerParams.Length) variantIndex = 0; // 끝까지 가면 다시 처음으로 돌아갑니다.
        }

        string chosenParam = attackVariantTriggerParams[chosenIndex]; // 이번에 사용할 trigger 이름을 꺼냅니다.
        if (string.IsNullOrEmpty(chosenParam)) return; // 이름이 비어 있으면 실행하지 않습니다.
        if (!HasTriggerParameter(chosenParam)) return; // Animator에 trigger 파라미터가 없으면 실행하지 않습니다.
        animator.SetTrigger(chosenParam); // 고른 trigger를 눌러 해당 공격 애니메이션 상태로 넘어가게 합니다.
    }

    private bool HasTriggerParameter(string paramName) // trigger 파라미터가 Animator에 있는지 확인하는 함수입니다.
    {
        if (string.IsNullOrEmpty(paramName)) return false; // 이름이 비어 있으면 없는 것으로 처리합니다.
        AnimatorControllerParameter[] parameters = animator.parameters; // 현재 Animator에 등록된 파라미터 목록을 가져옵니다.
        for (int i = 0; i < parameters.Length; i++) // 목록을 앞에서부터 하나씩 확인합니다.
        {
            if (parameters[i].type != AnimatorControllerParameterType.Trigger) continue; // trigger 타입이 아니면 건너뜁니다.
            if (parameters[i].name != paramName) continue; // 이름이 다르면 건너뜁니다.
            return true; // 이름과 타입이 모두 맞는 파라미터를 찾았으니 true를 반환합니다.
        }

        return false; // 끝까지 찾지 못했으니 false를 반환합니다.
    }
}
