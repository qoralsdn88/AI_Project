// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;
// 시간이 지나면 씬을 바꾸기 위해 필요합니다.
using UnityEngine.SceneManagement;

// 게임 시작 후 20초가 지나면 승리 처리하는 스크립트입니다.
public class GameFlowController : MonoBehaviour
{
    // 승리까지 걸리는 시간을 저장하는 변수입니다.
    // 핵심 요약: winAfterSeconds 값만 바꾸면 승리 시간을 쉽게 조절할 수 있습니다.
    [SerializeField] private float winAfterSeconds = 20f;

    // 현재까지 흐른 시간을 저장하는 변수입니다.
    // 핵심 요약: elapsedSeconds가 매 프레임 늘어나며 winAfterSeconds와 비교됩니다.
    private float elapsedSeconds = 0f;

    // 승리 처리를 한 번만 하게 막아주는 변수입니다.
    // 핵심 요약: isGameEnded가 true면 다시 승리 코드를 실행하지 않습니다.
    private bool isGameEnded = false;

    // 매 프레임마다 자동으로 실행되는 함수입니다.
    // 핵심 요약: 매 프레임 시간 누적 후 20초가 되면 메인 화면으로 이동합니다.
    private void Update()
    {
        // 이미 게임이 끝났다면 아래 로직을 더 이상 실행하지 않습니다.
        if (isGameEnded)
        {
            // 함수 실행을 여기서 바로 종료합니다.
            return;
        }

        // 한 프레임 동안 지난 실제 시간을 누적합니다.
        elapsedSeconds += Time.deltaTime;

        // 누적 시간이 승리 시간보다 크거나 같은지 확인합니다.
        if (elapsedSeconds >= winAfterSeconds)
        {
            // 승리 처리가 시작되었음을 기록해서 중복 실행을 막습니다.
            isGameEnded = true;

            // 콘솔에 승리 메시지를 남겨서 확인하기 쉽게 합니다.
            Debug.Log("20초가 지나 승리했습니다. 메인 화면으로 이동합니다.");

            // 메인 씬으로 바로 이동합니다.
            SceneManager.LoadScene("MainScene");
        }
    }
}