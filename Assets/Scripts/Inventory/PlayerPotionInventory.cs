using UnityEngine;

/// <summary>
/// 플레이어 포션 인벤토리 상태(동일 포션 스택).
/// </summary>
public class PlayerPotionInventory : MonoBehaviour
{
    public struct PotionItem
    {
        public string displayName;
        public Sprite icon;
        public GameObject holdPrefab;
    }

    [SerializeField] private bool hasPotion;
    [SerializeField, Min(0)] private int itemCount;
    [SerializeField, Min(1)] private int maxStackCount = 99;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite itemIcon;
    [SerializeField] private GameObject itemHoldPrefab;

    public bool HasPotion => hasPotion;
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;
    public GameObject ItemHoldPrefab => itemHoldPrefab;
    public int ItemCount => itemCount;

    public static PlayerPotionInventory Resolve(Transform t) => TransformHierarchy.FindComponent<PlayerPotionInventory>(t);

    public bool AddPotion(string displayName, Sprite icon, GameObject holdPrefab)
    {
        string nextName = string.IsNullOrWhiteSpace(displayName) ? "포션" : displayName;

        if (!hasPotion || itemCount <= 0)
        {
            hasPotion = true;
            itemCount = 1;
            itemName = nextName;
            itemIcon = icon;
            itemHoldPrefab = holdPrefab;
            return true;
        }

        bool isSameItem = itemName == nextName && itemIcon == icon && itemHoldPrefab == holdPrefab;
        if (!isSameItem) return false;
        if (itemCount >= maxStackCount) return false;
        itemCount++;
        return true;
    }

    public bool TryConsume(out PotionItem item)
    {
        if (!hasPotion)
        {
            item = default;
            return false;
        }

        item = new PotionItem
        {
            displayName = itemName,
            icon = itemIcon,
            holdPrefab = itemHoldPrefab
        };

        itemCount = Mathf.Max(0, itemCount - 1);
        if (itemCount <= 0)
        {
            hasPotion = false;
            itemName = string.Empty;
            itemIcon = null;
            itemHoldPrefab = null;
        }
        return true;
    }
}
