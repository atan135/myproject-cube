using UnityEngine;

namespace Cube.Utility
{
    /// <summary>
    /// 扩展方法工具类
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// 检查GameObject是否在指定层级
        /// </summary>
        public static bool IsInLayer(this GameObject gameObject, int layer)
        {
            return gameObject.layer == layer;
        }

        /// <summary>
        /// 获取组件，如果不存在则添加
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
    }
}
