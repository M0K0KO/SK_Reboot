using System;
using UnityEngine;

public class Tower : MonoBehaviour, ISelectable
{
    public MeshRenderer meshRenderer;
    
    public void OnInteract()
    {
        meshRenderer.material.color = Color.red;
        Debug.Log("Interact");
    }

    public void OnInteractExit()
    {
        meshRenderer.material.color = Color.blue;
    }
    
    public bool CanInteract()
    {
        return true;
    }


    private void DrawGizmo()
    {
    }
}
