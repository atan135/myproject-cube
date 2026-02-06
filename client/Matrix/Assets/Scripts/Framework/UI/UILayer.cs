namespace Cube.Framework.UI
{
    /// <summary>
    /// UI层级定义
    /// </summary>
    public enum UILayer
    {
        /// <summary>
        /// 背景层（最低层级）
        /// </summary>
        Background = 0,
        
        /// <summary>
        /// 普通界面层
        /// </summary>
        Normal = 100,
        
        /// <summary>
        /// 弹窗层
        /// </summary>
        Popup = 200,
        
        /// <summary>
        /// 提示层
        /// </summary>
        Tips = 300,
        
        /// <summary>
        /// 加载层
        /// </summary>
        Loading = 400,
        
        /// <summary>
        /// 系统提示层（最高层级）
        /// </summary>
        System = 500
    }
}