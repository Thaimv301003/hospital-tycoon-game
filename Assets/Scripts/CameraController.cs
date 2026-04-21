using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; // Thêm thư viện Input System mới

/// <summary>
/// Quản lý di chuyển Camera quanh Map (Dùng New Input System)
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Pan Settings (Trượt)")]
    [Tooltip("Tốc độ bắt mượt của Camera (10 là vừa đẹp)")]
    public float smoothSpeed = 10f;

    [Header("Map Boundaries (Giới hạn di chuyển)")]
    [Tooltip("Chặn không cho kéo quá xa về 2 bên phải/trái")]
    public Vector2 limitX = new Vector2(-50f, 50f);
    [Tooltip("Chặn không cho kéo quá xa về 2 mặt trên/dưới")]
    public Vector2 limitZ = new Vector2(-50f, 50f);

    [Header("Zoom Settings (Cuộn chuột)")]
    public float zoomSpeed = 2f;
    [Tooltip("Độ thu nhỏ ngần màn hình nhất")]
    public float minZoom = 5f;
    [Tooltip("Độ phóng to xa màn hình nhất")]
    public float maxZoom = 40f;

    private Camera cam;
    private Vector3 dragOrigin;
    private Vector3 targetPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        
        targetPosition = transform.position;
    }

    void LateUpdate()
    {
        HandleZoom();
        HandlePan();
        
        // Di chuyển camera mượt mà từ từ đuổi theo targetPosition
        // Lưu ý: CHỈ Lerp bắt mượt khi đang KHÔNG kéo chuột để tránh rung lắc
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
    }

    private void HandlePan()
    {
        if (Mouse.current == null) return;

        // Khi vừa bắt đầu ấn chuột trái
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // Bỏ qua nếu người dùng đang click vào một cục UI (ví dụ Nút bấm)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            dragOrigin = GetMouseGroundPosition();
        }

        // Khi đang giữ đè chuột trái và rê
        if (Mouse.current.leftButton.isPressed)
        {
            Vector3 currentPos = GetMouseGroundPosition();
            if (currentPos != Vector3.zero && dragOrigin != Vector3.zero)
            {
                // Tính độ chênh lệch bị dịch chuyển
                Vector3 difference = dragOrigin - currentPos;

                // Cộng độ chênh vào vị trí đích đến của camera
                targetPosition += difference;
                
                // Ép vị trí không đi quá giới hạn biên cắm của map
                targetPosition.x = Mathf.Clamp(targetPosition.x, limitX.x, limitX.y);
                targetPosition.z = Mathf.Clamp(targetPosition.z, limitZ.x, limitZ.y);

                // QUAN TRỌNG: Snap (Khóa nhòe) khung hình ngay sang vị trí đích để dứt điểm sự cố RUNG GIẬT,
                // do tia bắt nền đập lại độ trễ Lerp của camera gây ra
                transform.position = targetPosition;
            }
        }
        else
        {
            // Reset khi nhả chuột
            dragOrigin = Vector3.zero;
        }
    }

    private void HandleZoom()
    {
        if (Mouse.current == null) return;

        // Đọc giá trị cuộn chuột thô (raw)
        float rawScroll = Mouse.current.scroll.ReadValue().y;
        
        if (Mathf.Abs(rawScroll) > 0.01f)
        {
            // Chặn cuộn lố nếu đang cuộn qua UI to
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            // Chuyển đổi giá trị scroll của hệ thống mới (thường là 120, -120, v.v) về tương đương Legacy Input (0.1, -0.1)
            float scroll = Mathf.Clamp(rawScroll, -1f, 1f) * 0.1f;

            if (cam.orthographic)
            {
                // Cho game 2D cơ bản hoặc gắn Camera mode Orthographic
                cam.orthographicSize -= scroll * zoomSpeed * 10f;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
            else
            {
                // Cho game 3D Perspective (Phóng theo trục nhìn tới - forward)
                targetPosition += transform.forward * scroll * zoomSpeed * 15f;
                
                // Khóa độ cao zoom tránh lún đất hay bay lên mây (so tương đối qua trục Y)
                targetPosition.y = Mathf.Clamp(targetPosition.y, minZoom, maxZoom);
            }
        }
    }

    /// <summary>
    /// Thuật toán bắn tia thẳng từ ống kính xuống "sàn vô hình" để tìm điểm neo kéo chuột
    /// (Rất kinh điển dùng trong game Tycoon/MOBA)
    /// </summary>
    private Vector3 GetMouseGroundPosition()
    {
        if (cam == null || Mouse.current == null) return Vector3.zero;

        // Tạo 1 mặt sàn ngửa (Up) nằm tại điểm cao độ Y = 0 (zero)
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        
        // Lấy toạ độ chuột màn hình từ New Input System
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (groundPlane.Raycast(ray, out float hitDistance))
        {
            return ray.GetPoint(hitDistance);
        }

        return Vector3.zero;
    }
}
