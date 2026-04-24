using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CharacterNavigator : MonoBehaviour
{
    public enum PatientState { Light, Heavy }
    public enum PatientPhase { FindingRoom, GoingToQueue, WaitingInQueue, WalkingToDesk, Treating, Leaving }

    [Header("Bệnh án")]
    public PatientState myState;
    public PatientPhase currentPhase = PatientPhase.FindingRoom;
    public bool hasPassedReception = false;
    public bool isHealed = false; // Bệnh nhân đã khỏi bệnh chưa?
    
    [HideInInspector] public bool hasArrivedAtDesk = false;
    private Vector3 deskTargetPos;

    [Header("Cấu hình di chuyển")]
    public float stopDistance = 0.25f;
    public float waypointTolerance = 0.5f;
    
    [Header("Cấu hình NavMesh")]
    public float moveSpeed = 2.5f;
    public float turnSpeed = 500f;
    public float acceleration = 40f;
    
    [Header("Cấu hình Ra về")]
    public Transform exitPoint;
    public WaypointNode finalExitNode; // Node cuối cùng do SpawnManager truyền vào

    private NavMeshAgent agent;
    private Animator anim; // Thêm Animator
    private bool isExiting = false;
    private bool isCelebrating = false; // Đang nhảy ăn mừng?
    
    private Queue<RoomType> mySchedule = new Queue<RoomType>();
    private RoomController targetRoom;
    private QueueManager targetQueue;

    // Lộ trình Waypoint hiện tại
    private Queue<WaypointNode> currentPath = new Queue<WaypointNode>();
    private WaypointNode currentTargetNode;
    private WaypointNode lastEntryNode; // Ghi nhớ cửa phòng vừa dùng

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>(); // Tự động lấy Animator trên cùng Object
        if (anim == null) anim = GetComponentInChildren<Animator>(); // Hoặc lấy ở con
        
        agent.speed = moveSpeed;
        agent.angularSpeed = turnSpeed;
        agent.acceleration = acceleration;
        
        myState = (Random.value > 0.5f) ? PatientState.Heavy : PatientState.Light;
        if (GetComponentInChildren<Renderer>() != null)
            GetComponentInChildren<Renderer>().material.color = (myState == PatientState.Heavy) ? Color.red : Color.white;

        GenerateSchedule();
        StartCoroutine(SafeStartRoutine());
    }

    IEnumerator SafeStartRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        AdvanceSchedule();
    }

    void GenerateSchedule()
    {
        mySchedule.Enqueue(RoomType.Reception);
        mySchedule.Enqueue(RoomType.Diagnosis);
        if (myState == PatientState.Light) mySchedule.Enqueue(RoomType.NormalClinic);
        else
        {
            mySchedule.Enqueue(RoomType.XRay);
            mySchedule.Enqueue(RoomType.Surgery);
        }
        mySchedule.Enqueue(RoomType.Pharmacy);
        mySchedule.Enqueue(RoomType.Payment);
    }

    void Update()
    {
        // Cập nhật Animation dựa trên tốc độ thực tế (Dùng Bool "isWalking" theo ý bạn)
        if (anim != null && agent != null)
        {
            // Kiểm tra cả tốc độ thực tế và ý định di chuyển (desiredVelocity) để animation mượt hơn
            bool isMoving = (agent.velocity.sqrMagnitude > 0.01f || agent.desiredVelocity.sqrMagnitude > 0.01f);
            
            // CƯỠNG ÉP: Nếu đang nhảy thì không được tính là đang đi bộ
            if (isCelebrating) isMoving = false;

            anim.SetBool("isWalking", isMoving);
            anim.SetBool("isHealed", isHealed); // Cập nhật trạng thái khỏi bệnh cho Animator
            anim.SetBool("isExiting", isExiting); // Gửi tín hiệu đang ra cổng để vỗ tay/vẫy tay
        }

        // CƯỠNG ÉP: Dừng di chuyển tuyệt đối khi đang nhảy
        if (isCelebrating)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // Triệt tiêu quán tính để không bị trượt
        }

        if (currentPhase == PatientPhase.GoingToQueue || currentPhase == PatientPhase.WaitingInQueue || currentPhase == PatientPhase.Leaving)
        {
            if (!isCelebrating) HandleMovement();
        }
        else if (currentPhase == PatientPhase.WalkingToDesk)
        {
            if (!isCelebrating) HandleDeskMovement();
        }
    }

    void HandleMovement()
    {
        // 1. Xác định vị trí mục tiêu cuối cùng
        Vector3 finalTargetPos = transform.position;

        if (currentPhase == PatientPhase.GoingToQueue && targetQueue != null)
        {
            finalTargetPos = targetQueue.GetQueuePosition(this);
        }
        else if (currentPhase == PatientPhase.Leaving)
        {
            if (exitPoint != null) finalTargetPos = exitPoint.position;
        }

        // 2. Logic bám theo Waypoint trung gian
        if (currentPath != null && currentPath.Count > 0 && currentTargetNode == null)
        {
            currentTargetNode = currentPath.Dequeue();
            Debug.Log($"[Navigator] {gameObject.name} đang tới Waypoint: {currentTargetNode.name}");
        }

        Vector3 moveDestination = finalTargetPos;
        bool isMovingToWaypoint = false;

        if (currentTargetNode != null)
        {
            moveDestination = currentTargetNode.transform.position;
            isMovingToWaypoint = true;

            if (Vector3.Distance(transform.position, moveDestination) < waypointTolerance)
            {
                currentTargetNode = null; 
            }
        }

        // 3. Điều khiển Agent
        if (Vector3.Distance(agent.destination, moveDestination) > 0.1f)
        {
            agent.SetDestination(moveDestination);
        }

        // 4. Kiểm tra về đích cuối
        if (!isMovingToWaypoint)
        {
            Vector3 flatTarget = new Vector3(finalTargetPos.x, transform.position.y, finalTargetPos.z);
            float currentDist = Vector3.Distance(transform.position, flatTarget);
            
            // Yêu cầu đi thật sát điểm ra về (0.1f) để không bị vẫy tay từ xa
            float requiredDist = (currentPhase == PatientPhase.Leaving) ? 0.1f : stopDistance;

            if (currentDist < requiredDist)
            {
                agent.isStopped = true;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

                if (currentPhase == PatientPhase.GoingToQueue && targetQueue != null)
                {
                    Vector3 lookDir = -(targetQueue.transform.rotation * targetQueue.queueDirection);
                    if (lookDir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 10f);
                    
                    if (targetQueue.IsFirstInLine(this)) 
                    {
                        currentPhase = PatientPhase.WaitingInQueue;
                        if (targetRoom != null) targetRoom.ReceivePatient(this);
                    }
                }

                if (currentPhase == PatientPhase.Leaving && !isExiting)
                {
                    isExiting = true;
                    agent.velocity = Vector3.zero; // Xoá bỏ quán tính để NPC đứng im lập tức
                    StartCoroutine(DestroyAfterDelay());
                }
            }
            else
            {
                agent.isStopped = false;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }
    }

    public void WalkToDesk(Vector3 deskPos)
    {
        hasArrivedAtDesk = false;
        deskTargetPos = deskPos;
        currentPhase = PatientPhase.WalkingToDesk;
        
        if (targetQueue != null)
        {
            targetQueue.LeaveQueue(this);
            targetQueue = null;
        }

        agent.isStopped = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        agent.SetDestination(deskTargetPos);
    }

    void HandleDeskMovement()
    {
        Vector3 flatTarget = new Vector3(deskTargetPos.x, transform.position.y, deskTargetPos.z);
        
        // Dừng lại khi cách bàn lễ tân một khoảng nhỏ (0.6f) để tránh đâm xuyên
        if (Vector3.Distance(transform.position, flatTarget) < 0.6f)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            
            // Cố gắng xoay mặt về đích
            Vector3 lookDir = flatTarget - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(lookDir);

            hasArrivedAtDesk = true;
            currentPhase = PatientPhase.Treating; // Chính thức bước vào giai đoạn điều trị
        }
        else
        {
            if (Vector3.Distance(agent.destination, deskTargetPos) > 0.1f)
                agent.SetDestination(deskTargetPos);
        }
    }

    public void FinishTreatmentAndMoveOn()
    {
        if (targetQueue != null)
        {
            targetQueue.LeaveQueue(this);
            targetQueue = null;
        }
        targetRoom = null;
        AdvanceSchedule();
    }

    void AdvanceSchedule()
    {
        if (mySchedule.Count > 0)
        {
            RoomType nextRoomType = mySchedule.Dequeue();
            targetRoom = HospitalManager.Instance.GetBestAvailableRoom(nextRoomType);

            if (targetRoom != null)
            {
                if (targetRoom.CurrentQueueCount >= HospitalManager.Instance.maxQueueTolerance)
                {
                    LeaveHospital(true);
                    return;
                }

                targetQueue = targetRoom.roomQueue;
                
                // TÌM ĐƯỜNG WAYPOINT TỚI PHÒNG
                if (PathManager.Instance != null)
                {
                    // Lấy vị trí bắt đầu mới: Ưu tiên quay lại Node cửa cũ nếu có cấu hình forceReturn
                    WaypointNode startNode = (lastEntryNode != null) ? lastEntryNode : PathManager.Instance.GetClosestNode(transform.position);
                    WaypointNode endNode = PathManager.Instance.GetNodeForRoom(targetRoom);
                    
                    if (startNode != null && endNode != null)
                    {
                        currentPath = PathManager.Instance.FindPath(startNode, endNode);
                        if (currentPath != null && currentPath.Count > 0)
                        {
                            // ĐẢM BẢO QUAY LẠI CỬA: Nếu Node cũ yêu cầu quay lại (forceReturn), giữ lại điểm đầu tiên trong lộ trình
                            if (lastEntryNode != null)
                            {
                                if (!lastEntryNode.forceReturnAfterTreatment)
                                {
                                    // Chỉ bỏ qua điểm đầu (nút cửa cũ) nếu user cho phép đi xiên
                                    if (currentPath.Peek() == lastEntryNode) currentPath.Dequeue();
                                }
                                else
                                {
                                    Debug.Log($"[Navigator] {gameObject.name} bắt buộc quay lại cửa cũ: {lastEntryNode.name}");
                                }
                            }

                            lastEntryNode = endNode; // Ghi nhớ cửa của phòng mới này cho lượt sau
                        }
                        else
                        {
                            Debug.LogWarning($"[Navigator] {gameObject.name} KHÔNG tìm thấy đường đi tới {targetRoom.gameObject.name} bằng Waypoint!");
                        }
                    }
                }

                targetQueue.JoinQueue(this);
                currentPhase = PatientPhase.GoingToQueue;
                agent.isStopped = false;
            }
            else
            {
                LeaveHospital(true);
            }
        }
        else
        {
            LeaveHospital(false);
        }
    }

    void LeaveHospital(bool droppedOut = false)
    {
        currentPhase = PatientPhase.Leaving;
        agent.isStopped = false;
        
        if (droppedOut && hasPassedReception)
        {
            if (HospitalManager.Instance != null) HospitalManager.Instance.RemoveActivePatient();
            hasPassedReception = false;
        }

        // Tìm lộ trình Ra về thông qua Waypoint
        if (PathManager.Instance != null && exitPoint != null)
        {
            WaypointNode start = (lastEntryNode != null) ? lastEntryNode : PathManager.Instance.GetClosestNode(transform.position);
            
            // Xử lý node đích (Được truyền từ SpawnManager hoặc tự động tìm)
            WaypointNode nearExitNode = (finalExitNode != null) ? finalExitNode : PathManager.Instance.GetClosestNode(exitPoint.position);
            
            if (start != null && nearExitNode != null)
            {
                currentPath = PathManager.Instance.FindPath(start, nearExitNode);
                if (currentPath != null)
                {
                    // ĐẢM BẢO QUAY LẠI CỬA: Tương tự logic khi đi tìm phòng
                    if (lastEntryNode != null && !lastEntryNode.forceReturnAfterTreatment && currentPath.Count > 0)
                    {
                        if (currentPath.Peek() == lastEntryNode) currentPath.Dequeue();
                    }
                    Debug.Log($"[Navigator] {gameObject.name} đang tìm đường ra cổng ({currentPath.Count} điểm).");
                }
            }
        }
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    public IEnumerator PlayCelebration(float duration)
    {
        if (anim == null) yield break;

        isCelebrating = true;
        agent.isStopped = true;
        
        // Kích hoạt biến bool nhảy ăn mừng
        anim.SetBool("Celebrate", true);
        Debug.Log($"[Navigator] {gameObject.name} đang nhảy ăn mừng trong {duration} giây!");

        yield return new WaitForSeconds(duration);

        isCelebrating = false;
        agent.isStopped = false;
        anim.SetBool("Celebrate", false); // Tắt nhảy sau khi hết thời gian
        Debug.Log($"[Navigator] {gameObject.name} đã nhảy xong và tiếp tục di chuyển.");
    }

    // Hàm nhận sự kiện từ Animation (để fix lỗi no receiver)
    public void NewEvent() { }
}