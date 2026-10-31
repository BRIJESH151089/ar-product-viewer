using ARProductViewer.Core;
using UnityEngine;

namespace ARProductViewer.AR
{
    /// <summary>
    /// Converts between Unity types and the engine-agnostic Core types.
    /// </summary>
    static class CoreConversions
    {
        public static Float3 ToFloat3(this Vector3 v) => new Float3(v.x, v.y, v.z);

        public static Vector3 ToVector3(this Float3 v) => new Vector3(v.X, v.Y, v.Z);

        public static Pose ToUnityPose(this PlacementPose pose) =>
            new Pose(pose.Position.ToVector3(), Quaternion.Euler(0f, pose.YawDegrees, 0f));
    }
}
