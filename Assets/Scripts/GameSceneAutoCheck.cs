// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;

// 게임 씬 시작 시 필수 연결 상태를 자동으로 점검하는 스크립트입니다.
public class GameSceneAutoCheck : MonoBehaviour
{
    // 점검 대상 플레이어 오브젝트를 인스펙터에서 직접 연결받습니다.
    // 핵심 요약: playerObject가 비어 있으면 Player 연결이 안 된 상태입니다.
    [SerializeField] private GameObject playerObject;

    // 플레이어 이동 스크립트 연결 상태를 점검하기 위해 저장하는 변수입니다.
    // 핵심 요약: playerMover가 null이면 PlayerSimpleMover가 없는 상태입니다.
    private PlayerSimpleMover playerMover;

    // 화면 왼쪽 위에 점검 결과를 보여주기 위해 문장을 저장하는 변수입니다.
    // 핵심 요약: checkMessage에 현재 점검 결과가 한 줄로 들어갑니다.
    private string checkMessage = "점검 대기 중";

    // 화면 디버그 글자 모양을 재사용하기 위한 변수입니다.
    // 핵심 요약: checkStyle을 한 번만 만들어 성능 낭비를 줄입니다.
    private GUIStyle checkStyle;

    // 게임 시작 시 한 번 자동으로 실행되는 함수입니다.
    // 핵심 요약: Start에서 연결 상태를 모두 확인하고 결과를 로그로 남깁니다.
    private void Start()
    {
        // 첫 번째 점검으로 Player 오브젝트 연결 여부를 확인합니다.
        if (playerObject == null)
        {
            // 점검 결과 문장을 사용자 친화적으로 저장합니다.
            checkMessage = "실패: Player 오브젝트가 비어 있습니다. GameManager에 Player를 연결해주세요.";
            // 콘솔에 빨간 경고로 남겨서 바로 보이게 합니다.
            Debug.LogError("[GameSceneAutoCheck] Player 오브젝트가 비어 있습니다. GameManager 오브젝트의 GameSceneAutoCheck 컴포넌트에 Player를 연결해주세요.");
            // 더 진행할 수 없으므로 함수를 종료합니다.
            return;
        }

        // 두 번째 점검으로 Player 오브젝트의 활성화 상태를 확인합니다.
        if (!playerObject.activeInHierarchy)
        {
            // 점검 결과 문장을 저장합니다.
            checkMessage = "실패: Player 오브젝트가 꺼져 있습니다. 체크박스를 켜주세요.";
            // 콘솔에 빨간 경고로 남깁니다.
            Debug.LogError("[GameSceneAutoCheck] Player 오브젝트가 비활성화 상태입니다. Hierarchy에서 Player 체크박스를 켜주세요.");
            // 더 진행할 수 없으므로 함수를 종료합니다.
            return;
        }

        // 세 번째 점검으로 PlayerSimpleMover 컴포넌트가 붙어 있는지 확인합니다.
        playerMover = playerObject.GetComponent<PlayerSimpleMover>();

        // PlayerSimpleMover 연결이 없는 경우를 처리합니다.
        if (playerMover == null)
        {
            // 점검 결과 문장을 저장합니다.
            checkMessage = "실패: PlayerSimpleMover 컴포넌트가 없습니다.";
            // 콘솔에 빨간 경고를 남깁니다.
            Debug.LogError("[GameSceneAutoCheck] Player 오브젝트에 PlayerSimpleMover 컴포넌트가 없습니다. Add Component로 추가해주세요.");
            // 더 진행할 수 없으므로 함수를 종료합니다.
            return;
        }

        // 네 번째 점검으로 PlayerSimpleMover 컴포넌트 자체가 켜져 있는지 확인합니다.
        if (!playerMover.enabled)
        {
            // 점검 결과 문장을 저장합니다.
            checkMessage = "실패: PlayerSimpleMover 컴포넌트가 꺼져 있습니다.";
            // 콘솔에 빨간 경고를 남깁니다.
            Debug.LogError("[GameSceneAutoCheck] PlayerSimpleMover 컴포넌트가 비활성화 상태입니다. 컴포넌트 체크박스를 켜주세요.");
            // 더 진행할 수 없으므로 함수를 종료합니다.
            return;
        }

        // 모든 점검을 통과했음을 문장으로 저장합니다.
        checkMessage = "성공: Player 연결, 오브젝트 활성화, 이동 컴포넌트 활성화 모두 정상입니다.";
        // 콘솔에 성공 로그를 남깁니다.
        Debug.Log("[GameSceneAutoCheck] 점검 완료: 게임 시작에 필요한 연결 상태가 정상입니다.");
    }

    // 화면에 점검 결과 문장을 보이기 위해 매 프레임 호출되는 함수입니다.
    // 핵심 요약: OnGUI에서 checkMessage를 항상 화면 왼쪽 위에 출력합니다.
    private void OnGUI()
    {
        // 글자 스타일이 아직 없으면 한 번만 생성합니다.
        if (checkStyle == null)
        {
            // 기본 라벨 스타일을 복사해서 새 스타일을 만듭니다.
            checkStyle = new GUIStyle(GUI.skin.label);
            // 글자 크기를 설정해서 읽기 쉽게 만듭니다.
            checkStyle.fontSize = 18;
            // 글자 색을 노란색으로 설정해서 눈에 잘 띄게 합니다.
            checkStyle.normal.textColor = Color.yellow;
        }

        // 점검 결과 문장을 화면 왼쪽 위에 그립니다.
        GUI.Label(new Rect(10f, 40f, 1400f, 30f), $"[자동 점검] {checkMessage}", checkStyle);
    }
}
