using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class Connection
{
    public WaypointNode targetNode;
    [Range(-10, 10)] public float curveStrength = 0f;

    public Vector3 GetControlPoint(Vector3 startPos)
    {
        if (targetNode == null) return startPos;
        Vector3 endPos = targetNode.transform.position;
        Vector3 mid = Vector3.Lerp(startPos, endPos, 0.5f);
        Vector3 dir = (endPos - startPos).normalized;
        Vector3 perpendicular = new Vector3(-dir.z, 0, dir.x);
        return mid + perpendicular * curveStrength;
    }
}

public class WaypointNode : MonoBehaviour
{
    [Header("Tương tác Phòng")]
    public RoomController connectedRoom;
    
    [Header("Kết nối Đồ thị")]
    public List<Connection> connections = new List<Connection>();
    [HideInInspector] public List<WaypointNode> incomingNodes = new List<WaypointNode>();

    [Header("Cấu hình Node")]
    public bool isBranch = false;
    public bool forceReturnAfterTreatment = true; // NPC có cần quay lại đây sau khi khám xong ko?
    public float waitTime = 0f;
    [Range(0, 5)] public float cornerSize = 1.0f;

    [Header("Hệ thống Chuẩn Đoán (Legacy)")] 
    public bool isDiagnosisNode = false;

    public static Vector3 CalculateBezier(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return Mathf.Pow(1 - t, 2) * p0 + 2 * (1 - t) * t * p1 + Mathf.Pow(t, 2) * p2;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = (isBranch ? Color.yellow : Color.cyan);
        Gizmos.DrawSphere(transform.position, 0.2f);

        if (connections == null) return;
        for (int i = 0; i < connections.Count; i++)
        {
            var conn = connections[i];
            if (conn.targetNode == null) continue;
            
            Gizmos.color = Color.green;
            Vector3 p0 = transform.position;
            Vector3 p1 = conn.GetControlPoint(p0);
            Vector3 p2 = conn.targetNode.transform.position;

            Vector3 lastP = p0;
            int segments = (conn.curveStrength == 0) ? 1 : 15;
            for (int j = 1; j <= segments; j++)
            {
                Vector3 nextP = CalculateBezier(j / (float)segments, p0, p1, p2);
                Gizmos.DrawLine(lastP, nextP);
                lastP = nextP;
            }

            // Vẽ mũi tên chỉ hướng ở giữa đoạn kết nối
            Vector3 arrowPos = CalculateBezier(0.5f, p0, p1, p2);
            Vector3 arrowDir = (p2 - p0).normalized;
            if (arrowDir != Vector3.zero)
            {
                Gizmos.DrawLine(arrowPos, arrowPos - (Quaternion.Euler(0, 150, 0) * arrowDir) * 0.3f);
                Gizmos.DrawLine(arrowPos, arrowPos - (Quaternion.Euler(0, -150, 0) * arrowDir) * 0.3f);
            }
        }

        if (connectedRoom != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, connectedRoom.transform.position);
        }
    }

    public void RegisterIncoming()
    {
        foreach (var conn in connections)
        {
            if (conn.targetNode != null && !conn.targetNode.incomingNodes.Contains(this))
            {
                conn.targetNode.incomingNodes.Add(this);
            }
        }
    }
}