using UnityEngine;

/// <summary>
/// 코드로 UI Image를 만들 때 공통으로 쓰는 1×1 흰 스프라이트입니다.
/// </summary>
public static class UiSpriteUtility
{
    private static Sprite _white;

    public static Sprite WhiteSprite
    {
        get
        {
            if (_white != null) return _white;
            Texture2D tex = Texture2D.whiteTexture;
            _white = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return _white;
        }
    }
}
