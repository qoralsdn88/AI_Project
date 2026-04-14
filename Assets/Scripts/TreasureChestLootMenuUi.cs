using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// 보물상자 전리품 UI. 실제 UI 오브젝트는 Hierarchy(Canvas)에서 관리하고 이 스크립트는 동작만 담당합니다.
/// </summary>
[DisallowMultipleComponent]
public class TreasureChestLootMenuUi : MonoBehaviour
{
    [Header("Hierarchy 참조")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI itemNameLabel;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Button takeButton;
    [SerializeField] private Button discardButton;
    [Header("샘플 아이템")]
    [SerializeField] private string sampleItemName = "붉은 포션";
    [SerializeField] private Sprite sampleItemSprite;
    [SerializeField] private GameObject samplePotionHoldPrefab;

    private TreasureChestInteractStation _openedChest;
    private bool _isOpen;
    public bool IsOpen => _isOpen;

    private void Awake()
    {
        AutoBindReferencesIfNeeded();
        WireButtons();
        HideImmediate();
        if (targetCanvas != null) targetCanvas.enabled = false;
    }

    private void LateUpdate()
    {
        if (!_isOpen && rootPanel != null) _isOpen = rootPanel.activeInHierarchy;
        if (!_isOpen) return;
        if (!BlacksmithGameplayLock.IsMenuOpen) BlacksmithGameplayLock.SetMenuOpen(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OpenForChest(TreasureChestInteractStation chest)
    {
        _openedChest = chest;
        _openedChest?.MarkAsOpened();
        EnsureEventSystemExists();
        if (targetCanvas != null) targetCanvas.enabled = true;
        if (rootPanel != null) rootPanel.SetActive(true);
        _isOpen = true;
        BlacksmithGameplayLock.SetMenuOpen(true);
        ApplyGameplayFocus(true);

        if (titleLabel != null) titleLabel.text = "보물 상자";
        if (itemNameLabel != null) itemNameLabel.text = $"획득 아이템: {sampleItemName}";
        if (itemIconImage != null)
        {
            itemIconImage.sprite = sampleItemSprite != null ? sampleItemSprite : UiSpriteUtility.WhiteSprite;
            itemIconImage.preserveAspect = true;
            itemIconImage.color = Color.white;
        }
    }

    public void CloseFullUi()
    {
        if (!BlacksmithGameplayLock.IsMenuOpen) return;
        HideImmediate();
        if (targetCanvas != null) targetCanvas.enabled = false;
        _openedChest = null;
        _isOpen = false;
        BlacksmithGameplayLock.SetMenuOpen(false);
        ApplyGameplayFocus(false);
    }

    private void OnDisable()
    {
        if (!BlacksmithGameplayLock.IsMenuOpen) return;
        HideImmediate();
        if (targetCanvas != null) targetCanvas.enabled = false;
        _isOpen = false;
        BlacksmithGameplayLock.SetMenuOpen(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void ApplyGameplayFocus(bool menuBlocking)
    {
        if (menuBlocking)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HideImmediate()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    private void WireButtons()
    {
        BindButton(takeButton, OnTakeClicked);
        BindButton(discardButton, OnDiscardClicked);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button == null) return;
        button.onClick.RemoveListener(listener);
        button.onClick.AddListener(listener);
    }

    private void OnTakeClicked()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerPotionInventory inventory = PlayerPotionInventory.Resolve(player.transform);
            if (inventory == null) inventory = player.AddComponent<PlayerPotionInventory>();
            inventory.AddPotion(sampleItemName, sampleItemSprite, samplePotionHoldPrefab);
        }

        CloseFullUi();
    }

    private void OnDiscardClicked()
    {
        if (itemNameLabel != null) itemNameLabel.text = "아이템을 버렸습니다.";
        CloseFullUi();
    }

    private void AutoBindReferencesIfNeeded()
    {
        if (targetCanvas == null) targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null) return;

        if (rootPanel == null) rootPanel = FindChild(targetCanvas.transform, "ChestLootRoot");
        if (titleLabel == null && rootPanel != null) titleLabel = FindText(rootPanel.transform, "Title");
        if (itemNameLabel == null && rootPanel != null) itemNameLabel = FindText(rootPanel.transform, "ItemName");
        if (itemIconImage == null && rootPanel != null)
        {
            GameObject iconGo = FindChild(rootPanel.transform, "ItemIcon");
            if (iconGo != null) itemIconImage = iconGo.GetComponent<Image>();
        }

        if (takeButton == null && rootPanel != null) takeButton = FindButton(rootPanel.transform, "Button_획득하기");
        if (discardButton == null && rootPanel != null) discardButton = FindButton(rootPanel.transform, "Button_버리기");
    }

    private static GameObject FindChild(Transform root, string childName)
    {
        if (root == null) return null;
        Transform t = root.Find(childName);
        return t != null ? t.gameObject : null;
    }

    private static TextMeshProUGUI FindText(Transform root, string childName)
    {
        GameObject go = FindChild(root, childName);
        return go != null ? go.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Button FindButton(Transform root, string childName)
    {
        GameObject go = FindChild(root, childName);
        return go != null ? go.GetComponent<Button>() : null;
    }

    public static TreasureChestLootMenuUi EnsureInstance()
    {
        TreasureChestLootMenuUi existing = Object.FindFirstObjectByType<TreasureChestLootMenuUi>(FindObjectsInactive.Include);
        if (existing != null) return existing;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null) return canvas.gameObject.AddComponent<TreasureChestLootMenuUi>();

        GameObject host = new GameObject("TreasureChestLootMenu");
        return host.AddComponent<TreasureChestLootMenuUi>();
    }

    private static void EnsureEventSystemExists()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            es = esGo.AddComponent<EventSystem>();
        }

        if (es.GetComponent<StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<StandaloneInputModule>();
        }
#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            es.gameObject.AddComponent<InputSystemUIInputModule>();
        }
#endif
    }
}
