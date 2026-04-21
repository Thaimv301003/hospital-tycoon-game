using UnityEngine;

// Nếu bạn sử dụng Sirenix Odin Inspector (như trong code SpawnManager của bạn)
// thì thêm dòng này, nếu không thì có thể xóa đi.


[CreateAssetMenu(fileName = "HospitalSettings", menuName = "Tycoon/Settings/Hospital Settings")]
public class HospitalSettingsSO : ScriptableObject
{
    [Header("Spawn Configuration")]
    [Tooltip("Hệ số tải mục tiêu. 1.0 là cân bằng, > 1.0 là tạo hàng đợi.")]
    [Range(0.5f, 2.0f)]
    public float targetLoad = 1.2f;

    [Tooltip("Số lượng bệnh nhân lý tưởng trong hàng đợi để game nhìn sôi động.")]
    public int idealQueueSize = 5;

    [Tooltip("Tốc độ điều chỉnh spawn rate khi hàng đợi quá ngắn hoặc quá dài.")]
    public float adaptiveSmoothness = 0.5f;
    
    [Tooltip("Số bệnh nhân mỗi giây tại Level 1.")]
    public float baseSpawnRate = 0.1f;

    [Tooltip("Tỉ lệ tăng trưởng độ khó theo từng Level (1.15 = 15%).")]
    public float levelMultiplier = 1.15f;

    /// <summary>
    /// Tính toán năng lực xử lý cơ bản dựa trên Level của bệnh viện.
    /// </summary>
    public float GetBaseThroughput(int level)
    {
        // Công thức: BaseRate * (Hệ số ^ (Level - 1))
        return baseSpawnRate * Mathf.Pow(levelMultiplier, level - 1);
    }

    [ContextMenu("Log Throughput Table")]
    private void DebugThroughput()
    {
        for (int i = 1; i <= 10; i++)
        {
            Debug.Log($"Level {i}: Throughput = {GetBaseThroughput(i):F2} pts/sec");
        }
    }
}