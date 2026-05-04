using UnityEngine;

namespace JxModule
{
    public static class DebugExtension
    {
        public static void LogColor(string logText, Color color)
        {
            var colorHex = ColorUtility.ToHtmlStringRGB(color);
            Debug.Log($"<color=#{colorHex}>{logText}</color>");
        }
    }
}