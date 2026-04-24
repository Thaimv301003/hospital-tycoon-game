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

    [Header("Điểm Khám/Phục Vụ")]
    [Tooltip("Vị trí bệnh nhân sẽ đứng để được khám (ngoài hàng đợi)")]
    public Transform servicePoint;

    [Header("Vệ Sinh Phòng (Cleaning)")]
    public bool canGetDirty = true;
    public bool isDirty = false;
    [Tooltip("Số lượng bệnh nhân khám tối đa trước khi phòng bị bẩn")]
    public int treatmentsBeforeDirty = 1;
    private int treatmentsSinceClean = 0;
    
    [Header("UI Dọn Dẹp (Giống UI Nâng Cấp)")]
    [Tooltip("Kéo Prefab nút Dọn Dẹp (nút cái chổi) vào đây")]
    public GameObject cleanUIPrefab;
    [Tooltip("Kéo Canvas tổng (chứa UI) vào đây")]
    public Transform mainCanvasTransform;
    [Tooltip("Vị trí lơ lửng của nút Dọn dẹp (có thể dùng chung UISpawn của UI nâng cấp)")]
    public Transform cleanUISpawnPosition;
    
    private GameObject cleanUIInstance;
    private RectTransform cleanUIRect;
    private Camera mainCamera;

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
        mainCamera = Camera.main;
        
        // Sinh ra nút Dọn dẹp từ Prefab vào Canvas Tổng (Nhưng ẩn đi)
        if (cleanUIPrefab != null && mainCanvasTransform != null)
        {
            cleanUIInstance = Instantiate(cleanUIPrefab, mainCanvasTransform);
            cleanUIInstance.name = "UI_Clean_" + gameObject.name;
            cleanUIRect = cleanUIInstance.GetComponent<RectTransform>();
            
            // Tìm nút bấm (Button) trong prefab và gắn sự kiện
            UnityEngine.UI.Button btn = cleanUIInstance.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null) btn.onClick.AddListener(CleanRoom);
            
            cleanUIInstance.SetActive(false); // Ẩn lúc mới vào game
        }
        
        if (roomType == RoomType.Reception || roomType == RoomType.Payment)
        {
            canGetDirty = false;
        }

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

            // Tăng số lượng bệnh nhân khám được trước khi phòng bị bẩn dựa theo level CỦA PHÒNG
            // Level 1: 1 người, Level 2: 3 người, Level 3: 5 người...
            treatmentsBeforeDirty = 1 + (roomLevel - 1) * 2;
        }
        else
        {
            // Giá trị mặc định nếu quên kéo ScriptableObject vào
            currentCapacity = 1;
            currentProcessTime = 3f;
        }
    }

    public float GetRoomThroughput() => currentCapacity / currentProcessTime;

    private void Update()
    {
        // Cập nhật vị trí nút Dọn dẹp chạy theo camera giống hệt UI nâng cấp
        if (cleanUIInstance != null && cleanUIInstance.activeSelf && cleanUIRect != null && cleanUISpawnPosition != null && mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(cleanUISpawnPosition.position);
            
            // Tránh việc UI nhảy ra sau lưng camera
            if (screenPos.z > 0)
            {
                RectTransform canvasRect = mainCanvasTransform as RectTransform;
                if (canvasRect != null)
                {
                    Vector2 localPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out localPos);
                    cleanUIRect.localPosition = localPos;
                }
                else
                {
                    cleanUIRect.position = screenPos;
                }
            }
            else
            {
                cleanUIRect.position = new Vector3(-9999, -9999, 0); // Giấu UI đi nếu quay lưng lại
            }
        }
    }

    public void ReceivePatient(CharacterNavigator patient)
    {
        waitingPatients.Enqueue(patient);
        TryStartTreating();
    }

    private void TryStartTreating()
    {
        // NẾU PHÒNG ĐANG BẨN -> CHẶN KHÔNG CHO KHÁM, BỆNH NHÂN ĐỨNG ĐỢI
        if (isDirty) return;

        if (waitingPatients.Count > 0 && currentPatientsProcessing < currentCapacity)
        {
            CharacterNavigator patientToTreat = waitingPatients.Dequeue();
            currentPatientsProcessing++;
            StartCoroutine(ProcessRoutine(patientToTreat));
        }
    }

    IEnumerator ProcessRoutine(CharacterNavigator patient)
    {
        // 1. Yêu cầu bệnh nhân đi tới Service Point nếu có gán
        if (servicePoint != null)
        {
            patient.WalkToDesk(servicePoint.position);
            // Đợi cho đến khi bệnh nhân thực sự đi tới nơi
            yield return new WaitUntil(() => patient.hasArrivedAtDesk);
        }
        else
        {
            // Nếu không có ServicePoint, bệnh nhân đứng tại đầu hàng khám luôn (Cơ chế cũ)
            patient.hasArrivedAtDesk = true;
        }

        // 2. Chuyển trạng thái sang Treating và xoay mặt
        patient.currentPhase = CharacterNavigator.PatientPhase.Treating;

        Vector3 lookTarget = transform.position;
        if (receptionist != null) lookTarget = receptionist.transform.position;
        else if (doctor != null) lookTarget = doctor.transform.position;
        
        Vector3 lookDir = lookTarget - patient.transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.001f)
            patient.transform.rotation = Quaternion.LookRotation(lookDir);

        // 3. Bắt đầu phục vụ (Animation nhân viên)
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
        
        // --- CƠ CHẾ DỌN DẸP ---
        if (canGetDirty)
        {
            treatmentsSinceClean++;
            if (treatmentsSinceClean >= treatmentsBeforeDirty)
            {
                isDirty = true;
                if (cleanUIInstance != null) cleanUIInstance.SetActive(true);
                Debug.Log($"[RoomController] Phòng {gameObject.name} đã bị bẩn! Đang chờ dọn dẹp.");
            }
            else
            {
                TryStartTreating();
            }
        }
        else
        {
            TryStartTreating();
        }
    }

    /// <summary>
    /// Hàm này được gọi khi Player bấm vào nút Dọn Dẹp trên UI
    /// </summary>
    public void CleanRoom()
    {
        isDirty = false;
        treatmentsSinceClean = 0;
        if (cleanUIInstance != null) cleanUIInstance.SetActive(false);
        Debug.Log($"[RoomController] Phòng {gameObject.name} đã sạch sẽ.");
        
        // Tiếp tục gọi bệnh nhân tiếp theo vào khám ngay lập tức
        TryStartTreating();
    }

    private void OnDestroy()
    {
        if (HospitalManager.Instance != null) HospitalManager.Instance.UnregisterRoom(this);
        // Nhớ xóa cục UI khi phòng bị xóa để tránh rác
        if (cleanUIInstance != null) Destroy(cleanUIInstance);
    }
}