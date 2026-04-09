using UnityEngine; // 오브젝트와 색·시간 계산을 쓰기 위해 가져옵니다.

// 오크 어쌔신처럼 주기적으로 은신했다가, 공격하거나 맞으면 은신이 풀리게 만드는 스크립트입니다.
// 핵심 요약: 타이머로 은신을 켜고, 은신 중에는 투명 전용 재질로 바꾼 뒤, 공격 창이 열리거나 플레이어에게 맞으면 원래 재질로 되돌립니다.
public class MonsterOrcAssassinStealthSimple : MonoBehaviour
{
    [Header("다른 스크립트 연결")] // 인스펙터에서 수동으로 묶어 보여주는 제목 줄입니다.
    [SerializeField] private MonsterAttackSimple attackSource; // 공격이 시작됐는지 확인할 때 쓰는 공격 스크립트 연결입니다.
    [SerializeField] private MonsterDetectChaseSimple detectChase; // 전투 시작 여부와 추격 정지를 제어할 때 쓰는 추격 스크립트 연결입니다.
    [SerializeField] private Animator animator; // 은신 시전 애니메이션 트리거를 보낼 애니메이터 연결입니다.
    [SerializeField] private Renderer[] extraRenderersToHide; // 몸 말고 따로 숨기고 싶은 메시가 있으면 추가로 넣습니다.

    [Header("은신 타이밍(초 단위)")] // 숫자는 전부 초 기준으로 생각하면 됩니다.
    [SerializeField] private float stealthIntervalSeconds = 6f; // 전투 중 이 시간마다 은신을 다시 시도합니다.
    [SerializeField] private float stealthMaxDurationSeconds = 5f; // 은신이 풀리지 않으면 최대 이 시간만 은신합니다.
    [SerializeField] private float afterBreakCooldownSeconds = 2f; // 맞거나 공격해서 은신이 풀린 뒤 다시 은신을 막는 짧은 쉬는 시간입니다.
    [SerializeField] private float stealthCastStopSeconds = 1.5f; // 은신을 쓰기 직전에 멈춰 서서 시전하는 시간입니다.
    [SerializeField] private bool castImmediatelyOnDetect = true; // 플레이어를 처음 감지한 순간 바로 은신 시전을 시작할지 정하는 옵션입니다.
    [SerializeField] private string stealthCastTriggerParam = "StealthCast"; // 은신 시전 시작 때 보낼 트리거 파라미터 이름입니다.

    [Header("은신 때 보이는 정도")] // 완전 투명은 너무 싸워지기 어려우니 살짝 남겨둘 수 있습니다.
    [SerializeField] [Range(0f, 1f)] private float stealthAlpha = 0.15f; // 은신 중 불투명도(1이 원래, 0이 완전 투명)입니다.
    [SerializeField] private string colorPropertyName = "_BaseColor"; // URP Lit에서 자주 쓰는 색 이름입니다.
    [SerializeField] private string secondaryColorPropertyName = "_Color"; // 일부 셰이더는 이 이름을 씁니다.

    [Header("은신 이펙트(선택)")] // 없어도 동작하고, 있으면 은신 중에만 켜집니다.
    [SerializeField] private GameObject stealthVfxRoot; // 파티클/연기 프리팹을 자식으로 두고 그 뿌리를 여기 연결합니다.

    public bool IsStealthed { get; private set; } // 지금 은신 상태인지 다른 스크립트에서 읽을 수 있게 합니다.

    private float _intervalTimerSeconds; // 다음 은신 시도까지 남은 시간을 줄여 가는 변수입니다.
    private float _stealthRemainSeconds; // 은신 유지 시간이 얼마나 남았는지 줄여 가는 변수입니다.
    private float _cooldownAfterBreakTimerSeconds; // 은신이 강제로 풀린 뒤 대기 시간을 줄여 가는 변수입니다.
    private bool _wasWeaponWindowActive; // 지난 프레임에 무기 피해 창이 켜져 있었는지 기억합니다.
    private bool _isCastingStealth; // 지금 은신 시전 정지 시간인지 기록하는 변수입니다.
    private float _castRemainSeconds; // 은신 시전 정지 시간이 얼마나 남았는지 줄여 가는 변수입니다.
    private bool _restoreChaseEnabledAfterCast; // 시전이 끝난 뒤 추격 스크립트를 다시 켤지 기억하는 변수입니다.
    private bool _restoreAttackEnabledAfterCast; // 시전이 끝난 뒤 공격 스크립트를 다시 켤지 기억하는 변수입니다.
    private bool _wasInCombatLastFrame; // 지난 프레임에 전투 상태였는지 기록해 감지 시작 순간을 잡는 변수입니다.

    private Renderer[] _renderers; // 몸·옷 등 색을 바꿀 메시 렌더러 목록을 보관합니다.
    private Material[][] _normalRuntimeMaterials; // 평소(원래) 상태에서 쓸 재질 복사본 배열입니다.
    private Material[][] _stealthRuntimeMaterials; // 은신 상태에서만 쓸 투명 재질 복사본 배열입니다.

    private void Awake() // 게임이 처음 올라올 때 한 번만 준비합니다.
    {
        if (attackSource == null) { TryGetComponent(out attackSource); } // 공격 스크립트가 비어 있으면 같은 오브젝트에서 찾습니다.
        if (detectChase == null) { TryGetComponent(out detectChase); } // 추격 스크립트가 비어 있으면 같은 오브젝트에서 찾습니다.
        if (animator == null) { animator = GetComponentInChildren<Animator>(); } // 애니메이터가 비어 있으면 자식에서 자동으로 찾습니다.
        _renderers = CollectRenderableMeshesInChildren(); // 자식 중에서 실제 메시만 골라 목록을 만듭니다.
        CacheAndBranchMaterials(); // 재질을 복사하고 원래 색을 저장해 둡니다.
        _intervalTimerSeconds = Mathf.Max(0.1f, stealthIntervalSeconds); // 첫 은신까지 대기 시간을 시작값으로 넣습니다.
    } // Awake 함수 끝입니다.

    private void OnEnable() // 오브젝트가 켜질 때마다 안전하게 초기 표시로 돌립니다.
    {
        _wasWeaponWindowActive = false; // 공격 창 추적을 처음부터 다시 시작합니다.
        ForceVisibleInstant(); // 은신 색과 이펙트를 즉시 보이는 상태로 맞춥니다.
        IsStealthed = false; // 은신 여부를 거짓으로 고정합니다.
        _stealthRemainSeconds = 0f; // 남은 은신 시간을 0으로 비웁니다.
        _cooldownAfterBreakTimerSeconds = 0f; // 강제 해제 쿨도 0으로 비웁니다.
        _isCastingStealth = false; // 시전 상태를 꺼 둡니다.
        _castRemainSeconds = 0f; // 시전 남은 시간을 0으로 맞춥니다.
        _restoreChaseEnabledAfterCast = false; // 복원 플래그를 초기화합니다.
        _restoreAttackEnabledAfterCast = false; // 복원 플래그를 초기화합니다.
        _wasInCombatLastFrame = false; // 전투 시작 체크 상태를 초기화합니다.
    } // OnEnable 함수 끝입니다.

    private void OnDisable() // 오브젝트가 꺼질 때 시전 중 비활성화한 컴포넌트를 원상복구합니다.
    {
        RestoreMovementAndAttackAfterCast(); // 추격/공격이 꺼진 채 남지 않도록 안전 복원을 실행합니다.
    } // OnDisable 함수 끝입니다.

    private void Update() // 매 프레임 은신 타이머와 해제 조건을 업데이트합니다.
    {
        if (_cooldownAfterBreakTimerSeconds > 0f) { _cooldownAfterBreakTimerSeconds -= Time.deltaTime; } // 강제 해제 쿨이 있으면 줄입니다.

        TryBreakStealthBecauseAttackStarted(); // 이번 프레임에 공격 피해 창이 켜졌는지 검사하고 은신을 끕니다.

        if (_isCastingStealth) // 지금 은신 시전 정지 중이면
        {
            TickStealthCastDuration(); // 시전 정지 시간을 줄이고 끝나면 은신을 켭니다.
            return; // 시전 중에는 다른 로직을 돌리지 않습니다.
        } // if 끝입니다.

        if (IsStealthed) // 지금 은신 중이면
        {
            TickStealthDuration(); // 은신 최대 시간을 줄이고 시간이 다 되면 해제합니다.
            return; // 은신 중에는 주기 타이머를 줄이지 않고 여기서 멈춥니다.
        } // if 끝입니다.

        bool isInCombatNow = IsInCombatNow(); // 이번 프레임의 전투 상태를 먼저 읽어 둡니다.
        bool justEnteredCombat = isInCombatNow && !_wasInCombatLastFrame; // 전투가 방금 시작된 순간인지 계산합니다.
        _wasInCombatLastFrame = isInCombatNow; // 다음 프레임 비교를 위해 현재 전투 상태를 저장합니다.

        if (!isInCombatNow) // 아직 전투가 시작되지 않았으면
        {
            _intervalTimerSeconds = Mathf.Max(0.1f, stealthIntervalSeconds); // 전투 전에는 타이머를 초기값으로 유지해 미리 은신하지 않게 합니다.
            return; // 전투 전이므로 은신 시도를 멈춥니다.
        } // if 끝입니다.

        if (justEnteredCombat && castImmediatelyOnDetect) // 감지한 바로 그 순간 즉시 시전 옵션이 켜져 있으면
        {
            if (_cooldownAfterBreakTimerSeconds <= 0f) // 강제 해제 쿨이 없을 때만 바로 시전합니다.
            {
                BeginStealthCastStop(); // 감지 순간 즉시 은신 시전을 시작합니다.
                _intervalTimerSeconds = Mathf.Max(0.1f, stealthIntervalSeconds); // 다음 주기 타이머를 다시 채웁니다.
                return; // 이번 프레임 처리는 여기서 끝냅니다.
            } // if 끝입니다.
        } // if 끝입니다.

        if (_cooldownAfterBreakTimerSeconds > 0f) return; // 강제 해제 쿨이 남아 있으면 아직 은신을 시도하지 않습니다.

        _intervalTimerSeconds -= Time.deltaTime; // 다음 은신까지 남은 시간을 줄입니다.
        if (_intervalTimerSeconds > 0f) return; // 아직 시간이 남았으면 기다립니다.

        BeginStealthCastStop(); // 시간이 다 되면 먼저 멈춰 서서 은신 시전을 시작합니다.
        _intervalTimerSeconds = Mathf.Max(0.1f, stealthIntervalSeconds); // 다음 주기를 다시 채워 넣습니다.
    } // Update 함수 끝입니다.

    public void NotifyHitByPlayer() // 플레이어 공격에 맞았을 때 외부(체력 스크립트)가 호출하는 함수입니다.
    {
        CancelStealthCastBecauseHit(); // 맞았을 때 시전 중이면 시전을 취소합니다.
        BreakStealthBecauseHit(); // 맞으면 은신을 즉시 끕니다.
    } // NotifyHitByPlayer 함수 끝입니다.

    private void TryBreakStealthBecauseAttackStarted() // 공격이 시작됐는지 매 프레임 비교합니다.
    {
        if (attackSource == null) return; // 공격 스크립트가 없으면 검사를 하지 않습니다.
        bool windowActive = attackSource.IsWeaponDamageWindowActive; // 지금 무기 판정 창이 열려 있는지 읽습니다.
        bool edgeStarted = windowActive && !_wasWeaponWindowActive; // 이번에 막 켜진 순간인지 이전 값과 비교합니다.
        _wasWeaponWindowActive = windowActive; // 다음 프레임 비교를 위해 현재 값을 저장합니다.
        if (!edgeStarted) return; // 막 켜진 순간이 아니면 은신 해제를 하지 않습니다.
        BreakStealthBecauseAttack(); // 몬스터가 공격을 시작했으므로 은신을 끕니다.
    } // TryBreakStealthBecauseAttackStarted 함수 끝입니다.

    private void TickStealthDuration() // 은신 유지 시간을 줄입니다.
    {
        _stealthRemainSeconds -= Time.deltaTime; // 남은 은신 시간을 매 프레임 줄입니다.
        if (_stealthRemainSeconds > 0f) return; // 아직 시간이 남았으면 유지합니다.
        ExitStealthBecauseTimeUp(); // 시간이 다 되면 자연스럽게 은신을 끕니다.
    } // TickStealthDuration 함수 끝입니다.

    private bool IsInCombatNow() // 전투가 시작된 상태인지 확인하는 함수입니다.
    {
        if (detectChase == null) return false; // 추격 스크립트가 없으면 전투 상태를 알 수 없으니 전투 아님으로 처리합니다.
        return detectChase.IsDetected; // 플레이어를 감지한 상태면 전투 중으로 봅니다.
    } // IsInCombatNow 함수 끝입니다.

    private void BeginStealthCastStop() // 은신 직전 1.5초 멈추는 시전을 시작합니다.
    {
        PlayStealthCastAnimation(); // 시전 시작과 동시에 은신 시전 애니메이션 트리거를 보냅니다.
        _isCastingStealth = true; // 시전 중 플래그를 켭니다.
        _castRemainSeconds = Mathf.Max(0.05f, stealthCastStopSeconds); // 시전 시간을 최소값 이상으로 채웁니다.

        _restoreChaseEnabledAfterCast = detectChase != null && detectChase.enabled; // 원래 추격이 켜져 있었는지 저장합니다.
        _restoreAttackEnabledAfterCast = attackSource != null && attackSource.enabled; // 원래 공격이 켜져 있었는지 저장합니다.

        if (detectChase != null) { detectChase.enabled = false; } // 시전 중에는 추격 이동을 멈추기 위해 추격 스크립트를 끕니다.
        if (attackSource != null) { attackSource.enabled = false; } // 시전 중에는 공격이 나가지 않게 공격 스크립트를 끕니다.
    } // BeginStealthCastStop 함수 끝입니다.

    private void TickStealthCastDuration() // 은신 시전 정지 시간을 줄이고 완료를 처리합니다.
    {
        _castRemainSeconds -= Time.deltaTime; // 남은 시전 시간을 매 프레임 줄입니다.
        if (_castRemainSeconds > 0f) return; // 아직 시간이 남았으면 계속 멈춰 있습니다.

        _isCastingStealth = false; // 시전 플래그를 끕니다.
        RestoreMovementAndAttackAfterCast(); // 시전 중 껐던 추격/공격을 원래 상태로 복원합니다.
        EnterStealth(); // 시전이 끝났으므로 실제 은신을 켭니다.
    } // TickStealthCastDuration 함수 끝입니다.

    private void CancelStealthCastBecauseHit() // 맞았을 때 진행 중인 은신 시전을 취소합니다.
    {
        if (!_isCastingStealth) return; // 시전 중이 아니면 취소할 것이 없습니다.
        _isCastingStealth = false; // 시전 플래그를 끕니다.
        _castRemainSeconds = 0f; // 남은 시전 시간을 비웁니다.
        RestoreMovementAndAttackAfterCast(); // 시전 중 껐던 추격/공격을 원래 상태로 복원합니다.
        _cooldownAfterBreakTimerSeconds = Mathf.Max(0f, afterBreakCooldownSeconds); // 맞아서 취소된 경우 재시전을 잠깐 막습니다.
    } // CancelStealthCastBecauseHit 함수 끝입니다.

    private void RestoreMovementAndAttackAfterCast() // 시전 때문에 껐던 컴포넌트를 원래 값으로 복원합니다.
    {
        if (detectChase != null && _restoreChaseEnabledAfterCast) { detectChase.enabled = true; } // 원래 켜져 있었던 추격만 다시 켭니다.
        if (attackSource != null && _restoreAttackEnabledAfterCast) { attackSource.enabled = true; } // 원래 켜져 있었던 공격만 다시 켭니다.
        _restoreChaseEnabledAfterCast = false; // 복원 플래그를 지워 다음 시전을 준비합니다.
        _restoreAttackEnabledAfterCast = false; // 복원 플래그를 지워 다음 시전을 준비합니다.
    } // RestoreMovementAndAttackAfterCast 함수 끝입니다.

    private void PlayStealthCastAnimation() // 은신 시전 애니메이션 트리거를 안전하게 재생하는 함수입니다.
    {
        if (animator == null) return; // 애니메이터가 없으면 애니메이션 호출을 건너뜁니다.
        if (string.IsNullOrEmpty(stealthCastTriggerParam)) return; // 트리거 이름이 비어 있으면 호출을 건너뜁니다.
        if (!HasTriggerParameter(stealthCastTriggerParam)) return; // Animator에 트리거가 없으면 오류 없이 건너뜁니다.
        animator.ResetTrigger(stealthCastTriggerParam); // 같은 트리거가 쌓이지 않게 먼저 초기화합니다.
        animator.SetTrigger(stealthCastTriggerParam); // 은신 시전 애니메이션 트리거를 실제로 보냅니다.
    } // PlayStealthCastAnimation 함수 끝입니다.

    private bool HasTriggerParameter(string paramName) // Animator에 트리거 파라미터가 있는지 확인하는 함수입니다.
    {
        if (animator == null) return false; // 애니메이터가 없으면 없다고 반환합니다.
        if (string.IsNullOrEmpty(paramName)) return false; // 이름이 비어 있으면 없다고 반환합니다.
        AnimatorControllerParameter[] parameters = animator.parameters; // Animator 파라미터 목록을 가져옵니다.
        for (int i = 0; i < parameters.Length; i++) // 목록을 앞에서부터 하나씩 검사합니다.
        {
            if (parameters[i].type != AnimatorControllerParameterType.Trigger) continue; // 트리거 타입이 아니면 건너뜁니다.
            if (parameters[i].name != paramName) continue; // 이름이 다르면 건너뜁니다.
            return true; // 타입과 이름이 모두 맞으면 있다고 반환합니다.
        } // for 끝입니다.
        return false; // 끝까지 못 찾았으면 없다고 반환합니다.
    } // HasTriggerParameter 함수 끝입니다.

    private void EnterStealth() // 은신을 켜고 보이기 값을 조절합니다.
    {
        IsStealthed = true; // 은신 상태 플래그를 참으로 바꿉니다.
        _stealthRemainSeconds = Mathf.Max(0.1f, stealthMaxDurationSeconds); // 최대 유지 시간을 다시 채웁니다.
        ApplyStealthVisual(true); // 재질 투명도와 이펙트를 은신용으로 바꿉니다.
    } // EnterStealth 함수 끝입니다.

    private void BreakStealthBecauseHit() // 플레이어에게 맞았을 때 은신을 끕니다.
    {
        if (!IsStealthed) return; // 원래 보이는 상태면 할 일이 없습니다.
        ExitStealthBecauseInterrupted(); // 강제 해제와 동일하게 정리합니다.
        _cooldownAfterBreakTimerSeconds = Mathf.Max(0f, afterBreakCooldownSeconds); // 잠깐 재은신을 막는 쿨을 겁니다.
    } // BreakStealthBecauseHit 함수 끝입니다.

    private void BreakStealthBecauseAttack() // 몬스터가 공격을 시작했을 때 은신을 끕니다.
    {
        if (!IsStealthed) return; // 은신 중이 아니면 무시합니다.
        ExitStealthBecauseInterrupted(); // 강제 해제 처리로 돌아갑니다.
        _cooldownAfterBreakTimerSeconds = Mathf.Max(0f, afterBreakCooldownSeconds); // 재은신 쿨을 겁니다.
    } // BreakStealthBecauseAttack 함수 끝입니다.

    private void ExitStealthBecauseTimeUp() // 시간이 다 돼서 은신이 풀릴 때 씁니다.
    {
        IsStealthed = false; // 은신 상태를 거짓으로 바꿉니다.
        ApplyStealthVisual(false); // 원래 색과 이펙트 상태로 되돌립니다.
    } // ExitStealthBecauseTimeUp 함수 끝입니다.

    private void ExitStealthBecauseInterrupted() // 맞거나 공격해서 은신이 깨질 때 씁니다.
    {
        IsStealthed = false; // 은신 상태를 거짓으로 바꿉니다.
        ApplyStealthVisual(false); // 원래 색과 이펙트 상태로 되돌립니다.
    } // ExitStealthBecauseInterrupted 함수 끝입니다.

    private void ForceVisibleInstant() // 처음부터 보이게 강제로 맞춥니다.
    {
        ApplyStealthVisual(false); // 시각 값을 일반 상태로 돌립니다.
    } // ForceVisibleInstant 함수 끝입니다.

    private Renderer[] CollectRenderableMeshesInChildren() // 자식 중 메시 렌더러만 모아 중복 없이 반환합니다.
    {
        Renderer[] all = GetComponentsInChildren<Renderer>(true); // 모든 렌더러를 자식까지 찾습니다.
        if (all == null || all.Length == 0) return System.Array.Empty<Renderer>(); // 없으면 빈 배열을 돌려줍니다.
        var list = new System.Collections.Generic.List<Renderer>(); // 결과를 담을 리스트를 만듭니다.
        for (int i = 0; i < all.Length; i++) // 후보를 하나씩 검사합니다.
        {
            Renderer r = all[i]; // 현재 렌더러를 꺼냅니다.
            if (r == null) continue; // 비어 있으면 건너뜁니다.
            if (r is MeshRenderer || r is SkinnedMeshRenderer) { list.Add(r); } // 실제 메시만 넣습니다.
        } // for 끝입니다.
        if (extraRenderersToHide != null) // 추가로 반드시 포함할 렌더러가 있으면
        {
            for (int i = 0; i < extraRenderersToHide.Length; i++) // 추가 목록을 돕니다.
            {
                Renderer r = extraRenderersToHide[i]; // 추가 렌더러를 꺼냅니다.
                if (r == null) continue; // 비어 있으면 건너뜁니다.
                if (!list.Contains(r)) { list.Add(r); } // 아직 없을 때만 넣어 중복을 막습니다.
            } // for 끝입니다.
        } // if 끝입니다.
        return list.ToArray(); // 배열로 바꿔 돌려줍니다.
    } // CollectRenderableMeshesInChildren 함수 끝입니다.

    private void CacheAndBranchMaterials() // 평소 재질과 은신 전용 재질을 각각 인스턴스로 준비합니다.
    {
        if (_renderers == null || _renderers.Length == 0) return; // 메시가 없으면 준비를 건너뜁니다.
        _normalRuntimeMaterials = new Material[_renderers.Length][]; // 렌더러마다 평소 재질 배열을 둡니다.
        _stealthRuntimeMaterials = new Material[_renderers.Length][]; // 렌더러마다 은신 재질 배열을 둡니다.
        for (int i = 0; i < _renderers.Length; i++) // 렌더러를 앞에서부터 하나씩 처리합니다.
        {
            Material[] shared = _renderers[i].sharedMaterials; // 공유 재질 목록을 읽습니다.
            if (shared == null || shared.Length == 0) continue; // 재질이 없으면 건너뜁니다.
            var normalCopies = new Material[shared.Length]; // 평소 재질 복사본 배열을 만듭니다.
            var stealthCopies = new Material[shared.Length]; // 은신 재질 복사본 배열을 만듭니다.
            for (int m = 0; m < shared.Length; m++) // 슬롯마다 복사합니다.
            {
                normalCopies[m] = new Material(shared[m]); // 평소용 복사 재질을 만듭니다.
                stealthCopies[m] = new Material(shared[m]); // 은신용 복사 재질을 만듭니다.
                PrepareStealthMaterial(stealthCopies[m]); // 은신용 복사 재질을 투명 설정으로 바꿉니다.
            } // 내부 for 끝입니다.
            _renderers[i].materials = normalCopies; // 시작 시점에는 평소 재질이 보이게 적용합니다.
            _normalRuntimeMaterials[i] = normalCopies; // 평소 재질 배열을 보관합니다.
            _stealthRuntimeMaterials[i] = stealthCopies; // 은신 재질 배열을 보관합니다.
        } // 바깥 for 끝입니다.
    } // CacheAndBranchMaterials 함수 끝입니다.

    private void PrepareStealthMaterial(Material mat) // 은신 재질 1개를 투명 설정으로 바꾸는 함수입니다.
    {
        if (mat == null) return; // 재질이 없으면 종료합니다.

        // URP Lit 계열에서 투명 렌더로 바꾸기 위한 대표 속성을 순서대로 세팅합니다.
        if (mat.HasProperty("_Surface")) { mat.SetFloat("_Surface", 1f); } // Surface를 Transparent 값으로 설정합니다.
        if (mat.HasProperty("_Blend")) { mat.SetFloat("_Blend", 0f); } // 일반 알파 블렌드 모드로 맞춥니다.
        if (mat.HasProperty("_SrcBlend")) { mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); } // 소스 블렌드를 SrcAlpha로 설정합니다.
        if (mat.HasProperty("_DstBlend")) { mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); } // 목적지 블렌드를 OneMinusSrcAlpha로 설정합니다.
        if (mat.HasProperty("_ZWrite")) { mat.SetFloat("_ZWrite", 0f); } // 투명 렌더에서 깊이 기록을 꺼 겹침 문제를 줄입니다.

        Color c = ReadMaterialColor(mat); // 현재 재질의 기준 색을 읽습니다.
        c.a = Mathf.Clamp01(stealthAlpha); // 은신 때 쓸 알파 값으로 낮춥니다.
        WriteMaterialColor(mat, c); // 은신용 재질 색에 낮은 알파를 적용합니다.

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); // URP 투명 키워드를 켭니다.
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 렌더 큐를 투명 순서로 바꿉니다.
    } // PrepareStealthMaterial 함수 끝입니다.

    private Color ReadMaterialColor(Material mat) // 재질에서 색을 읽는 공통 함수입니다.
    {
        if (mat == null) return Color.white; // 재질이 없으면 흰색을 반환합니다.
        if (mat.HasProperty(colorPropertyName)) return mat.GetColor(colorPropertyName); // 첫 번째 색 속성 이름이 있으면 읽습니다.
        if (mat.HasProperty(secondaryColorPropertyName)) return mat.GetColor(secondaryColorPropertyName); // 두 번째 색 속성 이름이 있으면 읽습니다.
        return Color.white; // 둘 다 없으면 흰색을 반환합니다.
    } // ReadMaterialColor 함수 끝입니다.

    private void WriteMaterialColor(Material mat, Color c) // 재질에 색을 쓰는 공통 함수입니다.
    {
        if (mat == null) return; // 재질이 없으면 종료합니다.
        if (mat.HasProperty(colorPropertyName)) { mat.SetColor(colorPropertyName, c); return; } // 첫 번째 색 속성이 있으면 기록하고 종료합니다.
        if (mat.HasProperty(secondaryColorPropertyName)) { mat.SetColor(secondaryColorPropertyName, c); } // 두 번째 색 속성이 있으면 기록합니다.
    } // WriteMaterialColor 함수 끝입니다.

    private void ApplyStealthVisual(bool enabled) // 은신 보이기를 켜거나 끕니다.
    {
        if (_renderers == null || _normalRuntimeMaterials == null || _stealthRuntimeMaterials == null) return; // 준비가 안 됐으면 종료합니다.
        for (int i = 0; i < _renderers.Length; i++) // 렌더러 슬롯마다 처리합니다.
        {
            if (_renderers[i] == null) continue; // 렌더러가 비어 있으면 건너뜁니다.
            Material[] target = enabled ? _stealthRuntimeMaterials[i] : _normalRuntimeMaterials[i]; // 상태에 맞는 재질 배열을 고릅니다.
            if (target == null || target.Length == 0) continue; // 대상 재질이 없으면 건너뜁니다.
            _renderers[i].materials = target; // 은신이면 투명 재질, 해제면 원래 재질로 통째로 교체합니다.
        } // 바깥 for 끝입니다.

        if (stealthVfxRoot == null) return; // 이펙트 뿌리가 없으면 여기서 종료합니다.
        stealthVfxRoot.SetActive(enabled); // 은신 중에만 이펙트 오브젝트를 켭니다.
    } // ApplyStealthVisual 함수 끝입니다.
}
