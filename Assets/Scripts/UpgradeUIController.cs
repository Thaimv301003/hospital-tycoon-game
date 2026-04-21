using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // Sử dụng thư viện TextMeshPro

public class UpgradeUIController : MonoBehaviour
{
    public static UpgradeUIController Instance { get; private set; }

    [Header("Main Panel")]
    public GameObject mainPanel;

    [Header("Room Information UI")]
    public TextMeshProUGUI roomNameText; 
    public TextMeshProUGUI roomLevelText;
    public TextMeshProUGUI roomCostText;
    public Button roomUpgradeButton;

    [Header("Doctor Information UI")]
    public GameObject doctorSection; // Ẩn/Hiện nếu phòng không có bác sĩ
    public TextMeshProUGUI doctorNameText; 
    public TextMeshProUGUI doctorLevelText;
    public TextMeshProUGUI doctorCostText;
    public Button doctorUpgradeButton;

    [Header("Close Button")]
    public Button closeButton;

    // Tham chiếu đến dữ liệu đang duyệt
    private RoomUpgradeManager currentRoom;
    private DoctorUpgradeManager currentDoctor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        // Ẩn panel mặc định
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
        }

        // Gắn sự kiện nâng cấp
        if (roomUpgradeButton != null)
            roomUpgradeButton.onClick.AddListener(OnRoomUpgradeClicked);

        if (doctorUpgradeButton != null)
            doctorUpgradeButton.onClick.AddListener(OnDoctorUpgradeClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    /// <summary>
    /// Hàm mở bảng UI, truyền vào Room và Doctor nếu có.
    /// </summary>
    public void OpenPanel(RoomUpgradeManager roomManager, DoctorUpgradeManager doctorManager)
    {
        currentRoom = roomManager;
        currentDoctor = doctorManager;

        if (mainPanel != null)
            mainPanel.SetActive(true);

        RefreshUI();
    }

    /// <summary>
    /// Hàm đóng bảng UI.
    /// </summary>
    public void ClosePanel()
    {
        if (mainPanel != null)
            mainPanel.SetActive(false);
            
        currentRoom = null;
        currentDoctor = null;
    }

    /// <summary>
    /// Cập nhật các thông số hiển thị lên Text
    /// </summary>
    private void RefreshUI()
    {
        // Cập nhật thông tin phòng
        if (currentRoom != null)
        {
            if (roomNameText != null) roomNameText.text = currentRoom.roomName;
            if (roomLevelText != null) roomLevelText.text = "Cấp " + currentRoom.currentLevel + "/" + currentRoom.maxLevel;
            
            if (currentRoom.currentLevel < currentRoom.maxLevel)
            {
                if (roomCostText != null) roomCostText.text = Mathf.RoundToInt(currentRoom.GetUpgradeCost()) + "$";
                if (roomUpgradeButton != null) roomUpgradeButton.interactable = true;
            }
            else
            {
                if (roomCostText != null) roomCostText.text = "Tối Đa";
                if (roomUpgradeButton != null) roomUpgradeButton.interactable = false;
            }
        }

        // Cập nhật thông tin Bác sĩ
        if (currentDoctor != null)
        {
            if (doctorSection != null) doctorSection.SetActive(true);

            if (doctorNameText != null) doctorNameText.text = currentDoctor.doctorName;
            if (doctorLevelText != null) doctorLevelText.text = "Cấp " + currentDoctor.currentLevel + "/" + currentDoctor.maxLevel;

            if (currentDoctor.currentLevel < currentDoctor.maxLevel)
            {
                if (doctorCostText != null) doctorCostText.text = Mathf.RoundToInt(currentDoctor.GetUpgradeCost()) + "$";
                if (doctorUpgradeButton != null) doctorUpgradeButton.interactable = true;
            }
            else
            {
                if (doctorCostText != null) doctorCostText.text = "Tối Đa";
                if (doctorUpgradeButton != null) doctorUpgradeButton.interactable = false;
            }
        }
        else
        {
            // Phòng không có bác sĩ thì ẩn mẹt Doctor Section đi
            if (doctorSection != null) doctorSection.SetActive(false);
        }
    }

    private void OnRoomUpgradeClicked()
    {
        if (currentRoom != null)
        {
            bool success = currentRoom.UpgradeRoom(); // Thực hiện trừ tiền & lên cấp bên Manager
            if (success)
            {
                RefreshUI(); // Update lại giá với level sau khi mua
            }
        }
    }

    private void OnDoctorUpgradeClicked()
    {
        if (currentDoctor != null)
        {
            bool success = currentDoctor.UpgradeDoctor(); // Thực hiện nâng cấp bên Doctor
            if (success)
            {
                RefreshUI(); // Update UI
            }
        }
    }
}
