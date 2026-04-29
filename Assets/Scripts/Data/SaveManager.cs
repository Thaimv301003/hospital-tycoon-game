using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private string savePath;
    private const string SAVE_FILE_NAME = "hospital_save.json";

    // Giới hạn thu nhập ngoại tuyến tối đa (giây) - ví dụ 4 tiếng
    private const double MAX_OFFLINE_SECONDS = 14400; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) SaveGame();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    // ===================================================================
    // PUBLIC API
    // ===================================================================

    public void SaveGame()
    {
        if (HospitalManager.Instance == null) return;

        HospitalSaveData data = new HospitalSaveData();
        data.totalRevenue = HospitalManager.Instance.totalRevenue;
        data.hospitalLevel = HospitalManager.Instance.currentLevel;
        data.lastSaveTime = DateTime.Now.ToBinary().ToString();

        // Thu thập dữ liệu từng phòng
        RoomController[] rooms = FindObjectsByType<RoomController>(FindObjectsSortMode.None);
        foreach (var room in rooms)
        {
            if (string.IsNullOrEmpty(room.roomID))
            {
                Debug.LogWarning($"[SaveManager] Phòng {room.name} chưa có ID, sẽ không được lưu!");
                continue;
            }

            RoomSaveData rData = new RoomSaveData();
            rData.roomID = room.roomID;
            
            // 1. Lưu Level Phòng
            RoomUpgradeManager roomUpgrade = room.GetComponent<RoomUpgradeManager>();
            rData.roomLevel = (roomUpgrade != null) ? roomUpgrade.currentLevel : room.roomLevel;

            // 2. Lưu Level Bác sĩ
            if (room.doctor != null && room.doctor.upgradeManager != null)
            {
                rData.doctorLevel = room.doctor.upgradeManager.currentLevel;
            }

            // 3. Lưu Level Lễ tân/Thu ngân
            ReceptionistUpgradeManager staffUpgrade = room.GetComponent<ReceptionistUpgradeManager>();
            if (staffUpgrade != null)
            {
                rData.staffLevel = staffUpgrade.currentLevel;
            }

            data.rooms.Add(rData);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"[SaveManager] Đã lưu game (bao gồm nhân viên) vào: {savePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("[SaveManager] Không tìm thấy file save.");
            return;
        }

        string json = File.ReadAllText(savePath);
        HospitalSaveData data = JsonUtility.FromJson<HospitalSaveData>(json);

        // 1. Khôi phục dữ liệu bệnh viện
        if (HospitalManager.Instance != null)
        {
            HospitalManager.Instance.totalRevenue = data.totalRevenue;
            HospitalManager.Instance.currentLevel = data.hospitalLevel;
            
            // 2. Tính toán tiền ngoại tuyến
            CalculateOfflineEarnings(data.lastSaveTime);
        }

        // 3. Khôi phục dữ liệu từng phòng và nhân viên
        RoomController[] sceneRooms = FindObjectsByType<RoomController>(FindObjectsSortMode.None);
        foreach (var rData in data.rooms)
        {
            foreach (var room in sceneRooms)
            {
                if (room.roomID == rData.roomID)
                {
                    // A. Nạp Level Phòng
                    room.roomLevel = rData.roomLevel;
                    RoomUpgradeManager roomUpgrade = room.GetComponent<RoomUpgradeManager>();
                    if (roomUpgrade != null)
                    {
                        roomUpgrade.currentLevel = rData.roomLevel;
                        // Cập nhật ngoại hình phòng (không chạy hiệu ứng)
                        roomUpgrade.Invoke("UpdateVisualsFalse", 0.1f); 
                    }

                    // B. Nạp Level Bác sĩ
                    if (room.doctor != null && room.doctor.upgradeManager != null)
                    {
                        room.doctor.upgradeManager.currentLevel = rData.doctorLevel;
                    }

                    // C. Nạp Level Lễ tân
                    ReceptionistUpgradeManager staffUpgrade = room.GetComponent<ReceptionistUpgradeManager>();
                    if (staffUpgrade != null)
                    {
                        staffUpgrade.currentLevel = rData.staffLevel;
                        // Gọi hàm đồng bộ thời gian phục vụ
                        staffUpgrade.Invoke("SyncProcessTimePublic", 0.1f);
                    }

                    room.UpdateRoomStats(); // Cập nhật lại chỉ số tổng thể của phòng
                    break;
                }
            }
        }

        Debug.Log("[SaveManager] Đã nạp dữ liệu game thành công.");
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
        {
            File.Delete(savePath);
            Debug.Log("[SaveManager] Đã xóa file save.");
        }
    }

    // ===================================================================
    // PRIVATE HELPERS
    // ===================================================================

    private void CalculateOfflineEarnings(string lastTimeStr)
    {
        if (string.IsNullOrEmpty(lastTimeStr)) return;

        try
        {
            long binaryTime = long.Parse(lastTimeStr);
            DateTime lastTime = DateTime.FromBinary(binaryTime);
            TimeSpan timePassed = DateTime.Now - lastTime;

            double totalSeconds = timePassed.TotalSeconds;
            if (totalSeconds > MAX_OFFLINE_SECONDS) totalSeconds = MAX_OFFLINE_SECONDS;

            if (totalSeconds > 10) // Chỉ tính nếu vắng mặt trên 10 giây
            {
                float throughput = HospitalManager.Instance.GetEffectiveThroughput();
                int earnings = Mathf.RoundToInt((float)totalSeconds * throughput * 10); // Giả sử mỗi lần phục vụ thu được trung bình 10$ (có thể điều chỉnh)
                
                HospitalManager.Instance.AddRevenue(earnings);
                Debug.Log($"[Offline] Bạn đã vắng mặt {(int)totalSeconds} giây và kiếm được ${earnings}!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Lỗi khi tính tiền ngoại tuyến: {e.Message}");
        }
    }
}
