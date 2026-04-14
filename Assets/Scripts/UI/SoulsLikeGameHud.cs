using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD: 플레이어 체력, 하단 조작 안내, 문/대장장이 상호작용 프롬프트.
/// 빈 오브젝트에 붙이고 <see cref="buildUiAtRuntime"/>를 켜면 Canvas를 런타임에 생성합니다.
/// </summary>
[DisallowMultipleComponent]
public class SoulsLikeGameHud : MonoBehaviour
{
    private static readonly Color PanelDark = new Color(0.06f, 0.06f, 0.07f, 0.92f);
    private static readonly Color BorderBlack = new Color(0f, 0f, 0f, 1f);
    private static readonly Color HpFillPlayer = new Color(0.72f, 0.18f, 0.14f, 1f);
    private static readonly Color TextIvory = new Color(0.96f, 0.94f, 0.88f, 1f);
    private static readonly Color TextMuted = new Color(0.75f, 0.72f, 0.65f, 1f);

    [Header("생성")]
    [SerializeField] private bool buildUiAtRuntime = true;

    [Header("참조 (buildUiAtRuntime 끄면 직접 연결)")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private Image playerHpFill;
    [SerializeField] private TextMeshProUGUI playerHpLabel;
    [SerializeField] private TextMeshProUGUI controlHintsLabel;
    [SerializeField] private TextMeshProUGUI doorPromptLabel;

    private GameObject _doorPromptPanel;

    [Header("문 프롬프트")]
    [SerializeField] private float doorPromptBottomOffset = 140f;

    [Header("조작 안내 (하단)")]
    [TextArea(2, 5)]
    [SerializeField] private string controlHints =
        "이동 · WASD   |   시야 · 마우스   |   공격 · 좌클릭   |   상호작용 · F";

    [Header("플레이어 체력")]
    [SerializeField] private bool findPlayerHealthEachFrame = true;
    [SerializeField] private SimplePlayerHealth playerHealth;

    private Transform _playerTransform;

    private void Awake()
    {
        if (buildUiAtRuntime)
        {
            BuildRuntimeUi();
        }

        CachePlayerHealth();
        ApplyHintsText();
        SetDoorPromptVisible(false);
    }

    private void CachePlayerHealth()
    {
        if (playerHealth != null) return;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            _playerTransform = p.transform;
            playerHealth = SimplePlayerHealth.Resolve(p.transform);
        }
    }

    private void LateUpdate()
    {
        if (findPlayerHealthEachFrame && playerHealth == null)
        {
            CachePlayerHealth();
        }

        UpdatePlayerHealthBar();
        UpdateDoorPrompt();
    }

    private void UpdatePlayerHealthBar()
    {
        if (playerHpFill == null) return;
        if (playerHealth == null)
        {
            playerHpFill.fillAmount = 0f;
            if (playerHpLabel != null) playerHpLabel.text = "-- / --";
            return;
        }

        int max = Mathf.Max(1, playerHealth.maxHp);
        int cur = Mathf.Clamp(playerHealth.currentHp, 0, max);
        playerHpFill.fillAmount = cur / (float)max;
        if (playerHpLabel != null)
        {
            playerHpLabel.text = $"{cur} / {max}";
        }
    }

    private void UpdateDoorPrompt()
    {
        if (doorPromptLabel == null) return;

        if (_playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _playerTransform = p.transform;
        }

        if (_playerTransform == null)
        {
            SetDoorPromptVisible(false);
            return;
        }

        Vector3 playerPos = _playerTransform.position;
        DoorOpenSignal bestDoor = null;
        BlackSmithInteractStation bestBlacksmith = null;
        TreasureChestInteractStation bestChest = null;
        float bestSq = float.MaxValue;

        foreach (DoorOpenSignal door in DoorOpenSignal.AllDoors)
        {
            if (door == null) continue;
            if (!door.ShouldShowInteractPrompt()) continue;

            float sq = (door.transform.position - playerPos).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                bestDoor = door;
                bestBlacksmith = null;
                bestChest = null;
            }
        }

        foreach (BlackSmithInteractStation station in BlackSmithInteractStation.AllStations)
        {
            if (station == null) continue;
            if (!station.ShouldShowInteractPrompt()) continue;

            float sq = (station.transform.position - playerPos).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                bestDoor = null;
                bestBlacksmith = station;
                bestChest = null;
            }
        }

        foreach (TreasureChestInteractStation station in TreasureChestInteractStation.AllStations)
        {
            if (station == null) continue;
            if (!station.ShouldShowInteractPrompt()) continue;

            float sq = (station.transform.position - playerPos).sqrMagnitude;
            if (sq < bestSq)
            {
                bestSq = sq;
                bestDoor = null;
                bestBlacksmith = null;
                bestChest = station;
            }
        }

        if (bestDoor == null && bestBlacksmith == null && bestChest == null)
        {
            SetDoorPromptVisible(false);
            return;
        }

        if (bestDoor != null)
        {
            doorPromptLabel.text = bestDoor.GetInteractPromptText();
        }
        else if (bestBlacksmith != null)
        {
            doorPromptLabel.text = bestBlacksmith.GetInteractPromptText();
        }
        else
        {
            doorPromptLabel.text = bestChest.GetInteractPromptText();
        }
        SetDoorPromptVisible(true);
    }

    private void SetDoorPromptVisible(bool visible)
    {
        GameObject panel = _doorPromptPanel;
        if (panel == null && doorPromptLabel != null && doorPromptLabel.transform.parent != null)
        {
            panel = doorPromptLabel.transform.parent.gameObject;
        }

        if (panel != null && panel.activeSelf != visible)
        {
            panel.SetActive(visible);
        }
    }

    private void ApplyHintsText()
    {
        if (controlHintsLabel != null)
        {
            controlHintsLabel.text = controlHints;
        }
    }

    /// <summary>
    /// KoreanUi SDF(한글) 우선, 없거나 깨지면 TMP 기본, 그다음 LiberationSans.
    /// </summary>
    private static TMP_FontAsset ResolveHudFont()
    {
        TMP_FontAsset koreanUi = Resources.Load<TMP_FontAsset>("Fonts & Materials/KoreanUi SDF");
        if (koreanUi != null && koreanUi.material != null) return koreanUi;

        TMP_FontAsset def = TMP_Settings.defaultFontAsset;
        if (def != null && def.material != null) return def;

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    private void BuildRuntimeUi()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        GameObject canvasGo = new GameObject("SoulsLikeHUD_Canvas");
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        targetCanvas = canvas;

        RectTransform root = canvasGo.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        CreatePlayerHealthBlock(canvasGo.transform);
        CreateControlHintsBar(canvasGo.transform);
        CreateDoorPrompt(canvasGo.transform);
    }

    private void CreatePlayerHealthBlock(Transform parent)
    {
        GameObject block = CreatePanel(parent, "PlayerHealth", TextAnchor.UpperLeft, new Vector2(48f, -48f), new Vector2(340f, 56f));

        GameObject borderGo = new GameObject("Border");
        borderGo.transform.SetParent(block.transform, false);
        Image borderImg = borderGo.AddComponent<Image>();
        borderImg.sprite = UiSpriteUtility.WhiteSprite;
        borderImg.color = BorderBlack;
        RectTransform borderRt = borderGo.GetComponent<RectTransform>();
        StretchFull(borderRt);
        borderRt.offsetMin = new Vector2(-3f, -3f);
        borderRt.offsetMax = new Vector2(3f, 3f);

        GameObject inner = CreatePanel(block.transform, "Inner", TextAnchor.UpperLeft, Vector2.zero, Vector2.zero);
        RectTransform innerRt = inner.GetComponent<RectTransform>();
        StretchFull(innerRt);
        innerRt.offsetMin = new Vector2(4f, 4f);
        innerRt.offsetMax = new Vector2(-4f, -4f);
        inner.GetComponent<Image>().color = PanelDark;

        GameObject fillBg = new GameObject("HpTrack");
        fillBg.transform.SetParent(inner.transform, false);
        Image track = fillBg.AddComponent<Image>();
        track.sprite = UiSpriteUtility.WhiteSprite;
        track.color = new Color(0.12f, 0.12f, 0.13f, 1f);
        RectTransform trackRt = fillBg.GetComponent<RectTransform>();
        trackRt.anchorMin = new Vector2(0f, 0.15f);
        trackRt.anchorMax = new Vector2(1f, 0.85f);
        trackRt.offsetMin = new Vector2(8f, 0f);
        trackRt.offsetMax = new Vector2(-8f, 0f);

        GameObject fillGo = new GameObject("HpFill");
        fillGo.transform.SetParent(fillBg.transform, false);
        Image fill = fillGo.AddComponent<Image>();
        fill.sprite = UiSpriteUtility.WhiteSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.color = HpFillPlayer;
        playerHpFill = fill;
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        StretchFull(fillRt);

        GameObject labelGo = new GameObject("HpText");
        labelGo.transform.SetParent(inner.transform, false);
        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 18f;
        tmp.alignment = TextAlignmentOptions.MidlineRight;
        tmp.color = TextIvory;
        tmp.font = ResolveHudFont();
        playerHpLabel = tmp;
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(10f, 2f);
        labelRt.offsetMax = new Vector2(-10f, -2f);
    }

    private void CreateControlHintsBar(Transform parent)
    {
        GameObject bar = CreatePanel(parent, "ControlHints", TextAnchor.LowerCenter, new Vector2(0f, 28f), new Vector2(900f, 52f));

        Image bg = bar.GetComponent<Image>();
        bg.color = PanelDark;

        GameObject borderGo = new GameObject("Border");
        borderGo.transform.SetParent(bar.transform, false);
        Image borderImg = borderGo.AddComponent<Image>();
        borderImg.sprite = UiSpriteUtility.WhiteSprite;
        borderImg.color = BorderBlack;
        RectTransform borderRt = borderGo.GetComponent<RectTransform>();
        StretchFull(borderRt);
        borderRt.offsetMin = new Vector2(-2f, -2f);
        borderRt.offsetMax = new Vector2(2f, 2f);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(bar.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 20f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextMuted;
        tmp.font = ResolveHudFont();
        controlHintsLabel = tmp;
        RectTransform tr = textGo.GetComponent<RectTransform>();
        StretchFull(tr);
        tr.offsetMin = new Vector2(16f, 6f);
        tr.offsetMax = new Vector2(-16f, -6f);
    }

    private void CreateDoorPrompt(Transform parent)
    {
        GameObject prompt = CreatePanel(parent, "DoorPrompt", TextAnchor.LowerCenter, new Vector2(0f, doorPromptBottomOffset), new Vector2(420f, 44f));
        _doorPromptPanel = prompt;
        prompt.SetActive(false);

        Image bg = prompt.GetComponent<Image>();
        bg.color = PanelDark;

        GameObject borderGo = new GameObject("Border");
        borderGo.transform.SetParent(prompt.transform, false);
        Image borderImg = borderGo.AddComponent<Image>();
        borderImg.sprite = UiSpriteUtility.WhiteSprite;
        borderImg.color = BorderBlack;
        RectTransform borderRt = borderGo.GetComponent<RectTransform>();
        StretchFull(borderRt);
        borderRt.offsetMin = new Vector2(-2f, -2f);
        borderRt.offsetMax = new Vector2(2f, 2f);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(prompt.transform, false);
        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = TextIvory;
        tmp.font = ResolveHudFont();
        doorPromptLabel = tmp;
        RectTransform tr = textGo.GetComponent<RectTransform>();
        StretchFull(tr);
    }

    private static GameObject CreatePanel(Transform parent, string name, TextAnchor anchor, Vector2 anchoredPos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        ApplyAnchor(rt, anchor, anchoredPos, size);
        Image img = go.AddComponent<Image>();
        img.sprite = UiSpriteUtility.WhiteSprite;
        img.color = PanelDark;
        return go;
    }

    private static void ApplyAnchor(RectTransform rt, TextAnchor anchor, Vector2 pos, Vector2 size)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                break;
            case TextAnchor.LowerCenter:
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                break;
            default:
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                break;
        }

        rt.pivot = rt.anchorMin;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
