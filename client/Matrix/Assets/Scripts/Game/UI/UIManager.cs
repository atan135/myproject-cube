using UnityEngine;

namespace Cube.Game.UI
{
    /// <summary>
    /// UI管理器
    /// 负责管理所有UI界面
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<UIManager>();
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 显示UI界面
        /// </summary>
        public void ShowUI(string uiName)
        {
            // UI显示逻辑
        }

        /// <summary>
        /// 隐藏UI界面
        /// </summary>
        public void HideUI(string uiName)
        {
            // UI隐藏逻辑
        }
    }
}
