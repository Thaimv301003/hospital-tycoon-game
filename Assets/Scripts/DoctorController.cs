using UnityEngine;
using System.Collections;

public class DoctorController : MonoBehaviour
{
    private Animator anim;
    
    [Header("Trạng thái")]
    public bool isBusy = false;

    [Header("Thông tin Nâng cấp")]
    public DoctorUpgradeManager upgradeManager;
    [Tooltip("Thời gian thực (giây) để hoàn thành trọn vẹn 1 chuỗi hoạt ảnh chữa bệnh. Dùng để tính toán độ tua nhanh (ép xung) Animation")]
    public float baseAnimationTime = 4.0f;

    void Awake()
    {
        // Tự động tìm Animator trên người bác sĩ
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// Hàm này được phòng (RoomController) gọi khi bệnh nhân tới thẳng mặt bác sĩ.
    /// Nó nhận vào thông tin bệnh nhân và thời gian cần thiết để khám.
    /// </summary>
    public IEnumerator ServePatient(CharacterNavigator patient, float processTime)
    {
        isBusy = true;

        // 1. Quay mặt về phía bệnh nhân để tương tác tự nhiên hơn
        if (patient != null)
        {
            Vector3 lookDirection = patient.transform.position - transform.position;
            lookDirection.y = 0; // Đảm bảo không bị ngửa mặt
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        // 2. Kích hoạt thông số StartTreat để Animator chạy chuỗi các Animation khám bệnh 
        // (Happy Hand Gesture -> Hands Forward -> Shaking Hands 2)
        if (anim != null)
        {
            // Tính toán ép xung tốc độ (Tua nhanh) animation nếu tốc độ xử lý do Level yêu cầu ngắn hơn thời lượng thật
            float speedMultiplier = baseAnimationTime / processTime;
            // Chỉ ép chạy nhanh lên hoặc bằng tốc độ thường, không làm chậm rì rì đi nếu khám quá lâu.
            anim.speed = Mathf.Max(1f, speedMultiplier); 

            anim.SetTrigger("StartTreat");
        }

        // 3. Chờ cho quá trình khám/chữa bệnh trôi qua dựa theo thời gian đã tính
        yield return new WaitForSeconds(processTime);

        // Khôi phục lại tốc độ bình thường để khi về trạng thái Idle không bị giật lag
        if (anim != null)
        {
            anim.speed = 1f;
        }

        // Sau khi khám xong, báo cho hệ thống biết bác sĩ đã rảnh
        isBusy = false;
    }
}
