using UnityEngine; // 유니티 기본 기능을 쓰기 위해 가져옵니다.

public class MonsterDetectChaseSimple : MonoBehaviour // 몬스터의 탐지와 추격만 담당하는 스크립트입니다.
{
    [Header("대상 설정")] // 인스펙터에서 보기 쉽게 묶는 제목입니다.
    public Transform player; // 추격할 대상인 플레이어 위치를 담는 변수입니다.
    public Animator animator; // 몬스터 애니메이터를 연결하는 변수입니다.

    [Header("이동/거리 설정")] // 인스펙터에서 이동과 거리 값을 모아 보여주는 제목입니다.
    public float moveSpeed = 3f; // 몬스터가 1초에 이동하는 속도입니다.
    public float detectRange = 10f; // 플레이어가 이 거리 안에 들어오면 추격을 시작합니다.
    public float attackRange = 1.8f; // 플레이어가 이 거리 안에 들어오면 공격 상태로 봅니다.
    public float monsterSpacingRadius = 1.1f; // 다른 몬스터와 최소로 벌리고 싶은 반경입니다.
    public float monsterSpacingStrength = 1.8f; // 겹침을 풀기 위해 옆으로 피하는 힘의 크기입니다.
    public LayerMask monsterLayerMask = ~0; // 겹침 검사에 사용할 레이어 마스크입니다.
    public string moveSpeedParam = "MoveSpeed"; // 이동 속도를 전달할 애니메이터 float 파라미터 이름입니다.
    public string isMovingParam = "IsMoving"; // 이동 중인지 전달할 애니메이터 bool 파라미터 이름입니다.

    public bool IsDetected { get; private set; } // 플레이어를 찾았는지 여부를 외부 스크립트가 읽을 수 있게 공개합니다.
    public bool IsInAttackRange { get; private set; } // 플레이어가 공격 거리 안인지 여부를 외부 스크립트가 읽을 수 있게 공개합니다.
    private Collider[] nearbyMonsterBuffer = new Collider[16]; // 주변 몬스터를 임시로 담아 분리 계산에 쓰는 버퍼입니다.

    void Start() // 게임 시작 시 한 번 실행되는 준비 함수입니다.
    {
        FindPlayerIfMissing(); // player가 비어 있으면 태그로 플레이어를 찾아 연결합니다.
        FindAnimatorIfMissing(); // animator가 비어 있으면 같은 오브젝트에서 자동으로 찾아 연결합니다.
    }

    void Update() // 매 프레임마다 실행되며 탐지와 추격을 반복합니다.
    {
        FindPlayerIfMissing(); // 플레이어 연결이 비어 있을 때 다시 찾아 연결합니다.
        FindAnimatorIfMissing(); // 애니메이터 연결이 비어 있을 때 다시 찾아 연결합니다.
        if (player == null) return; // 플레이어를 못 찾은 상태면 아무 행동도 하지 않고 종료합니다.

        UpdateDistanceState(); // 플레이어와의 거리로 탐지 상태와 공격 거리 상태를 갱신합니다.
        RunChaseIfNeeded(); // 탐지되었고 공격 거리 밖일 때만 추격 이동을 실행합니다.
    }

    private void FindPlayerIfMissing() // player 변수가 비어 있을 때만 플레이어를 찾아 넣는 함수입니다.
    {
        if (player != null) return; // 이미 연결되어 있으면 다시 찾지 않고 종료합니다.

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Player 태그가 붙은 오브젝트를 찾습니다.
        if (playerObject != null) player = playerObject.transform; // 찾았으면 그 오브젝트의 위치 정보를 player에 저장합니다.
    }

    private void FindAnimatorIfMissing() // animator 변수가 비어 있을 때만 같은 오브젝트에서 찾아 넣는 함수입니다.
    {
        if (animator != null) return; // 이미 연결되어 있으면 다시 찾지 않고 종료합니다.
        animator = GetComponentInChildren<Animator>(); // 자식 오브젝트까지 포함해서 애니메이터를 찾아 저장합니다.
    }

    private void UpdateDistanceState() // 거리 계산으로 현재 탐지 상태와 공격 거리 상태를 정하는 함수입니다.
    {
        float distance = Vector3.Distance(transform.position, player.position); // 몬스터와 플레이어 사이 거리를 계산합니다.
        IsDetected = distance <= detectRange; // 탐지 거리에 들어오면 true, 벗어나면 false가 됩니다.
        IsInAttackRange = distance <= attackRange; // 공격 거리에 들어오면 true, 벗어나면 false가 됩니다.
    }

    private void RunChaseIfNeeded() // 추격이 필요한 조건일 때만 이동을 실행하는 함수입니다.
    {
        if (!IsDetected) // 탐지되지 않았으면
        {
            SetMoveAnimation(false, 0f); // 걷기 애니메이션을 끄고 속도를 0으로 설정합니다.
            return; // 이동하지 않고 종료합니다.
        }

        if (IsInAttackRange) // 공격 거리 안이면
        {
            SetMoveAnimation(false, 0f); // 이동을 멈췄다는 애니메이션 상태로 바꿉니다.
            return; // 공격 스크립트가 처리하도록 이동하지 않고 종료합니다.
        }

        ChasePlayer(); // 플레이어 방향으로 이동을 실행합니다.
    }

    private void ChasePlayer() // 플레이어 방향으로 이동하는 함수입니다.
    {
        Vector3 direction = player.position - transform.position; // 몬스터에서 플레이어로 향하는 방향 벡터를 구합니다.
        direction.y = 0f; // 위아래 차이는 무시해서 바닥에서만 움직이게 만듭니다.
        if (direction.sqrMagnitude <= 0.0001f) return; // 방향 길이가 거의 0이면 떨림 방지를 위해 이동하지 않습니다.

        Vector3 chaseDirection = direction.normalized; // 플레이어를 향한 기본 이동 방향을 계산합니다.
        Vector3 separateDirection = CalculateMonsterSeparationDirection(); // 주변 몬스터와 겹침을 줄이기 위한 분리 방향을 계산합니다.
        Vector3 mixedDirection = chaseDirection + separateDirection * monsterSpacingStrength; // 추격 방향과 분리 방향을 섞어 최종 이동 방향을 만듭니다.
        mixedDirection.y = 0f; // 최종 이동도 바닥 기준으로만 처리합니다.
        if (mixedDirection.sqrMagnitude <= 0.0001f) mixedDirection = chaseDirection; // 섞은 결과가 너무 작으면 기본 추격 방향을 사용합니다.
        Vector3 moveDirection = mixedDirection.normalized; // 최종 방향 길이를 1로 맞춰 속도 계산을 일정하게 만듭니다.
        transform.position += moveDirection * moveSpeed * Time.deltaTime; // 현재 위치에 이동량을 더해 몬스터를 전진시킵니다.
        FaceDirection(moveDirection); // 이동 방향을 바라보도록 회전을 맞춥니다.
        SetMoveAnimation(true, moveSpeed); // 이동 중 애니메이션을 켜고 현재 이동 속도를 전달합니다.
    }

    private Vector3 CalculateMonsterSeparationDirection() // 주변 몬스터와 겹치지 않게 옆으로 피할 방향을 계산하는 함수입니다.
    {
        float radius = Mathf.Max(0.1f, monsterSpacingRadius); // 반경이 너무 작아지지 않게 최소값을 보장합니다.
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, nearbyMonsterBuffer, monsterLayerMask, QueryTriggerInteraction.Ignore); // 주변의 몬스터 후보 콜라이더를 버퍼에 담습니다.
        if (hitCount <= 0) return Vector3.zero; // 주변에 후보가 없으면 분리 방향 없이 0 벡터를 반환합니다.

        Vector3 sum = Vector3.zero; // 피해야 할 방향을 누적할 합계 벡터입니다.
        int used = 0; // 실제로 분리 계산에 사용한 몬스터 수를 세는 카운터입니다.
        for (int i = 0; i < hitCount; i++) // 감지된 후보를 하나씩 검사합니다.
        {
            Collider c = nearbyMonsterBuffer[i]; // 현재 후보 콜라이더를 꺼냅니다.
            if (c == null) continue; // 콜라이더가 비어 있으면 건너뜁니다.
            MonsterDetectChaseSimple other = c.GetComponentInParent<MonsterDetectChaseSimple>(); // 같은 종류의 몬스터 이동 스크립트를 부모 기준으로 찾습니다.
            if (other == null) continue; // 몬스터 스크립트가 없으면 건너뜁니다.
            if (other == this) continue; // 자기 자신이면 건너뜁니다.

            Vector3 away = transform.position - other.transform.position; // 상대 몬스터에서 나 자신 쪽으로 도망 방향 벡터를 구합니다.
            away.y = 0f; // 바닥 기준 계산을 위해 y를 제거합니다.
            float sqr = away.sqrMagnitude; // 거리 제곱을 계산해 루트 연산을 줄입니다.
            if (sqr <= 0.0001f) continue; // 위치가 거의 같으면 불안정해질 수 있어 건너뜁니다.
            if (sqr > radius * radius) continue; // 반경보다 멀면 분리 계산 대상에서 제외합니다.

            float distance = Mathf.Sqrt(sqr); // 실제 거리를 계산합니다.
            float weight = 1f - Mathf.Clamp01(distance / radius); // 가까울수록 더 크게 피하도록 가중치를 계산합니다.
            sum += away.normalized * weight; // 도망 방향을 가중치만큼 누적합니다.
            used++; // 사용한 개수를 1 증가시킵니다.
        }

        if (used == 0) return Vector3.zero; // 유효 후보를 하나도 못 찾았으면 분리 방향 없이 종료합니다.
        Vector3 average = sum / used; // 누적 방향을 평균 내어 과도한 흔들림을 줄입니다.
        if (average.sqrMagnitude <= 0.0001f) return Vector3.zero; // 평균 방향이 너무 작으면 0 벡터를 반환합니다.
        return average.normalized; // 최종 분리 방향을 정규화해서 반환합니다.
    }

    public void FaceDirection(Vector3 direction) // 다른 스크립트도 재사용할 수 있게 바라보기 함수를 공개합니다.
    {
        if (direction.sqrMagnitude <= 0.0001f) return; // 방향 벡터가 너무 작으면 회전 계산을 하지 않고 종료합니다.
        Quaternion lookRotation = Quaternion.LookRotation(direction); // 방향 벡터를 회전값으로 바꿉니다.
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime); // 회전을 부드럽게 목표 방향으로 맞춥니다.
    }

    private void SetMoveAnimation(bool isMoving, float speedValue) // 이동 관련 애니메이터 값을 한 곳에서 관리하는 함수입니다.
    {
        if (animator == null) return; // 애니메이터가 없으면 애니메이션 처리를 하지 않고 종료합니다.
        if (HasBoolParameter(isMovingParam)) animator.SetBool(isMovingParam, isMoving); // bool 파라미터가 실제로 있을 때만 이동 여부를 전달합니다.
        if (HasFloatParameter(moveSpeedParam)) animator.SetFloat(moveSpeedParam, speedValue); // float 파라미터가 실제로 있을 때만 이동 속도를 전달합니다.
    }

    private bool HasBoolParameter(string paramName) // bool 파라미터가 Animator에 있는지 확인하는 함수입니다.
    {
        if (string.IsNullOrEmpty(paramName)) return false; // 이름이 비어 있으면 없는 것으로 처리합니다.
        AnimatorControllerParameter[] parameters = animator.parameters; // 현재 Animator에 등록된 파라미터 목록을 가져옵니다.
        for (int i = 0; i < parameters.Length; i++) // 목록을 앞에서부터 하나씩 확인합니다.
        {
            if (parameters[i].type != AnimatorControllerParameterType.Bool) continue; // bool 타입이 아니면 건너뜁니다.
            if (parameters[i].name != paramName) continue; // 이름이 다르면 건너뜁니다.
            return true; // 이름과 타입이 모두 맞는 파라미터를 찾았으니 true를 반환합니다.
        }

        return false; // 끝까지 찾지 못했으니 false를 반환합니다.
    }

    private bool HasFloatParameter(string paramName) // float 파라미터가 Animator에 있는지 확인하는 함수입니다.
    {
        if (string.IsNullOrEmpty(paramName)) return false; // 이름이 비어 있으면 없는 것으로 처리합니다.
        AnimatorControllerParameter[] parameters = animator.parameters; // 현재 Animator에 등록된 파라미터 목록을 가져옵니다.
        for (int i = 0; i < parameters.Length; i++) // 목록을 앞에서부터 하나씩 확인합니다.
        {
            if (parameters[i].type != AnimatorControllerParameterType.Float) continue; // float 타입이 아니면 건너뜁니다.
            if (parameters[i].name != paramName) continue; // 이름이 다르면 건너뜁니다.
            return true; // 이름과 타입이 모두 맞는 파라미터를 찾았으니 true를 반환합니다.
        }

        return false; // 끝까지 찾지 못했으니 false를 반환합니다.
    }
}
