// 유니티 기본 기능을 사용하기 위해 꼭 필요합니다.
using UnityEngine;

// 방패와 칼 프리팹을 손 본에 붙이고, 칼에 맞춤 트리거를 만듭니다.
// 핵심 요약: Humanoid의 왼손·오른손 본 위치를 Animator에서 가져옵니다.
public class PlayerEquipmentHolder : MonoBehaviour
{
    // 왼손에 붙일 방패 프리팹입니다.
    // 핵심 요약: Shield 프리팹을 넣습니다.
    [SerializeField] private GameObject shieldPrefab;
    // 오른손에 붙일 칼 프리팹입니다.
    // 핵심 요약: LongSword 프리팹을 넣습니다.
    [SerializeField] private GameObject longSwordPrefab;

    // 본을 찾을 Animator입니다.
    // 핵심 요약: 비어 있으면 자식에서 찾습니다.
    [SerializeField] private Animator targetAnimator;

    // 플레이어 최상위(태그 Player를 붙인 오브젝트)입니다.
    // 핵심 요약: 맞춤 무시 판정에 씁니다.
    [SerializeField] private Transform playerRoot;

    // 칼에 붙일 전투 스크립트입니다.
    // 핵심 요약: 비어 있으면 같은 오브젝트에서 찾습니다.
    [SerializeField] private PlayerMeleeCombat meleeCombat;

    // 방패의 로컬 위치입니다.
    // 핵심 요약: 씬에서 손에 맞게 손으로 조절합니다.
    [SerializeField] private Vector3 shieldLocalPosition = Vector3.zero;
    // 방패의 로컬 회전(도 단위)입니다.
    // 핵심 요약: 인스펙터에서 보기 편하게 Euler로 둡니다.
    [SerializeField] private Vector3 shieldLocalEuler = Vector3.zero;
    // 방패의 로컬 스케일입니다.
    // 핵심 요약: 손 크기에 맞게 줄입니다.
    [SerializeField] private Vector3 shieldLocalScale = Vector3.one;
    [SerializeField] private bool lockShieldRotationDuringDefense = true;
    [SerializeField] private int shieldDefenseAnimatorLayer = 0;
    [SerializeField] private string[] shieldDefenseStateNames = { "ShieldImpact", "Block", "Guard" };

    // 칼의 로컬 위치입니다.
    // 핵심 요약: 손 안에 잡히게 옮깁니다.
    [SerializeField] private Vector3 swordLocalPosition = Vector3.zero;
    // 칼의 로컬 회전입니다.
    // 핵심 요약: 칼날 방향을 맞춥니다.
    [SerializeField] private Vector3 swordLocalEuler = Vector3.zero;
    // 칼의 로컬 스케일입니다.
    // 핵심 요약: 너무 크면 줄입니다.
    [SerializeField] private Vector3 swordLocalScale = Vector3.one;

    // 트리거 박스 크기입니다.
    // 핵심 요약: 칼 길이에 맞게 인스펙터에서 조절합니다.
    [SerializeField] private Vector3 swordTriggerSize = new Vector3(0.12f, 0.85f, 0.06f);
    // 트리거 박스 중심입니다.
    // 핵심 요약: 칼날 중간쯤 오도록 옮깁니다.
    [SerializeField] private Vector3 swordTriggerCenter = new Vector3(0f, 0.35f, 0.02f);

    // 준비 단계에서 참조를 맞춥니다.
    // 핵심 요약: 자동으로 비어 있는 참조를 채웁니다.
    private void Awake()
    {
        // 전투 스크립트: 같은 오브젝트 → 부모 쪽 순으로 찾습니다(프리팹 구조가 달라져도 연결되게).
        if (meleeCombat == null) { meleeCombat = GetComponent<PlayerMeleeCombat>(); }
        if (meleeCombat == null) { meleeCombat = GetComponentInParent<PlayerMeleeCombat>(); }

        // 플레이어 루트: 태그가 붙은 조상 → 전투 스크립트가 있는 오브젝트 → 이 스크립트의 루트 순입니다.
        if (playerRoot == null || !playerRoot.CompareTag("Player"))
        {
            Transform t = transform;
            while (t != null && !t.CompareTag("Player")) { t = t.parent; }
            if (t != null) { playerRoot = t; }
            else if (meleeCombat != null) { playerRoot = meleeCombat.transform; }
            else if (playerRoot == null) { playerRoot = transform.root; }
        }

        // 애니메이터가 비어 있으면 자식에서 찾습니다.
        if (targetAnimator == null) { targetAnimator = GetComponentInChildren<Animator>(); }
    }

    // 시작 시점에 장비를 실제로 붙입니다.
    // 핵심 요약: Start에서 하면 다른 Awake가 끝난 뒤입니다.
    private void Start()
    {
        // 애니메이터가 없으면 종료합니다.
        if (targetAnimator == null)
        {
            // 에러를 남깁니다.
            Debug.LogError("[PlayerEquipmentHolder] Animator가 없어 장비를 붙일 수 없습니다.");
            return;
        }

        // Humanoid 손 본을 가져옵니다.
        Transform leftHand = targetAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
        // 오른손 본을 가져옵니다.
        Transform rightHand = targetAnimator.GetBoneTransform(HumanBodyBones.RightHand);
        // 본이 없으면 Humanoid 설정이 아닌 것입니다.
        if (leftHand == null || rightHand == null)
        {
            // 안내 로그를 남깁니다.
            Debug.LogError("[PlayerEquipmentHolder] LeftHand/RightHand 본을 찾지 못했습니다. Avatar가 Humanoid인지 확인해주세요.");
            return;
        }

        // 방패를 붙입니다.
        AttachShield(leftHand);
        // 칼을 붙입니다.
        AttachSword(rightHand);

        ValidateMeleeSetup();
    }

    // 한 번만 점검해 두면 “맞는데 로그가 없음” 원인을 빨리 좁힐 수 있습니다.
    private void ValidateMeleeSetup()
    {
        if (meleeCombat == null)
        {
            Debug.LogError("[PlayerEquipmentHolder] PlayerMeleeCombat이 없습니다. 플레이어 루트에 붙여야 검칼이 데미지 창과 연결됩니다.");
        }
        if (longSwordPrefab == null)
        {
            Debug.LogWarning("[PlayerEquipmentHolder] Long Sword Prefab이 비어 있습니다. 검·SwordDamageTrigger가 생성되지 않습니다.");
        }
        if (playerRoot != null && !playerRoot.CompareTag("Player"))
        {
            Debug.LogWarning("[PlayerEquipmentHolder] Player Root에 Player 태그가 없습니다. 피격 로그의 ‘플레이어 공격 적중’은 공격자가 Player 태그일 때만 나옵니다.");
        }
    }

    // 방패 한 개를 붙이는 함수입니다.
    // 핵심 요약: Equipment 태그를 달고 위치를 맞춥니다.
    private void AttachShield(Transform hand)
    {
        // 프리팹이 없으면 그냥 끝냅니다.
        if (shieldPrefab == null) { return; }

        // 손 아래에 인스턴스를 만듭니다.
        GameObject instance = Instantiate(shieldPrefab, hand);
        // 장비 태그를 붙입니다.
        instance.tag = "Equipment";
        // 로컬 위치를 맞춥니다.
        instance.transform.localPosition = shieldLocalPosition;
        // 로컬 회전을 맞춥니다.
        instance.transform.localRotation = Quaternion.Euler(shieldLocalEuler);
        // 로컬 크기를 맞춥니다.
        instance.transform.localScale = shieldLocalScale;

        StabilizeShieldAttachment(instance);

        if (lockShieldRotationDuringDefense)
        {
            ShieldDefenseRotationLock rotationLock = instance.GetComponent<ShieldDefenseRotationLock>();
            if (rotationLock == null) { rotationLock = instance.AddComponent<ShieldDefenseRotationLock>(); }
            rotationLock.Initialize(targetAnimator, shieldDefenseAnimatorLayer, shieldDefenseStateNames);
        }
    }

    // 방패 프리팹 내부 물리/애니메이터 간섭으로 손에서 이탈하는 상황을 방지합니다.
    private static void StabilizeShieldAttachment(GameObject shieldInstance)
    {
        if (shieldInstance == null) { return; }

        Rigidbody[] rigidbodies = shieldInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody rb = rigidbodies[i];
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        Animator[] animators = shieldInstance.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
        }
    }

    // 칼 한 개를 붙이고 충돌 트리거를 만드는 함수입니다.
    // 핵심 요약: 원래 박스 콜라이더는 끄고 트리거만 씁니다.
    private void AttachSword(Transform hand)
    {
        // 프리팹이 없으면 그냥 끝냅니다.
        if (longSwordPrefab == null) { return; }

        // 손 아래에 인스턴스를 만듭니다.
        GameObject instance = Instantiate(longSwordPrefab, hand);
        // 장비 태그를 붙입니다.
        instance.tag = "Equipment";
        // 로컬 위치를 맞춥니다.
        instance.transform.localPosition = swordLocalPosition;
        // 로컬 회전을 맞춥니다.
        instance.transform.localRotation = Quaternion.Euler(swordLocalEuler);
        // 로컬 크기를 맞춥니다.
        instance.transform.localScale = swordLocalScale;

        // 프리팹에 들어 있는 모든 콜라이더를 끕니다.
        Collider[] cols = instance.GetComponentsInChildren<Collider>();
        // 하나씩 끕니다.
        for (int i = 0; i < cols.Length; i++) { cols[i].enabled = false; }

        // 맞춤용 빈 오브젝트를 만듭니다.
        GameObject hitObj = new GameObject("SwordDamageTrigger");
        // 칼 인스턴스 아래에 둡니다.
        hitObj.transform.SetParent(instance.transform, false);
        // 위치는 칼의 기준에 맞춥니다.
        hitObj.transform.localPosition = Vector3.zero;
        // 회전도 기본으로 둡니다.
        hitObj.transform.localRotation = Quaternion.identity;

        // 박스 콜라이더를 추가합니다.
        BoxCollider box = hitObj.AddComponent<BoxCollider>();
        // 트리거로 켭니다.
        box.isTrigger = true;
        // 크기를 넣습니다.
        box.size = swordTriggerSize;
        // 중심을 넣습니다.
        box.center = swordTriggerCenter;

        // ★ 트리거가 본 애니메이션으로만 움직일 때 Rigidbody가 없으면 Unity가 OnTrigger 이벤트를 거의 보내지 않습니다.
        //    (콘솔에 피격 로그가 전혀 안 뜨는 가장 흔한 원인) 키네마틱이라 중력·물리 밀기는 없고, 트리거 검출만 켜 줍니다.
        Rigidbody hitRb = hitObj.AddComponent<Rigidbody>();
        hitRb.isKinematic = true;
        hitRb.useGravity = false;
        hitRb.interpolation = RigidbodyInterpolation.None;
        hitRb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // 맞춤 스크립트를 붙입니다.
        MeleeWeaponHitbox hitbox = hitObj.AddComponent<MeleeWeaponHitbox>();
        // 플레이어와 전투 스크립트를 연결합니다(전투가 같은 루트에 있으면 그쪽을 우선).
        Transform rootForHits = meleeCombat != null ? meleeCombat.transform : playerRoot;
        hitbox.Initialize(rootForHits, meleeCombat);
    }
}
