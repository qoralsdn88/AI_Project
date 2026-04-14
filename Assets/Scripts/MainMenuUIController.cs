using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUIController : MonoBehaviour
{
    public void OnClickStartGame() => SceneManager.LoadScene(GameScenes.UITest);

    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
        Debug.Log("게임 종료 요청을 보냈습니다.");
    }
}