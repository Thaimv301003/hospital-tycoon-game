using UnityEngine;
using UnityEngine.UI; // Thêm thư viện UI để dùng Button

public class RoomUpgradeManager : MonoBehaviour
{
    [Header("Room Info")]
    public string roomName = "Phòng Khám";
    public int currentLevel = 1;
    public int maxLevel = 5;

    [Header("Economy Settings")]
    public float baseUpgradeCost = 100f;
    public float baseIncome = 50f;
    public float costMultiplier = 1.4f;
    public float incomeMultiplier = 1.25f;

    [Header("UI Overlay Settings")]
    [Tooltip("Prefab của Button Nâng cấp (chứa component Button)")]
    public GameObject uiPrefab;
    [Tooltip("Gốc UI Canvas Overlay trên màn hình (để chứa button sinh ra)")]
    public Transform mainCanvasTransform;
    [Tooltip("Điểm neo (vị trí thế giới) để đặt UI đè lên (vd: 1 object trống nằm giữa phòng)")]
    public Transform uiSpawnPosition;

    private GameObject uiInstance;
    private RectTransform uiRectTransform;

    [System.Serializable]
    public class DecorationStep
    {
        [Tooltip("Danh sách đồ vật MỚI sẽ xuất hiện")]
        public GameObject[] newDecorations;
        [Tooltip("Danh sách đồ vật CŨ sẽ bị ẩn đi (Kéo cái ghế cũ/rách vào đây để nó biến mất khi có ghế mới. Có thể để trống)")]
        public GameObject[] oldDecorationsToHide;
    }

    [Header("Level Decorations (Index 0 = Level 2, v.v...)")]
    [Tooltip("Danh sách cấu hình đổi nội thất theo level. Vị trí 0 sẽ áp dụng ở Cấp 2, vị trí 1 cho Cấp 3...")]
    public DecorationStep[] levelDecorations;

    [Header("Upgrade Effects")]
    [Tooltip("Particle System (hiệu ứng hạt) dựng sẵn trong Scene, nếu có")]
    public ParticleSystem upgradeParticles;
    public AudioSource audioSource;
    public AudioClip upgradeSound;
    
    [Tooltip("Kéo Prefab hiệu ứng (VFX) tải từ ngoài vào đây. Nó sẽ được Instantiate ra khi nâng cấp.")]
    public GameObject upgradeEffectPrefab;
    [Tooltip("Vị trí sinh ra Prefab hiệu ứng ngoài. Nếu để trống, sẽ sinh ra ở ngay gốc tọa độ của phòng.")]
    public Transform effectSpawnPoint;

    private void Start()
    {
        // Đảm bảo ngoại hình hiển thị đúng với cấp độ hiện tại lúc khởi động game (không bật animation nhún nảy)
        UpdateVisuals(false);

        // Khởi tạo UI (Spawn Prefab vào Canvas)
        InitOverlayUI();
    }

    private void Update()
    {
        // Liên tục cập nhật vị trí của cục UI để chạy theo phòng trên màn hình
        if (uiInstance != null && uiSpawnPosition != null && Camera.main != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(uiSpawnPosition.position);
            
            // Xoá/Ẩn UI nếu object lọt ra đằng sau Camera
            if (screenPos.z < 0)
            {
                uiInstance.SetActive(false);
            }
            else
            {
                uiInstance.SetActive(true);
                uiRectTransform.position = screenPos;
            }
        }
    }

    private void InitOverlayUI()
    {
        if (uiPrefab != null && mainCanvasTransform != null)
        {
            // Sinh ra cục UI trong Canvas chính
            uiInstance = Instantiate(uiPrefab, mainCanvasTransform);
            uiInstance.name = "UI_Upgrade_" + roomName; // Đổi tên cho dễ nhìn trong Hierarchy
            uiRectTransform = uiInstance.GetComponent<RectTransform>();

            // Gắn tự động sự kiện OnClick cho Button
            // Tìm component Button ở ngay gốc mặt ngoài hoặc ẩn trong con cháu
            Button btn = uiInstance.GetComponent<Button>();
            if (btn == null) btn = uiInstance.GetComponentInChildren<Button>(true);

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners(); // Xoá sạch rác sự kiện cũ nhỡ có gắn bừa
                btn.onClick.AddListener(OnUpgradeButtonClicked);
                Debug.Log($"[{roomName}] Đã tự động gắn event Click thành công vào nút UI.");
            }
            else
            {
                Debug.LogError($"[{roomName}] Không tìm thấy component 'Button' bên trong Prefab UI. Vui lòng kiểm tra lại Prefab!");
            }

            // Gọi thử lần đầu để cập nhật giá
            UpdateUIValues();
        }
        else
        {
            Debug.LogWarning($"[{roomName}] Bỏ qua tạo UI, chưa gán Prefab hoặc Main Canvas cho RoomUpgradeManager.");
        }
    }

    /// <summary>
    /// Hàm này dùng để cập nhật text/giá trị cho cục UI khi vừa sinh ra hoặc sau khi nâng cấp
    /// </summary>
    private void UpdateUIValues()
    {
        if (uiInstance == null) return;

        int cost = Mathf.RoundToInt(GetUpgradeCost());
        
        // --- Cách cập nhật hiển thị giá tiền ---
        
        // 1. NẾU DÙNG TEXT THƯỜNG CỦA UNITY:
        // Text[] texts = uiInstance.GetComponentsInChildren<Text>(true);
        // foreach (var t in texts) {
        //     if (t.name == "Text" || t.name == "PriceText") { // Sửa tên obj chứa Text cho trùng
        //         t.text = cost.ToString() + "$";
        //     }
        // }

        // 2. NẾU DÙNG TEXTMESHPRO (Vì thấy ở hình bạn có thư mục TextMesh Pro):
        // (Nhớ thêm using TMPro; ở đầu file)
        // TMPro.TextMeshProUGUI[] tmpros = uiInstance.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
        // foreach (var t in tmpros) {
        //     if (t.gameObject.name == "PriceText" || t.gameObject.name == "Text (TMP)") {
        //         t.text = cost.ToString() + "$";
        //     }
        // }
    }

    /// <summary>
    /// Công thức tính giá nâng cấp lên cấp tiếp theo
    /// </summary>
    public float GetUpgradeCost()
    {
        return baseUpgradeCost * Mathf.Pow(costMultiplier, currentLevel - 1);
    }

    /// <summary>
    /// Công thức tính số tiền thưởng phòng kiếm được dựa trên level hiện tại
    /// </summary>
    public float GetCurrentIncome()
    {
        // Ví dụ: Mỗi cấp tăng thêm 25% thu nhập so với cấp trước
        return baseIncome * Mathf.Pow(incomeMultiplier, currentLevel - 1);
    }

    /// <summary>
    /// Hàm này sẽ được gọi khi User bấm vào nút "Nâng cấp" trên UI
    /// </summary>
    public bool UpgradeRoom()
    {
        if (currentLevel >= maxLevel)
        {
            Debug.Log($"[{roomName}] Đã đạt cấp độ tối đa!");
            return false;
        }

        int cost = Mathf.RoundToInt(GetUpgradeCost());
        
        // --- Tích hợp Trừ Tiền ---
        if (HospitalManager.Instance != null)
        {
            if (!HospitalManager.Instance.HasEnoughMoney(cost))
            {
                Debug.Log($"[{roomName}] Không đủ tiền để nâng cấp! Cần {cost} nhưng bạn chỉ có {HospitalManager.Instance.totalRevenue}.");
                return false;
            }
            HospitalManager.Instance.SpendMoney(cost);
        }
        // -------------------------

        currentLevel++;
        Debug.Log($"[{roomName}] Đã nâng cấp lên Level {currentLevel}!");

        // Cập nhật lại UI sau khi mua (để đổi level mới, giá mới)
        UpdateUIValues();

        // Khởi chạy hiệu ứng Âm thanh & Ánh sáng
        PlayUpgradeEffects();

        // Cập nhật lại đồ vật trang trí trong phòng và bật hiệu ứng nhún nảy
        UpdateVisuals(true);

        return true;
    }

    /// <summary>
    /// Hàm này được gọi từ UI Button vì Unity Event chỉ nhận hàm trả về kiểu void.
    /// </summary>
    public void OnUpgradeButtonClicked()
    {
        Debug.LogWarning(">>>>> KÍCH HOẠT NÚT BẤM NÂNG CẤP! Bắt đầu kiểm tra... <<<<<");
        UpgradeRoom();
    }

    /// <summary>
    /// Quản lý việc bật/tắt đồ vật trang trí dựa vào cấp hiện tại. Hỗ trợ thay thế đồ cũ bằng đồ mới.
    /// </summary>
    private void UpdateVisuals(bool animateNewItem)
    {
        for (int i = 0; i < levelDecorations.Length; i++)
        {
            // Logic: levelDecorations[0] sẽ yêu cầu cấp >= 2 để hiện.
            bool isLevelReached = currentLevel > (i + 1);
            
            GameObject[] newItems = levelDecorations[i].newDecorations;
            GameObject[] oldItems = levelDecorations[i].oldDecorationsToHide;

            if (isLevelReached)
            {
                // ĐÃ ĐẠT CẤP -> Bật cái mới, Tắt cái cũ đi
                if (newItems != null)
                {
                    foreach (var newItem in newItems)
                    {
                        if (newItem != null)
                        {
                            if (!newItem.activeSelf && animateNewItem)
                            {
                                newItem.SetActive(true);
                                StartCoroutine(PopUpEffect(newItem.transform));
                            }
                            else
                            {
                                newItem.SetActive(true);
                            }
                        }
                    }
                }

                if (oldItems != null)
                {
                    foreach (var oldItem in oldItems)
                    {
                        if (oldItem != null)
                        {
                            oldItem.SetActive(false);
                        }
                    }
                }
            }
            else
            {
                // CHƯA ĐẠT CẤP -> Tắt cái mới, Bật cái cũ
                if (newItems != null)
                {
                    foreach (var newItem in newItems)
                    {
                        if (newItem != null)
                        {
                            newItem.SetActive(false);
                        }
                    }
                }

                if (oldItems != null)
                {
                    foreach (var oldItem in oldItems)
                    {
                        if (oldItem != null)
                        {
                            oldItem.SetActive(true);
                        }
                    }
                }
            }
        }
    }

    private void PlayUpgradeEffects()
    {
        // 1. Bật Particle System tĩnh có sẵn trong Scene
        if (upgradeParticles != null)
        {
            upgradeParticles.Play();
        }

        // 2. Phát âm thanh
        if (audioSource != null && upgradeSound != null)
        {
            audioSource.PlayOneShot(upgradeSound);
        }

        // 3. Sinh ra Prefab hiệu ứng từ bên ngoài (Asset Store)
        if (upgradeEffectPrefab != null)
        {
            Transform spawnTarget = effectSpawnPoint != null ? effectSpawnPoint : transform;
            // Tạo ra hiệu ứng
            GameObject vfx = Instantiate(upgradeEffectPrefab, spawnTarget.position, Quaternion.identity);
            
            // Xoá rác (huỷ effect) sau 3 giây để tránh làm nặng game.
            // Nếu Effect bên ngoài có sẵn script tự huỷ của asset thì bạn có thể bỏ dòng này.
            Destroy(vfx, 3f);
        }
    }

    /// <summary>
    /// Hiệu ứng nảy lố (Overshoot) cho các đồ vật mới xuất hiện
    /// </summary>
    private System.Collections.IEnumerator PopUpEffect(Transform target)
    {
        Vector3 originalScale = target.localScale;
        target.localScale = Vector3.zero;

        float time = 0.3f;
        float elapsedTime = 0f;

        // Phóng to lố kích thước thật một chút (1.2x)
        while (elapsedTime < time)
        {
            target.localScale = Vector3.Lerp(Vector3.zero, originalScale * 1.25f, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        elapsedTime = 0f;
        float bounceTime = 0.15f;
        // Thu nhỏ lại về kích thước thật
        while (elapsedTime < bounceTime)
        {
            target.localScale = Vector3.Lerp(originalScale * 1.25f, originalScale, elapsedTime / bounceTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        target.localScale = originalScale;
    }
}
