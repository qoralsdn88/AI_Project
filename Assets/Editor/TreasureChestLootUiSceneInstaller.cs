using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// TreasureChest가 있는 씬에 전리품 UI(Canvas 계층)를 자동 생성합니다.
/// 생성된 오브젝트는 Hierarchy에서 직접 수정 가능합니다.
/// </summary>
[InitializeOnLoad]
public static class TreasureChestLootUiSceneInstaller
{
    private const string SampleSpritePath = "Assets/Store/Lima Computational Design/Epic 2D RPG Treasure Collection/Potions/potions_s1_06.png";
    private const string SamplePotionPrefabPath = "Assets/Prefabs/Items/Potion.prefab";
    private static bool _isInstalling;

    static TreasureChestLootUiSceneInstaller()
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
        if (!SceneHasTreasureChest(scene)) return;
        TreasureChestLootMenuUi ui = Object.FindFirstObjectByType<TreasureChestLootMenuUi>(FindObjectsInactive.Include);
        if (ui != null)
        {
            ApplySampleItemReferences(ui);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            return;
        }

        EnsureEventSystem(scene);

        GameObject host = new GameObject("TreasureChestLootMenu");
        SceneManager.MoveGameObjectToScene(host, scene);
        TreasureChestLootMenuUi menu = host.AddComponent<TreasureChestLootMenuUi>();

        GameObject canvasGo = new GameObject("TreasureChestLoot_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(host.transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 81;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject root = CreatePanel(canvasGo.transform, "ChestLootRoot", new Vector2(620f, 340f));
        root.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -40f);

        CreateTitle(root.transform, "보물 상자", new Vector2(0f, 120f), 30f);
        CreateItemIcon(root.transform);
        CreateItemName(root.transform);
        CreateButton(root.transform, "Button_획득하기", "획득하기", new Vector2(130f, -110f), new Color(0.2f, 0.45f, 0.2f, 1f));
        CreateButton(root.transform, "Button_버리기", "버리기", new Vector2(300f, -110f), new Color(0.45f, 0.2f, 0.2f, 1f));

        ApplySampleItemReferences(menu);

        EditorUtility.SetDirty(host);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void ApplySampleItemReferences(TreasureChestLootMenuUi menu)
    {
        if (menu == null) return;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SampleSpritePath);
        GameObject potionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamplePotionPrefabPath);
        SerializedObject so = new SerializedObject(menu);
        so.FindProperty("sampleItemSprite").objectReferenceValue = sprite;
        so.FindProperty("samplePotionHoldPrefab").objectReferenceValue = potionPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool SceneHasTreasureChest(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (FindByName(root.transform, "TreasureChest") != null) return true;
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

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem existing = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (existing != null) return;
        GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        SceneManager.MoveGameObjectToScene(go, scene);
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset koreanUi = Resources.Load<TMP_FontAsset>("Fonts & Materials/KoreanUi SDF");
        if (koreanUi != null && koreanUi.material != null) return koreanUi;
        TMP_FontAsset def = TMP_Settings.defaultFontAsset;
        if (def != null && def.material != null) return def;
        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        Image bg = panel.GetComponent<Image>();
        bg.sprite = UiSpriteUtility.WhiteSprite;
        bg.color = new Color(0.05f, 0.06f, 0.08f, 0.95f);
        AddBorder(panel.transform);
        return panel;
    }

    private static void AddBorder(Transform panel)
    {
        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel, false);
        Image img = border.GetComponent<Image>();
        img.sprite = UiSpriteUtility.WhiteSprite;
        img.color = new Color(0.82f, 0.66f, 0.26f, 1f);
        RectTransform rt = border.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-3f, -3f);
        rt.offsetMax = new Vector2(3f, 3f);
    }

    private static void CreateTitle(Transform parent, string text, Vector2 pos, float size)
    {
        GameObject go = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = text;
        txt.font = ResolveFont();
        txt.fontSize = size;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.96f, 0.93f, 0.84f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(460f, 56f);
        rt.anchoredPosition = pos;
    }

    private static void CreateItemIcon(Transform parent)
    {
        GameObject go = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = UiSpriteUtility.WhiteSprite;
        img.color = new Color(0.88f, 0.75f, 0.3f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120f, 120f);
        rt.anchoredPosition = new Vector2(-190f, -12f);
    }

    private static void CreateItemName(Transform parent)
    {
        GameObject go = new GameObject("ItemName", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = "샘플 아이템: 신비한 광석";
        txt.font = ResolveFont();
        txt.fontSize = 26f;
        txt.alignment = TextAlignmentOptions.MidlineLeft;
        txt.color = new Color(0.95f, 0.92f, 0.86f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(340f, 120f);
        rt.anchoredPosition = new Vector2(80f, -12f);
    }

    private static void CreateButton(Transform parent, string name, string label, Vector2 pos, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = UiSpriteUtility.WhiteSprite;
        img.color = bgColor;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(150f, 46f);
        rt.anchoredPosition = pos;

        GameObject text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(go.transform, false);
        TextMeshProUGUI txt = text.GetComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.font = ResolveFont();
        txt.fontSize = 22f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        RectTransform tr = text.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
    }
}
