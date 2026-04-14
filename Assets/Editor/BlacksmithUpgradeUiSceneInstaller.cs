using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬에 대장장이 UI 계층이 없으면 자동으로 생성해 저장 가능한 Hierarchy 오브젝트로 제공합니다.
/// 생성 후에는 디자이너가 인스펙터/Hierarchy에서 자유롭게 수정할 수 있습니다.
/// </summary>
[InitializeOnLoad]
public static class BlacksmithUpgradeUiSceneInstaller
{
    private static bool _isInstalling;

    static BlacksmithUpgradeUiSceneInstaller()
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
        if (!SceneHasBlacksmith(scene)) return;

        BlacksmithUpgradeMenuUi ui = Object.FindFirstObjectByType<BlacksmithUpgradeMenuUi>(FindObjectsInactive.Include);
        if (ui != null) return;

        EnsureEventSystem(scene);

        GameObject host = new GameObject("BlacksmithUpgradeMenu");
        SceneManager.MoveGameObjectToScene(host, scene);
        BlacksmithUpgradeMenuUi menu = host.AddComponent<BlacksmithUpgradeMenuUi>();

        GameObject canvasGo = new GameObject("BlacksmithUpgrade_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(host.transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject rootPanel = CreatePanel(canvasGo.transform, "ForgeRoot", new Vector2(520f, 380f));
        rootPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        CreateTitle(rootPanel.transform);
        CreateHint(rootPanel.transform);
        CreateButton(rootPanel.transform, "Button_무기 강화", "무기 강화", new Vector2(0f, 24f));
        CreateButton(rootPanel.transform, "Button_방패 강화", "방패 강화", new Vector2(0f, -56f));

        GameObject previewPanel = CreatePanel(canvasGo.transform, "ForgePreview", new Vector2(540f, 400f));
        previewPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        CreateCompare(previewPanel.transform);
        CreateButton(previewPanel.transform, "Button_강화 진행", "강화 진행", new Vector2(0f, -72f));
        CreateButton(previewPanel.transform, "Button_돌아가기", "돌아가기", new Vector2(0f, -140f));

        GameObject successPanel = CreatePanel(canvasGo.transform, "ForgeSuccess", new Vector2(480f, 280f));
        successPanel.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        CreateMessage(successPanel.transform);
        CreateButton(successPanel.transform, "Button_닫기", "닫기", new Vector2(0f, -90f));

        EditorUtility.SetDirty(host);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static bool SceneHasBlacksmith(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (FindByName(root.transform, "BlackSmith") != null) return true;
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
        bg.color = new Color(0.07f, 0.07f, 0.08f, 0.94f);
        AddBorder(panel.transform);
        return panel;
    }

    private static void AddBorder(Transform panel)
    {
        GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel, false);
        Image borderImg = border.GetComponent<Image>();
        borderImg.sprite = UiSpriteUtility.WhiteSprite;
        borderImg.color = Color.black;
        RectTransform rt = border.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-3f, -3f);
        rt.offsetMax = new Vector2(3f, 3f);
    }

    private static void CreateTitle(Transform panel)
    {
        GameObject go = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(panel, false);
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = "대장장이 — 강화";
        txt.fontSize = 28f;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.96f, 0.94f, 0.88f, 1f);
        txt.font = ResolveFont();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.72f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(20f, 0f);
        rt.offsetMax = new Vector2(-20f, -12f);
    }

    private static void CreateHint(Transform panel)
    {
        GameObject go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(panel, false);
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.text = "F — 닫기 · 마우스로 선택";
        txt.fontSize = 18f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.75f, 0.72f, 0.65f, 1f);
        txt.font = ResolveFont();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0.58f);
        rt.anchorMax = new Vector2(1f, 0.7f);
        rt.offsetMin = new Vector2(16f, 0f);
        rt.offsetMax = new Vector2(-16f, 0f);
    }

    private static void CreateCompare(Transform panel)
    {
        GameObject go = new GameObject("Compare", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(panel, false);
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize = 24f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.96f, 0.94f, 0.88f, 1f);
        txt.font = ResolveFont();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 0.38f);
        rt.anchorMax = new Vector2(0.95f, 0.88f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CreateMessage(Transform panel)
    {
        GameObject go = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(panel, false);
        TextMeshProUGUI txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize = 26f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.96f, 0.94f, 0.88f, 1f);
        txt.font = ResolveFont();
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.08f, 0.35f);
        rt.anchorMax = new Vector2(0.92f, 0.82f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = UiSpriteUtility.WhiteSprite;
        img.color = new Color(0.55f, 0.42f, 0.28f, 1f);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(280f, 48f);
        rt.anchoredPosition = anchoredPos;

        GameObject text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        text.transform.SetParent(go.transform, false);
        TextMeshProUGUI txt = text.GetComponent<TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 22f;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = new Color(0.96f, 0.94f, 0.88f, 1f);
        txt.font = ResolveFont();
        RectTransform textRt = text.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
    }
}
