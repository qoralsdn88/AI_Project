using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectUIController : MonoBehaviour
{
    public void OnClickStartStage1() => SceneManager.LoadScene(GameScenes.Game);

    public void OnClickBackToMain() => SceneManager.LoadScene(GameScenes.MainMenu);
}