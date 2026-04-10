using UnityEngine;

/// <summary>
/// 피격 지점이 충돌 계산에서 오지 않았을 때 사용하는 값입니다.
/// </summary>
public static class HitPoint
{
    /// <summary>히트박스가 없는 경로에서 데미지만 줄 때 사용합니다.</summary>
    public static readonly Vector3 Unspecified = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

    public static bool IsUnspecified(Vector3 p) =>
        float.IsPositiveInfinity(p.x) && float.IsPositiveInfinity(p.y) && float.IsPositiveInfinity(p.z);
}
