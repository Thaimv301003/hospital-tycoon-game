using UnityEngine;
using System.Collections; // Dòng này cực kỳ quan trọng để sửa lỗi CS0246
using System.Collections.Generic; // Để dùng List nếu cần

public class SpawnManager : MonoBehaviour
{
    [Header("Vehicle Settings")]
    public GameObject carPrefab;
    [Tooltip("Thời gian giữa mỗi lần xuất hiện xe mới (Giây)")]
    public float carSpawnInterval = 10f;
    public Transform carSpawnPoint;
    public Transform[] roadWaypoints;
    public int dropOffWaypointIndex = 1;
    public int pickupWaypointIndex = 3;

    [Header("Patient Data (Truyền cho xe)")]
    public GameObject patientPrefab;
    public Transform exitPoint;
    public WaypointNode finalExitNode;

    void Start()
    {
        if (carPrefab == null || carSpawnPoint == null)
        {
            Debug.LogError("SpawnManager: Bạn chưa kéo CarPrefab hoặc CarSpawnPoint!");
            return;
        }

        StartCoroutine(VehicleSpawnRoutine());
    }

    IEnumerator VehicleSpawnRoutine()
    {
        // Đợi một chút lúc bắt đầu
        yield return new WaitForSeconds(1f);

        while (true)
        {
            SpawnVehicle();
            yield return new WaitForSeconds(carSpawnInterval);
        }
    }

    void SpawnVehicle()
    {
        GameObject newCar = Instantiate(carPrefab, carSpawnPoint.position, carSpawnPoint.rotation);
        
        VehicleNavigator nav = newCar.GetComponent<VehicleNavigator>();
        if (nav != null)
        {
            // Truyền dữ liệu lộ trình cho xe
            nav.waypoints = roadWaypoints;
            nav.dropOffIndex = dropOffWaypointIndex;
            nav.pickupIndex = pickupWaypointIndex;
            
            // Truyền dữ liệu bệnh nhân để xe đẻ ra đúng loại
            nav.patientPrefab = patientPrefab;
            nav.patientExitPoint = exitPoint;
            nav.patientFinalExitNode = finalExitNode;
        }
        else
        {
            Debug.LogError("SpawnManager: CarPrefab không có component VehicleNavigator!");
        }
    }
}