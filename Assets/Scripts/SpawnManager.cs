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

    [Header("Cấu hình Tốc độ (Thủ công)")]
    [Tooltip("Thời gian đẻ bệnh nhân (đơn vị: Giây). Ví dụ gõ 5 thì cứ 5 giây đẻ 1 người.")]
    public float spawnIntervalInSeconds = 5f;

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
            // 1. Spawn NPC
            SpawnPatient();

            // 2. Nghỉ theo đúng số giây bạn đã nhập trên Inspector
            yield return new WaitForSeconds(spawnIntervalInSeconds);
        }
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