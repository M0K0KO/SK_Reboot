using UnityEngine;

public interface ISelectable
{
    void OnInteract();

    void OnInteractExit();
    bool CanInteract();
}
