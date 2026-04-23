using UnityEngine;

public class DoctorUpgradeManager : MonoBehaviour
{
    [Header("Doctor Upgrade Info")]
    public string doctorName = "Bác Sĩ Trưởng";
    public int currentLevel = 1;
    public int maxLevel = 5;

    [Header("Economy Settings")]
    public float baseUpgradeCost = 150f;
    public float costMultiplier = 1.5f;

    [Header("References")]
    [Tooltip("Kéo RoomController của phòng chứa bác sĩ này vào đây để tự động cập nhật thời gian khám khi nâng cấp")]
    public RoomController roomController;

    /// <summary>
    /// Công thức tính giá bạc để nâng cấp bác sĩ lên cấp tiếp theo
    /// Cost = BaseCost * (Multiplier ^ (Level - 1))
    /// </summary>
    public float GetUpgradeCost()
    {
        return baseUpgradeCost * Mathf.Pow(costMultiplier, currentLevel - 1);
    }

    /// <summary>
    /// Gọi khi bấm Nâng cấp Bác sĩ
    /// </summary>
    public bool UpgradeDoctor()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[{doctorName}] Đã đạt level tối đa!");
            return false;
        }

        int cost = Mathf.RoundToInt(GetUpgradeCost());
        
        // --- Tích hợp Trừ Tiền ---
        if (HospitalManager.Instance != null)
        {
            if (!HospitalManager.Instance.HasEnoughMoney(cost))
            {
                Debug.Log($"[{doctorName}] Không đủ tiền để nâng cấp bác sĩ! Cần {cost}$.");
                return false;
            }
            HospitalManager.Instance.SpendMoney(cost);
        }
        // -------------------------

        currentLevel++;
        Debug.Log($"[{doctorName}] Đã được nâng lên Level {currentLevel}!");

        // Rất quan trọng: Báo cho căn phòng biết bác sĩ đã giỏi hơn, yêu cầu rút ngắn thời gian khám!
        if (roomController != null)
        {
            roomController.UpdateRoomStats();
        }

        // --- TODO: Gắn hiệu ứng nhảy nho nhỏ hoặc Particle Effect vào đây tương tự phòng ---

        return true;
    }

    [Header("UI Elements (Nâng cấp)")]
    [Tooltip("UI Nút Nâng cấp xuất hiện khi nhấn vào bác sĩ (chứa nút bấm Mở Confirm)")]
    public GameObject upgradeButtonUI;
    [Tooltip("UI Panel Xác nhận (chứa nút Dấu Tích và nút Dấu X)")]
    public GameObject confirmPanelUI;

    private void Start()
    {
        // Khởi tạo ẩn các UI
        HideAllUpgradeUI();
    }

    private void Update()
    {
        // TÍNH NĂNG NÀY ĐÃ BỊ TẮT do việc nâng cấp được gộp vào bảng Upgrade Tổng của phòng:
        /*
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Bỏ qua nếu đang bấm trúng một cái UI
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Ray ray = mainCam.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Cho phép bấm trúng bản thân Object này, HOẶC bất kỳ cục con nào bên trong nó (phòng khi bạn gắn Collider ở cục Mesh bên trong)
                    if (hit.collider.gameObject == this.gameObject || hit.collider.transform.IsChildOf(this.transform))
                    {
                        Debug.Log($"[DoctorUpgrade] Đã click trúng bác sĩ: {gameObject.name}");
                        OnDoctorClicked();
                    }
                }
            }
        }
        */
    }

    /// <summary>
    /// Xử lý hiện giao diện Upgrade khi bác sĩ bị click trúng
    /// </summary>
    private void OnDoctorClicked()
    {
        if (upgradeButtonUI == null) 
        {
            Debug.LogError($"[DoctorUpgrade] LỖI: Chưa kéo giao diện btn_NangCap vào ô 'Upgrade Button UI' ở Inspector của bác sĩ {gameObject.name}!");
        }
        else 
        {
            upgradeButtonUI.SetActive(true);
        }

        if (confirmPanelUI != null) confirmPanelUI.SetActive(false);
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Nâng cấp (xuất hiện sau khi bấm vào bác sĩ)
    /// </summary>
    public void OnUpgradeButtonClicked()
    {
        // Ẩn nút Nâng cấp đi, hiện 2 nút Tick và X lên
        if (upgradeButtonUI != null) upgradeButtonUI.SetActive(false);
        if (confirmPanelUI != null) confirmPanelUI.SetActive(true);
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Dấu Tích (Đồng ý nâng cấp)
    /// </summary>
    public void OnConfirmUpgradeClicked()
    {
        // Thực hiện nâng cấp
        bool success = UpgradeDoctor();
        
        // Dù thành công (đủ tiền) hay thất bại (thiếu tiền, max cấp) thì đều ẩn UI đi cho gọn
        HideAllUpgradeUI();
    }

    /// <summary>
    /// Gắn hàm này vào OnClick của nút Dấu X (Từ chối nâng cấp)
    /// </summary>
    public void OnCancelUpgradeClicked()
    {
        HideAllUpgradeUI();
    }

    /// <summary>
    /// Hàm tiện ích để tắt sạch UI
    /// </summary>
    public void HideAllUpgradeUI()
    {
        if (upgradeButtonUI != null) upgradeButtonUI.SetActive(false);
        if (confirmPanelUI != null) confirmPanelUI.SetActive(false);
    }
}
