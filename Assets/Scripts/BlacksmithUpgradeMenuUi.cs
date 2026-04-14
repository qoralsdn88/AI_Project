using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대장장이 강화 UI(루트 선택 → 수치 비교 → 진행 → 성공).
/// UI 오브젝트는 씬/프리팹 Hierarchy에서 구성하고, 이 스크립트는 동작만 담당합니다.
/// F 토글은 <see cref="BlackSmithInteractStation"/>이 처리합니다.
/// </summary>
[DisallowMultipleComponent]
public class BlacksmithUpgradeMenuUi : MonoBehaviour
{
    [Header("Hierarchy 참조")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private GameObject previewPanel;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI compareLabel;
    [SerializeField] private TextMeshProUGUI successLabel;
    [SerializeField] private Button weaponButton;
    [SerializeField] private Button shieldButton;
    [SerializeField] private Button proceedButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button closeSuccessButton;

    private enum ForgeKind
    {
        None,
        Weapon,
        Shield,
    }

    private ForgeKind _pendingKind;

    private void Awake()
    {
        AutoBindReferencesIfNeeded();
        WireButtons();
        HideAllPanelsImmediate();
        if (targetCanvas != null) targetCanvas.enabled = false;
    }

    public void OpenPickTypeUi()
    {
        CachePlayerRefs();
        if (targetCanvas != null) targetCanvas.enabled = true;
        BlacksmithGameplayLock.SetMenuOpen(true);
        ApplyGameplayFocus(true);
        ShowOnly(rootPanel);
        if (titleLabel != null) titleLabel.text = "대장장이 — 강화";
        RefreshPickButtonsInteractable();
    }

    public void CloseFullUi()
    {
        if (!BlacksmithGameplayLock.IsMenuOpen) return;
        HideAllPanelsImmediate();
        if (targetCanvas != null) targetCanvas.enabled = false;
        BlacksmithGameplayLock.SetMenuOpen(false);
        _pendingKind = ForgeKind.None;
        ApplyGameplayFocus(false);
    }

    private void OnDisable()
    {
        if (!BlacksmithGameplayLock.IsMenuOpen) return;
        HideAllPanelsImmediate();
        if (targetCanvas != null) targetCanvas.enabled = false;
        BlacksmithGameplayLock.SetMenuOpen(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HideAllPanelsImmediate()
    {
        if (rootPanel != null) rootPanel.SetActive(false);
        if (previewPanel != null) previewPanel.SetActive(false);
        if (successPanel != null) successPanel.SetActive(false);
    }

    private void ShowOnly(GameObject panel)
    {
        HideAllPanelsImmediate();
        if (panel != null) panel.SetActive(true);
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

    private void WireButtons()
    {
        BindButton(weaponButton, OnWeaponChosen);
        BindButton(shieldButton, OnShieldChosen);
        BindButton(proceedButton, OnProceedClicked);
        BindButton(backButton, OnBackFromPreview);
        BindButton(closeSuccessButton, CloseFullUi);
    }

    private static void BindButton(Button button, UnityEngine.Events.UnityAction listener)
    {
        if (button == null) return;
        button.onClick.RemoveListener(listener);
        button.onClick.AddListener(listener);
    }

    private void AutoBindReferencesIfNeeded()
    {
        if (targetCanvas == null) targetCanvas = GetComponentInChildren<Canvas>(true);
        if (targetCanvas == null)
        {
            BlacksmithUpgradeMenuUi existing = Object.FindFirstObjectByType<BlacksmithUpgradeMenuUi>(FindObjectsInactive.Include);
            if (existing != null && existing != this && existing.targetCanvas != null)
            {
                targetCanvas = existing.targetCanvas;
            }
        }

        if (targetCanvas == null) return;

        if (rootPanel == null) rootPanel = FindChild(targetCanvas.transform, "ForgeRoot");
        if (previewPanel == null) previewPanel = FindChild(targetCanvas.transform, "ForgePreview");
        if (successPanel == null) successPanel = FindChild(targetCanvas.transform, "ForgeSuccess");

        if (titleLabel == null && rootPanel != null) titleLabel = FindText(rootPanel.transform, "Title");
        if (compareLabel == null && previewPanel != null) compareLabel = FindText(previewPanel.transform, "Compare");
        if (successLabel == null && successPanel != null) successLabel = FindText(successPanel.transform, "Message");

        if (weaponButton == null && rootPanel != null) weaponButton = FindButton(rootPanel.transform, "Button_무기 강화");
        if (shieldButton == null && rootPanel != null) shieldButton = FindButton(rootPanel.transform, "Button_방패 강화");
        if (proceedButton == null && previewPanel != null) proceedButton = FindButton(previewPanel.transform, "Button_강화 진행");
        if (backButton == null && previewPanel != null) backButton = FindButton(previewPanel.transform, "Button_돌아가기");
        if (closeSuccessButton == null && successPanel != null) closeSuccessButton = FindButton(successPanel.transform, "Button_닫기");
    }

    private void RefreshPickButtonsInteractable()
    {
        CachePlayerRefs();
        if (shieldButton != null && _cachedUpgrade != null)
        {
            shieldButton.interactable = _cachedUpgrade.CanApplyShieldUpgrade();
        }
    }

    private void OnWeaponChosen()
    {
        CachePlayerRefs();
        if (_cachedMelee == null) return;
        _pendingKind = ForgeKind.Weapon;
        ShowOnly(previewPanel);
        int cur = _cachedMelee.CurrentAttackDamage;
        int next = cur + 10;
        if (compareLabel != null) compareLabel.text = $"무기 공격력\n<color=#888888>{cur}</color>  →  <color=#c9a66b>{next}</color>";
    }

    private void OnShieldChosen()
    {
        CachePlayerRefs();
        if (_cachedUpgrade == null) return;
        if (!_cachedUpgrade.CanApplyShieldUpgrade()) return;

        _pendingKind = ForgeKind.Shield;
        ShowOnly(previewPanel);
        float before = _cachedUpgrade.GuardDamageTakenMultiplier * 100f;
        if (compareLabel != null)
        {
            compareLabel.text = $"가드 성공 시 받는 피해(원 피해 대비)\n<color=#888888>{before:0.#}%</color>  →  <color=#c9a66b>1%</color>";
        }
    }

    private void OnProceedClicked()
    {
        CachePlayerRefs();
        if (_pendingKind == ForgeKind.Weapon)
        {
            if (_cachedMelee == null || _cachedUpgrade == null) return;
            _cachedUpgrade.TryApplyWeaponUpgrade(_cachedMelee);
            ShowOnly(successPanel);
            if (successLabel != null) successLabel.text = $"강화 성공\n공격력 +10 (현재 {_cachedMelee.CurrentAttackDamage})";
            return;
        }

        if (_pendingKind == ForgeKind.Shield)
        {
            if (_cachedUpgrade == null) return;
            if (!_cachedUpgrade.TryApplyShieldUpgrade()) return;
            ShowOnly(successPanel);
            if (successLabel != null) successLabel.text = "강화 성공\n가드 시 받는 피해가 1%로 낮아졌습니다.";
        }
    }

    private void OnBackFromPreview()
    {
        _pendingKind = ForgeKind.None;
        ShowOnly(rootPanel);
        if (titleLabel != null) titleLabel.text = "대장장이 — 강화";
        RefreshPickButtonsInteractable();
    }
    
    private PlayerMeleeCombat _cachedMelee;
    private PlayerUpgradeState _cachedUpgrade;

    private void CachePlayerRefs()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        _cachedMelee = PlayerMeleeCombat.Resolve(p.transform);
        _cachedUpgrade = PlayerUpgradeState.Resolve(p.transform);
        if (_cachedUpgrade == null) _cachedUpgrade = p.AddComponent<PlayerUpgradeState>();
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

    public static BlacksmithUpgradeMenuUi EnsureInstance()
    {
        BlacksmithUpgradeMenuUi existing = Object.FindFirstObjectByType<BlacksmithUpgradeMenuUi>(FindObjectsInactive.Include);
        if (existing != null) return existing;
        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null)
        {
            return canvas.gameObject.AddComponent<BlacksmithUpgradeMenuUi>();
        }
        
        GameObject host = new GameObject("BlacksmithUpgradeMenu");
        return host.AddComponent<BlacksmithUpgradeMenuUi>();
    }
}
