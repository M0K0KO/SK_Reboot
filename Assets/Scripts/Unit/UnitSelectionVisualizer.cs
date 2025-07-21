using System;
using Unity.Collections;
using UnityEngine;

public class UnitSelectionVisualizer : MonoBehaviour
{
    private bool[] wasSelectedLastFrame; 
    private UnitDataManager dataManager;

    void Start()
    {
        dataManager = UnitDataManager.Instance;
        wasSelectedLastFrame = new bool[dataManager.isSelected.Length];
    }

    void LateUpdate()
    {
        NativeArray<bool> isSelected = dataManager.isSelected;
        
        for (int i = 0; i < isSelected.Length; i++)
        {
            if (isSelected[i] != wasSelectedLastFrame[i])
            {
                if (i < dataManager.unitReferences.Count) 
                {
                    Unit unit = dataManager.unitReferences[i];
                    unit.selectionCircle.SetActive(isSelected[i]);
                }
            }
        }
        
        isSelected.CopyTo(wasSelectedLastFrame);
    }
}