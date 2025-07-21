using System;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public int dataIndex = -1;
    public GameObject selectionCircle;
    
    
    private void OnEnable()
    {
        dataIndex = UnitDataManager.Instance.RegisterUnit(this);
    }

    private void OnDisable()
    {
        if (UnitDataManager.Instance != null)
            UnitDataManager.Instance.UnregisterUnit(dataIndex);
    }
}
