using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    [Tooltip("How often the FPS is updated (in seconds).")]
    public float updateInterval = 0.5f;

    private float _accum = 0f; // FPS accumulated over the interval
    private int _frames = 0;   // Frames drawn over the interval
    private float _timeLeft;   // Time left for current interval
    private float _fps = 0f;   // Current FPS

    private GUIStyle _style;

    void Start()
    {
        _timeLeft = updateInterval;
    }

    void Update()
    {
        _timeLeft -= Time.deltaTime;
        _accum += Time.timeScale / Time.deltaTime;
        _frames++;

        // Interval ended - update GUI text and start new interval
        if (_timeLeft <= 0.0)
        {
            _fps = _accum / _frames;
            _timeLeft = updateInterval;
            _accum = 0.0f;
            _frames = 0;
        }
    }

    void OnGUI()
    {
        if (_style == null)
        {
            _style = new GUIStyle();
            _style.alignment = TextAnchor.UpperLeft;
            
            // Adjust font size based on screen height for better scaling
            int h = Screen.height;
            _style.fontSize = h * 2 / 100;
            if (_style.fontSize < 24) _style.fontSize = 24; 
        }

        Rect rect = new Rect(10, 10, Screen.width, _style.fontSize);
        string text = string.Format("{0:0.0} FPS", _fps);
        
        // Dynamic color modification based on FPS
        if (_fps < 30)
            _style.normal.textColor = Color.red;
        else if (_fps < 50)
            _style.normal.textColor = Color.yellow;
        else
            _style.normal.textColor = Color.green;

        // Draw shadow/outline for better visibility
        Color originalColor = _style.normal.textColor;
        _style.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, _style);
        
        // Draw actual text
        _style.normal.textColor = originalColor;
        GUI.Label(rect, text, _style);
    }
}
