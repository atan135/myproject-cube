using UnityEngine;
using UnityEngine.UIElements;

namespace Cube.Framework.UI
{
    /// <summary>
    /// UI基类
    /// 所有UI界面都应该继承此类
    /// </summary>
    public abstract class UIBase : MonoBehaviour
    {
        protected VisualElement _root;
        protected UIDocument _uiDocument;
        
        /// <summary>
        /// UI名称
        /// </summary>
        public abstract string UIName { get; }
        
        /// <summary>
        /// UI层级
        /// </summary>
        public virtual int UILayer => 0;
        
        /// <summary>
        /// 是否全屏UI
        /// </summary>
        public virtual bool IsFullScreen => true;
        
        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError($"UI Document component not found on {gameObject.name}");
                return;
            }
            
            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError($"Root VisualElement not found on {gameObject.name}");
                return;
            }
            
            OnInit();
        }
        
        private void Start()
        {
            OnShow();
        }
        
        private void OnDestroy()
        {
            OnHide();
            OnDispose();
        }
        
        /// <summary>
        /// 初始化UI（Awake时调用）
        /// </summary>
        protected virtual void OnInit()
        {
            // 子类重写此方法进行初始化
        }
        
        /// <summary>
        /// 显示UI时调用
        /// </summary>
        protected virtual void OnShow()
        {
            gameObject.SetActive(true);
            // 子类重写此方法进行显示逻辑
        }
        
        /// <summary>
        /// 隐藏UI时调用
        /// </summary>
        protected virtual void OnHide()
        {
            gameObject.SetActive(false);
            // 子类重写此方法进行隐藏逻辑
        }
        
        /// <summary>
        /// 销毁UI时调用
        /// </summary>
        protected virtual void OnDispose()
        {
            // 子类重写此方法进行清理工作
        }
        
        /// <summary>
        /// 显示UI
        /// </summary>
        public void Show()
        {
            OnShow();
        }
        
        /// <summary>
        /// 隐藏UI
        /// </summary>
        public void Hide()
        {
            OnHide();
        }
        
        /// <summary>
        /// 查找UI元素
        /// </summary>
        protected T FindElement<T>(string name) where T : VisualElement
        {
            return _root.Q<T>(name);
        }
        
        /// <summary>
        /// 注册按钮点击事件
        /// </summary>
        protected void RegisterButtonClick(string buttonName, System.Action callback)
        {
            var button = FindElement<Button>(buttonName);
            if (button != null && callback != null)
            {
                button.clicked += callback;
            }
        }
    }
}