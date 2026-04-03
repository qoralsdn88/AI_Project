// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;
// 신 입력 시스템이 켜져 있을 때만 이 기능을 사용하도록 준비합니다.
#if ENABLE_INPUT_SYSTEM
// 마우스 움직임을 직접 읽기 위해 필요합니다.
using UnityEngine.InputSystem;
#endif

// 마우스로 플레이어를 회전시키고 카메라 시점을 움직이는 스크립트입니다.
public class PlayerMouseLook : MonoBehaviour
{
    // 카메라가 붙어 있을 피벗(회전 중심) 오브젝트입니다.
    // 핵심 요약: cameraPivot을 기준으로 위아래 회전을 적용합니다.
    [SerializeField] private Transform cameraPivot;

    // 마우스를 좌우로 움직였을 때 회전 속도를 조절하는 값입니다.
    // 핵심 요약: mouseSensitivityX가 클수록 좌우 회전이 더 빨라집니다.
    [SerializeField] private float mouseSensitivityX = 3f;

    // 마우스를 위아래로 움직였을 때 회전 속도를 조절하는 값입니다.
    // 핵심 요약: mouseSensitivityY가 클수록 위아래 회전이 더 빨라집니다.
    [SerializeField] private float mouseSensitivityY = 3f;

    // 카메라가 위를 볼 수 있는 최대 각도입니다.
    // 핵심 요약: minPitch는 보통 음수로 설정해 위를 보는 정도를 제한합니다.
    [SerializeField] private float minPitch = -40f;

    // 카메라가 아래를 볼 수 있는 최대 각도입니다.
    // 핵심 요약: maxPitch는 양수로 설정해 아래를 보는 정도를 제한합니다.
    [SerializeField] private float maxPitch = 70f;

    // 현재 카메라의 위아래 회전 각도를 저장하는 변수입니다.
    // 핵심 요약: currentPitch는 누적된 위아래 회전을 기억합니다.
    private float currentPitch = 0f;

    // 게임 시작 시 한 번 실행되는 함수입니다.
    // 핵심 요약: Start에서 카메라 피벗을 점검하고 마우스 커서를 잠급니다.
    private void Start()
    {
        // cameraPivot이 비어 있으면 경고를 띄워서 연결을 요청합니다.
        if (cameraPivot == null)
        {
            // 개발자가 바로 원인을 알 수 있도록 노란 경고 로그를 남깁니다.
            Debug.LogWarning("[PlayerMouseLook] cameraPivot이 비어 있습니다. 인스펙터에서 CameraPivot을 연결해주세요.");
        }

        // 게임 플레이 중에는 마우스 커서를 화면 중앙에 고정합니다.
        Cursor.lockState = CursorLockMode.Locked;
        // 게임 중에는 마우스 포인터를 숨겨서 몰입감을 높입니다.
        Cursor.visible = false;
    }

    // 매 프레임마다 자동으로 실행되는 함수입니다.
    // 핵심 요약: 마우스 움직임을 읽어 캐릭터와 카메라를 회전시킵니다.
    private void Update()
    {
        // 마우스 입력을 읽어서 좌우와 위아래 이동량을 가져옵니다.
        Vector2 mouseDelta = ReadMouseDelta();

        // 마우스 좌우 이동량으로 Y축 회전값을 계산합니다.
        float yaw = mouseDelta.x * mouseSensitivityX;
        // 마우스 위아래 이동량으로 피치(위아래 각도)를 계산합니다.
        // 위로 마우스를 올리면 아래를 보게 하고 싶다면 부호를 반대로 합니다.
        float pitch = -mouseDelta.y * mouseSensitivityY;

        // 캐릭터의 Y축(좌우) 회전을 누적해서 적용합니다.
        // transform.Rotate는 현재 회전에 yaw 만큼 더해 줍니다.
        transform.Rotate(0f, yaw, 0f);

        // cameraPivot이 없으면 카메라 회전은 건너뜁니다.
        if (cameraPivot == null)
        {
            // 더 진행하지 않고 함수를 바로 종료합니다.
            return;
        }

        // 위아래 각도를 누적해서 현재 피치 값에 더해 줍니다.
        currentPitch += pitch;
        // 위아래 각도가 설정한 최소/최대 범위를 벗어나지 않도록 묶습니다.
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // 카메라 피벗의 로컬 회전을 위아래 각도만 적용한 상태로 만듭니다.
        // Vector3.right는 X축이므로 X축 기준으로만 위아래 회전을 넣습니다.
        cameraPivot.localEulerAngles = new Vector3(currentPitch, 0f, 0f);
    }

    // 구 입력과 신 입력을 함께 지원하는 마우스 이동 읽기 함수입니다.
    // 핵심 요약: 새 입력이 있으면 새 입력을, 없으면 옛 입력 축을 사용합니다.
    private Vector2 ReadMouseDelta()
    {
        // 구 입력 시스템에서 마우스 이동을 읽기 위한 변수입니다.
        Vector2 legacyDelta = Vector2.zero;

        // 구 입력 시스템이 켜져 있으면 Mouse X/Y 축을 사용합니다.
#if ENABLE_LEGACY_INPUT_MANAGER
        // 좌우 움직임은 Mouse X 축에서 읽습니다.
        float mouseX = Input.GetAxis("Mouse X");
        // 위아래 움직임은 Mouse Y 축에서 읽습니다.
        float mouseY = Input.GetAxis("Mouse Y");
        // 두 값을 합쳐서 하나의 2차원 값으로 만듭니다.
        legacyDelta = new Vector2(mouseX, mouseY);
#endif

        // 신 입력 시스템에서 마우스 이동을 읽기 위한 변수입니다.
        Vector2 newDelta = Vector2.zero;

        // 신 입력 시스템이 켜져 있을 때만 Mouse.current를 사용합니다.
#if ENABLE_INPUT_SYSTEM
        // 마우스 장치가 실제로 있는지 먼저 확인합니다.
        if (Mouse.current != null)
        {
            // 이번 프레임 동안 움직인 마우스 거리를 읽습니다.
            newDelta = Mouse.current.delta.ReadValue();
            // 화면 해상도에 따라 너무 크게 느껴질 수 있어 간단히 줄여 줍니다.
            newDelta *= 0.05f;
        }
#endif

        // 새 입력이 0이 아니면 새 입력을 우선으로 사용합니다.
        if (newDelta.sqrMagnitude > 0f)
        {
            // 신 입력 시스템에서 읽은 값을 그대로 반환합니다.
            return newDelta;
        }

        // 그렇지 않으면 구 입력 시스템에서 읽은 값을 사용합니다.
        return legacyDelta;
    }
}