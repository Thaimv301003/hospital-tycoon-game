using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script gắn vào ROOT của Prefab FloatingReceptionistMenu.
/// Prefab này dành riêng cho phòng LỄ TÂN / THU NGÂN — chỉ có 1 card nhân viên duy nhất (căn giữa).
///
/// CÁCH SETUP PREFAB:
/// Root (có script này)
/// ├── CollapsedView     ← nút pin nhỏ, bấm để mở
/// └── ExpandedView      ← panel xổ ra (1 card nhân viên + nút X)
///     ├── CloseButton
///     └── StaffCard
///         ├── StaffLevelText
///         ├── StaffTimeText
///         ├── StaffCostText
///         └── StaffUpgradeButton
///             └── StaffUpgradeButtonText (optional)
/// </summary>
public class FloatingReceptionistMenu : MonoBehaviour
{
    // ===== DỮ LIỆU (được gán bởi ReceptionistUpgradeManager khi Spawn) =====
    [HideInInspector] public ReceptionistUpgradeManager receptionistManager;

    // ===================================================================
    // =====  TRẠNG THÁI THU GỌN  =====
    // ===================================================================
    [Header("=== Thu Gọn (CollapsedView) ===")]
    [Tooltip("GameObject chứa nút ghim bé (icon ⬆ / ✦)")]
    public GameObject collapsedView;
    [Tooltip("Nút để mở menu ra")]
    public Button expandButton;

    // ===================================================================
    // =====  TRẠNG THÁI XỔ RA  =====
    // ===================================================================
    [Header("=== Xổ Ra (ExpandedView) ===")]
    [Tooltip("Panel chứa card nhân viên. Mặc định ẩn.")]
    public GameObject expandedView;
    [Tooltip("Nút X để thu gọn lại")]
    public Button closeButton;

    // ----- Card Nhân Viên -----
    [Header("--- Card Nhân Viên ---")]
    [Tooltip("'Cấp 1 → 2' hoặc 'Cấp 5 (TỐI ĐA)'")]
    public TextMeshProUGUI staffLevelText;
    [Tooltip("'Phục vụ: 3.0s → 2.6s'")]
    public TextMeshProUGUI staffTimeText;
    [Tooltip("'Chi phí: 120$'")]
    public TextMeshProUGUI staffCostText;
    public Button staffUpgradeButton;
    [Tooltip("Text bên trong nút (tuỳ chọn, vd: '120$' hoặc 'TỐI ĐA')")]
    public TextMeshProUGUI staffUpgradeButtonText;

    // ----- Thiếu tiền -----
    [Header("--- Thông Báo Thiếu Tiền ---")]
    [Tooltip("Panel popup hiện khi không đủ tiền")]
    public GameObject insufficientFundsPanel;
    [Tooltip("Text hiện số tiền cần thêm")]
    public TextMeshProUGUI insufficientFundsText;

    // ===================================================================
    // =====  MÀU SẮC  =====
    // ===================================================================
    [Header("=== Màu Sắc ===")]
    public Color activeColor   = new Color(0.18f, 0.68f, 0.22f);  // Xanh lá tươi
    public Color disabledColor = new Color(0.40f, 0.40f, 0.40f);  // Xám

    // ===================================================================
    // STATE NỘI BỘ
    // ===================================================================
    private bool _isExpanded = false;

    // ===================================================================
    // UNITY LIFECYCLE
    // ===================================================================

    private void Start()
    {
        if (expandButton != null)
            expandButton.onClick.AddListener(OnExpandClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (staffUpgradeButton != null)
        {
            staffUpgradeButton.onClick.AddListener(OnStaffUpgradeClicked);
            staffUpgradeButton.transition = Selectable.Transition.None;
        }

        // Bấm vào panel thiếu tiền → tự dismiss
        if (insufficientFundsPanel != null)
        {
            Button panelBtn = insufficientFundsPanel.GetComponent<Button>();
            if (panelBtn == null)
                panelBtn = insufficientFundsPanel.AddComponent<Button>();
            panelBtn.transition = Selectable.Transition.None;
            panelBtn.onClick.AddListener(DismissInsufficientFunds);
        }

        // Trạng thái mặc định: thu gọn
        SetExpanded(false, refresh: false);
        if (insufficientFundsPanel != null)
            insufficientFundsPanel.SetActive(false);
    }

    private void Update()
    {
        if (_isExpanded)
            RefreshButtonState();
    }

    // ===================================================================
    // PUBLIC API — GỌI BỞI ReceptionistUpgradeManager SAU KHI SPAWN
    // ===================================================================

    /// <summary>Khởi tạo menu với dữ liệu từ phòng. Gọi ngay sau Instantiate.</summary>
    public void Initialize(ReceptionistUpgradeManager manager)
    {
        receptionistManager = manager;
    }

    // ===================================================================
    // UI EVENTS
    // ===================================================================

    private void OnExpandClicked()
    {
        // Chặn không cho mở nếu đã có menu (phòng khám hoặc lễ tân) nào khác đang mở
        if (FloatingUpgradeMenu.IsAnyMenuExpanded) return;

        SetExpanded(true, refresh: true);
    }

    private void OnCloseClicked()
    {
        // Nếu đang hiện popup thiếu tiền → chỉ đóng popup
        if (insufficientFundsPanel != null && insufficientFundsPanel.activeSelf)
        {
            insufficientFundsPanel.SetActive(false);
            return;
        }
        SetExpanded(false, refresh: false);
    }

    public void DismissInsufficientFunds()
    {
        if (insufficientFundsPanel != null)
            insufficientFundsPanel.SetActive(false);
    }

    private void OnStaffUpgradeClicked()
    {
        if (receptionistManager == null) return;

        int cost = Mathf.RoundToInt(receptionistManager.GetUpgradeCost());

        if (HospitalManager.Instance != null && !HospitalManager.Instance.HasEnoughMoney(cost))
        {
            ShowInsufficientFunds(cost);
            return;
        }

        bool success = receptionistManager.UpgradeStaff();
        if (success)
        {
            // Nâng cấp thành công → thu gọn về CollapsedView
            SetExpanded(false, refresh: false);
        }
    }

    // ===================================================================
    // HIỂN THỊ & CẬP NHẬT
    // ===================================================================

    private void SetExpanded(bool expanded, bool refresh)
    {
        _isExpanded = expanded;
        
        // Cập nhật trạng thái khóa toàn cục
        if (expanded) FloatingUpgradeMenu.IsAnyMenuExpanded = true;
        else FloatingUpgradeMenu.IsAnyMenuExpanded = false;

        if (collapsedView != null)  collapsedView.SetActive(!expanded);
        if (expandedView != null)   expandedView.SetActive(expanded);

        if (expanded && refresh)
            RefreshCard();
    }

    private void RefreshCard()
    {
        if (receptionistManager == null) return;

        int   curLevel = receptionistManager.currentLevel;
        int   maxLevel = receptionistManager.maxLevel;
        bool  isMaxed  = curLevel >= maxLevel;

        // Level text
        if (staffLevelText != null)
            staffLevelText.text = isMaxed
                ? $"Cấp {curLevel} (TỐI ĐA)"
                : $"Cấp {curLevel} → {curLevel + 1}";

        // Thời gian phục vụ
        if (staffTimeText != null)
        {
            float cur  = receptionistManager.GetCurrentProcessTime();
            float next = receptionistManager.GetUpgradedProcessTime();
            staffTimeText.text = isMaxed
                ? $"Phục vụ: {cur:F1}s (MIN)"
                : $"Phục vụ: {cur:F1}s → {next:F1}s";
        }

        // Chi phí & text trong nút
        string costLabel = isMaxed
            ? "ĐÃ MAX"
            : $"{Mathf.RoundToInt(receptionistManager.GetUpgradeCost())}$";

        if (staffCostText != null)
            staffCostText.text = $"Chi phí: {costLabel}";

        if (staffUpgradeButtonText != null)
            staffUpgradeButtonText.text = isMaxed ? "TỐI ĐA" : costLabel;

        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (staffUpgradeButton == null || receptionistManager == null) return;

        bool maxed = receptionistManager.currentLevel >= receptionistManager.maxLevel;
        staffUpgradeButton.interactable = !maxed;

        if (maxed)
            SetButtonColor(staffUpgradeButton, disabledColor);
        // Khi chưa max: giữ nguyên màu gốc đã design trong prefab
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private void ShowInsufficientFunds(int requiredCost)
    {
        if (insufficientFundsPanel == null) return;

        insufficientFundsPanel.SetActive(true);

        if (insufficientFundsText != null && HospitalManager.Instance != null)
        {
            int have     = Mathf.RoundToInt(HospitalManager.Instance.totalRevenue);
            int shortage = requiredCost - have;
            insufficientFundsText.text =
                $"Không đủ tiền!\nCần thêm <color=#FF5555>{shortage}$</color> để nâng cấp.";
        }
    }
}
