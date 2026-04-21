using UnityEngine;
using System.Collections.Generic;

public class QueueManager : MonoBehaviour
{
    [Header("Cấu hình Hàng đợi")]
    public float spacing = 1.2f;
    public Vector3 queueDirection = new Vector3(0, 0, -1);

    public List<CharacterNavigator> patientsInLine = new List<CharacterNavigator>();

    public void JoinQueue(CharacterNavigator patient)
    {
        if (!patientsInLine.Contains(patient)) patientsInLine.Add(patient);
    }

    public void LeaveQueue(CharacterNavigator patient)
    {
        if (patientsInLine.Contains(patient)) patientsInLine.Remove(patient);
    }

    public Vector3 GetQueuePosition(CharacterNavigator patient)
    {
        int index = patientsInLine.IndexOf(patient);
        if (index <= 0) return transform.position;
        return transform.position + (transform.rotation * queueDirection * spacing * index);
    }

    public bool IsFirstInLine(CharacterNavigator patient)
    {
        if (patientsInLine.Count == 0) return false;
        return patientsInLine[0] == patient;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = transform.position + (transform.rotation * queueDirection * spacing * i);
            Gizmos.DrawWireSphere(pos, 0.3f);
        }
    }
}