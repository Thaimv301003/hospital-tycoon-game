using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomSaveData
{
    public string roomID;
    public int roomLevel;
    public int doctorLevel; // Cấp độ bác sĩ
    public int staffLevel;  // Cấp độ lễ tân/thu ngân
}

[System.Serializable]
public class HospitalSaveData
{
    public int totalRevenue;
    public int hospitalLevel;
    public string lastSaveTime; // Lưu dạng string từ DateTime.ToBinary().ToString()
    public List<RoomSaveData> rooms = new List<RoomSaveData>();
}
