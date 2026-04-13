using UnityEngine; // Unity 기본 기능을 쓰기 위해 가져옵니다.
public class RoomMonsterSpawner : MonoBehaviour // 방 안에서 몬스터를 스폰하는 역할을 맡습니다.
{ // 이 클래스의 시작 중괄호입니다.
    [Header("몬스터 프리팹")] // 인스펙터에서 몬스터 관련 값을 보기 좋게 묶습니다.
    [SerializeField] private GameObject monsterPrefab; // 문이 열리면 여기 있는 몬스터 프리팹을 복제합니다.
    [Header("스폰 지점")] // 인스펙터에서 스폰 위치 값을 보기 좋게 묶습니다.
    [SerializeField] private Transform[] spawnPoints; // 몬스터가 태어날 위치들을 담아 두는 배열입니다.
    [Header("한 번에 스폰할 수")] // 인스펙터에서 한 번에 생성할 개수를 묶습니다.
    [SerializeField] private int monsterCount = 1; // 한 번 문 열림 때 몇 마리 만들지 정하는 숫자입니다.
    [Header("스폰 제한")] // 인스펙터에서 스폰을 언제/몇 번 할지 묶습니다.
    [SerializeField] private bool spawnOnlyOnce = true; // true면 문이 여러 번 열려도 스폰은 한 번만 합니다.
    private bool hasSpawned = false; // 이미 스폰했는지 기억하는 값이라서 중복을 막습니다.
    public void OnDoorOpened() // 문이 열렸을 때 문 쪽에서 호출됩니다.
    { // 문 열림 신호를 받아서 스폰을 시작하는 블록입니다.
        if (spawnOnlyOnce && hasSpawned) return; // 한 번만 스폰이면 이미 스폰했을 때는 그냥 끝냅니다.
        hasSpawned = true; // 스폰이 시작되었다고 표시해서 다음 호출을 막습니다.
        SpawnNow(); // 실제로 몬스터를 만드는 일을 SpawnNow에 맡깁니다.
    } // OnDoorOpened 블록의 끝 중괄호입니다.
    private void SpawnNow() // Instantiate를 실제로 실행하는 함수입니다.
    { // 몬스터 생성이 들어있는 블록입니다.
        if (monsterPrefab == null) // 프리팹이 연결 안 되어 있으면 스폰할 수 없습니다.
        { // 프리팹이 비어 있을 때 경고를 찍고 끝내는 블록입니다.
            Debug.LogError("[RoomMonsterSpawner] monsterPrefab이 비어 있습니다. RoomMonsterSpawner에서 프리팹을 연결해주세요."); // 연결 누락 원인을 콘솔에 표시합니다.
            return; // 프리팹이 없으니 생성 로직을 멈춥니다.
        } // 프리팹 없음 블록의 끝 중괄호입니다.
        if (spawnPoints == null || spawnPoints.Length == 0) // 스폰 지점이 없으면 위치를 정할 수 없습니다.
        { // 스폰 지점이 비어 있을 때 경고를 찍는 블록입니다.
            Debug.LogError("[RoomMonsterSpawner] spawnPoints가 비어 있습니다. 스폰 지점을 여러 개 등록해주세요."); // 스폰 지점 누락 원인을 콘솔에 표시합니다.
            return; // 스폰 지점이 없으니 생성 로직을 멈춥니다.
        } // 스폰 지점 없음 블록의 끝 중괄호입니다.
        if (monsterCount <= 0) // 만들 몬스터 수가 0 이하이면 의미가 없습니다.
        { // 몬스터 수가 잘못된 경우 경고 후 종료하는 블록입니다.
            Debug.LogWarning("[RoomMonsterSpawner] monsterCount가 0 이하입니다. 아무것도 만들지 않습니다."); // 설정 실수 가능성을 알려줍니다.
            return; // 수가 0 이하이니 스폰을 멈춥니다.
        } // 몬스터 수 오류 블록의 끝 중괄호입니다.
        for (int i = 0; i < monsterCount; i++) // i를 0부터 monsterCount-1까지 늘리며 몬스터를 계속 만듭니다.
        { // i번째 몬스터를 만드는 처리 블록입니다.
            int spawnIndex = GetSpawnIndex(i); // i에 맞는 스폰 지점 번호를 계산합니다.
            Transform point = spawnPoints[spawnIndex]; // 계산된 번호의 스폰 지점을 point로 받습니다.
            Vector3 spawnPosition = point.position; // 생성 위치는 스폰 지점의 위치 값만 사용합니다.
            Quaternion spawnRotation = Quaternion.identity; // 회전은 기본값으로 두어서 예측 가능한 시작 상태를 만듭니다.
            GameObject monster = Instantiate(monsterPrefab, spawnPosition, spawnRotation); // 프리팹을 복제해서 스폰 지점에 둡니다.
            monster.name = $"{monsterPrefab.name}_Spawned_{i}"; // 생성된 오브젝트 이름을 바꿔서 확인을 쉽게 합니다.
            TryConfigureNecromancer(monster); // 네크로맨서 프리팹이라면 필요한 전투/AI 컴포넌트를 자동으로 붙입니다.
        } // for 반복 블록의 끝 중괄호입니다.
    } // SpawnNow 블록의 끝 중괄호입니다.
    private int GetSpawnIndex(int i) // i와 spawnPoints 길이로 어떤 지점을 쓸지 정합니다.
    { // 스폰 지점 번호를 계산하는 블록입니다.
        int length = spawnPoints.Length; // 스폰 지점이 몇 개인지 길이로 저장합니다.
        int index = i % length; // 지점 수보다 많아져도 반복해서 쓰게 나머지를 씁니다.
        return index; // 몬스터가 쓸 최종 스폰 지점 번호를 돌려줍니다.
    } // GetSpawnIndex 블록의 끝 중괄호입니다.

    private void TryConfigureNecromancer(GameObject spawnedMonster)
    {
        if (spawnedMonster == null) return;

        Animator animator = spawnedMonster.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;

        string controllerName = animator.runtimeAnimatorController.name;
        if (string.IsNullOrEmpty(controllerName)) return;
        bool isNecromancer =
            controllerName.Contains("Necromanser") ||
            controllerName.Contains("Necromancer");
        if (!isNecromancer) return;

        if (spawnedMonster.GetComponent<SimpleMonsterHealth>() == null)
        {
            spawnedMonster.AddComponent<SimpleMonsterHealth>();
        }

        NecromancerBossController boss = spawnedMonster.GetComponent<NecromancerBossController>();
        if (boss == null)
        {
            boss = spawnedMonster.AddComponent<NecromancerBossController>();
        }

        GameObject skeletonPrefab = LoadSkeletonPrefabForSpawner();
        if (boss != null && skeletonPrefab != null)
        {
            boss.InjectSkeletonPrefab(skeletonPrefab);
        }
    }

    private static GameObject LoadSkeletonPrefabForSpawner()
    {
        GameObject byResources = Resources.Load<GameObject>("Monster/Skeleton");
        if (byResources != null) return byResources;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Monster/Skeleton.prefab");
#else
        return null;
#endif
    }
} // RoomMonsterSpawner 클래스의 끝 중괄호입니다.
