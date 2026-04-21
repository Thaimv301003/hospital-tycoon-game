using UnityEngine;
using System.Collections; // Dòng này cực kỳ quan trọng để sửa lỗi CS0246
using System.Collections.Generic; // Để dùng List nếu cần

public class SpawnManager : MonoBehaviour
{
    [Header("Data & Prefabs")]
    public HospitalSettingsSO settings;
    public GameObject patientPrefab;
    public Transform spawnPoint;
    public Transform exitPoint; // Điểm đi về dành cho các NPC

    [Header("Routing Customization")]
    [Tooltip("Kéo thả Node Waypoint cuối cùng mà NPC bắt buộc phải đi tới trước khi rẽ ra cổng (Exit Point)")]
    public WaypointNode finalExitNode; 

    [Header("Status (Read Only)")]
    [SerializeField] private float currentInterval = 3f;

    void Start()
    {
        // Kiểm tra an toàn trước khi chạy
        if (settings == null)
        {
            Debug.LogError("SpawnManager: Bạn chưa kéo HospitalSettingsSO vào ô Settings!");
            return;
        }

        // Bắt đầu vòng lặp đẻ NPC
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        // Đợi 0.5 giây để đảm bảo HospitalManager đã Awake xong
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            // 1. Cập nhật tốc độ đẻ dựa trên tình hình bệnh viện
            UpdateSpawnInterval();

            // 2. Spawn NPC trực tiếp không cần kiểm tra Waypoint
            SpawnPatient();

            // 3. Nghỉ một khoảng thời gian trước khi đẻ người tiếp theo
            yield return new WaitForSeconds(currentInterval);
        }
    }

    void UpdateSpawnInterval()
    {
        // Nếu không tìm thấy Manager, dùng tốc độ mặc định trong settings
        if (HospitalManager.Instance == null)
        {
            currentInterval = 1f / settings.baseSpawnRate;
            return;
        }

        // Lấy năng lực xử lý (Throughput)
        float effectiveTP = HospitalManager.Instance.GetEffectiveThroughput();

        // Lấy tổng số người đang đợi
        int currentQueue = HospitalManager.Instance.GetTotalCurrentQueue();

        // Thuật toán điều chỉnh (Adaptive Bias)
        float queueBias = 1.0f;
        if (currentQueue < settings.idealQueueSize)
            queueBias = 1.5f; // Đẻ nhanh hơn nếu hàng đợi quá vắng
        else if (currentQueue > settings.idealQueueSize * 2)
            queueBias = 0.5f; // Đẻ chậm lại nếu hàng đợi quá tải

        // Công thức: Rate = Năng lực * Tải mục tiêu * Bias
        float spawnRatePerSecond = effectiveTP * settings.targetLoad * queueBias;

        // Đảm bảo không bị chia cho 0
        spawnRatePerSecond = Mathf.Max(spawnRatePerSecond, 0.05f);
        currentInterval = 1f / spawnRatePerSecond;
    }

    void SpawnPatient()
    {
        if (patientPrefab == null || spawnPoint == null) return;

        GameObject newPatient = Instantiate(patientPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // NPC sẽ tự tìm Waypoint gần nhất và di chuyển theo lộ trình đồ thị
        CharacterNavigator nav = newPatient.GetComponent<CharacterNavigator>();
        if (nav != null)
        {
            nav.exitPoint = exitPoint;
            
            // --- TRUYỀN NODE KẾT THÚC CHO NPC ---
            nav.finalExitNode = finalExitNode; 
        }
    }
}