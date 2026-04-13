using UnityEngine; // 유니티 기본 기능을 쓰기 위해 가져옵니다. — 요약: 이 파일은 유니티 API를 사용하기 위해 필요한 한 줄입니다.

public class MonsterDetectChaseSimple : MonoBehaviour // 몬스터의 탐지와 추격만 담당하는 스크립트입니다. — 요약: 이 컴포넌트는 거리 판정과 이동만 맡고 공격은 다른 스크립트가 맡도록 나눕니다.
{
    [Header("대상 설정")] // 인스펙터에서 보기 쉽게 묶는 제목입니다. — 요약: 아래 변수들이 무엇을 연결하는지 한눈에 보이게 라벨을 붙입니다.
    public Transform player; // 추격할 대상인 플레이어 위치를 담는 변수입니다. — 요약: 이동 목표 좌표는 항상 player가 가리키는 월드 위치에서 읽습니다.
    public Animator animator; // 몬스터 애니메이터를 연결하는 변수입니다. — 요약: 걷기 여부와 속도 숫자를 animator로 넘겨 애니메이션과 발걸음을 맞춥니다.

    [Header("이동/거리 설정")] // 인스펙터에서 이동과 거리 값을 모아 보여주는 제목입니다. — 요약: 속도와 거리 관련 숫자는 여기만 보면 조정할 수 있게 모아 둡니다.
    public float moveSpeed = 3f; // 몬스터가 1초에 이동하는 속도입니다. — 요약: 한 프레임 이동량은 moveSpeed에 Time.deltaTime을 곱한 만큼입니다.
    public float detectRange = 10f; // 플레이어가 이 거리 안에 들어오면 추격을 시작합니다. — 요약: IsDetected는 플레이어와의 거리가 detectRange 이하일 때 참이 됩니다.
    public float attackRange = 1.8f; // 플레이어가 이 거리 안에 들어오면 공격 상태로 봅니다. — 요약: IsInAttackRange가 참이면 이 스크립트는 이동 대신 제자리에서 애니메이션만 정리합니다.
    public float monsterSpacingRadius = 1.1f; // 다른 몬스터와 최소로 벌리고 싶은 반경입니다. — 요약: 이 반경 안에 들어온 다른 몬스터에게만 옆으로 피하는 계산을 합니다.
    public float monsterSpacingStrength = 1.8f; // 겹침을 풀기 위해 옆으로 피하는 힘의 크기입니다. — 요약: 숫자가 클수록 추격 방향보다 벌어지는 쪽 비중이 커집니다.
    public float separationSmoothTime = 0.15f; // 옆으로 피하는 방향이 바뀌는 속도를 부드럽게 만드는 시간 값입니다. — 요약: 값이 클수록 이웃 몬스터 때문에 방향이 덜 자주 튑니다(떨림이 줄어듭니다).
    public float moveFacingSmoothTime = 0.12f; // 몸이 플레이어를 바라보는 속도를 부드럽게 만드는 시간 값입니다. — 요약: 회전은 이동보다 약간 느리게 따라가서 프레임마다 튀는 느낌을 줄입니다.
    public float rotateSlerpSharpness = 10f; // 목표 각도로 돌아가는 회전 예민함 숫자입니다(클수록 더 빨리 맞춥니다). — 요약: Quaternion.Slerp의 보간 비율에 Time.deltaTime과 이 값을 곱해 넣습니다.

    [Header("포위(앞몬스터 옆돌기) 설정")] // 앞에 동료가 막을 때 옆으로 돌아가며 포위하듯 움직이는 값입니다. — 요약: 추격 방향만 쓰면 뒤 몬스터가 제자리에서 밀기만 하는 현상을 줄입니다.
    public float flankPathCheckRadius = 2.8f; // 앞에 막는 몬스터가 있는지 찾을 때 쓰는 원 반지름입니다. — 요약: 이 반경 안의 동료만 "플레이어 쪽 길목" 후보로 봅니다.
    public float flankInFrontDot = 0.28f; // 내가 플레이어를 볼 때 앞쪽으로 몇 치우쳐도 "앞에 있다"고 볼지 낮은 한계입니다. — 요약: 나에서 동료로 가는 방향과 플레이어 방향의 맞닿음 숫자가 이 값 이상이면 앞에 있다고 봅니다(1에 가까울수록 정면).
    public float flankMinForward = 0.35f; // 동료가 플레이어 방향으로 나보다 앞에 최소 얼마나 있어야 막는 존재로 볼지입니다(미터 느낌). — 요약: 투영 거리가 너무 짧으면 옆돌기를 켜지 않아 불필요한 빙 돌기를 막습니다.
    public float flankCloserThanPlayerRatio = 0.94f; // 동료까지 거리가 나~플레이어 거리의 몇 배 이하일 때만 사이에 낀 것으로 볼지입니다. — 요약: 1에 가깝게 두면 거의 플레이어 앞에 있을 때만 옆돌기가 켜집니다.
    public float flankStrength = 1.2f; // 옆으로 도는 방향을 섞을 때 세기입니다. — 요약: chaseDirection과 같은 방식으로 더해지며 숫자가 클수록 옆걸음 비중이 커집니다.
    public float flankSmoothTime = 0.14f; // 옆돌기 방향이 바뀔 때 부드럽게 만드는 시간입니다. — 요약: separationSmoothTime과 비슷하게 급한 좌우 전환을 줄입니다.

    [Header("맵 관통 방지 설정")] // 벽이나 장애물을 뚫고 지나가지 않게 하는 설정입니다. — 요약: 이동 전에 캡슐 모양으로 다음 위치 겹침을 검사합니다.
    public LayerMask mapBlockLayerMask = ~0; // 벽 판정에 사용할 레이어 마스크입니다. — 요약: 벽/장애물 레이어만 켜 두면 몬스터가 맵을 관통하지 않습니다.
    public float bodyRadius = 0.35f; // 몬스터 몸 반지름입니다. — 요약: 이 반지름이 클수록 벽을 더 일찍 감지합니다.
    public float bodyHeight = 1.7f; // 몬스터 몸 높이입니다. — 요약: 캡슐의 위아래 점 계산에 같이 쓰입니다.
    public float bodyCenterY = 0.9f; // 몬스터 몸 중심 높이입니다. — 요약: transform.position 기준으로 캡슐이 어느 높이에 있는지 맞춥니다.
    public float wallSkin = 0.03f; // 벽과 아주 살짝 떨어지게 유지하는 간격입니다. — 요약: 0보다 약간 크게 두면 벽 떨림이 줄어듭니다.

    public LayerMask monsterLayerMask = ~0; // 겹침 검사에 사용할 레이어 마스크입니다. — 요약: 이 마스크에 포함된 콜라이더만 "다른 몬스터 후보"로 셉니다.
    public string moveSpeedParam = "MoveSpeed"; // 이동 속도를 전달할 애니메이터 float 파라미터 이름입니다. — 요약: Animator 안에 같은 이름의 숫자 파라미터가 있어야 속도가 전달됩니다.
    public string isMovingParam = "IsMoving"; // 이동 중인지 전달할 애니메이터 bool 파라미터 이름입니다. — 요약: Animator 안에 같은 이름의 참거짓 파라미터가 있어야 걷기 전환이 됩니다.

    public bool IsDetected { get; private set; } // 플레이어를 찾았는지 여부를 외부 스크립트가 읽을 수 있게 공개합니다. — 요약: 다른 코드는 이 값만 읽고, 바꾸기는 이 스크립트 안에서만 합니다.
    public bool IsInAttackRange { get; private set; } // 플레이어가 공격 거리 안인지 여부를 외부 스크립트가 읽을 수 있게 공개합니다. — 요약: 공격 스크립트가 이 값을 보고 근접 공격을 허용할지 판단할 수 있습니다.
    private Collider[] nearbyMonsterBuffer = new Collider[16]; // 주변 몬스터를 임시로 담아 분리 계산에 쓰는 버퍼입니다. — 요약: 매번 새 목록을 만들지 않고 같은 배열을 재사용해 찌꺼기 생성을 줄입니다.

    private Vector3 smoothedSeparationDirection; // 지난 프레임까지 부드럽게 이어 온 분리 방향 값을 저장합니다. — 요약: 급한 분리 방향과 이 변수를 SmoothDamp로 섞어 튀는 움직임을 줄입니다.
    private Vector3 separationSmoothVelocity; // 분리 방향을 부드럽게 할 때 SmoothDamp가 쓰는 내부 보조 벡터입니다. — 요약: 매 프레임 자동으로 바뀌므로 직접 대입하지 않고 ref로만 넘깁니다.
    private Vector3 smoothedFacingDirection; // 지난 프레임까지 부드럽게 이어 온 바라보기 방향을 저장합니다. — 요약: 플레이어 방향으로 즉시 튀지 않고 이 값이 목표를 쫓아 회전을 만듭니다.
    private Vector3 facingSmoothVelocity; // 바라보기 방향을 부드럽게 할 때 SmoothDamp가 쓰는 내부 보조 벡터입니다. — 요약: separationSmoothVelocity와 따로 두어 서로 덮어쓰지 않게 합니다.
    private Vector3 smoothedFlankDirection; // 옆돌기 방향을 프레임마다 부드럽게 이어 저장하는 벡터입니다. — 요약: 급한 옆돌기 목표를 SmoothDamp로 담아 몸이 흔들리지 않게 합니다.
    private Vector3 flankSmoothVelocity; // 옆돌기 SmoothDamp 전용 내부 보조 벡터 ref입니다. — 요약: 분리·바라보기 보조 벡터와 섞이면 안 되므로 따로 둡니다.
    private CapsuleCollider cachedCapsule; // 같은 오브젝트의 캡슐 콜라이더를 캐시하는 변수입니다. — 요약: 있으면 몸 반지름/높이를 자동으로 맞추는 데 사용합니다.
    private MonsterAttackSimple cachedAttack; // 같은 오브젝트의 공격 스크립트를 캐시하는 변수입니다. — 요약: 공격 중 고정 상태를 빠르게 읽기 위해 저장해 둡니다.

    void Start() // 게임 시작 시 한 번 실행되는 준비 함수입니다. — 요약: 씬이 켜진 뒤 플레이어와 애니메이터 참조를 자동으로 채 웁니다.
    {
        FindPlayerIfMissing(); // player가 비어 있으면 태그로 플ей어를 찾아 연결합니다. — 요약: 인스펙터에서 비워 두어도 Player 태그 오브젝트를 찾아 채 웁니다.
        FindAnimatorIfMissing(); // animator가 비어 있으면 같은 오브젝트에서 자동으로 찾아 연결합니다. — 요약: 자식에 Animator가 있어도 GetComponentInChildren으로 잡습니다.
        SetupBodyFromCapsuleColliderIfExists(); // 캡슐 콜라이더가 있으면 몸 크기 값을 자동 반영합니다. — 요약: 프리팹마다 수치를 손으로 다시 넣는 작업을 줄입니다.
        FindAttackIfMissing(); // 공격 스크립트를 찾아 캐시합니다. — 요약: 공격 중에는 이동을 멈추기 위해 참조를 확보합니다.
    }

    void Update() // 매 프레임마다 실행되며 탐지와 추격을 반복합니다. — 요약: 매 프레임 거리를 보고 이동할지 멈출지를 다시 결정합니다.
    {
        FindPlayerIfMissing(); // 플레이어 연결이 비어 있을 때 다시 찾아 연결합니다. — 요약: 씬 전환 등으로 참조가 끊기면 다시 찾아 안전하게 유지합니다.
        FindAnimatorIfMissing(); // 애니메이터 연결이 비어 있을 때 다시 찾아 연결합니다. — 요약: 런타임에 Animator가 붙는 경우를 대비한 재시도입니다.
        FindAttackIfMissing(); // 공격 스크립트 연결이 비어 있으면 다시 찾습니다. — 요약: 런타임에 컴포넌트가 붙어도 놓치지 않게 합니다.
        if (player == null) return; // 플레이어를 못 찾은 상태면 아무 행동도 하지 않고 종료합니다. — 요약: player가 없으면 아래 계산은 모두 건너뜁니다.

        UpdateDistanceState(); // 플레이어와의 거리로 탐지 상태와 공격 거리 상태를 갱신합니다. — 요약: IsDetected와 IsInAttackRange를 한 번에 갱신합니다.
        RunChaseIfNeeded(); // 탐지되었고 공격 거리 밖일 때만 추격 이동을 실행합니다. — 요약: 상태에 따라 걷기 애니메이션과 위치 이동을 켜거나 끕니다.
    }

    private void FindPlayerIfMissing() // player 변수가 비어 있을 때만 플레이어를 찾아 넣는 함수입니다. — 요약: 이미 player가 있으면 검색 비용을 내지 않고 바로 반환합니다.
    {
        if (player != null) return; // 이미 연결되어 있으면 다시 찾지 않고 종료합니다. — 요약: 수동으로 연결된 Transform을 덮어쓰지 않도록 막습니다.

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Player 태그가 붙은 오브젝트를 찾습니다. — 요약: 씬에 Player 태그가 하나 있어야 자동 연결이 됩니다.
        if (playerObject != null) player = playerObject.transform; // 찾았으면 그 오브젝트의 위치 정보를 player에 저장합니다. — 요약: GameObject 대신 움직임에 쓸 Transform만 보관합니다.
    }

    private void FindAnimatorIfMissing() // animator 변수가 비어 있을 때만 같은 오브젝트에서 찾아 넣는 함수입니다. — 요약: 본체가 아닌 자식에 Animator가 있어도 찾을 수 있습니다.
    {
        if (animator != null) return; // 이미 연결되어 있으면 다시 찾지 않고 종료합니다. — 요약: 인스펙터에서 연결했다면 그대로 존중합니다.
        animator = GetComponentInChildren<Animator>(); // 자식 오브젝트까지 포함해서 애니메이터를 찾아 저장합니다. — 요약: 모델 루트와 분리된 Animator 위치에 대응합니다.
    }

    private void FindAttackIfMissing() // cachedAttack 변수가 비어 있을 때만 공격 스크립트를 찾아 넣는 함수입니다. — 요약: 공격 상태를 읽기 위한 참조를 자동으로 준비합니다.
    {
        if (cachedAttack != null) return; // 이미 연결되어 있으면 다시 찾지 않고 종료합니다. — 요약: 매 프레임 탐색 비용을 줄입니다.
        cachedAttack = GetComponent<MonsterAttackSimple>(); // 같은 오브젝트에서 공격 스크립트를 찾아 저장합니다. — 요약: 추격과 공격이 같은 루트에 붙어 있다는 현재 구조를 사용합니다.
    }

    private void UpdateDistanceState() // 거리 계산으로 현재 탐지 상태와 공격 거리 상태를 정하는 함수입니다. — 요약: 몬스터 위치와 player 위치 사이 거리로 두 불 값을 갱신합니다.
    {
        float distance = Vector3.Distance(transform.position, player.position); // 몬스터와 플레이어 사이 거리를 계산합니다. — 요약: distance는 이후 두 임계값과 비교되는 기준 값입니다.
        IsDetected = distance <= detectRange; // 탐지 거리에 들어오면 true, 벗어나면 false가 됩니다. — 요약: detectRange는 추격을 시작하는 바깥 원의 반지름입니다.
        IsInAttackRange = distance <= attackRange; // 공격 거리에 들어오면 true, 벗어나면 false가 됩니다. — 요약: attackRange는 더 작은 안쪽 원의 반지름입니다.
    }

    private void RunChaseIfNeeded() // 추격이 필요한 조건일 때만 이동을 실행하는 함수입니다. — 요약: 멈출 때 분리 스무딩을 서서히 비워 다음 추격이 자연스럽게 합니다.
    {
        if (!IsDetected) // 탐지되지 않았으면
        {
            ResetSeparationWhenIdle(); // 쉴 때는 분리 방향 스무딩을 서서히 0으로 돌려 둡니다. — 요약: 다음에 다시 추격할 때 이전 프레임의 튀는 값이 남지 않게 합니다.
            SetMoveAnimation(false, 0f); // 걷기 애니메이션을 끄고 속도를 0으로 설정합니다. — 요약: animator에 멈춤 상태를 알려 교착 애니메이션을 막습니다.
            return; // 이동하지 않고 종료합니다. — 요약: 탐지 밖이면 위치도 회전도 추격 로직을 타지 않습니다.
        }

        if (IsInAttackRange) // 공격 거리 안이면
        {
            ResetSeparationWhenIdle(); // 공격 자세 중에는 옆으로 비키는 값이 남지 않게 비웁니다. — 요약: 공격 스크립트가 제자리 행동을 할 때 믹스 방향 잔상을 줄입니다.
            SetMoveAnimation(false, 0f); // 이동을 멈췄다는 애니메이션 상태로 바꿉니다. — 요약: 공격 거리에서는 걷기보다 공격 애니가 우선이어야 합니다.
            return; // 공격 스크립트가 처리하도록 이동하지 않고 종료합니다. — 요약: transform.position은 이 함수에서 더 이상 바꾸지 않습니다.
        }

        if (cachedAttack != null && cachedAttack.IsAttackMoveLocked) // 공격 모션 고정 시간이 남아 있으면
        {
            ResetSeparationWhenIdle(); // 이동 보조 방향들을 0으로 천천히 돌립니다. — 요약: 제자리 공격 중 잔여 이동 벡터가 남지 않게 합니다.
            SetMoveAnimation(false, 0f); // 이동 애니메이션을 끕니다. — 요약: 공격 중 제자리 상태를 애니메이터에 명확히 전달합니다.
            return; // 이동을 실행하지 않고 종료합니다. — 요약: 공격 중에는 추격 이동이 완전히 멈춥니다.
        }

        ChasePlayer(); // 플레이어 방향으로 이동을 실행합니다. — 요약: 추격, 분리, 스무딩, 회전을 한 흐름에서 처리합니다.
    }

    private void ChasePlayer() // 플레이어 방향으로 이동하는 함수입니다. — 요약: 이동은 추격과 분리를 섞고, 회전은 플레이어만 보도록 분리해 떨림을 줄입니다.
    {
        Vector3 direction = player.position - transform.position; // 몬스터에서 플레이어로 향하는 방향 벡터를 구합니다. — 요약: 이 벡터의 가로세로 길이가 곧 남은 이동 거리 성격을 나타냅니다.
        direction.y = 0f; // 위아래 차이는 무시해서 바닥에서만 움직이게 만듭니다. — 요약: Y를 지우면 경사와 관계없이 수평 추격만 합니다.
        if (direction.sqrMagnitude <= 0.0001f) return; // 방향 길이가 거의 0이면 떨림 방지를 위해 이동하지 않습니다. — 요약: 같은 발밑일 때 튀는 각도를 막습니다.

        float distToPlayerSqr = direction.sqrMagnitude; // 나와 플레이어 사이 거리의 제곱을 미리 저장합니다. — 요약: 다른 몬스터가 그보다 안쪽에 있는지 비교할 때 sqrt를 여러 번 쓰지 않게 합니다.
        Vector3 chaseDirection = direction.normalized; // 플레이어를 향한 기본 이동 방향을 계산합니다. — 요약: chaseDirection 길이는 항상 1로 고정됩니다.
        Vector3 rawSeparationDirection = CalculateMonsterSeparationDirection(); // 주변 몬스터와 겹침을 줄이기 위한 즉시 분리 방향을 계산합니다. — 요약: 이 값은 매 프레임 크게 바뀔 수 있어 그대로 쓰면 떨립니다.
        smoothedSeparationDirection = Vector3.SmoothDamp(smoothedSeparationDirection, rawSeparationDirection, ref separationSmoothVelocity, Mathf.Max(0.01f, separationSmoothTime), Mathf.Infinity, Time.deltaTime); // 분리 방향을 직전 값에서 이번 목표로 부드럽게 옮깁니다. — 요약: smoothedSeparationDirection이 rawSeparationDirection을 쫓아가며 급격한 좌우 전환을 줄입니다.

        Vector3 separationForMix = Vector3.zero; // 실제로 추격과 섞을 분리 방향을 담을 변수입니다. — 요약: 길이가 너무 작으면 섞지 않고 순수 추격만 합니다.
        if (smoothedSeparationDirection.sqrMagnitude > 0.0004f) // 부드러워진 분리 벡터가 의미 있을 만큼 큰지 검사합니다. — 요약: 거의 0이면 옆으로 피하기 기여를 넣지 않습니다.
            separationForMix = smoothedSeparationDirection.normalized; // 길이를 1로 맞춰 추격 벡터와 합치기 쉽게 만듭니다. — 요약: monsterSpacingStrength는 이 단위 벡터에만 곱해집니다.

        Vector3 rawFlankDirection = CalculateRawFlankDirection(chaseDirection, distToPlayerSqr); // 앞 동료에게 막혔을 때만 옆으로 도는 목표 방향을 구합니다. — 요약: 플레이어 방향에 수직인 좌우 중 하나를 안정적으로 고릅니다.
        smoothedFlankDirection = Vector3.SmoothDamp(smoothedFlankDirection, rawFlankDirection, ref flankSmoothVelocity, Mathf.Max(0.01f, flankSmoothTime), Mathf.Infinity, Time.deltaTime); // 옆돌기 방향을 직전 값에서 이번 목표로 부드럽게 옮깁니다. — 요약: smoothedFlankDirection이 rawFlankDirection을 쫓아가며 갑자기 옆으로 튀지 않게 합니다.

        Vector3 flankForMix = Vector3.zero; // 추격과 섞을 옆돌기 방향을 담는 변수입니다. — 요약: 막힘이 없으면 0이라 이동은 추격+분리만 남습니다.
        if (smoothedFlankDirection.sqrMagnitude > 0.0004f) // 부드러워진 옆돌기 벡터가 의미 있을 만큼 큰지 검사합니다. — 요약: 거의 0이면 옆걸음 기여를 넣지 않습니다.
            flankForMix = smoothedFlankDirection.normalized; // 길이를 1로 맞춥니다. — 요약: flankStrength는 이 단위 벡터에만 곱해집니다.

        Vector3 mixedDirection = chaseDirection + separationForMix * monsterSpacingStrength + flankForMix * flankStrength; // 추격·분리·옆돌기를 한 벡터에 섞습니다. — 요약: mixedDirection은 세 방향의 가중 합으로 포위하듯 돌아갈 수 있습니다.
        mixedDirection.y = 0f; // 최종 이동도 바닥 기준으로만 처리합니다. — 요약: 분리 과정에서 생긴 작은 Y 오차를 제거합니다.
        if (mixedDirection.sqrMagnitude <= 0.0001f) mixedDirection = chaseDirection; // 섞은 결과가 너무 작으면 기본 추격 방향을 사용합니다. — 요약: 완전히 0에 가까우면 플레이어 방향으로 되돌립니다.

        Vector3 moveDirection = mixedDirection.normalized; // 최종 이동 방향 길이를 1로 맞춰 속도 계산을 일정하게 만듭니다. — 요약: moveSpeed는 이 단위 방향에만 곱해집니다.
        float moveDistance = moveSpeed * Time.deltaTime; // 이번 프레임 이동할 실제 거리입니다. — 요약: 속도와 프레임 시간 곱으로 이동 길이를 만듭니다.
        TryMoveWithMapCollision(moveDirection, moveDistance); // 벽 겹침을 검사하면서 가능한 만큼 이동합니다. — 요약: 직진이 막히면 축 슬라이드로 우회해 벽 관통을 막습니다.
        FaceDirectionSmooth(chaseDirection); // 몸은 플레이어 쪽으로만 천천히 돌립니다. — 요약: 회전은 옆 피하기 벡터와 분리해 몸이 좌우로 떨리지 않게 합니다.
        SetMoveAnimation(true, moveSpeed); // 이동 중 애니메이션을 켜고 현재 이동 속도를 전달합니다. — 요약: 애니메이션은 실제 이동 속도 moveSpeed와 동기됩니다.
    }

    private void SetupBodyFromCapsuleColliderIfExists() // 캡슐 콜라이더가 있으면 몸 크기 설정을 자동으로 맞추는 함수입니다. — 요약: collider 반경/높이/중심값을 이 스크립트 변수에 복사합니다.
    {
        cachedCapsule = GetComponent<CapsuleCollider>(); // 루트 오브젝트의 캡슐 콜라이더를 찾습니다. — 요약: 없으면 수동 입력값(bodyRadius/bodyHeight/bodyCenterY)을 그대로 사용합니다.
        if (cachedCapsule == null) return; // 캡슐이 없으면 자동 반영을 건너뜁니다. — 요약: null 접근 오류를 막습니다.

        bodyRadius = Mathf.Max(0.05f, cachedCapsule.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.z))); // 월드 스케일을 반영한 반지름으로 맞춥니다. — 요약: 스케일이 커진 프리팹도 벽 감지가 작아지지 않게 합니다.
        bodyHeight = Mathf.Max(bodyRadius * 2f + 0.05f, cachedCapsule.height * Mathf.Abs(transform.lossyScale.y)); // 최소한 지름보다 큰 높이가 되게 보정합니다. — 요약: 캡슐 위아래 점이 뒤집히지 않게 합니다.
        bodyCenterY = cachedCapsule.center.y * Mathf.Abs(transform.lossyScale.y); // 캡슐 중심 높이를 월드 기준으로 변환합니다. — 요약: 몸통 충돌 높이를 모델에 맞춰 유지합니다.
    }

    private void TryMoveWithMapCollision(Vector3 moveDirection, float moveDistance) // 벽 관통을 막으면서 이동하는 함수입니다. — 요약: 목표 위치가 막히면 X/Z 축 슬라이드로 대체 이동을 시도합니다.
    {
        if (moveDistance <= 0.00001f) return; // 이동 거리가 너무 작으면 계산을 건너뜁니다. — 요약: 미세 이동에서 불필요한 물리 검사를 줄입니다.

        Vector3 fullDelta = moveDirection * moveDistance; // 원래 가고 싶은 이동 벡터입니다. — 요약: 직진 시도는 이 벡터 하나로 표현합니다.
        Vector3 targetPosition = transform.position + fullDelta; // 직진 시도의 목표 위치입니다. — 요약: 먼저 이 위치가 막히는지 검사합니다.
        if (!IsMapBlockedAtPosition(targetPosition)) // 목표 위치가 벽과 겹치지 않으면
        {
            transform.position = targetPosition; // 그대로 직진 이동합니다. — 요약: 빈 공간에서는 기존과 같은 감각으로 이동합니다.
            return; // 이동 처리 종료입니다. — 요약: 직진 성공 시 슬라이드 계산은 하지 않습니다.
        }

        Vector3 slideDeltaX = new Vector3(fullDelta.x, 0f, 0f); // X축으로만 이동하는 후보 벡터입니다. — 요약: 코너에서 한 축만 열려 있어도 앞으로 진행할 수 있게 합니다.
        Vector3 slideDeltaZ = new Vector3(0f, 0f, fullDelta.z); // Z축으로만 이동하는 후보 벡터입니다. — 요약: X가 막혀도 Z만 열리면 미끄러지듯 이동합니다.

        bool canSlideX = slideDeltaX.sqrMagnitude > 0.0000001f && !IsMapBlockedAtPosition(transform.position + slideDeltaX); // X축 후보가 실제로 이동 가능인지 검사합니다. — 요약: 막혀 있지 않으면 true가 됩니다.
        bool canSlideZ = slideDeltaZ.sqrMagnitude > 0.0000001f && !IsMapBlockedAtPosition(transform.position + slideDeltaZ); // Z축 후보가 실제로 이동 가능인지 검사합니다. — 요약: 막혀 있지 않으면 true가 됩니다.

        if (canSlideX && canSlideZ) // 두 축 모두 가능하면
        {
            if (slideDeltaX.sqrMagnitude >= slideDeltaZ.sqrMagnitude) transform.position += slideDeltaX; // 더 큰 축부터 적용해 직진 감각을 최대한 유지합니다. — 요약: 이동량이 더 큰 쪽을 우선 선택합니다.
            else transform.position += slideDeltaZ; // Z가 더 크면 Z축 이동을 적용합니다. — 요약: 코너에서 덜 답답한 방향으로 흐르도록 합니다.
            return; // 한 번 이동했으니 종료합니다. — 요약: 같은 프레임에 두 축을 모두 넣어 과속되지 않게 합니다.
        }

        if (canSlideX) // X축만 가능하면
        {
            transform.position += slideDeltaX; // X축으로만 슬라이드 이동합니다. — 요약: 벽을 따라 옆으로 미끄러지며 진행합니다.
            return; // 이동 후 종료합니다. — 요약: Z축은 막혀 있으므로 시도하지 않습니다.
        }

        if (canSlideZ) // Z축만 가능하면
        {
            transform.position += slideDeltaZ; // Z축으로만 슬라이드 이동합니다. — 요약: 벽을 따라 앞/뒤 방향으로 미끄러지며 진행합니다.
            return; // 이동 후 종료합니다. — 요약: X축은 막혀 있으므로 시도하지 않습니다.
        }
    }

    private bool IsMapBlockedAtPosition(Vector3 targetPosition) // 주어진 위치에 몸 캡슐을 놓았을 때 벽과 겹치는지 검사하는 함수입니다. — 요약: 겹치면 true(막힘), 아니면 false(이동 가능)입니다.
    {
        float radius = Mathf.Max(0.05f, bodyRadius); // 반지름 최소값을 보장합니다. — 요약: 0에 가까운 반지름으로 검사 누락이 생기지 않게 합니다.
        float height = Mathf.Max(radius * 2f + 0.05f, bodyHeight); // 높이 최소값을 보장합니다. — 요약: 캡슐 계산이 뒤집히지 않도록 합니다.
        float halfLine = Mathf.Max(0.001f, (height * 0.5f) - radius); // 캡슐 중앙선 반길이를 계산합니다. — 요약: 위아래 구의 중심 간격을 만들기 위한 값입니다.

        Vector3 center = targetPosition + Vector3.up * bodyCenterY; // 목표 위치 기준 캡슐 중심점입니다. — 요약: 발밑이 아닌 몸통 높이에서 충돌을 봅니다.
        Vector3 point1 = center + Vector3.up * halfLine; // 캡슐 위쪽 구 중심점입니다. — 요약: CheckCapsule의 첫 번째 점입니다.
        Vector3 point2 = center - Vector3.up * halfLine; // 캡슐 아래쪽 구 중심점입니다. — 요약: CheckCapsule의 두 번째 점입니다.

        bool blocked = Physics.CheckCapsule(point1, point2, radius + Mathf.Max(0f, wallSkin), mapBlockLayerMask, QueryTriggerInteraction.Ignore); // 벽 레이어와 겹치는지 검사합니다. — 요약: true면 벽/장애물에 걸린 상태입니다.
        return blocked; // 검사 결과를 호출한 쪽으로 전달합니다. — 요약: 이동 허용 여부 판단의 최종 값입니다.
    }

    private void ResetSeparationWhenIdle() // 추격하지 않을 때 분리 스무딩 상태를 정리하는 함수입니다. — 요약: 분리 관련 SmoothDamp만 0 쪽으로 당겨 다음 시작이 부드럽습니다.
    {
        smoothedSeparationDirection = Vector3.SmoothDamp(smoothedSeparationDirection, Vector3.zero, ref separationSmoothVelocity, Mathf.Max(0.01f, separationSmoothTime * 0.5f), Mathf.Infinity, Time.deltaTime); // 분리 방향을 0으로 부드럽게 수렴시킵니다. — 요약: 한 번에 끄지 않고 조금씩 줄여 값이 튀지 않게 합니다.
        if (smoothedSeparationDirection.sqrMagnitude < 0.000001f) // 거의 0에 도달했는지 검사합니다. — 요약: 부동소수점 잔여 값을 깨끗이 비울지 결정합니다.
        {
            smoothedSeparationDirection = Vector3.zero; // 남은 값을 정확히 0으로 고정합니다. — 요약: 아주 작은 잔여 벡터가 섞이지 않게 합니다.
            separationSmoothVelocity = Vector3.zero; // 내부 보조 속도도 같이 비웁니다. — 요약: 다음 SmoothDamp가 이전 프레임 속도를 끌고 오지 않게 합니다.
        }

        smoothedFlankDirection = Vector3.SmoothDamp(smoothedFlankDirection, Vector3.zero, ref flankSmoothVelocity, Mathf.Max(0.01f, flankSmoothTime * 0.5f), Mathf.Infinity, Time.deltaTime); // 쉴 때 옆돌기 목표도 0으로 부드럽게 당깁니다. — 요약: 다음 추격 때 옆걸음이 남아 한쪽으로 치우치지 않게 합니다.
        if (smoothedFlankDirection.sqrMagnitude < 0.000001f) // 옆돌기 값이 거의 사라졌는지 검사합니다. — 요약: 남은 잔여를 깨끗이 지울지 정합니다.
        {
            smoothedFlankDirection = Vector3.zero; // 옆돌기 저장 값을 0으로 고정합니다. — 요약: 다음에 막힘을 감지할 때 새로 계산하게 합니다.
            flankSmoothVelocity = Vector3.zero; // 옆돌기 보조 속도도 비웁니다. — 요약: 분리 보조 속도와는 다른 변수이므로 따로 지웁니다.
        }
    }

    private void FaceDirectionSmooth(Vector3 faceTargetDirection) // 추격 중 플레이어 쪽으로 몸통을 부드럽게 돌리는 함수입니다. — 요약: 방향 벡터를 먼저 SmoothDamp 하고 그 결과로 회전을 만듭니다.
    {
        if (faceTargetDirection.sqrMagnitude <= 0.0001f) return; // 바라볼 방향이 거의 없으면 회전 계산을 건너뜁니다. — 요약: 빈 방향으로 LookRotation을 만들면 오류가 날 수 있습니다.

        smoothedFacingDirection = Vector3.SmoothDamp(smoothedFacingDirection, faceTargetDirection, ref facingSmoothVelocity, Mathf.Max(0.01f, moveFacingSmoothTime), Mathf.Infinity, Time.deltaTime); // 저장된 방향이 목표 방향을 부드럽게 따라가게 합니다. — 요약: smoothedFacingDirection과 facingSmoothVelocity만 바라보기에 써서 분리와 섞이지 않습니다.
        if (smoothedFacingDirection.sqrMagnitude <= 0.0001f) smoothedFacingDirection = faceTargetDirection; // 첫 프레임처럼 저장 값이 0에 가까우면 목표를 바로 넣습니다. — 요약: 시작 시 한동안 뒤를 보는 현상을 막습니다.

        Vector3 forward = smoothedFacingDirection.normalized; // 회전에 쓸 단위 방향으로 만듭니다. — 요약: 길이 1인 앞 방향만 Quaternion.LookRotation에 넘깁니다.
        Quaternion lookRotation = Quaternion.LookRotation(forward); // 앞 방향을 실제 회전 값으로 바꿉니다. — 요약: lookRotation은 transform.rotation이 가야 할 목표 자세입니다.
        float t = Mathf.Clamp01(rotateSlerpSharpness * Time.deltaTime); // 이번 프레임에 목표 쪽으로 얼마나 돌릴지 0~1 사이로 만듭니다. — 요약: 클수록 더 빨리 목표를 맞춥니다.
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, t); // 현재 자세에서 목표 자세로 조금씩 맞춥니다. — 요약: Slerp는 구면에서 부드럽게 돌리는 보간입니다.
    }

    public void FaceDirection(Vector3 direction) // 다른 스크립트(예: 공격)에서 즉시 방향을 맞출 때 부르는 공개 함수입니다. — 요약: 공격 직전에는 튀지 않게 바로 플레이어 쪽을 보려면 이 함수를 씁니다.
    {
        if (direction.sqrMagnitude <= 0.0001f) return; // 방향 벡터가 너무 작으면 회전 계산을 하지 않고 종료합니다. — 요약: 0에 가까운 입력은 무시합니다.

        Vector3 forward = direction; // 입력 방향을 복사해 위아래를 지울 준비를 합니다. — 요약: 원본을 바꾸지 않기 위해 복사본을 씁니다.
        forward.y = 0f; // 바닥 위에서만 회전하도록 높이 차이를 제거합니다. — 요약: 플레이어가 위에 있어도 고개만 위로 숙이지 않습니다.
        if (forward.sqrMagnitude <= 0.0001f) return; // 가로 방향이 없으면 더 진행하지 않습니다. — 요약: 전부 수직인 경우를 막습니다.
        forward.Normalize(); // 길이를 1로 맞춥니다. — 요약: LookRotation은 방향만 필요하고 길이는 상관없지만 단위로 맞춰 둡니다.

        smoothedFacingDirection = forward; // 다음 추격 때 회전 연속이 끊기지 않게 내부 저장 방향을 맞춥니다. — 요약: 공격 직후 다시 걸을 때 얼굴이 이전 프레임에서 튀지 않게 합니다.
        facingSmoothVelocity = Vector3.zero; // 공격용 즉시 회전 직후에는 보조 속도를 비웁니다. — 요약: 이전 프레임의 남은 속도가 다음 보행 회전을 흔들지 않게 합니다.

        Quaternion lookRotation = Quaternion.LookRotation(forward); // 목표 회전 값을 만듭니다. — 요약: forward가 보는 쪽이 몬스터 앞면이 됩니다.
        transform.rotation = lookRotation; // 즉시 그 방향으로 몸을 돌립니다. — 요약: 공격 모션과 피격 방향이 맞아야 하므로 보간 없이 한 번에 맞춥니다.
    }

    private Vector3 CalculateRawFlankDirection(Vector3 chaseDirection, float distToPlayerSqr) // 앞에 동료가 끼어든 것처럼 보일 때만 옆으로 도는 단위 방향을 줍니다. — 요약: chaseDirection에 수직인 좌우와 distToPlayerSqr 비교로 막힘을 판단합니다.
    {
        if (distToPlayerSqr <= 0.0001f) return Vector3.zero; // 플레이어까지 거리가 없으면 옆돌기도 없습니다. — 요약: 0으로 나누거나 의미 없는 비교를 막습니다.

        float closerThanPlayerSqr = distToPlayerSqr * flankCloserThanPlayerRatio * flankCloserThanPlayerRatio; // 동료가 플레이어보다 얼마나 가까이 있어야 "사이에 낀다"고 볼지 제곱 거리 한계입니다. — 요약: 동료 거리 제곱이 이 값보다 크면 플레이어 지나친 쪽이라 옆돌기 대상이 아닙니다.
        float checkR = Mathf.Max(0.25f, flankPathCheckRadius); // 막는 동료를 찾는 원의 반지름을 너무 작지 않게 만듭니다. — 요약: 이 반경 밖 동료는 이번 계산에서 무시됩니다.
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, checkR, nearbyMonsterBuffer, monsterLayerMask, QueryTriggerInteraction.Ignore); // 주변 콜라이더를 같은 버퍼에 다시 담습니다. — 요약: 분리 계산 직후라도 값은 이번 호출에서 새로 채 웁니다.

        bool pathBlockedByAlly = false; // 앞에 동료가 하나라도 걸리면 참이 됩니다. — 요약: 참일 때만 옆걸음 목표를 돌려줍니다.
        for (int i = 0; i < hitCount; i++) // 후보를 하나씩 검사합니다. — 요약: hitCount는 이번 검색에 실제로 들어 온 개수입니다.
        {
            Collider c = nearbyMonsterBuffer[i]; // 현재 후보 콜라이더입니다. — 요약: null이면 건너뜁니다.
            if (c == null) continue; // 비어 있으면 다음 후보로 넘어갑니다. — 요약: 슬롯이 비는 경우를 대비합니다.
            MonsterDetectChaseSimple other = c.GetComponentInParent<MonsterDetectChaseSimple>(); // 같은 추격 스크립트가 있는지 봅니다. — 요약: 벽 장애물은 여기서 걸러집니다.
            if (other == null) continue; // 몬스터가 아니면 건너뜁니다. — 요약: 옆돌기는 동료 몬스터 때문일 때만 켭니다.
            if (other == this) continue; // 자기 자신은 건너뜁니다. — 요약: 자기 콜라이더로 막혔다고 보지 않습니다.

            Vector3 meToOther = other.transform.position - transform.position; // 나에서 동료로 가는 벡터입니다. — 요약: 이 방향이 플레이어 방향과 비슷하면 앞줄에 서 있는 동료입니다.
            meToOther.y = 0f; // 바닥 기준으로만 봅니다. — 요약: 높이 차이는 옆돌기 판단에서 빼 줍니다.
            float sqr = meToOther.sqrMagnitude; // 거리 제곱입니다. — 요약: 비교용으로 sqrt를 줄여 씁니다.
            if (sqr < 0.02f) continue; // 너무 가까우면 방향이 불안정합니다. — 요약: 같은 발밑으로 보고 건너뜁니다.

            float horizontalDist = Mathf.Sqrt(sqr); // 실제 수평 거리입니다. — 요약: 단위 방향을 만들 때 한 번만 sqrt합니다.
            Vector3 meToOtherDir = meToOther / horizontalDist; // 길이 1인 동료 방향입니다. — 요약: chaseDirection과 맞닿음을 재려면 단위 벡터가 필요합니다.
            float inFrontAlign = Vector3.Dot(meToOtherDir, chaseDirection); // 두 방향이 얼마나 같은 쪽을 보는지 나타내는 숫자입니다. — 요약: 1이면 완전 정면, 0이면 옆입니다.
            if (inFrontAlign < flankInFrontDot) continue; // 앞쪽이 아니면 막는 줄로 보지 않습니다. — 요약: flankInFrontDot보다 작으면 측면·뒤 동료입니다.

            float forwardAlong = Vector3.Dot(meToOther, chaseDirection); // 플레이어 쪽 축으로 동료가 얼마나 앞에 있는지 미터 단위 느낌입니다. — 요약: chaseDirection 길이가 1이라 내적이 곧 앞으로 치우친 거리입니다.
            if (forwardAlong < flankMinForward) continue; // 앞으로 충분히 박지 않았으면 무시합니다. — 요약: 살짝 옆에만 있으면 직진해도 되는 경우입니다.

            if (sqr > closerThanPlayerSqr) continue; // 플레이어보다 거의 멀거나 같으면 길목이 아닙니다. — 요약: 플레이어에 더 가까운 동료만 막는 벽으로 취급합니다.

            pathBlockedByAlly = true; // 조건을 통과한 동료가 있으면 막힌 것으로 표시합니다. — 요약: 하나만 찾아도 옆돌기를 켭니다.
            break; // 더 찾지 않고 반복을 멈춥니다. — 요약: 성능과 단순함을 위해 첫 막는 동료만 씁니다.
        }

        if (!pathBlockedByAlly) return Vector3.zero; // 막힘이 없으면 옆돌기 목표는 0 벡터입니다. — 요약: mixedDirection에는 추격과 분리만 남습니다.

        Vector3 toSide = Vector3.Cross(Vector3.up, chaseDirection); // 플레이어 방향에 수평으로 수직인 "왼손 좌표계" 옆 방향입니다. — 요약: 위쪽 축과 chaseDirection으로 외적 만들기를 합니다.
        if (toSide.sqrMagnitude <= 0.0001f) return Vector3.zero; // 외적이 거의 0이면 방향을 만들 수 없습니다. — 요약: chaseDirection이 수직일 때만 생기며 보통은 발생하지 않습니다.
        toSide.Normalize(); // 옆 방향 길이를 1로 맞춥니다. — 요약: 최종 반환은 단위 벡터 또는 그 반대입니다.

        float pickLeftOrRight = (GetInstanceID() & 1) == 0 ? 1f : -1f; // 이 오브젝트만의 고정된 부호입니다. — 요약: 짝수·홀수 id로 절반은 오른쪽, 절반은 왼쪽으로 돌아 포위합니다.
        return toSide * pickLeftOrRight; // 옆 방향에 부호를 곱해 반환합니다. — 요약: pickLeftOrRight가 toSide와 chaseDirection 관계를 좌우로 갈라 줍니다.
    }

    private Vector3 CalculateMonsterSeparationDirection() // 주변 몬스터와 겹치지 않게 옆으로 피할 방향을 계산하는 함수입니다. — 요약: 반경 안 이웃마다 밀어내는 방향을 모아 평균 내어 한 방향으로 만듭니다.
    {
        float radius = Mathf.Max(0.1f, monsterSpacingRadius); // 반경이 너무 작아지지 않게 최소값을 보장합니다. — 요약: radius는 이후 거리 비교와 겹침 검색 크기에 같이 쓰입니다.
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, radius, nearbyMonsterBuffer, monsterLayerMask, QueryTriggerInteraction.Ignore); // 주변의 콜라이더를 버퍼에 담습니다. — 요약: 레이어 마스크로 몬스터만 골라 담는 것이 좋습니다(설정에 따라 다름).
        if (hitCount <= 0) return Vector3.zero; // 주변에 후보가 없으면 분리 방향 없이 0 벡터를 반환합니다. — 요약: 이웃이 없으면 피할 필요가 없습니다.

        Vector3 sum = Vector3.zero; // 피해야 할 방향을 누적할 합계 벡터입니다. — 요약: 각 이웃이 더해 주는 밀어내기 값이 여기 쌓입니다.
        int used = 0; // 실제로 분리 계산에 사용한 몬스터 수를 세는 카운터입니다. — 요약: 평균낼 때 sum을 몇 개로 나눌지 정합니다.
        for (int i = 0; i < hitCount; i++) // 감지된 후보를 하나씩 검사합니다. — 요약: 버퍼의 0번부터 hitCount 전까지 돕니다.
        {
            Collider c = nearbyMonsterBuffer[i]; // 현재 후보 콜라이더를 꺼냅니다. — 요약: c가 붙은 오브젝트가 진짜 몬스터인지 아래에서 확인합니다.
            if (c == null) continue; // 콜라이더가 비어 있으면 건너뜁니다. — 요약: 배열 슬롯이 비는 경우를 대비합니다.
            MonsterDetectChaseSimple other = c.GetComponentInParent<MonsterDetectChaseSimple>(); // 같은 종류의 몬스터 이동 스크립트를 부모 기준으로 찾습니다. — 요약: 벽이나 문 콜라이더는 이 컴포넌트가 없어 자동으로 제외됩니다.
            if (other == null) continue; // 몬스터 스크립트가 없으면 건너뜁니다. — 요약: 진짜 몬스터끼리만 서로 피합니다.
            if (other == this) continue; // 자기 자신이면 건너뜁니다. — 요약: 자기 콜라이더는 분리 대상이 아닙니다.

            Vector3 away = transform.position - other.transform.position; // 상대 몬스터에서 나 자신 쪽으로 도망 방향 벡터를 구합니다. — 요약: away는 서로 멀어지려는 화살표입니다.
            away.y = 0f; // 바닥 기준 계산을 위해 y를 제거합니다. — 요약: 분리도 수평으로만 계산합니다.
            float sqr = away.sqrMagnitude; // 거리 제곱을 계산해 루트 연산을 줄입니다. — 요약: 0에 가까운지 비교할 때 sqrt 없이 씁니다.
            if (sqr <= 0.0001f) continue; // 위치가 거의 같으면 불안정해질 수 있어 건너뜁니다. — 요약: 무한대로 튀는 방향을 막습니다.
            if (sqr > radius * radius) continue; // 반경보다 멀면 분리 계산 대상에서 제외합니다. — 요약: 멀리 있는 몬스터는 당장 겹치지 않는다고 봅니다.

            float distance = Mathf.Sqrt(sqr); // 실제 거리를 계산합니다. — 요약: 가중치에서 거리 비율을 쓰기 위해 한 번만 sqrt를 합니다.
            float weight = 1f - Mathf.Clamp01(distance / radius); // 가까울수록 더 크게 피하도록 가중치를 계산합니다. — 요약: 거의 겹치면 weight가 1에 가깝고 멀면 0에 가깝습니다.
            sum += away.normalized * weight; // 도망 방향을 가중치만큼 누적합니다. — 요약: 가까운 이웃이 합 벡터에 더 큰 영향을 줍니다.
            used++; // 사용한 개수를 1 증가시킵니다. — 요약: 평균을 낼 분모를 만듭니다.
        }

        if (used == 0) return Vector3.zero; // 유효 후보를 하나도 못 찾았으면 분리 방향 없이 종료합니다. — 요약: 합계가 비었으면 0을 돌려줍니다.
        Vector3 average = sum / used; // 누적 방향을 평균 내어 한 방향으로 합칩니다. — 요약: 여러 이웃이 있을 때 한쪽으로만 튀지 않게 합니다.
        if (average.sqrMagnitude <= 0.0001f) return Vector3.zero; // 평균 방향이 너무 작으면 0 벡터를 반환합니다. — 요약: 의미 있는 밀어내기가 없다고 봅니다.
        return average.normalized; // 최종 분리 방향을 길이 1로 만들어 반환합니다. — 요약: SmoothDamp에 넣기 좋은 단위 벡터입니다.
    }

    private void SetMoveAnimation(bool isMoving, float speedValue) // 이동 관련 애니메이터 값을 한 곳에서 관리하는 함수입니다. — 요약: Animator 파라미터 이름이 틀리면 조용히 건너뛰도록 안전하게 넣습니다.
    {
        if (animator == null) return; // 애니메이터가 없으면 애니메이션 처리를 하지 않고 종료합니다. — 요약: 잘못된 호출로 오류가 나지 않게 합니다.
        if (HasBoolParameter(isMovingParam)) animator.SetBool(isMovingParam, isMoving); // bool 파라미터가 실제로 있을 때만 이동 여부를 전달합니다. — 요약: isMoving이 참이면 걷기로, 거짓이면 정지로 연결됩니다.
        if (HasFloatParameter(moveSpeedParam)) animator.SetFloat(moveSpeedParam, speedValue); // float 파라미터가 실제로 있을 때만 이동 속도를 전달합니다. — 요약: speedValue는 blend tree나 속도 배율에 쓰일 수 있습니다.
    }

    private bool HasBoolParameter(string paramName) // bool 파라미터가 Animator에 있는지 확인하는 함수입니다. — 요약: 없는 이름으로 SetBool을 부르면 경고가 나서 먼저 확인합니다.
    {
        if (string.IsNullOrEmpty(paramName)) return false; // 이름이 비어 있으면 없는 것으로 처리합니다. — 요약: 빈 문자열은 절대 일치하는 파라미터가 없습니다.
        AnimatorControllerParameter[] parameters = animator.parameters; // 현재 Animator에 등록된 파라미터 목록을 가져옵니다. — 요약: 인스펙터에 보이는 목록과 같습니다.
        for (int i = 0; i < parameters.Length; i++) // 목록을 앞에서부터 하나씩 확인합니다. — 요약: 개수가 많아도 한 번씩만 봅니다.
        {
            if (parameters[i].type != AnimatorControllerParameterType.Bool) continue; // bool 타입이 아니면 건너뜁니다. — 요약: 같은 이름이라도 타입이 다르면 무시합니다.
            if (parameters[i].name != paramName) continue; // 이름이 다르면 건너뜁니다. — 요약: 찾는 문자열과 정확히 같아야 합니다.
            return true; // 이름과 타입이 모두 맞는 파라미터를 찾았으니 true를 반환합니다. — 요약: 이제 SetBool을 안전하게 호출할 수 있습니다.
        }

        return false; // 끝까지 찾지 못했으니 false를 반환합니다. — 요약: 해당 이름의 bool이 없다는 뜻입니다.
    }

    private bool HasFloatParameter(string paramName) // float 파라미터가 Animator에 있는지 확인하는 함수입니다. — 요약: bool 확인과 같은 패턴으로 float만 골라 봅니다.
    {
        if (string.IsNullOrEmpty(paramName)) return false; // 이름이 비어 있으면 없는 것으로 처리합니다. — 요약: 빈 이름은 검사할 필요가 없습니다.
        AnimatorControllerParameter[] parameters = animator.parameters; // 현재 Animator에 등록된 파라미터 목록을 가져옵니다. — 요약: 컨트롤러가 바뀌어도 매번 최신 목록을 읽습니다.
        for (int i = 0; i < parameters.Length; i++) // 목록을 앞에서부터 하나씩 확인합니다. — 요약: 전체를 순회하며 일치 항목을 찾습니다.
        {
            if (parameters[i].type != AnimatorControllerParameterType.Float) continue; // float 타입이 아니면 건너뜁니다. — 요약: Int나 Trigger와 헷갈리지 않게 합니다.
            if (parameters[i].name != paramName) continue; // 이름이 다르면 건너뜁니다. — 요약: 대소문자까지 맞아야 합니다.
            return true; // 이름과 타입이 모두 맞는 파라미터를 찾았으니 true를 반환합니다. — 요약: SetFloat를 호출해도 됩니다.
        }

        return false; // 끝까지 찾지 못했으니 false를 반환합니다. — 요약: 그 float 이름은 Animator에 없습니다.
    }
}
