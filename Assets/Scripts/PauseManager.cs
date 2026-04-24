using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện cần thiết để chơi lại màn chơi (load lại scene)

public class PauseManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Kéo thả Panel Menu Tạm Dừng (Pause Menu) vào đây")]
    public GameObject pauseMenuPanel;

    private void Start()
    {
        // Khi bắt đầu game, đảm bảo bảng Pause luôn ẩn
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Hàm này để DỪNG GAME.
    /// Bạn hãy gắn hàm này vào nút Pause nhỏ ở góc màn hình.
    /// </summary>
    public void PauseGame()
    {
        Time.timeScale = 0f; // Dừng thời gian
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true); // Hiện bảng Pause Menu lên
        }
    }

    /// <summary>
    /// Hàm này để TIẾP TỤC CHƠI (Continue).
    /// Gắn vào nút "Continue" trong bảng Pause.
    /// </summary>
    public void ContinueGame()
    {
        Time.timeScale = 1f; // Tiếp tục thời gian
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false); // Ẩn bảng Pause Menu đi
        }
    }

    /// <summary>
    /// Hàm này để CHƠI LẠI (Start Again).
    /// Gắn vào nút "Start Again" trong bảng Pause.
    /// </summary>
    public void RestartGame()
    {
        // Trả lại thời gian bình thường trước khi load lại cảnh (rất quan trọng)
        Time.timeScale = 1f; 
        
        // Lấy tên của Scene hiện tại và nạp lại nó (reset toàn bộ game về ban đầu)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Hàm này để THOÁT GAME (Exit).
    /// Gắn vào nút "Exit" trong bảng Pause.
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Đang thoát game..."); // Báo log trong Editor để bạn biết nó hoạt động
        
        // Lệnh này sẽ tắt hẳn game (lưu ý: nó chỉ có tác dụng khi bạn đã Build game ra điện thoại hoặc máy tính. Trong Unity Editor nó sẽ không tự tắt đâu nhé!)
        Application.Quit(); 
    }
}
