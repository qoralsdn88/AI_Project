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
        // 플레이어 루트가 비어 있으면 자기 자신을 씁니다.
        if (playerRoot == null) { playerRoot = transform; }
        // 애니메이터가 비어 있으면 자식에서 찾습니다.
        if (targetAnimator == null) { targetAnimator = GetComponentInChildren<Animator>(); }
        // 전투 스크립트가 비어 있으면 같이 붙어 있는지 찾습니다.
        if (meleeCombat == null) { meleeCombat = GetComponent<PlayerMeleeCombat>(); }
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

        // 맞춤 스크립트를 붙입니다.
        MeleeWeaponHitbox hitbox = hitObj.AddComponent<MeleeWeaponHitbox>();
        // 플레이어와 전투 스크립트를 연결합니다.
        hitbox.Initialize(playerRoot, meleeCombat);
    }
}
