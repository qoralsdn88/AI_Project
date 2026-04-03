// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;
// 신 입력 시스템이 켜져 있을 때만 이 기능을 사용하도록 준비합니다.
#if ENABLE_INPUT_SYSTEM
// 키보드 입력을 직접 읽기 위해 필요합니다.
using UnityEngine.InputSystem;
#endif

// 방향키로 캐릭터를 단순 이동시키는 스크립트입니다.
public class PlayerSimpleMover : MonoBehaviour
{
    // 게임 오브젝트가 준비되는 가장 이른 시점에 한 번 실행되는 함수입니다.
    // 핵심 요약: Awake 로그로 이 스크립트가 실제로 실행되는지 바로 확인할 수 있습니다.
    private void Awake()
    {
        // 현재 오브젝트에서 이 스크립트가 켜졌다는 것을 콘솔로 알려줍니다.
        Debug.Log("[PlayerSimpleMover] Awake 호출됨");
    }

    // 초당 이동 속도를 저장하는 변수입니다.
    // 핵심 요약: moveSpeed 값이 클수록 같은 시간에 더 멀리 이동합니다.
    [SerializeField] private float moveSpeed = 5f;

    // 게임 화면 왼쪽 위에 디버그 글자를 보일지 정하는 변수입니다.
    // 핵심 요약: showOnScreenDebug가 true면 실행 상태를 화면에서 바로 볼 수 있습니다.
    [SerializeField] private bool showOnScreenDebug = true;

    // 이동 로그를 너무 자주 찍지 않기 위한 다음 로그 시간입니다.
    // 핵심 요약: nextMoveLogTime 전에 로그를 막아 콘솔이 너무 길어지지 않게 합니다.
    private float nextMoveLogTime = 0f;

    // 입력이 없다는 안내를 한 번만 보여주기 위한 변수입니다.
    // 핵심 요약: hasShownNoInputLog가 true가 되면 같은 경고를 다시 찍지 않습니다.
    private bool hasShownNoInputLog = false;

    // 화면에 보여줄 현재 상태 문장을 저장하는 변수입니다.
    // 핵심 요약: statusMessage 값이 바뀌면 화면 디버그 문구도 같이 바뀝니다.
    private string statusMessage = "대기 중";

    // 화면 디버그 글자 스타일을 한 번만 만들기 위한 변수입니다.
    // 핵심 요약: debugStyle을 재사용해서 매 프레임 새로 만들지 않게 합니다.
    private GUIStyle debugStyle;

    // 마지막으로 입력이 확인된 시간을 저장하는 변수입니다.
    // 핵심 요약: lastInputTime으로 입력이 멈춘 시간을 계산할 수 있습니다.
    private float lastInputTime = 0f;

    // 오브젝트가 활성화될 때마다 한 번 실행되는 함수입니다.
    // 핵심 요약: OnEnable 로그로 컴포넌트가 켜지는 순간을 확인할 수 있습니다.
    private void OnEnable()
    {
        // 컴포넌트가 활성화되었음을 콘솔에 알려줍니다.
        Debug.Log($"[PlayerSimpleMover] OnEnable 호출됨 - 오브젝트 이름: {gameObject.name}");
        // 화면 디버그 문구를 활성화 상태로 바꿉니다.
        statusMessage = "컴포넌트 활성화됨";
    }

    // 게임 시작 시 한 번 실행되는 함수입니다.
    // 핵심 요약: 스크립트가 실제로 붙어서 동작 중인지 콘솔로 먼저 확인합니다.
    private void Start()
    {
        // 어떤 오브젝트에서 이동 스크립트가 시작됐는지 콘솔에 보여줍니다.
        Debug.Log($"[PlayerSimpleMover] 시작됨 - 오브젝트 이름: {gameObject.name}, 이동 속도: {moveSpeed}");
        // 시작 직후 화면 디버그 문구를 준비 완료 상태로 바꿉니다.
        statusMessage = "Start 완료, 입력 대기 중";
    }

    // 매 프레임마다 자동으로 실행되는 함수입니다.
    // 핵심 요약: 입력 방향을 읽고 transform.position에 직접 더해 이동합니다.
    private void Update()
    {
        // 구 입력과 신 입력을 모두 확인해서 최종 이동 입력을 가져옵니다.
        Vector2 moveInput = ReadMoveInput();

        // x와 z 이동 값을 하나의 방향 값으로 만듭니다.
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        // 대각선 이동이 더 빨라지는 문제를 막기 위해 길이를 1로 맞춥니다.
        moveDirection = moveDirection.normalized;

        // 한 프레임 동안 실제로 이동할 거리를 계산합니다.
        Vector3 moveAmount = moveDirection * moveSpeed * Time.deltaTime;

        // 현재 위치에 이동 거리를 더해서 위치를 직접 바꿉니다.
        transform.position += moveAmount;

        // 입력이 들어오면 현재 입력값을 0.5초 간격으로 콘솔에 보여줍니다.
        if (moveInput.sqrMagnitude > 0f && Time.time >= nextMoveLogTime)
        {
            // 로그 간격 기준 시간을 다음으로 미룹니다.
            nextMoveLogTime = Time.time + 0.5f;
            // 입력이 잘 들어오는지 숫자로 바로 확인할 수 있게 출력합니다.
            Debug.Log($"[PlayerSimpleMover] 입력 감지 - X: {moveInput.x}, Y: {moveInput.y}");
            // 마지막 입력 시간을 현재 시간으로 기록합니다.
            lastInputTime = Time.time;
            // 화면 디버그 문구를 입력 감지 상태로 바꿉니다.
            statusMessage = $"입력 감지됨 (X:{moveInput.x}, Y:{moveInput.y})";
        }

        // 일정 시간 뒤에도 입력이 계속 0이면 원인 점검 안내를 한 번 보여줍니다.
        if (moveInput.sqrMagnitude == 0f && Time.time > 1f && !hasShownNoInputLog)
        {
            // 같은 안내를 반복하지 않도록 상태를 바꿉니다.
            hasShownNoInputLog = true;
            // 입력 설정 또는 컴포넌트 연결 상태를 점검하라고 알려줍니다.
            Debug.LogWarning("[PlayerSimpleMover] 입력값이 계속 0입니다. Player의 스크립트 연결, 이동 속도, 그리고 Project Settings > Player > Active Input Handling 값을 확인해주세요.");
            // 화면 디버그 문구를 입력 없음 경고 상태로 바꿉니다.
            statusMessage = "입력값 0 상태입니다. 연결/설정을 확인해주세요.";
        }
    }

    // 화면에 간단한 디버그 문구를 그리는 함수입니다.
    // 핵심 요약: OnGUI로 현재 실행 상태를 게임 화면 왼쪽 위에 표시합니다.
    private void OnGUI()
    {
        // 화면 디버그를 끈 경우에는 아무것도 그리지 않고 끝냅니다.
        if (!showOnScreenDebug) { return; }
        // 글자 스타일이 아직 없으면 여기서 한 번만 만듭니다.
        if (debugStyle == null)
        {
            // 새로운 글자 스타일 객체를 만듭니다.
            debugStyle = new GUIStyle(GUI.skin.label);
            // 글자 크기를 크게 해서 읽기 쉽게 만듭니다.
            debugStyle.fontSize = 18;
            // 글자 색을 흰색으로 설정합니다.
            debugStyle.normal.textColor = Color.white;
        }
        // 화면에 보여줄 문장을 한 줄로 만듭니다.
        string debugText = $"PlayerSimpleMover 상태: {statusMessage} | 마지막 입력 후 지난 시간: {(Time.time - lastInputTime):0.00}초";
        // 왼쪽 위 위치에 디버그 문장을 그립니다.
        GUI.Label(new Rect(10f, 10f, 1200f, 30f), debugText, debugStyle);
    }

    // 구 입력과 신 입력을 함께 읽어 최종 입력 벡터를 반환하는 함수입니다.
    // 핵심 요약: finalInput은 신 입력 우선, 없으면 구 입력을 사용합니다.
    private Vector2 ReadMoveInput()
    {
        // 구 입력 시스템 값을 담을 변수입니다.
        Vector2 legacyInput = Vector2.zero;

        // 구 입력 시스템이 켜져 있으면 Horizontal/Vertical 축 값을 읽습니다.
        #if ENABLE_LEGACY_INPUT_MANAGER
        // 좌우 축과 상하 축을 읽어서 2차원 입력값으로 만듭니다.
        legacyInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        #endif

        // 신 입력 시스템 값을 담을 변수입니다.
        Vector2 newInput = Vector2.zero;

        // 신 입력 시스템이 켜져 있으면 키보드 상태를 직접 읽습니다.
        #if ENABLE_INPUT_SYSTEM
        // 키보드 장치가 정상적으로 있는지 먼저 확인합니다.
        if (Keyboard.current != null)
        {
            // 오른쪽 방향키 또는 D 키가 눌렸을 때 x를 +1로 만듭니다.
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) { newInput.x += 1f; }
            // 왼쪽 방향키 또는 A 키가 눌렸을 때 x를 -1로 만듭니다.
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) { newInput.x -= 1f; }
            // 위쪽 방향키 또는 W 키가 눌렸을 때 y를 +1로 만듭니다.
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) { newInput.y += 1f; }
            // 아래쪽 방향키 또는 S 키가 눌렸을 때 y를 -1로 만듭니다.
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) { newInput.y -= 1f; }
        }
        #endif

        // 신 입력이 있으면 신 입력을 우선으로 사용합니다.
        Vector2 finalInput = newInput.sqrMagnitude > 0f ? newInput : legacyInput;

        // 대각선 입력 크기를 1로 제한해서 속도를 일정하게 유지합니다.
        finalInput = Vector2.ClampMagnitude(finalInput, 1f);

        // 이동 계산에 사용할 최종 입력을 반환합니다.
        return finalInput;
    }
}