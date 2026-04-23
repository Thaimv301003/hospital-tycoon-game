using UnityEngine;
using System.Collections;

public class ReceptionistController : MonoBehaviour
{
    private Animator anim;

    [Header("Trạng thái")]
    public bool isBusy = false;

    [Header("Thời gian xử lý")]
    [Tooltip("Thời gian (giây) để nhân viên phục vụ 1 bệnh nhân. Sẽ giảm khi nâng cấp nhân viên.")]
    public float processTime = 3f;

    void Awake()
    {
        // Tự động tìm component Animator trên object này hoặc object con
        anim = GetComponent<Animator>();
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }
    }

    /// <summary>
    /// Hàm này được phòng (RoomController) gọi khi có bệnh nhân tới quầy.
    /// Nó nhận vào thông tin bệnh nhân và thời gian cần thiết để xử lý.
    /// </summary>
    public IEnumerator ServePatient(CharacterNavigator patient, float processTime)
    {
        isBusy = true;

        // 1. Kích hoạt thông số isServing để Animator chuyển qua Bow -> Talking
        if (anim != null)
        {
            anim.SetBool("isServing", true);
        }

        // 2. Quay mặt về phía bệnh nhân (để phần diễn đạt tự nhiên hơn)
        if (patient != null)
        {
            Vector3 lookDirection = patient.transform.position - transform.position;
            lookDirection.y = 0; // Đảm bảo không bị ngửa mặt lên trời
            if (lookDirection.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        // 3. Đợi quá trình đăng ký (chờ hết thời gian processTime của phòng)
        yield return new WaitForSeconds(processTime);

        // 4. Kết thúc quá trình, bệnh nhân đi, nhân viên ngừng phục vụ -> Trở về Idle
        if (anim != null)
        {
            anim.SetBool("isServing", false);
        }

        isBusy = false;
    }
}
