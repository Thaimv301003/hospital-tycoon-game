using UnityEngine;

public enum RoomType
{
    Reception,      // Lễ tân
    Diagnosis,      // Khám bệnh (Tổng quát)
    NormalClinic,   // Phòng khám thường
    XRay,           // Chụp XQuang
    Surgery,        // Phẫu thuật
    Pharmacy,       // Quầy thuốc
    Payment         // Đóng viện phí
}

[CreateAssetMenu(fileName = "NewRoomData", menuName = "Tycoon/Data/Room Data")]
public class RoomDataSO : ScriptableObject
{
    [Header("Room Setup")]
    public string roomName = "New Room";
    
    [Header("Base Stats")]
    [Tooltip("Số người tối đa phòng có thể chứa cùng lúc")]
    public int baseCapacity = 1;
    
    [Tooltip("Thời gian khám/xử lý cho mỗi người (giây)")]
    public float baseProcessTime = 3f;
}
