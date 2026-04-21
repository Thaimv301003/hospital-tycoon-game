using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PathManager))]
[InitializeOnLoad]
public class PathManagerEditor : Editor
{
    private static PathManager _pathManager;
    private static WaypointNode _selectedNode;
    
    // ĐỔI THÀNH FALSE: Mặc định tắt để không làm phiền khi thao tác scene bình thường
    private static bool _isEditMode = false; 

    static PathManagerEditor()
    {
        SceneView.duringSceneGui += GlobalSceneGUI;
    }

    void OnEnable()
    {
        _pathManager = (PathManager)target;
    }

    public override void OnInspectorGUI()
    {
       //bật tắt tính năng shift+click
        GUILayout.Space(10);
        
        // Đổi màu nút dựa trên trạng thái
        GUI.backgroundColor = _isEditMode ? Color.green : new Color(0.8f, 0.8f, 0.8f);
        
        // Style cho text to và in đậm
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.fontSize = 13;
        
        string btnText = _isEditMode ? "🟢 WAYPOINT EDIT MODE: ON" : "🔴 WAYPOINT EDIT MODE: OFF";
        
        if (GUILayout.Button(btnText, btnStyle, GUILayout.Height(40)))
        {
            _isEditMode = !_isEditMode;
            SceneView.RepaintAll(); // Cập nhật lại Scene View ngay lập tức
        }
        
        GUI.backgroundColor = Color.white; // Trả lại màu mặc định
        GUILayout.Space(10);
        // 

        DrawDefaultInspector();
        
        GUILayout.Space(10);
        GUI.color = Color.cyan;
        if (GUILayout.Button("Refresh All Nodes (Scan Scene)"))
        {
            if (_pathManager != null) _pathManager.RefreshAllNodes();
            EditorUtility.SetDirty(_pathManager);
        }
        GUI.color = Color.white;

        if (GUILayout.Button("Auto Link Rooms (Closest)"))
        {
            LinkRooms();
        }

        GUILayout.Space(20);
        GUI.color = Color.red;
        if (GUILayout.Button("Clear All Nodes"))
        {
            if (EditorUtility.DisplayDialog("Clear Nodes", "Are you sure you want to delete all Waypoints?", "Yes", "No"))
            {
                if (_pathManager != null)
                {
                    foreach (var n in _pathManager.allNodes) if (n != null) Undo.DestroyObjectImmediate(n.gameObject);
                    _pathManager.allNodes.Clear();
                    _pathManager.rootNode = null;
                }
                _selectedNode = null;
                EditorUtility.SetDirty(_pathManager);
            }
        }
        GUI.color = Color.white;
    }

    private static void GlobalSceneGUI(SceneView sceneView)
    {
        if (_pathManager == null)
        {
            _pathManager = GameObject.FindObjectOfType<PathManager>();
            if (_pathManager == null) return;
        }

        Event e = Event.current;

        // Chỉ vẽ bảng hướng dẫn trên Scene View KHI CHẾ ĐỘ EDIT ĐANG BẬT
        if (_isEditMode)
        {
            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 250, 100));
            var style = new GUIStyle(GUI.skin.box);
            style.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
            style.normal.textColor = Color.white;
            
            GUILayout.BeginVertical(style);
            GUILayout.Label("🛠 BẬT VẼ WAYPOINT", EditorStyles.boldLabel);
            GUILayout.Label("• Shift + Click: Tạo & Nối tiếp", style);
            GUILayout.Label("• Ctrl + Click: Tạo Nhánh mới", style);
            GUILayout.Label("• Alt + Click vào Node: Nối tới điểm cũ", style);
            if (_selectedNode != null) GUILayout.Label("ĐANG CHỌN: " + _selectedNode.name, style);
            else GUILayout.Label("ĐANG CHỌN: Trống", style);
            GUILayout.EndVertical();
            GUILayout.EndArea();
            Handles.EndGUI();

            // QUAN TRỌNG: Chiếm quyền điều khiển chuột khi đang ở Edit Mode để tránh click nhầm object khác
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(controlID);

            // Xử lý logic Click
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                WaypointNode hitNode = null;

                // Kiểm tra click trúng node (tăng khoảng cách nhạy để dễ click)
                foreach (var node in _pathManager.allNodes)
                {
                    if (node == null) continue;
                    float dist = Vector3.Cross(ray.direction, node.transform.position - ray.origin).magnitude;
                    if (dist < 0.6f)
                    {
                        hitNode = node;
                        break;
                    }
                }

                // xử lý alt+click
                if ((e.modifiers & EventModifiers.Alt) != 0 && _selectedNode != null && hitNode != null && hitNode != _selectedNode)
                {
                    Undo.RecordObject(_selectedNode, "Connect Waypoints");
                    _selectedNode.connections.Add(new Connection { targetNode = hitNode });
                    EditorUtility.SetDirty(_selectedNode);
                    e.Use();
                    return;
                }

                // click chọn node
                if (hitNode != null)
                {
                    _selectedNode = hitNode;
                    if (e.modifiers == EventModifiers.None)
                    {
                        Selection.activeGameObject = hitNode.gameObject;
                    }
                    e.Use();
                    return;
                }

                // shift/ctrl+click tạo node mới
                bool isShift = (e.modifiers & EventModifiers.Shift) != 0;
                bool isCtrl = (e.modifiers & EventModifiers.Control) != 0 || (e.modifiers & EventModifiers.Command) != 0;

                if (isShift || isCtrl)
                {
                    Vector3 spawnPos = Vector3.zero;
                    if (Physics.Raycast(ray, out RaycastHit hit)) spawnPos = hit.point;
                    else
                    {
                        Plane ground = new Plane(Vector3.up, Vector3.zero);
                        if (ground.Raycast(ray, out float d)) spawnPos = ray.GetPoint(d);
                    }

                    WaypointNode newNode = StaticCreateNode(spawnPos);
                    
                    if (_selectedNode != null)
                    {
                        Undo.RecordObject(_selectedNode, "Connect Waypoints");
                        _selectedNode.connections.Add(new Connection { targetNode = newNode });
                        if (isCtrl) _selectedNode.isBranch = true;
                        EditorUtility.SetDirty(_selectedNode);
                    }

                    _selectedNode = newNode;
                    e.Use();
                }
            }
        }
    }

    static WaypointNode StaticCreateNode(Vector3 position)
    {
        GameObject newNodeObj;
        if (_pathManager.waypointPrefab != null)
        {
            newNodeObj = (GameObject)PrefabUtility.InstantiatePrefab(_pathManager.waypointPrefab);
        }
        else
        {
            newNodeObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newNodeObj.name = "Waypoint_" + _pathManager.allNodes.Count;
            newNodeObj.transform.localScale = Vector3.one * 0.4f;
        }

        newNodeObj.transform.position = position;
        newNodeObj.transform.SetParent(_pathManager.transform);

        WaypointNode newNode = newNodeObj.GetComponent<WaypointNode>();
        if (newNode == null) newNode = newNodeObj.AddComponent<WaypointNode>();

        _pathManager.allNodes.Add(newNode);
        if (_pathManager.rootNode == null) _pathManager.rootNode = newNode;

        Undo.RegisterCreatedObjectUndo(newNodeObj, "Create Waypoint");
        EditorUtility.SetDirty(_pathManager);
        return newNode;
    }

    private static Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void LinkRooms()
    {
        if (_pathManager == null) return;

        RoomController[] rooms = GameObject.FindObjectsByType<RoomController>(FindObjectsSortMode.None);
        foreach (var room in rooms)
        {
            WaypointNode closest = null;
            float minDist = float.MaxValue;
            foreach (var node in _pathManager.allNodes)
            {
                float d = Vector3.Distance(node.transform.position, room.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    closest = node;
                }
            }
            if (closest != null)
            {
                Undo.RecordObject(closest, "Link Room");
                closest.connectedRoom = room;
                EditorUtility.SetDirty(closest);
            }
        }
        Debug.Log("Finished linking rooms to closest waypoints.");
    }
}