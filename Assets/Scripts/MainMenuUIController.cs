using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIController : MonoBehaviour
{
    public void OnClickStartGame() => SceneManager.LoadScene(GameScenes.StageSelect);

    public void OnClickSettings() => Debug.Log("설정 화면은 추후 구현 예정입니다.");

    public void OnClickQuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료 요청을 보냈습니다.");
    }
}