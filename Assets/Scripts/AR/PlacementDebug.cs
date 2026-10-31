using UnityEngine;

namespace ARProductViewer.AR
{
    /// <summary>
    /// TEMPORARY on-device diagnostics for placement accuracy. Filter logcat with "[Placement]".
    /// Remove once placement is confirmed on device.
    /// </summary>
    static class PlacementDebug
    {
        public static bool Enabled = true;

        public static void Log(string message)
        {
            if (Enabled)
                Debug.Log("[Placement] " + message);
        }

        public static string Format(Vector3 v) => $"({v.x:F3}, {v.y:F3}, {v.z:F3})";
    }
}
