using UnityEngine;

/// <summary>
/// 게임 씬 시작 시 Player·이동·CharacterController 연결을 점검합니다(개발용).
/// </summary>
public class GameSceneAutoCheck : MonoBehaviour
{
    [SerializeField] private GameObject playerObject;

    private string _message = "점검 대기 중";
    private GUIStyle _style;

    private void Start()
    {
        if (playerObject == null)
        {
            Fail("실패: Player 오브젝트가 비어 있습니다. GameManager에 Player를 연결해주세요.",
                "[GameSceneAutoCheck] Player 오브젝트가 비어 있습니다. GameManager 오브젝트의 GameSceneAutoCheck 컴포넌트에 Player를 연결해주세요.");
            return;
        }

        if (!playerObject.activeInHierarchy)
        {
            Fail("실패: Player 오브젝트가 꺼져 있습니다. 체크박스를 켜주세요.",
                "[GameSceneAutoCheck] Player 오브젝트가 비활성화 상태입니다. Hierarchy에서 Player 체크박스를 켜주세요.");
            return;
        }

        var mover = playerObject.GetComponent<PlayerSimpleMover>();
        if (mover == null)
        {
            Fail("실패: PlayerSimpleMover 컴포넌트가 없습니다.",
                "[GameSceneAutoCheck] Player 오브젝트에 PlayerSimpleMover 컴포넌트가 없습니다. Add Component로 추가해주세요.");
            return;
        }

        if (!mover.enabled)
        {
            Fail("실패: PlayerSimpleMover 컴포넌트가 꺼져 있습니다.",
                "[GameSceneAutoCheck] PlayerSimpleMover 컴포넌트가 비활성화 상태입니다. 컴포넌트 체크박스를 켜주세요.");
            return;
        }

        var cc = playerObject.GetComponent<CharacterController>();
        if (cc == null)
        {
            Fail("실패: CharacterController가 없습니다.",
                "[GameSceneAutoCheck] Player 오브젝트에 CharacterController가 없습니다. 중력과 바닥 충돌을 위해 추가해주세요.");
            return;
        }

        Collider extra = playerObject.GetComponent<CapsuleCollider>();
        if (extra != null && extra.enabled)
        {
            Debug.LogWarning(
                "[GameSceneAutoCheck] Player에 CapsuleCollider가 켜져 있습니다. CharacterController만 쓰려면 CapsuleCollider를 끄거나 제거하는 것을 권장합니다.");
        }

        _message = "성공: Player, 이동 스크립트, CharacterController 연결이 정상입니다.";
        if (extra != null && extra.enabled) _message += " (CapsuleCollider도 켜져 있음 — 겹침 주의)";
        Debug.Log("[GameSceneAutoCheck] 점검 완료: 게임 시작에 필요한 연결 상태가 정상입니다.");
    }

    private void Fail(string screenMessage, string consoleMessage)
    {
        _message = screenMessage;
        Debug.LogError(consoleMessage);
    }

    private void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            _style.normal.textColor = Color.yellow;
        }

        GUI.Label(new Rect(10f, 40f, 1400f, 30f), $"[자동 점검] {_message}", _style);
    }
}
