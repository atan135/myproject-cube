using UnityEngine;

namespace Cube.Game.Character
{
    /// <summary>
    /// 角色控制器基类
    /// </summary>
    public class CharacterController : MonoBehaviour
    {
        [Header("Character Properties")]
        public float moveSpeed = 5f;
        public float health = 100f;
        public float maxHealth = 100f;

        protected virtual void Start()
        {
            health = maxHealth;
        }

        protected virtual void Update()
        {
            HandleMovement();
        }

        protected virtual void HandleMovement()
        {
            // 移动逻辑将在子类中实现
        }

        public virtual void TakeDamage(float damage)
        {
            health = Mathf.Max(0, health - damage);
            if (health <= 0)
            {
                OnDeath();
            }
        }

        protected virtual void OnDeath()
        {
            // 死亡逻辑
        }
    }
}
