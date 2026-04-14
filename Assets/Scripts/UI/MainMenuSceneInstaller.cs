using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public static class MainMenuSceneInstaller
{
    private const string MainMenuCanvasName = "MainMenuCanvas_Auto";
    private const string MainMenuRootName = "MainMenuRoot_Auto";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        EnsureMenuInScene(SceneManager.GetActiveScene(), false);
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void RegisterEditorHooks()
    {
        EditorSceneManager.sceneOpened -= OnEditorSceneOpened;
        EditorSceneManager.sceneOpened += OnEditorSceneOpened;
        EditorApplication.delayCall += EnsureForActiveSceneInEditor;
    }

    private static void OnEditorSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        if (Application.isPlaying)
        {
            return;
        }

        EnsureMenuInScene(scene, true);
    }

    private static void EnsureForActiveSceneInEditor()
    {
        if (Application.isPlaying)
        {
            return;
        }

        EnsureMenuInScene(SceneManager.GetActiveScene(), true);
    }
#endif

    private static void EnsureMenuInScene(UnityEngine.SceneManagement.Scene scene, bool markDirtyInEditor)
    {
        if (scene.name != GameScenes.MainMenu)
        {
            return;
        }

        var existingCanvas = GameObject.Find(MainMenuCanvasName);
        if (existingCanvas != null)
        {
            EnsureButtonActions(existingCanvas.transform);
            EnsureBackgroundRaycastConfig(existingCanvas.transform);
            return;
        }

        EnsureEventSystem();

        var canvasObject = new GameObject(MainMenuCanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var canvasScaler = canvasObject.GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        canvasScaler.matchWidthOrHeight = 0.5f;

        var root = new GameObject(MainMenuRootName, typeof(MainMenuUIController));
        root.transform.SetParent(canvasObject.transform, false);
        var controller = root.GetComponent<MainMenuUIController>();

        CreateBackground(canvasObject.transform);
        CreateButtonGroup(canvasObject.transform, controller);
        EnsureButtonActions(canvasObject.transform);
        EnsureBackgroundRaycastConfig(canvasObject.transform);

#if UNITY_EDITOR
        if (markDirtyInEditor)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }
#endif
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void CreateBackground(Transform canvasTransform)
    {
        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(canvasTransform, false);

        var rect = background.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = background.GetComponent<Image>();
        image.sprite = TryLoadMainMenuSprite();
        image.preserveAspect = false;
        image.type = Image.Type.Simple;
        image.raycastTarget = false;
        image.color = image.sprite == null ? new Color(0.08f, 0.08f, 0.08f, 1f) : Color.white;
    }

    private static void CreateButtonGroup(Transform canvasTransform, MainMenuUIController controller)
    {
        var panel = new GameObject("ButtonPanel", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panel.transform.SetParent(canvasTransform, false);

        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.08f);
        panelRect.anchorMax = new Vector2(0.5f, 0.08f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;

        var layout = panel.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 18f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = panel.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CreateButton(panel.transform, "게임 시작", controller.OnClickStartGame);
        CreateButton(panel.transform, "게임 종료", controller.OnClickQuitGame);
    }

    private static void CreateButton(Transform parent, string label, UnityAction onClick)
    {
        var buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        var layout = buttonObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 340f;
        layout.preferredHeight = 86f;

        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0f, 0f, 0f, 0.62f);

        var button = buttonObject.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.pressedColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 34;
        text.color = Color.white;
        text.font = TryLoadKoreanFont();
    }

    private static TMP_FontAsset TryLoadKoreanFont()
    {
        var font = Resources.Load<TMP_FontAsset>("Fonts & Materials/KoreanUi SDF");
        if (font != null)
        {
            return font;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/KoreanUi SDF.asset");
#else
        return null;
#endif
    }

    private static Sprite TryLoadMainMenuSprite()
    {
        var loadedFromResources = Resources.Load<Sprite>("UI/MainScreen");
        if (loadedFromResources != null)
        {
            return loadedFromResources;
        }

#if UNITY_EDITOR
        var loadedFromAsset = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/MainScreen.png");
        if (loadedFromAsset != null)
        {
            return loadedFromAsset;
        }
#endif

        var imagePath = Path.Combine(Application.dataPath, "Art", "UI", "MainScreen.png");
        if (!File.Exists(imagePath))
        {
            return null;
        }

        var bytes = File.ReadAllBytes(imagePath);
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            return null;
        }

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private static void EnsureButtonActions(Transform canvasTransform)
    {
        var root = canvasTransform.Find(MainMenuRootName);
        if (root == null)
        {
            return;
        }

        var controller = root.GetComponent<MainMenuUIController>();
        if (controller == null)
        {
            controller = root.gameObject.AddComponent<MainMenuUIController>();
        }

        var panel = canvasTransform.Find("ButtonPanel");
        if (panel == null)
        {
            return;
        }

        var startButton = panel.Find("게임 시작")?.GetComponent<Button>();
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(controller.OnClickStartGame);
        }

        var quitButton = panel.Find("게임 종료")?.GetComponent<Button>();
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(controller.OnClickQuitGame);
        }

        panel.SetAsLastSibling();
    }

    private static void EnsureBackgroundRaycastConfig(Transform canvasTransform)
    {
        var background = canvasTransform.Find("Background");
        if (background == null)
        {
            return;
        }

        var backgroundImage = background.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.raycastTarget = false;
        }

        background.SetAsFirstSibling();
    }
}
