using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Cần thiết cho List<>
using TMPro;

public class HospitalManager : MonoBehaviour
{
    public static HospitalManager Instance;

    [Header("Settings")]
    public HospitalSettingsSO settings;
    public int currentLevel = 1;
    [Tooltip("Số người tối đa đang đợi ở một phòng. Nếu đông hơn, bệnh nhân sẽ chán và bỏ về")]
    public int maxQueueTolerance = 5;

    [Header("Runtime Data")]
    [SerializeField] private List<RoomController> activeRooms = new List<RoomController>();
    
    [Header("Economy & Statistics")]
    public int currentActivePatients = 0;
    
    [Tooltip("Tiền mặt hiện tại của bệnh viện")]
    public int totalRevenue = 500; // Cấp số vốn ban đầu là 500đ để có tiền Test game
    
    
    [Header("UI References")]
    public TMP_Text moneyText;
    public TMP_Text patientsText;

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            if (moneyText == null)
            {
                // Tự động tìm kiếm UI thay vì bắt user kéo thả
                GameObject uiObj = GameObject.Find("Moneytext");
                if (uiObj != null) moneyText = uiObj.GetComponent<TMP_Text>();
            }
            if (patientsText == null)
            {
                GameObject patientObj = GameObject.Find("Patientstext");
                if (patientObj != null) patientsText = patientObj.GetComponent<TMP_Text>();
            }
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateMoneyUI();
        UpdatePatientsUI();
    }

    // Hàm để các phòng tự thêm mình vào danh sách quản lý
    public void RegisterRoom(RoomController room)
    {
        if (!activeRooms.Contains(room)) activeRooms.Add(room);
    }

    public void UnregisterRoom(RoomController room)
    {
        if (activeRooms.Contains(room)) activeRooms.Remove(room);
    }

    public void AddActivePatient()
    {
        currentActivePatients++;
        Debug.Log($"[HospitalManager] Bệnh nhân nhập viện. Tổng bệnh nhân hiện tại: {currentActivePatients}");
        UpdatePatientsUI();
    }

    public void RemoveActivePatient()
    {
        currentActivePatients--;
        if (currentActivePatients < 0) currentActivePatients = 0;
        Debug.Log($"[HospitalManager] Bệnh nhân đã thanh toán/xuất viện. Tổng bệnh nhân hiện tại: {currentActivePatients}");
        UpdatePatientsUI();
    }

    private void UpdatePatientsUI()
    {
        if (patientsText != null)
        {
            patientsText.text = $"Patients: {currentActivePatients}";
            // Chạy hiệu ứng mượt
            StartCoroutine(BounceTextEffect(patientsText));
        }
        else 
        {
            Debug.LogWarning("[HospitalManager] KHÔNG THẤY GIAO DIỆN TEXT SỐ BỆNH NHÂN. Hãy kéo Patientstext vào ô Patients Text nhé!");
        }
    }

    public void AddRevenue(int amount)
    {
        totalRevenue += amount;
        Debug.Log($"[HospitalManager] Ghi nhận doanh thu: +{amount}. Tổng tiền mặt: {totalRevenue}");
        UpdateMoneyUI();
    }

    // Kiểm tra xem số dư có đủ để mua/nâng cấp không
    public bool HasEnoughMoney(int amount)
    {
        return totalRevenue >= amount;
    }

    // Hàm trừ tiền khi thực hiện nâng cấp
    public void SpendMoney(int amount)
    {
        totalRevenue -= amount;
        Debug.Log($"[HospitalManager] Đã chi tiêu: -{amount}. Số dư còn: {totalRevenue}");
        UpdateMoneyUI();
    }

    // CÔNG CỤ HỖ TRỢ TEST NHANH TRÊN UNITY (Click chuột phải vào script Hospital Manager -> Chọn "Cheat: Add 1000 Đô")
    [ContextMenu("Cheat: Add 1000 Đô")]
    public void CheatMoney()
    {
        totalRevenue += 1000;
        UpdateMoneyUI();
        Debug.Log("Ăn gian thành công! +1000 Đô");
    }

    private void UpdateMoneyUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"${totalRevenue}";
            StartCoroutine(BounceTextEffect(moneyText));
        }
        else 
        {
            Debug.LogWarning("[HospitalManager] KHÔNG THẤY GIAO DIỆN TEXT. Hãy kéo Moneytext vào ô UI References trong HospitalManager nhé!");
        }
    }

    IEnumerator BounceTextEffect(TMP_Text targetText)
    {
        Transform textT = targetText.transform;
        // Phóng to nhẹ
        textT.localScale = Vector3.one * 1.3f; 
        float timer = 0;
        while (timer < 0.2f)
        {
            timer += Time.deltaTime;
            textT.localScale = Vector3.Lerp(Vector3.one * 1.3f, Vector3.one, timer / 0.2f); // Thu nhỏ mượt dần về 1
            yield return null;
        }
        textT.localScale = Vector3.one;
    }

    public float GetEffectiveThroughput()
    {
        // Nếu chưa có phòng nào, lấy throughput cơ bản từ level
        if (activeRooms.Count == 0) return settings.GetBaseThroughput(currentLevel);

        float minThroughput = float.MaxValue;
        foreach (var room in activeRooms)
        {
            float tp = room.GetRoomThroughput();
            // Bottleneck: Tìm phòng có throughput thấp nhất
            if (tp < minThroughput) minThroughput = tp;
        }
        return minThroughput;
    }

    public int GetTotalCurrentQueue()
    {
        int total = 0;
        foreach (var room in activeRooms)
        {
            total += room.CurrentQueueCount;
        }
        return total;
    }

    // TÌM PHÒNG CÙNG LOẠI VẮNG KHÁCH NHẤT
    public RoomController GetBestAvailableRoom(RoomType type)
    {
        RoomController bestRoom = null;
        int minQueue = int.MaxValue;

        // Auto-recovery: Nếu hệ thống chưa kịp đăng ký phòng (Race condition, bug Unity) thì tự động lục soát
        if (activeRooms.Count == 0)
        {
            activeRooms.AddRange(FindObjectsByType<RoomController>(FindObjectsSortMode.None));
        }

        foreach (var room in activeRooms)
        {
            // Bảo vệ lỗi NullReference nếu người chơi vô tình xoá object khi game đang chạy
            if (room == null) continue;

            if (room.roomType == type)
            {
                if (room.CurrentQueueCount < minQueue)
                {
                    minQueue = room.CurrentQueueCount;
                    bestRoom = room;
                }
            }
        }

        if (bestRoom == null)
        {
            Debug.LogError($"[HospitalManager] ĐÃ QUÉT TOÀN BỘ SCENE nhưng vẫn KHÔNG TÌM THẤY phòng loại {type}!");
            Debug.Log($"[HospitalManager] Danh sách các phòng thực tế đang tồn tại trong Scene: (Tổng {activeRooms.Count})");
            foreach (var r in activeRooms)
            {
                if (r != null) Debug.Log($" + Tên Object: {r.gameObject.name} | Đang cài RoomType là: {r.roomType}");
            }
        }
        
        return bestRoom; // Có thể null nếu chưa xây phòng loại này
    }
}