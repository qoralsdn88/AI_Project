// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;
// 버튼을 눌렀을 때 씬을 바꾸기 위해 필요합니다.
using UnityEngine.SceneManagement;

// 메인 화면 버튼 동작을 맡는 스크립트입니다.
public class MainMenuUIController : MonoBehaviour
{
    // 게임 시작 버튼에서 호출되어 스테이지 선택 화면으로 이동합니다.
    // 핵심 요약: 시작 버튼을 누르면 StageSelectScene으로 넘어갑니다.
    public void OnClickStartGame()
    {
        // 이름이 StageSelectScene인 씬을 불러옵니다.
        SceneManager.LoadScene("StageSelectScene");
    }

    // 설정 버튼에서 호출되어 지금은 안내만 띄웁니다.
    // 핵심 요약: 설정 버튼은 현재 임시로 로그만 출력합니다.
    public void OnClickSettings()
    {
        // 아직 설정 화면이 없어서 콘솔에 임시 메시지를 보여줍니다.
        Debug.Log("설정 화면은 추후 구현 예정입니다.");
    }

    // 게임 종료 버튼에서 호출되어 실행 중인 게임을 끝냅니다.
    // 핵심 요약: 종료 버튼을 누르면 게임 실행이 종료됩니다.
    public void OnClickQuitGame()
    {
        // 에디터가 아닌 실제 실행 파일에서 게임을 종료합니다.
        Application.Quit();
        // 에디터에서는 종료가 안 보여서 확인용 로그를 남깁니다.
        Debug.Log("게임 종료 요청을 보냈습니다.");
    }
}