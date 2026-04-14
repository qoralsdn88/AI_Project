using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerSimpleMover : MonoBehaviour
{
    private CharacterController characterController;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float groundedStickY = -2f;

    private float verticalVelocity;

    [SerializeField] private bool showOnScreenDebug = true;
    private float nextMoveLogTime;
    private bool hasShownNoInputLog;
    private string statusMessage = "대기 중";
    private GUIStyle debugStyle;

    [SerializeField] private PlayerMeleeCombat meleeCombat;
    [SerializeField] private SimplePlayerHealth playerHealth;
    [SerializeField] private PlayerPotionUseController potionUseController;

    private float lastInputTime;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
            Debug.LogError("[PlayerSimpleMover] CharacterController가 없습니다. Add Component로 추가해주세요.");
        if (meleeCombat == null) meleeCombat = PlayerMeleeCombat.Resolve(transform);
        if (playerHealth == null) playerHealth = SimplePlayerHealth.Resolve(transform);
        if (potionUseController == null) potionUseController = GetComponent<PlayerPotionUseController>();
    }

    private void OnEnable() => statusMessage = "컴포넌트 활성화됨";

    private void Start() => statusMessage = "Start 완료, 입력 대기 중 (중력 적용)";

    private void Update()
    {
        if (characterController == null) return;

        Vector2 moveInput = ReadMoveInput();

        Vector3 rightDir = transform.right;
        Vector3 forwardDir = transform.forward;
        rightDir.y = 0f;
        forwardDir.y = 0f;
        rightDir.Normalize();
        forwardDir.Normalize();

        Vector3 moveDirection = (rightDir * moveInput.x + forwardDir * moveInput.y).normalized;
        Vector3 horizontalMotion = moveDirection * moveSpeed * Time.deltaTime;
        if (BlacksmithGameplayLock.IsMenuOpen) horizontalMotion = Vector3.zero;
        if (meleeCombat != null && meleeCombat.IsAttacking) horizontalMotion = Vector3.zero;
        if (playerHealth != null && playerHealth.IsActionLocked) horizontalMotion = Vector3.zero;
        if (potionUseController != null && potionUseController.IsDrinking) horizontalMotion = Vector3.zero;

        bool isGrounded = characterController.isGrounded;
        if (isGrounded && verticalVelocity < 0f) verticalVelocity = groundedStickY;
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 verticalMotion = new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime;
        characterController.Move(horizontalMotion + verticalMotion);

        if (moveInput.sqrMagnitude > 0f && Time.time >= nextMoveLogTime)
        {
            nextMoveLogTime = Time.time + 0.5f;
            lastInputTime = Time.time;
            statusMessage = $"입력 (X:{moveInput.x}, Y:{moveInput.y}) | 땅: {characterController.isGrounded}";
        }

        if (moveInput.sqrMagnitude == 0f && Time.time > 1f && !hasShownNoInputLog)
        {
            hasShownNoInputLog = true;
            Debug.LogWarning(
                "[PlayerSimpleMover] 입력값이 계속 0입니다. Player의 스크립트 연결, 이동 속도, 그리고 Project Settings > Player > Active Input Handling 값을 확인해주세요.");
            statusMessage = "입력값 0 상태입니다. 연결/설정을 확인해주세요.";
        }
    }

    private void OnGUI()
    {
        if (!showOnScreenDebug) return;
        if (debugStyle == null)
        {
            debugStyle = new GUIStyle(GUI.skin.label) { fontSize = 18 };
            debugStyle.normal.textColor = Color.white;
        }

        string groundedText = characterController != null ? characterController.isGrounded.ToString() : "N/A";
        string debugText =
            $"PlayerSimpleMover: {statusMessage} | 땅: {groundedText} | 세로속도: {verticalVelocity:0.00} | 입력 후 경과: {(Time.time - lastInputTime):0.00}초";
        GUI.Label(new Rect(10f, 10f, 1200f, 30f), debugText, debugStyle);
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 legacyInput = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
        legacyInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        Vector2 newInput = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.isPressed || Keyboard.current.dKey.isPressed) newInput.x += 1f;
            if (Keyboard.current.leftArrowKey.isPressed || Keyboard.current.aKey.isPressed) newInput.x -= 1f;
            if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.wKey.isPressed) newInput.y += 1f;
            if (Keyboard.current.downArrowKey.isPressed || Keyboard.current.sKey.isPressed) newInput.y -= 1f;
        }
#endif
        Vector2 finalInput = newInput.sqrMagnitude > 0f ? newInput : legacyInput;
        return Vector2.ClampMagnitude(finalInput, 1f);
    }
}
