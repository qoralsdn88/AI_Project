using System.Collections;
using UnityEngine;

/// <summary>
/// 전역 히트 스탑(짧은 시간 게임 시간 정지)을 관리합니다.
/// 별도 배치가 없어도 자동으로 생성되어 어디서든 호출할 수 있습니다.
/// </summary>
public class HitStopController : MonoBehaviour
{
    private static HitStopController _instance;
    private static float _defaultFixedDeltaTime;

    private Coroutine _activeRoutine;
    private float _restoreAtRealtime;
    private float _cachedTimeScale = 1f;
    private float _cachedFixedDeltaTime;
    private bool _isStopping;

    public static bool IsActive => _instance != null && _instance._isStopping;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (_instance != null) return;
        _defaultFixedDeltaTime = Time.fixedDeltaTime;

        GameObject go = new GameObject("[HitStopController]");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<HitStopController>();
    }

    /// <summary>
    /// 히트 스탑을 요청합니다.
    /// </summary>
    /// <param name="durationSeconds">정지 시간(실시간 기준).</param>
    /// <param name="timeScale">정지 배율. 완전 정지는 0.</param>
    public static void Request(float durationSeconds, float timeScale = 0f)
    {
        EnsureExists();
        if (_instance == null) return;
        _instance.RequestInternal(durationSeconds, timeScale);
    }

    private void RequestInternal(float durationSeconds, float timeScale)
    {
        float duration = Mathf.Max(0f, durationSeconds);
        if (duration <= 0f) return;

        float clampedScale = Mathf.Clamp01(timeScale);
        float newRestoreAt = Time.realtimeSinceStartup + duration;

        // 이미 히트 스탑이 진행 중이면 더 긴 요청으로 갱신만 합니다.
        if (_isStopping)
        {
            if (newRestoreAt > _restoreAtRealtime) _restoreAtRealtime = newRestoreAt;
            if (clampedScale < Time.timeScale)
            {
                ApplyTimeScale(clampedScale);
            }
            return;
        }

        _cachedTimeScale = Time.timeScale;
        _cachedFixedDeltaTime = Time.fixedDeltaTime;
        _restoreAtRealtime = newRestoreAt;
        ApplyTimeScale(clampedScale);

        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(CoHitStop());
    }

    private IEnumerator CoHitStop()
    {
        _isStopping = true;
        while (Time.realtimeSinceStartup < _restoreAtRealtime)
        {
            yield return null;
        }

        RestoreTimeScale();
        _isStopping = false;
        _activeRoutine = null;
    }

    private static void ApplyTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = _defaultFixedDeltaTime * scale;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = _cachedTimeScale;
        Time.fixedDeltaTime = _cachedFixedDeltaTime > 0f ? _cachedFixedDeltaTime : _defaultFixedDeltaTime;
    }
}
