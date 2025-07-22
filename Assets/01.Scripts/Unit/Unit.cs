using System;
using UnityEngine;

// Unit의 index와 GameObject Reference들만 갖고있음
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
