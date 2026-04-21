using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RoomController : MonoBehaviour
{
    [Header("Configurations")]
    public RoomDataSO roomData;
    public RoomType roomType; 
    public int roomLevel = 1;
    public int roomIncome = 50; 
    public QueueManager roomQueue; 

    [Header("Treatment Configuration")]
    public bool canHealPatient = false; 
    public float celebrationDuration = 2.0f; 
    
    [Header("Nhân viên của phòng")]
    public ReceptionistController receptionist;
    public DoctorController doctor;

    [Header("Runtime State")]
    public int currentPatientsProcessing = 0;

    [Header("Upgradable Stats")]
    public int currentCapacity;
    public float currentProcessTime;

    // Thuộc tính lấy số lượng người trong hàng đợi
    public int CurrentQueueCount => roomQueue != null ? roomQueue.patientsInLine.Count : 0;
    private Queue<CharacterNavigator> waitingPatients = new Queue<CharacterNavigator>();

    void Start()
    {
        // Bước 1: Khởi tạo chỉ số phòng
        UpdateRoomStats(); 
        
        // Bước 2: Đăng ký với Manager sau một khoảng delay nhỏ
        StartCoroutine(RegisterDelay());
    }

    IEnumerator RegisterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        if (HospitalManager.Instance != null)
        {
            HospitalManager.Instance.RegisterRoom(this);
            Debug.Log($"[RoomController] Đã đăng ký phòng: {gameObject.name} ({roomType})");
        }
    }

    // ĐÂY LÀ HÀM QUAN TRỌNG ĐỂ CẬP NHẬT CHỈ SỐ NHANH CHẬM/SỨC CHỨA CỦA PHÒNG
    public void UpdateRoomStats()
    {
        if (roomData != null)
        {
            // Tạm thời sức chứa phụ thuộc vào level của căn phòng
            currentCapacity = roomData.baseCapacity + (roomLevel / 5);
            
            // Tính Lực lượng thực tế để lấy Level nhét vào công thức
            int levelToCalculate = roomLevel; // Mặc định dùng level của phòng (ví dụ cho Phòng Lễ Tân)
            
            // Nếu phòng có bác sĩ và bác sĩ đó có Manager nâng cấp riêng, ưu tiên tính mốc theo sức của Bác Sĩ
            if (doctor != null && doctor.upgradeManager != null)
            {
                levelToCalculate = doctor.upgradeManager.currentLevel;
            }

            // Áp dụng công thức Toán học: baseTime * 0.8^(Level - 1)
            currentProcessTime = roomData.baseProcessTime * Mathf.Pow(0.8f, levelToCalculate - 1);
        }
        else
        {
            // Giá trị mặc định nếu quên kéo ScriptableObject vào
            currentCapacity = 1;
            currentProcessTime = 3f;
        }
    }

    public float GetRoomThroughput() => currentCapacity / currentProcessTime;

    public void ReceivePatient(CharacterNavigator patient)
    {
        waitingPatients.Enqueue(patient);
        TryStartTreating();
    }

    private void TryStartTreating()
    {
        if (waitingPatients.Count > 0 && currentPatientsProcessing < currentCapacity)
        {
            CharacterNavigator patientToTreat = waitingPatients.Dequeue();
            currentPatientsProcessing++;
            StartCoroutine(ProcessRoutine(patientToTreat));
        }
    }

    IEnumerator ProcessRoutine(CharacterNavigator patient)
    {
        // 1. Theo thiết kế mới: Vị trí đầu tiên của hàng đợi (điểm ghim) chính là nơi tương tác làm việc.
        // Chuyển trạng thái bệnh nhân sang Treating luôn.
        patient.currentPhase = CharacterNavigator.PatientPhase.Treating;
        patient.hasArrivedAtDesk = true;

        // 2. Xoay mặt bệnh nhân đối diện với Lễ tân / Bác sĩ cho tự nhiên
        Vector3 lookTarget = transform.position;
        if (receptionist != null) lookTarget = receptionist.transform.position;
        else if (doctor != null) lookTarget = doctor.transform.position;
        
        Vector3 lookDir = lookTarget - patient.transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            patient.transform.rotation = Quaternion.LookRotation(lookDir);

        // 3. Lúc này bệnh nhân đã sẵn sàng, mới bắt đầu bật Animation phục vụ
        if (receptionist != null && !receptionist.isBusy)
        {
            StartCoroutine(receptionist.ServePatient(patient, currentProcessTime));
        }
        else if (doctor != null && !doctor.isBusy)
        {
            StartCoroutine(doctor.ServePatient(patient, currentProcessTime));
        }

        // 4. Bắt đầu đếm ngược thời gian xử lý thủ tục
        yield return new WaitForSeconds(currentProcessTime);
        currentPatientsProcessing--;

        if (HospitalManager.Instance != null)
        {
            HospitalManager.Instance.AddRevenue(roomIncome);
            if (roomType == RoomType.Reception)
            {
                HospitalManager.Instance.AddActivePatient();
                patient.hasPassedReception = true;
            }
            else if (roomType == RoomType.Payment)
            {
                HospitalManager.Instance.RemoveActivePatient();
                patient.hasPassedReception = false;
            }
        }

        if (canHealPatient)
        {
            patient.isHealed = true;
            yield return StartCoroutine(patient.PlayCelebration(celebrationDuration));
        }

        patient.FinishTreatmentAndMoveOn();
        TryStartTreating();
    }

    private void OnDestroy()
    {
        if (HospitalManager.Instance != null) HospitalManager.Instance.UnregisterRoom(this);
    }
}