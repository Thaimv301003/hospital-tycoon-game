using UnityEngine;

/// <summary>
/// Gắn script này lên phòng LỄ TÂN / THU NGÂN — những phòng không có cơ chế nâng cấp phòng
/// mà chỉ nâng cấp NHÂN VIÊN (giảm thời gian phục vụ mỗi bệnh nhân).
///
/// CÁCH SETUP:
/// 1. Thêm component này vào GameObject phòng (cùng chỗ với RoomController, v.v.)
/// 2. Kéo ReceptionistController vào slot "receptionistController"
/// 3. Kéo prefab FloatingReceptionistMenu vào "uiPrefab"
/// 4. Kéo Canvas chính vào "mainCanvasTransform"
/// 5. Tạo 1 Empty GameObject làm điểm neo UI, kéo vào "uiSpawnPosition"
/// </summary>
public class ReceptionistUpgradeManager : MonoBehaviour
{
    [Header("Thông Tin Nhân Viên")]
    [Tooltip("Tên hiển thị trong UI (vd: 'Lễ Tân', 'Thu Ngân')")]
    public string staffName = "Lễ Tân";
    public int currentLevel = 1;
    public int maxLevel = 5;

    [Header("Economy Settings")]
    [Tooltip("Chi phí nâng cấp cơ bản (cấp 1 → 2)")]
    public float baseUpgradeCost = 120f;
    [Tooltip("Hệ số nhân chi phí mỗi cấp (1.4 = tăng 40% mỗi level)")]
    public float costMultiplier = 1.4f;

    [Header("Staff Performance")]
    [Tooltip("Thời gian phục vụ cơ bản ở Cấp 1 (giây). Mỗi cấp giảm 15%.")]
    public float baseProcessTime = 3f;
    [Tooltip("Hệ số giảm thời gian mỗi cấp (0.85 = giảm 15%)")]
    public float timeDecayFactor = 0.85f;

    [Header("UI Overlay Settings")]
    [Tooltip("Prefab FloatingReceptionistMenu (prefab riêng cho lễ tân/thu ngân)")]
    public GameObject uiPrefab;
    [Tooltip("Canvas chính của Scene (Screen Space Overlay)")]
    public Transform mainCanvasTransform;
    [Tooltip("Empty GameObject đặt phía trên đầu phòng, làm điểm neo UI")]
    public Transform uiSpawnPosition;

    [Header("References")]
    [Tooltip("Kéo ReceptionistController của phòng này vào đây")]
    public ReceptionistController receptionistController;

    // ===================================================================
    // INTERNAL STATE
    // ===================================================================
    private GameObject uiInstance;
    private RectTransform uiRectTransform;

    // ===================================================================
    // UNITY LIFECYCLE
    // ===================================================================

    private void Start()
    {
        // Đồng bộ processTime của controller với level hiện tại (khi load game)
        SyncProcessTime();

        // Spawn UI lơ lửng
        InitOverlayUI();
    }

    private void Update()
    {
        // Theo dõi vị trí UI trên màn hình theo phòng
        if (uiInstance != null && uiSpawnPosition != null && Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(uiSpawnPosition.position);

            if (screenPos.z < 0)
            {
                uiInstance.SetActive(false);
                return;
            }

            uiInstance.SetActive(true);

            RectTransform canvasRect = mainCanvasTransform as RectTransform;
            if (canvasRect != null)
            {
                Canvas canvas = mainCanvasTransform.GetComponent<Canvas>();
                Camera uiCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    ? canvas.worldCamera
                    : null;

                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, screenPos, uiCamera, out localPos);

                uiRectTransform.localPosition = localPos;
            }
            else
            {
                uiRectTransform.position = screenPos;
            }
        }
    }

    // ===================================================================
    // INIT UI
    // ===================================================================

    private void InitOverlayUI()
    {
        if (uiPrefab == null || mainCanvasTransform == null)
        {
            Debug.LogWarning($"[{staffName}] Chưa gán uiPrefab hoặc mainCanvasTransform — bỏ qua spawn UI.");
            return;
        }

        uiInstance = Instantiate(uiPrefab, mainCanvasTransform);
        uiInstance.name = "UI_Receptionist_" + staffName;

        uiRectTransform = uiInstance.GetComponent<RectTransform>();
        if (uiRectTransform == null)
            uiRectTransform = uiInstance.GetComponentInChildren<RectTransform>(true);

        // Kết nối với FloatingReceptionistMenu
        FloatingReceptionistMenu menu = uiInstance.GetComponent<FloatingReceptionistMenu>();
        if (menu != null)
        {
            menu.Initialize(this);
            Debug.Log($"[{staffName}] Đã khởi tạo FloatingReceptionistMenu thành công.");
        }
        else
        {
            Debug.LogError($"[{staffName}] Không tìm thấy FloatingReceptionistMenu trên prefab UI!");
        }
    }

    // ===================================================================
    // PUBLIC API — ĐƯỢC GỌI BỞI FloatingReceptionistMenu
    // ===================================================================

    /// <summary>Chi phí để nâng cấp lên cấp tiếp theo.</summary>
    public float GetUpgradeCost()
    {
        return baseUpgradeCost * Mathf.Pow(costMultiplier, currentLevel - 1);
    }

    /// <summary>Thời gian phục vụ hiện tại (giây).</summary>
    public float GetCurrentProcessTime()
    {
        return baseProcessTime * Mathf.Pow(timeDecayFactor, currentLevel - 1);
    }

    /// <summary>Thời gian phục vụ SAU KHI nâng 1 cấp (dùng để preview A → B).</summary>
    public float GetUpgradedProcessTime()
    {
        return baseProcessTime * Mathf.Pow(timeDecayFactor, currentLevel); // level+1-1 = level
    }

    /// <summary>
    /// Thực hiện nâng cấp nhân viên: trừ tiền, tăng level, cập nhật processTime.
    /// Trả về true nếu thành công.
    /// </summary>
    public bool UpgradeStaff()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[{staffName}] Đã đạt cấp tối đa!");
            return false;
        }

        int cost = Mathf.RoundToInt(GetUpgradeCost());

        if (HospitalManager.Instance != null)
        {
            if (!HospitalManager.Instance.HasEnoughMoney(cost))
            {
                Debug.Log($"[{staffName}] Không đủ tiền! Cần {cost}$ nhưng chỉ có {HospitalManager.Instance.totalRevenue}$.");
                return false;
            }
            HospitalManager.Instance.SpendMoney(cost);
        }

        currentLevel++;
        Debug.Log($"[{staffName}] Đã nâng cấp lên Level {currentLevel}! ProcessTime mới: {GetCurrentProcessTime():F2}s");

        // Cập nhật processTime của ReceptionistController
        SyncProcessTime();

        return true;
    }

    // ===================================================================
    // INTERNAL HELPERS
    // ===================================================================

    /// <summary>Đồng bộ processTime của ReceptionistController theo level hiện tại.</summary>
    private void SyncProcessTime()
    {
        if (receptionistController != null)
            receptionistController.processTime = GetCurrentProcessTime();
    }
}
