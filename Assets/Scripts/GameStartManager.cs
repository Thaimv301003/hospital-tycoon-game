using UnityEngine;

public class GameStartManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Kéo thả Panel chứa nút Play vào đây để ẩn đi khi game bắt đầu")]
    public GameObject startUIPanel;

    private void Awake()
    {
        // Khi game vừa bật lên, dừng toàn bộ thời gian (tạm dừng mọi logic update, vật lý)
        Time.timeScale = 0f;
        
        // Đảm bảo UI Start Game luôn hiện
        if (startUIPanel != null)
        {
            startUIPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hàm này sẽ được gọi khi người chơi bấm nút "Play"
    /// Hãy gắn hàm này vào sự kiện OnClick của Button
    /// </summary>
    public void StartGame()
    {
        // Chạy lại thời gian với tốc độ bình thường (game bắt đầu)
        Time.timeScale = 1f;

        // Ẩn UI Start Game đi
        if (startUIPanel != null)
        {
            startUIPanel.SetActive(false);
        }
        
        Debug.Log("Trò chơi đã bắt đầu!");
    }
}
