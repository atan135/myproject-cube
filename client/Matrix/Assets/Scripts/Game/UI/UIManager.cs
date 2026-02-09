using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Threading.Tasks;
using YooAsset; // 引用 YooAsset 命名空间

[RequireComponent(typeof(UIDocument))]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private UIDocument _uiDocument;
    private VisualElement _root;
    
    // 缓存 Handle 用于卸载
    private Dictionary<string, AssetHandle> _handles = new Dictionary<string, AssetHandle>();
    private Dictionary<string, VisualElement> _activeScreens = new Dictionary<string, VisualElement>();

    private void Awake()
    {
        Instance = this;
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;
    }

    /// <summary>
    /// 使用 YooAsset 异步加载 UI
    /// </summary>
    /// <param name="location">资源定位地址（通常是 UXML 的名称或标签）</param>
    public async Task<VisualElement> OpenScreen(string location, System.Action<VisualElement> onComplete = null)
    {
        if (_activeScreens.ContainsKey(location)) return _activeScreens[location];

        var package = YooAssets.GetPackage("DefaultPackage");
        AssetHandle handle = package.LoadAssetAsync<VisualTreeAsset>(location);
        _handles[location] = handle;

        await handle.Task; 

        if (handle.AssetObject != null)
        {
            VisualTreeAsset uxml = handle.AssetObject as VisualTreeAsset;
            VisualElement screen = uxml.Instantiate();
            screen.style.flexGrow = 1;
            _root.Add(screen);
        
            _activeScreens.Add(location, screen);
            onComplete?.Invoke(screen);
            return screen; // 现在可以返回对象了
        }
    
        return null;
    }

    /// <summary>
    /// 关闭 UI 并销毁资源句柄
    /// </summary>
    public void CloseScreen(string location)
    {
        if (_activeScreens.TryGetValue(location, out var screen))
        {
            _root.Remove(screen);
            _activeScreens.Remove(location);

            // 3. 释放 YooAsset 句柄
            if (_handles.TryGetValue(location, out var handle))
            {
                handle.Dispose(); // 释放内存
                _handles.Remove(location);
            }
        }
    }
}
