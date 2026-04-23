using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script này được gắn trực tiếp vào PREFAB UI lơ lửng trên đầu từng phòng.
/// Nó quản lý việc hiển thị trạng thái "thu gọn" (1 nút ghim),
/// và trạng thái "xổ ra" (2 card nâng cấp Phòng + Bác Sĩ).
///
/// CÁCH SETUP PREFAB:
/// 1. Tạo một Prefab UI mới (là con của Canvas)
/// 2. Gắn script này vào Root của Prefab
/// 3. Tạo 2 child view bên trong (xem Header bên dưới để biết cần những gì)
/// 4. Kéo thả các object tương ứng vào các slot ở Inspector
/// </summary>
public class FloatingUpgradeMenu : MonoBehaviour
{
    // ===== THAM CHIẾU DỮ LIỆU (được gán bởi RoomUpgradeManager khi Spawn) =====
    [HideInInspector] public RoomUpgradeManager roomManager;
    [HideInInspector] public DoctorUpgradeManager doctorManager;

    // ===================================================================
    // =====  TRẠNG THÁI THU GỌN: Chỉ hiện 1 nút bé  =====
    // ===================================================================
    [Header("=== Trạng Thái Thu Gọn (Nút Ghim 1 nút) ===")]
    [Tooltip("GameObject chứa nút ghim bé xíu (icon ⬆ hoặc ✦)")]
    public GameObject collapsedView;
    [Tooltip("Nút ghim bé để người chơi bấm vào mở menu")]
    public Button expandButton;

    // ===================================================================
    // =====  TRẠNG THÁI XỔ RA: Hiện 2 card nâng cấp  =====
    // ===================================================================
    [Header("=== Trạng Thái Xổ Ra (2 Card Nâng Cấp) ===")]
    [Tooltip("GameObject chứa toàn bộ giao diện 2 card. Ẩn mặc định.")]
    public GameObject expandedView;
    [Tooltip("Nút X ở góc trên bên phải để thu gọn menu lại")]
    public Button closeButton;

    // ----- Card Nâng Cấp PHÒNG -----
    [Header("--- Card Phòng (bên trái) ---")]
    [Tooltip("'Cấp 1 → 2'")]
    public TextMeshProUGUI roomLevelText;
    [Tooltip("'Thu nhập: 50$ → 62$'")]
    public TextMeshProUGUI roomIncomeText;
    [Tooltip("'Chi phí: 100$'")]
    public TextMeshProUGUI roomCostText;
    public Button roomUpgradeButton;
    [Tooltip("Text ở giữa nút Nâng Cấp Phòng (tuỳ chọn)")]
    public TextMeshProUGUI roomUpgradeButtonText;

    // ----- Card Nâng Cấp BÁC SĨ -----
    [Header("--- Card Bác Sĩ (bên phải) ---")]
    [Tooltip("Bao ngoài toàn bộ Card Bác Sĩ — Script tự ẩn đi nếu phòng không có BS")]
    public GameObject doctorCard;
    [Tooltip("'Cấp 1 → 2'")]
    public TextMeshProUGUI doctorLevelText;
    [Tooltip("'Khám: 3.0s → 2.4s'")]
    public TextMeshProUGUI doctorTimeText;
    [Tooltip("'Chi phí: 150$'")]
    public TextMeshProUGUI doctorCostText;
    public Button doctorUpgradeButton;
    [Tooltip("Text ở giữa nút Nâng Cấp BS (tuỳ chọn)")]
    public TextMeshProUGUI doctorUpgradeButtonText;

    // ----- Thông báo "Thiếu tiền" -----
    [Header("--- Thông Báo Thiếu Tiền (bấm X khi thiếu tiền) ---")]
    [Tooltip("Panel popup hiện khi bấm X hoặc bấm nút khi thiếu tiền")]
    public GameObject insufficientFundsPanel;
    [Tooltip("Text hiện số tiền cần thêm")]
    public TextMeshProUGUI insufficientFundsText;

    // ===================================================================
    // =====  MÀU SẮC  =====
    // ===================================================================
    [Header("=== Màu Sắc Nút ===")]
    [Tooltip("Màu nút khi đủ tiền và chưa max cấp")]
    public Color activeColor = new Color(0.18f, 0.68f, 0.22f);   // Xanh lá tươi
    [Tooltip("Màu nút khi không thể bấm (thiếu tiền / max cấp)")]
    public Color disabledColor = new Color(0.4f, 0.4f, 0.4f);    // Xám

    // ===================================================================
    // =====  STATE NỘI BỘ  =====
    // ===================================================================
    public static bool IsAnyMenuExpanded = false; // Cờ toàn cục để khóa các menu khác
    private bool _isExpanded = false;
    private int _pendingCost = 0;

    // ===================================================================
    // UNITY LIFECYCLE
    // ===================================================================

    private void Start()
    {
        // Nối sự kiện cho các nút
        if (expandButton != null)
            expandButton.onClick.AddListener(OnExpandClicked);
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);
        if (roomUpgradeButton != null)
        {
            roomUpgradeButton.onClick.AddListener(OnRoomUpgradeClicked);
            // Tắt hiệu ứng màu tự động của Button → không tô xanh khi hover/affordable
            roomUpgradeButton.transition = Selectable.Transition.None;
        }
        if (doctorUpgradeButton != null)
        {
            doctorUpgradeButton.onClick.AddListener(OnDoctorUpgradeClicked);
            doctorUpgradeButton.transition = Selectable.Transition.None;
        }

        // Tự động gắn: chạm vào bất kỳ đâu trên InsufficientFundsPanel → dismiss
        if (insufficientFundsPanel != null)
        {
            // Lấy hoặc tự thêm Button component vào panel
            Button panelBtn = insufficientFundsPanel.GetComponent<Button>();
            if (panelBtn == null)
                panelBtn = insufficientFundsPanel.AddComponent<Button>();

            // Xoá màu transition để Button không đổi màu khi hover/click
            panelBtn.transition = Selectable.Transition.None;

            panelBtn.onClick.AddListener(DismissInsufficientFunds);
        }

        // Trạng thái mặc định: thu gọn, ẩn panel tiền thiếu
        SetExpanded(false, refresh: false);
        if (insufficientFundsPanel != null)
            insufficientFundsPanel.SetActive(false);
    }

    private void Update()
    {
        // Khi menu đang mở: liên tục kiểm tra túi tiền → cập nhật màu & interactable
        if (_isExpanded)
        {
            RefreshButtonStates();
        }
    }

    // ===================================================================
    // PUBLIC API — GỌI BỞI RoomUpgradeManager SAU KHI SPAWN PREFAB
    // ===================================================================

    /// <summary>
    /// Khởi tạo Menu với dữ liệu từ phòng. Gọi ngay sau Instantiate.
    /// </summary>
    public void Initialize(RoomUpgradeManager room, DoctorUpgradeManager doctor)
    {
        roomManager = room;
        doctorManager = doctor;

        // Tự ẩn Card Bác Sĩ nếu phòng không có bác sĩ
        if (doctorCard != null)
            doctorCard.SetActive(doctorManager != null);
    }

    // ===================================================================
    // UI EVENTS
    // ===================================================================

    private void OnExpandClicked()
    {
        if (IsAnyMenuExpanded) return; // Nếu đã có menu nào đó mở thì chặn, không cho mở menu này

        SetExpanded(true, refresh: true);
    }

    private void OnCloseClicked()
    {
        // Nếu đang hiện thông báo thiếu tiền → chỉ đóng popup đó
        if (insufficientFundsPanel != null && insufficientFundsPanel.activeSelf)
        {
            insufficientFundsPanel.SetActive(false);
            return;
        }
        // Ngược lại → thu gọn menu về 1 nút ghim
        SetExpanded(false, refresh: false);
    }

    /// <summary>
    /// Chạm vào bất kỳ đâu trên InsufficientFundsPanel → panel biến mất.
    /// Được tự động gắn vào Button của Panel trong Start().
    /// </summary>
    public void DismissInsufficientFunds()
    {
        if (insufficientFundsPanel != null)
            insufficientFundsPanel.SetActive(false);
    }

    private void OnRoomUpgradeClicked()
    {
        if (roomManager == null) return;

        int cost = Mathf.RoundToInt(roomManager.GetUpgradeCost());

        // Kiểm tra thiếu tiền
        if (HospitalManager.Instance != null && !HospitalManager.Instance.HasEnoughMoney(cost))
        {
            ShowInsufficientFunds(cost);
            return;
        }

        bool success = roomManager.UpgradeRoom();
        if (success)
        {
            // Nâng cấp thành công → thu gọn về CollapsedView
            SetExpanded(false, refresh: false);
        }
    }

    private void OnDoctorUpgradeClicked()
    {
        if (doctorManager == null) return;

        int cost = Mathf.RoundToInt(doctorManager.GetUpgradeCost());

        // Kiểm tra thiếu tiền
        if (HospitalManager.Instance != null && !HospitalManager.Instance.HasEnoughMoney(cost))
        {
            ShowInsufficientFunds(cost);
            return;
        }

        bool success = doctorManager.UpgradeDoctor();
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
        if (expanded) IsAnyMenuExpanded = true;
        else IsAnyMenuExpanded = false;

        if (collapsedView != null) collapsedView.SetActive(!expanded);
        if (expandedView != null) expandedView.SetActive(expanded);

        if (expanded && refresh)
            RefreshAllDisplay();
    }

    private void RefreshAllDisplay()
    {
        RefreshRoomCard();
        RefreshDoctorCard();
        RefreshButtonStates();
    }

    private void RefreshRoomCard()
    {
        if (roomManager == null) return;

        int curLevel = roomManager.currentLevel;
        int maxLevel = roomManager.maxLevel;
        bool isMaxed = curLevel >= maxLevel;

        // Level
        if (roomLevelText != null)
            roomLevelText.text = isMaxed
                ? $"Cấp {curLevel} (TỐI ĐA)"
                : $"Cấp {curLevel} → {curLevel + 1}";

        // Doanh thu
        if (roomIncomeText != null)
        {
            float curIncome = roomManager.GetCurrentIncome();
            roomIncomeText.text = isMaxed
                ? $"Thu nhập: {Mathf.RoundToInt(curIncome)}$ (MAX)"
                : $"Thu nhập: {Mathf.RoundToInt(curIncome)}$ → {Mathf.RoundToInt(roomManager.GetUpgradedIncome())}$";
        }

        // Chi phí + text trong nút
        string costLabel = isMaxed ? "ĐÃ MAX" : $"{Mathf.RoundToInt(roomManager.GetUpgradeCost())}$";
        if (roomCostText != null)          roomCostText.text = $"Chi phí: {costLabel}";
        if (roomUpgradeButtonText != null) roomUpgradeButtonText.text = isMaxed ? "TỐI ĐA" : costLabel;
    }

    private void RefreshDoctorCard()
    {
        if (doctorCard == null || doctorManager == null) return;

        int curLevel = doctorManager.currentLevel;
        int maxLevel = doctorManager.maxLevel;
        bool isMaxed = curLevel >= maxLevel;

        // Level
        if (doctorLevelText != null)
            doctorLevelText.text = isMaxed
                ? $"Cấp {curLevel} (TỐI ĐA)"
                : $"Cấp {curLevel} → {curLevel + 1}";

        // Thời gian khám
        if (doctorTimeText != null)
        {
            float baseTime = GetBaseProcessTime();
            float curTime  = baseTime * Mathf.Pow(0.8f, curLevel - 1);
            float nextTime = baseTime * Mathf.Pow(0.8f, curLevel);      // level + 1 - 1 = level
            doctorTimeText.text = isMaxed
                ? $"Khám: {curTime:F1}s (MIN)"
                : $"Khám: {curTime:F1}s → {nextTime:F1}s";
        }

        // Chi phí + text trong nút
        string costLabel = isMaxed ? "ĐÃ MAX" : $"{Mathf.RoundToInt(doctorManager.GetUpgradeCost())}$";
        if (doctorCostText != null)          doctorCostText.text = $"Chi phí: {costLabel}";
        if (doctorUpgradeButtonText != null) doctorUpgradeButtonText.text = isMaxed ? "TỐI ĐA" : costLabel;
    }

    /// <summary>
    /// Cập nhật màu sắc + interactable cho cả 2 nút dựa trên tiền hiện có.
    /// Gọi mỗi frame khi menu đang mở (Update).
    private void RefreshButtonStates()
    {
        // Nút Phòng
        if (roomUpgradeButton != null && roomManager != null)
        {
            bool maxed = roomManager.currentLevel >= roomManager.maxLevel;
            // Chỉ disable khi đã MAX — giữ nguyên màu gốc khi chưa max
            roomUpgradeButton.interactable = !maxed;
            // Chỉ tô xám khi đạt cấp tối đa, không đổi màu theo tiền
            if (maxed) SetButtonColor(roomUpgradeButton, disabledColor);
        }

        // Nút Bác Sĩ
        if (doctorUpgradeButton != null && doctorManager != null)
        {
            bool maxed = doctorManager.currentLevel >= doctorManager.maxLevel;
            doctorUpgradeButton.interactable = !maxed;
            if (maxed) SetButtonColor(doctorUpgradeButton, disabledColor);
        }
    }

    private void SetButtonColor(Button btn, Color color)
    {
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    /// <summary>
    /// Hiện popup "Bạn đang thiếu X$" (thường là khi bấm X hoặc bấm nút khi hết tiền).
    /// </summary>
    private void ShowInsufficientFunds(int requiredCost)
    {
        if (insufficientFundsPanel == null) return;

        insufficientFundsPanel.SetActive(true);

        if (insufficientFundsText != null && HospitalManager.Instance != null)
        {
            int have     = Mathf.RoundToInt(HospitalManager.Instance.totalRevenue);
            int shortage = requiredCost - have;
            insufficientFundsText.text = $"Không đủ tiền!\nCần thêm <color=#FF5555>{shortage}$</color> để nâng cấp.";
        }
    }

    // ===================================================================
    // TIỆN ÍCH
    // ===================================================================

    private float GetBaseProcessTime()
    {
        if (roomManager?.roomController?.roomData != null)
            return roomManager.roomController.roomData.baseProcessTime;
        return 3f;
    }
}
