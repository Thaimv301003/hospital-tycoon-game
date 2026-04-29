using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class VehicleNavigator : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float stopDistance = 0.1f;

    [Header("Path Data")]
    public Transform[] waypoints;
    public int dropOffIndex = 0;
    
    [Header("Patient Spawn Info")]
    public GameObject patientPrefab;
    public Transform patientExitPoint;
    public WaypointNode patientFinalExitNode;

    [Header("Pickup Settings")]
    public int pickupIndex = -1; // Điểm đón khách (thường là điểm cuối hoặc gần cuối)
    public float pickupRadius = 2f;
    public float pickupDuration = 1.5f;

    private int currentWaypointIndex = 0;
    private bool isWaiting = false;
    private bool hasDroppedOff = false;
    private bool hasPickedUp = false;

    void Update()
    {
        if (isWaiting || waypoints == null || waypoints.Length == 0) return;

        MoveTowardsWaypoint();
    }

    void MoveTowardsWaypoint()
    {
        if (currentWaypointIndex >= waypoints.Length)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = waypoints[currentWaypointIndex].position;
        targetPos.y = transform.position.y;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        Vector3 direction = targetPos - transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPos) < stopDistance)
        {
            // Kiểm tra thả khách
            if (currentWaypointIndex == dropOffIndex && !hasDroppedOff)
            {
                StartCoroutine(DropOffRoutine());
            }
            // Kiểm tra đón khách
            else if (currentWaypointIndex == pickupIndex && !hasPickedUp)
            {
                StartCoroutine(PickUpRoutine());
            }
            else
            {
                currentWaypointIndex++;
            }
        }
    }

    IEnumerator DropOffRoutine()
    {
        isWaiting = true;
        yield return new WaitForSeconds(1f);
        SpawnPatient();
        hasDroppedOff = true;
        yield return new WaitForSeconds(0.5f);
        isWaiting = false;
        currentWaypointIndex++;
    }

    IEnumerator PickUpRoutine()
    {
        isWaiting = true;
        Debug.Log("[Vehicle] Đang kiểm tra khách đợi về...");

        // Tìm tất cả bệnh nhân trong bán kính pickupRadius
        Collider[] colliders = Physics.OverlapSphere(transform.position, pickupRadius);
        List<CharacterNavigator> patientsToPick = new List<CharacterNavigator>();

        foreach (var col in colliders)
        {
            CharacterNavigator p = col.GetComponent<CharacterNavigator>();
            if (p != null && p.currentPhase == CharacterNavigator.PatientPhase.Leaving)
            {
                patientsToPick.Add(p);
            }
        }

        if (patientsToPick.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
            foreach (var p in patientsToPick)
            {
                // Hiệu ứng "biến vào xe"
                p.gameObject.SetActive(false); // Hoặc Destroy(p.gameObject)
                Debug.Log($"[Vehicle] Đã đón {p.name} về.");
            }
            yield return new WaitForSeconds(pickupDuration);
        }

        hasPickedUp = true;
        isWaiting = false;
        currentWaypointIndex++;
    }

    void SpawnPatient()
    {
        if (patientPrefab == null) return;

        // Sinh bệnh nhân tại vị trí của xe (có thể lệch sang bên cửa xe một chút nếu muốn)
        Vector3 spawnPos = transform.position;
        GameObject newPatient = Instantiate(patientPrefab, spawnPos, Quaternion.identity);

        // Thiết lập các thông số cho bệnh nhân (để họ đi theo Waypoint cũ của bạn)
        CharacterNavigator nav = newPatient.GetComponent<CharacterNavigator>();
        if (nav != null)
        {
            nav.exitPoint = patientExitPoint;
            nav.finalExitNode = patientFinalExitNode;
            
            // Bệnh nhân sẽ tự động tìm Waypoint gần nhất từ vị trí xe và bắt đầu lộ trình
            Debug.Log($"[Vehicle] Đã thả bệnh nhân: {newPatient.name} tại điểm dừng.");
        }
    }
}
