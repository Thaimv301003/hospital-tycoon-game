using UnityEngine;
using System.Collections.Generic;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance;

    public WaypointNode rootNode;
    public GameObject waypointPrefab;
    public List<WaypointNode> allNodes = new List<WaypointNode>();

    void Awake()
    {
        Instance = this;
        
        // Nếu danh sách trống (có thể do user manual add node), tự động làm mới
        if (allNodes == null || allNodes.Count == 0)
        {
            RefreshAllNodes();
        }

        // Tự động đăng ký các kết nối ngược để hỗ trợ logic nếu cần
        foreach (var node in allNodes)
        {
            if (node != null) node.RegisterIncoming();
        }
    }

    public void RefreshAllNodes()
    {
        allNodes.Clear();
        WaypointNode[] nodesInScene = GameObject.FindObjectsByType<WaypointNode>(FindObjectsSortMode.None);
        allNodes.AddRange(nodesInScene);
        Debug.Log($"[PathManager] Đã làm mới danh sách. Hiện đang quản lý {allNodes.Count} Waypoint Node.");
    }

    public WaypointNode GetClosestNode(Vector3 position)
    {
        WaypointNode closest = null;
        float minDist = float.MaxValue;
        foreach (var node in allNodes)
        {
            if (node == null) continue;
            float dist = Vector3.Distance(node.transform.position, position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = node;
            }
        }
        return closest;
    }

    public WaypointNode GetNodeForRoom(RoomController room)
    {
        foreach (var node in allNodes)
        {
            if (node != null && node.connectedRoom == room) return node;
        }
        return null;
    }

    // Thuật toán tìm đường BFS (Breadth-First Search) cho đồ thị Waypoint
    public Queue<WaypointNode> FindPath(WaypointNode start, WaypointNode end)
    {
        if (start == null || end == null) return null;
        if (start == end) return new Queue<WaypointNode>(new[] { end });

        Queue<WaypointNode> frontier = new Queue<WaypointNode>();
        frontier.Enqueue(start);

        Dictionary<WaypointNode, WaypointNode> cameFrom = new Dictionary<WaypointNode, WaypointNode>();
        cameFrom[start] = null;

        bool found = false;
        while (frontier.Count > 0)
        {
            WaypointNode current = frontier.Dequeue();
            if (current == end)
            {
                found = true;
                break;
            }

            foreach (var conn in current.connections)
            {
                if (conn.targetNode != null && !cameFrom.ContainsKey(conn.targetNode))
                {
                    frontier.Enqueue(conn.targetNode);
                    cameFrom[conn.targetNode] = current;
                }
            }
        }

        if (!found) return null;

        // Xây dựng lại đường đi từ end về start
        List<WaypointNode> pathList = new List<WaypointNode>();
        WaypointNode temp = end;
        while (temp != null)
        {
            pathList.Add(temp);
            temp = cameFrom[temp];
        }
        pathList.Reverse();

        return new Queue<WaypointNode>(pathList);
    }
}