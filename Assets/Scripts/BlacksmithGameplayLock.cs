using UnityEngine;

/// <summary>
/// 대장장이 UI가 열려 전투·이동 입력을 막을 때 사용하는 전역 플래그입니다.
/// (전투 스크립트가 UI 타입을 참조하지 않게 분리합니다.)
/// </summary>
public static class BlacksmithGameplayLock
{
    public static bool IsMenuOpen { get; private set; }

    public static void SetMenuOpen(bool open)
    {
        IsMenuOpen = open;
    }
}
