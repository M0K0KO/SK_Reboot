using UnityEngine;

public static class GameObjectExtensions
{
    public static bool HasInterface<T>(this GameObject obj) where T : class
    {
        return obj.GetComponent<T>() != null;
    }
    
    public static bool HasInterfaceInParent<T>(this GameObject obj) where T : class
    {
        return obj.GetComponentInParent<T>() != null;
    }
    
    public static bool HasInterfaceInChildren<T>(this GameObject obj) where T : class
    {
        return obj.GetComponentInChildren<T>() != null;
    }
    
    
    
    
    public static T GetInterface<T>(this GameObject obj) where T : class
    {
        return obj.GetComponent<T>();
    }
    
    public static T GetInterfaceInParent<T>(this GameObject obj) where T : class
    {
        return obj.GetComponentInParent<T>();
    }
    
    public static T GetInterfaceInChildren<T>(this GameObject obj) where T : class
    {
        return obj.GetComponentInChildren<T>();
    }
}