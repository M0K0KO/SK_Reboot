using UnityEngine;

public class PlayerTransformManager : MonoBehaviour
{
    public static PlayerTransformManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}