using UnityEngine;

/// <summary>
/// 피격 지점에 히트 이펙트 프리팹을 재생합니다.
/// Resources/CombatVfx/Hit_02 (원본: Assets/Art/Hit Impact Effects/Prefabs/Hits/Hit_02.prefab 복사본)를 로드합니다.
/// </summary>
public static class HitImpactVfx
{
    private const string ResourcesPath = "CombatVfx/Hit_02";
    private static GameObject _prefab;
    private const float AutoDestroySeconds = 3f;

    public static void PlayAt(Vector3 worldPosition, GameObject attacker)
    {
        if (_prefab == null)
        {
            _prefab = Resources.Load<GameObject>(ResourcesPath);
            if (_prefab == null)
            {
                Debug.LogWarning($"[HitImpactVfx] Resources에서 프리팹을 찾지 못했습니다: {ResourcesPath}");
                return;
            }
        }

        Vector3 forward = Vector3.forward;
        if (attacker != null)
        {
            Vector3 flat = worldPosition - attacker.transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                forward = flat.normalized;
            }
        }

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        GameObject instance = Object.Instantiate(_prefab, worldPosition, rot);
        Object.Destroy(instance, AutoDestroySeconds);
    }
}
