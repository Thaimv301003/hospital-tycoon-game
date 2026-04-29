using UnityEngine;
using System.Collections.Generic;

public class RoadManager : MonoBehaviour
{
    public static RoadManager Instance;

    [Header("Settings")]
    public GameObject roadNodePrefab;
    
    [Header("Road Graph")]
    public List<WaypointNode> roadNodes = new List<WaypointNode>();
    public WaypointNode entryNode; // Điểm đầu tiên xe xuất hiện
    
    void Awake()
    {
        Instance = this;
        // Tự động tìm các node con nếu có sẵn trong Scene
        RefreshRoadNodes();
    }

    public void RefreshRoadNodes()
    {
        roadNodes.Clear();
        WaypointNode[] nodes = GetComponentsInChildren<WaypointNode>();
        roadNodes.AddRange(nodes);
        if (roadNodes.Count > 0 && entryNode == null) entryNode = roadNodes[0];
    }

    public WaypointNode GetNodeAtPosition(Vector3 position, float radius = 0.5f)
    {
        foreach (var node in roadNodes)
        {
            if (node == null) continue;
            if (Vector3.Distance(node.transform.position, position) < radius) return node;
        }
        return null;
    }
}
