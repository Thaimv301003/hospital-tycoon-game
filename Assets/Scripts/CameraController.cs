using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 
using UnityEngine.InputSystem.Controls;

/// <summary>
/// Quản lý di chuyển Camera quanh Map (Hỗ trợ PC & Android Touch với New Input System)
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

    [Header("Zoom Settings (Cuộn chuột & Pinch to zoom)")]
    public float zoomSpeed = 2f;
    [Tooltip("Độ thu nhỏ ngần màn hình nhất (Hoặc độ cao tối thiểu cho 3D)")]
    public float minZoom = 5f;
    [Tooltip("Độ phóng to xa màn hình nhất (Hoặc độ cao tối đa cho 3D)")]
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
        // Chỉ Lerp bắt mượt khi đang KHÔNG kéo tay/chuột để tránh rung lắc
        if (!IsPointerPressed())
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        }
    }

    private bool IsPointerPressed()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
        if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
        return false;
    }

    private void HandlePan()
    {
        bool isPressed = false;
        bool wasPressed = false;
        int touchCount = GetActiveTouchCount();

        // Chỉ cho phép Pan khi có đúng 1 ngón tay chạm màn hình (tránh xung đột với Zoom bằng 2 ngón)
        if (Touchscreen.current != null && touchCount > 0)
        {
            if (touchCount == 1)
            {
                isPressed = Touchscreen.current.primaryTouch.press.isPressed;
                wasPressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
        }
        else if (Mouse.current != null)
        {
            isPressed = Mouse.current.leftButton.isPressed;
            wasPressed = Mouse.current.leftButton.wasPressedThisFrame;
        }

        if (wasPressed)
        {
            // Bỏ qua nếu click / chạm vào UI
            if (IsPointerOverUI())
                return;

            dragOrigin = GetPointerGroundPosition();
        }

        if (isPressed)
        {
            Vector3 currentPos = GetPointerGroundPosition();
            if (currentPos != Vector3.zero && dragOrigin != Vector3.zero)
            {
                // Tính độ chênh lệch bị dịch chuyển
                Vector3 difference = dragOrigin - currentPos;

                // Cộng độ chênh vào vị trí đích đến của camera
                targetPosition += difference;
                
                // Ép vị trí không đi quá giới hạn biên cắm của map
                targetPosition.x = Mathf.Clamp(targetPosition.x, limitX.x, limitX.y);
                targetPosition.z = Mathf.Clamp(targetPosition.z, limitZ.x, limitZ.y);

                // Quan trọng: Tách biệt transform.position ra để mượt mà khi nhả chuột (Lerp ở LateUpdate sẽ lo)
                // Phản hồi lập tức trên màn hình cảm ứng để người dùng có cảm giác dính tay 1:1
                transform.position = targetPosition;
            }
        }
        else
        {
            // Reset khi nhả ngón tay / chuột
            dragOrigin = Vector3.zero;
        }
    }

    private void HandleZoom()
    {
        int touchCount = GetActiveTouchCount();

        // 1. Phóng to / Thu nhỏ bằng 2 ngón tay (Pinch to zoom) trên điện thoại
        if (Touchscreen.current != null && touchCount >= 2)
        {
            // Cần lấy đủ 2 touch đang active
            var touch0 = GetActiveTouch(0);
            var touch1 = GetActiveTouch(1);

            if (touch0 != null && touch1 != null)
            {
                // Vị trí frame này
                Vector2 pos0 = touch0.position.ReadValue();
                Vector2 pos1 = touch1.position.ReadValue();

                // Vị trí frame trước (Bằng vị trí hiện tại trừ đi độ dịch chuyển delta của 1 frame)
                Vector2 prevPos0 = pos0 - touch0.delta.ReadValue();
                Vector2 prevPos1 = pos1 - touch1.delta.ReadValue();

                // Khoảng cách giữa 2 ngón tay ở frame trước và frame này
                float prevMagnitude = (prevPos0 - prevPos1).magnitude;
                float currentMagnitude = (pos0 - pos1).magnitude;

                // Độ chênh lệch (âm là thu nhỏ, dương là phóng to)
                float difference = currentMagnitude - prevMagnitude;

                // Ở màn hình cảm ứng, scale difference cho cảm giác vuốt tự nhiên (0.01 đến 0.05 tuỳ taste)
                ApplyZoom(difference * 0.05f);
            }
        }
        // 2. Phóng to / Thu nhỏ bằng cuộn chuột trên PC
        else if (Mouse.current != null)
        {
            float rawScroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(rawScroll) > 0.01f)
            {
                if (IsPointerOverUI()) return;

                float scroll = Mathf.Clamp(rawScroll, -1f, 1f) * 0.1f;
                ApplyZoom(scroll);
            }
        }
    }

    private void ApplyZoom(float scrollAmount)
    {
        if (cam.orthographic)
        {
            cam.orthographicSize -= scrollAmount * zoomSpeed * 10f;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }
        else
        {
            targetPosition += transform.forward * scrollAmount * zoomSpeed * 15f;
            targetPosition.y = Mathf.Clamp(targetPosition.y, minZoom, maxZoom);
            transform.position = targetPosition; // Cập nhật luôn tránh lag do Lerp
        }
    }

    /// <summary>
    /// Bắn tia tìm điểm neo (Ground) trên màn hình tại ngón tay hoặc chuột
    /// </summary>
    private Vector3 GetPointerGroundPosition()
    {
        if (cam == null) return Vector3.zero;

        Vector2 screenPos = Vector2.zero;

        if (Touchscreen.current != null && GetActiveTouchCount() > 0)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null)
        {
            screenPos = Mouse.current.position.ReadValue();
        }
        else
        {
            return Vector3.zero;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = cam.ScreenPointToRay(screenPos);

        if (groundPlane.Raycast(ray, out float hitDistance))
        {
            return ray.GetPoint(hitDistance);
        }

        return Vector3.zero;
    }

    /// <summary>
    /// Đếm số lượng ngón tay đang chạm
    /// </summary>
    private int GetActiveTouchCount()
    {
        if (Touchscreen.current == null) return 0;
        int count = 0;
        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.press.isPressed) count++;
        }
        return count;
    }

    /// <summary>
    /// Lấy ngón tay đang chạm theo thứ tự index (0 = ngón đầu, 1 = ngón thứ hai)
    /// </summary>
    private TouchControl GetActiveTouch(int index)
    {
        if (Touchscreen.current == null) return null;
        int currentIdx = 0;
        foreach (var touch in Touchscreen.current.touches)
        {
            if (touch.press.isPressed)
            {
                if (currentIdx == index) return touch;
                currentIdx++;
            }
        }
        return null;
    }

    /// <summary>
    /// Kiểm tra xem ngón tay/chuột có đang ấn lên UI (nút, panel...)
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // Xử lý kiểm tra cho cảm ứng Touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            if (EventSystem.current.IsPointerOverGameObject(touchId))
                return true;
        }
        
        // Xử lý kiểm tra cho chuột
        if (EventSystem.current.IsPointerOverGameObject())
            return true;

        return false;
    }
}
