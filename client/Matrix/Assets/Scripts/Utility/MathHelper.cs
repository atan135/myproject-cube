using UnityEngine;

namespace Cube.Utility
{
    /// <summary>
    /// 数学工具类
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        public static float Distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        /// <summary>
        /// 检查点是否在范围内
        /// </summary>
        public static bool IsInRange(Vector3 point, Vector3 center, float range)
        {
            return Distance(point, center) <= range;
        }
    }
}
