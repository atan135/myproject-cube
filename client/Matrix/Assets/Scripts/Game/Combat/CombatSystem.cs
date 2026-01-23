using UnityEngine;

namespace Cube.Game.Combat
{
    /// <summary>
    /// 战斗系统
    /// 负责处理战斗逻辑、伤害计算、技能释放
    /// </summary>
    public class CombatSystem : MonoBehaviour
    {
        /// <summary>
        /// 计算伤害
        /// </summary>
        public static float CalculateDamage(float baseDamage, float defense)
        {
            return Mathf.Max(1, baseDamage - defense);
        }

        /// <summary>
        /// 应用伤害
        /// </summary>
        public static void ApplyDamage(GameObject target, float damage)
        {
            // 伤害应用逻辑
            Debug.Log($"Applying {damage} damage to {target.name}");
        }
    }
}
