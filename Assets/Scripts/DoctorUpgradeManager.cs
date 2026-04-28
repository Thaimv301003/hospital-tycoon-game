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

    [Header("Upgrade Effects")]
    [Tooltip("Particle System (hiệu ứng hạt) dựng sẵn trong Scene, nếu có")]
    public ParticleSystem upgradeParticles;
    public AudioSource audioSource;
    public AudioClip upgradeSound;
    
    [Tooltip("Kéo Prefab hiệu ứng (VFX) tải từ ngoài vào đây. Nó sẽ được Instantiate ra khi nâng cấp.")]
    public GameObject upgradeEffectPrefab;
    [Tooltip("Vị trí sinh ra Prefab hiệu ứng ngoài. Nếu để trống, sẽ sinh ra ở ngay gốc tọa độ của bác sĩ.")]
    public Transform effectSpawnPoint;

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

        // Zoom Camera vào bác sĩ / phòng khi nâng cấp thành công
        CameraController camController = CameraController.Instance;
        if (camController == null) camController = FindObjectOfType<CameraController>();

        if (camController != null)
        {
            camController.FocusOnRoom(transform.position);
        }
        else
        {
            Debug.LogError($"[{doctorName}] KHÔNG TÌM THẤY CameraController TRONG SCENE ĐỂ ZOOM!");
        }

        currentLevel++;
        Debug.Log($"[{doctorName}] Đã được nâng lên Level {currentLevel}!");

        // Rất quan trọng: Báo cho căn phòng biết bác sĩ đã giỏi hơn, yêu cầu rút ngắn thời gian khám!
        if (roomController != null)
        {
            roomController.UpdateRoomStats();
        }

        // Chạy hiệu ứng hạt (Particles), âm thanh và VFX Prefab
        PlayUpgradeEffects();

        return true;
    }



    /// <summary>Khởi chạy các hiệu ứng ánh sáng, hạt bụi và âm thanh khi nâng cấp</summary>
    private void PlayUpgradeEffects()
    {
        // 1. Bật Particle System tĩnh có sẵn trong Scene
        if (upgradeParticles != null)
        {
            upgradeParticles.Play();
        }

        // 2. Chơi âm thanh nâng cấp
        if (audioSource != null && upgradeSound != null)
        {
            audioSource.PlayOneShot(upgradeSound);
        }

        // 3. Sinh ra Prefab hiệu ứng động (nếu có kéo vào inspector)
        if (upgradeEffectPrefab != null)
        {
            Vector3 spawnPos = effectSpawnPoint != null ? effectSpawnPoint.position : transform.position;
            GameObject vfx = Instantiate(upgradeEffectPrefab, spawnPos, Quaternion.identity);
            
            // Xóa hiệu ứng sau 3 giây để tránh rác RAM
            Destroy(vfx, 3f);
        }
    }
}
