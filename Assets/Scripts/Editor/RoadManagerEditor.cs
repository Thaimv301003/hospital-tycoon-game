using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(RoadManager))]
[InitializeOnLoad]
public class RoadManagerEditor : Editor
{
    private static RoadManager _targetManager;
    private static WaypointNode _selectedNode;
    private static bool _isEditMode = false;

    static RoadManagerEditor()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnEnable()
    {
        _targetManager = (RoadManager)target;
    }

    public override void OnInspectorGUI()
    {
        GUILayout.Space(10);
        GUI.backgroundColor = _isEditMode ? Color.green : new Color(0.8f, 0.8f, 0.8f);
        
        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.fontSize = 13;
        
        string btnText = _isEditMode ? "🟢 ROAD EDIT MODE: ON" : "🔴 ROAD EDIT MODE: OFF";
        if (GUILayout.Button(btnText, btnStyle, GUILayout.Height(40)))
        {
            _isEditMode = !_isEditMode;
            SceneView.RepaintAll();
        }
        
        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);

        if (GUILayout.Button("Refresh Road Nodes"))
        {
            _targetManager.RefreshRoadNodes();
            EditorUtility.SetDirty(_targetManager);
        }

        DrawDefaultInspector();
    }

    private static bool _aDown, _sDown, _dDown;

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_isEditMode) return;

        if (_targetManager == null)
        {
            _targetManager = GameObject.FindObjectOfType<RoadManager>();
            if (_targetManager == null) return;
        }

        Event e = Event.current;

        // --- THEO DÕI TRẠNG THÁI PHÍM ---
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.A) { _aDown = true; e.Use(); }
            if (e.keyCode == KeyCode.S) { _sDown = true; e.Use(); }
            if (e.keyCode == KeyCode.D) { _dDown = true; e.Use(); }
        }
        if (e.type == EventType.KeyUp)
        {
            if (e.keyCode == KeyCode.A) _aDown = false;
            if (e.keyCode == KeyCode.S) _sDown = false;
            if (e.keyCode == KeyCode.D) _dDown = false;
        }

        // Bảng hướng dẫn
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 280, 135));
        var style = new GUIStyle(GUI.skin.box);
        style.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.8f));
        style.normal.textColor = Color.white;
        
        GUILayout.BeginVertical(style);
        GUILayout.Label("🛣️ ROAD DRAWING TOOL", EditorStyles.boldLabel);
        GUILayout.Label($"• Giữ A + Click: Vẽ & Nối tiếp {(_aDown ? "✅" : "")}", style);
        GUILayout.Label($"• Giữ S + Click: Rẽ nhánh mới {(_sDown ? "✅" : "")}", style);
        GUILayout.Label($"• Giữ D + Click vào Node cũ: Nối 2 điểm {(_dDown ? "✅" : "")}", style);
        if (_selectedNode != null) GUILayout.Label("ĐANG CHỌN: " + _selectedNode.name, style);
        GUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();

        // Chiếm quyền điều khiển chuột
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(controlID);

        // Xử lý Click
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            WaypointNode hitNode = null;

            // Tìm node dưới chuột
            foreach (var node in _targetManager.roadNodes)
            {
                if (node == null) continue;
                float dist = Vector3.Cross(ray.direction, node.transform.position - ray.origin).magnitude;
                if (dist < 1.2f) { hitNode = node; break; }
            }

            if (_aDown || _sDown)
            {
                Vector3 spawnPos = Vector3.zero;
                Plane ground = new Plane(Vector3.up, Vector3.zero);
                if (ground.Raycast(ray, out float d)) spawnPos = ray.GetPoint(d);

                WaypointNode newNode = CreateNode(spawnPos);
                
                if (_selectedNode != null)
                {
                    Undo.RecordObject(_selectedNode, "Connect Road");
                    _selectedNode.connections.Add(new Connection { targetNode = newNode });
                    EditorUtility.SetDirty(_selectedNode);
                }

                if (_aDown) _selectedNode = newNode;
                e.Use();
                return;
            }

            if (_dDown && _selectedNode != null && hitNode != null && hitNode != _selectedNode)
            {
                Undo.RecordObject(_selectedNode, "Connect Road");
                _selectedNode.connections.Add(new Connection { targetNode = hitNode });
                EditorUtility.SetDirty(_selectedNode);
                e.Use();
                return;
            }

            // Click chọn node thông thường
            if (hitNode != null)
            {
                _selectedNode = hitNode;
                Selection.activeGameObject = hitNode.gameObject;
                e.Use();
            }
        }
    }

    static WaypointNode CreateNode(Vector3 position)
    {
        GameObject newNodeObj;
        if (_targetManager.roadNodePrefab != null)
        {
            newNodeObj = (GameObject)PrefabUtility.InstantiatePrefab(_targetManager.roadNodePrefab);
        }
        else
        {
            newNodeObj = new GameObject("Road_Node_" + _targetManager.roadNodes.Count);
            newNodeObj.AddComponent<WaypointNode>();
        }

        newNodeObj.transform.position = position;
        newNodeObj.transform.SetParent(_targetManager.transform);

        WaypointNode newNode = newNodeObj.GetComponent<WaypointNode>();
        _targetManager.roadNodes.Add(newNode);

        Undo.RegisterCreatedObjectUndo(newNodeObj, "Create Road Node");
        EditorUtility.SetDirty(_targetManager);
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
}
