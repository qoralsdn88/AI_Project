using UnityEngine;

/// <summary>
/// 자기·부모·자식 순으로 컴포넌트를 찾는 공통 규칙(플레이어 루트/자식 혼합 배치 대응).
/// </summary>
public static class TransformHierarchy
{
    public static T FindComponent<T>(Transform t) where T : Component
    {
        if (t == null) return null;
        if (t.TryGetComponent(out T direct)) return direct;
        T parent = t.GetComponentInParent<T>(true);
        if (parent != null) return parent;
        return t.GetComponentInChildren<T>(true);
    }
}
