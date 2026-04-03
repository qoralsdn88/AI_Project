// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;
// 버튼을 눌렀을 때 씬을 바꾸기 위해 필요합니다.
using UnityEngine.SceneManagement;

// 스테이지 선택 화면 버튼 동작을 맡는 스크립트입니다.
public class StageSelectUIController : MonoBehaviour
{
    // 첫 번째 스테이지 시작 버튼에서 호출되어 게임 씬으로 이동합니다.
    // 핵심 요약: 선택 버튼을 누르면 GameScene으로 넘어갑니다.
    public void OnClickStartStage1()
    {
        // 이름이 GameScene인 씬을 불러옵니다.
        SceneManager.LoadScene("GameScene");
    }

    // 뒤로 가기 버튼에서 호출되어 메인 화면으로 돌아갑니다.
    // 핵심 요약: 뒤로 가기 버튼을 누르면 MainScene으로 돌아갑니다.
    public void OnClickBackToMain()
    {
        // 이름이 MainScene인 씬을 불러옵니다.
        SceneManager.LoadScene("MainScene");
    }
}