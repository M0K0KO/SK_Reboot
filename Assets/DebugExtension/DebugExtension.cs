using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Moko
{
    public static class DebugExtension
    {
        [Conditional("UNITY_EDITOR")]
        public static void ColorLog(this string message, string color)
        {
            Debug.Log($"<color={color}>{message}</color>");
        }

        [Conditional("UNITY_EDITOR")]
        public static void BoldLog(this string message)
        {
            Debug.Log($"<b>{message}</b>");
        }

        [Conditional("UNITY_EDITOR")]
        public static void ItalicLog(this string message)
        {
            Debug.Log($"<i>{message}</i>");
        }

        [Conditional("UNITY_EDITOR")]
        public static void Log(this object obj, string message, string color = "white")
        {
            if (obj is UnityEngine.Object context)
            {
                Debug.Log($"<color={color}>{message}</color>", context);
            }
            else
            {
                Debug.Log($"<color={color}>[{obj.GetType().Name}] {message}</color>");
            }
        }
        
        [Conditional("UNITY_EDITOR")]
        public static void LogWarning(this object obj, string message)
        {
            if (obj is UnityEngine.Object context)
            {
                Debug.LogWarning(message, context);
            }
            else
            {
                Debug.LogWarning($"[{obj.GetType().Name}] {message}");
            }
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogError(this object obj, string message)
        {
            if (obj is UnityEngine.Object context)
            {
                Debug.LogError(message, context);
            }
            else
            {
                Debug.LogError($"[{obj.GetType().Name}] {message}");
            }
        }
    }
}
