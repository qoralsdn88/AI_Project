using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 우하단 1번 슬롯 UI. PlayerPotionInventory 상태를 표시합니다.
/// </summary>
public class PotionInventorySlotUi : MonoBehaviour
{
    [SerializeField] private GameObject slotRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI hotkeyLabel;
    [SerializeField] private TextMeshProUGUI nameLabel;

    private PlayerPotionInventory _inventory;

    private void Awake()
    {
        AutoBindIfNeeded();
        if (hotkeyLabel != null) hotkeyLabel.text = "0";
    }

    private void LateUpdate()
    {
        if (_inventory == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _inventory = PlayerPotionInventory.Resolve(player.transform);
        }

        bool hasItem = _inventory != null && _inventory.HasPotion;
        int itemCount = hasItem ? Mathf.Max(0, _inventory.ItemCount) : 0;
        if (slotRoot != null) slotRoot.SetActive(true);

        if (iconImage != null)
        {
            iconImage.enabled = hasItem;
            iconImage.sprite = hasItem ? _inventory.ItemIcon : null;
        }

        if (nameLabel != null)
        {
            nameLabel.text = hasItem ? _inventory.ItemName : string.Empty;
            nameLabel.enabled = hasItem;
        }

        if (hotkeyLabel != null)
        {
            hotkeyLabel.text = itemCount.ToString();
            hotkeyLabel.color = hasItem ? new Color(0.98f, 0.95f, 0.86f, 1f) : new Color(0.6f, 0.6f, 0.62f, 1f);
        }
    }

    private void AutoBindIfNeeded()
    {
        if (slotRoot == null) slotRoot = FindChildRecursive(transform, "SlotRoot")?.gameObject;
        if (iconImage == null && slotRoot != null) iconImage = FindChildRecursive(slotRoot.transform, "Icon")?.GetComponent<Image>();
        if (hotkeyLabel == null && slotRoot != null) hotkeyLabel = FindChildRecursive(slotRoot.transform, "Hotkey")?.GetComponent<TextMeshProUGUI>();
        if (nameLabel == null && slotRoot != null) nameLabel = FindChildRecursive(slotRoot.transform, "ItemName")?.GetComponent<TextMeshProUGUI>();
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name == targetName) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);
            if (found != null) return found;
        }

        return null;
    }
}
