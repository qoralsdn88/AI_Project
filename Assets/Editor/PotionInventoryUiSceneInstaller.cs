using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬에 플레이어가 있으면 우하단 포션 슬롯 UI를 자동 생성합니다.
/// </summary>
[InitializeOnLoad]
public static class PotionInventoryUiSceneInstaller
{
    private static bool _isInstalling;

    static PotionInventoryUiSceneInstaller()
    {
        EditorApplication.delayCall += InstallToOpenedScenes;
        EditorSceneManager.sceneOpened += (_, _) => InstallToOpenedScenes();
    }

    private static void InstallToOpenedScenes()
    {
        if (_isInstalling) return;
        _isInstalling = true;
        try
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                EnsureSceneUi(scene);
            }
        }
        finally
        {
            _isInstalling = false;
        }
    }

    private static void EnsureSceneUi(Scene scene)
    {
        if (!SceneHasPlayer(scene)) return;
        PotionInventorySlotUi existing = Object.FindFirstObjectByType<PotionInventorySlotUi>(FindObjectsInactive.Include);
        if (existing != null) return;

        GameObject host = new GameObject("PotionInventorySlotUi");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PotionInventorySlotUi>();

        GameObject canvasGo = new GameObject("PotionInventory_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(host.transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 70;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject slot = new GameObject("SlotRoot", typeof(RectTransform), typeof(Image));
        slot.transform.SetParent(canvasGo.transform, false);
        Image slotBg = slot.GetComponent<Image>();
        slotBg.sprite = UiSpriteUtility.WhiteSprite;
        slotBg.color = new Color(0.06f, 0.06f, 0.07f, 0.9f);
        RectTransform srt = slot.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(1f, 0f);
        srt.anchorMax = new Vector2(1f, 0f);
        srt.pivot = new Vector2(1f, 0f);
        srt.sizeDelta = new Vector2(240f, 90f);
        srt.anchoredPosition = new Vector2(-36f, 30f);

        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(slot.transform, false);
        RectTransform irt = icon.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0f, 0.5f);
        irt.anchorMax = new Vector2(0f, 0.5f);
        irt.pivot = new Vector2(0f, 0.5f);
        irt.sizeDelta = new Vector2(64f, 64f);
        irt.anchoredPosition = new Vector2(12f, 0f);

        GameObject hotkey = CreateText(slot.transform, "Hotkey", "1", 30f, TextAlignmentOptions.Center);
        RectTransform hrt = hotkey.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(1f, 0f);
        hrt.anchorMax = new Vector2(1f, 0f);
        hrt.pivot = new Vector2(1f, 0f);
        hrt.sizeDelta = new Vector2(34f, 34f);
        hrt.anchoredPosition = new Vector2(-6f, 6f);

        GameObject itemName = CreateText(slot.transform, "ItemName", "비어 있음", 22f, TextAlignmentOptions.MidlineLeft);
        RectTransform nrt = itemName.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0f, 0f);
        nrt.anchorMax = new Vector2(1f, 1f);
        nrt.offsetMin = new Vector2(84f, 8f);
        nrt.offsetMax = new Vector2(-12f, -8f);

        EditorUtility.SetDirty(host);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static bool SceneHasPlayer(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (FindByName(root.transform, "Player") != null) return true;
        }
        return false;
    }

    private static Transform FindByName(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform found = FindByName(t.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private static GameObject CreateText(Transform parent, string name, string value, float fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = value;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = new Color(0.95f, 0.92f, 0.85f, 1f);
        tmp.font = ResolveFont();
        return go;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset koreanUi = Resources.Load<TMP_FontAsset>("Fonts & Materials/KoreanUi SDF");
        if (koreanUi != null && koreanUi.material != null) return koreanUi;
        TMP_FontAsset def = TMP_Settings.defaultFontAsset;
        if (def != null && def.material != null) return def;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }
}
