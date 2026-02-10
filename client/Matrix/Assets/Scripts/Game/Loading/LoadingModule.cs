using UnityEngine;
using UnityEngine.UIElements;
using System.Threading.Tasks;
using YooAsset;

public class LoadingModule : MonoBehaviour
{
    private static LoadingModule _instance;
    private VisualElement _root;
    private ProgressBar _progressBar;
    private Label _statusLabel;

    // 静态获取实例：如果不存在，则通过资源系统创建
    public static async Task<LoadingModule> GetInstanceAsync()
    {
        if (_instance != null) return _instance;

        // 1. 从 YooAsset 加载 Loading 界面预制体
        // 假设你把预制体命名为 "LoadingPanel"
        var package = YooAssets.GetPackage("DefaultPackage");
        var handle = package.LoadAssetAsync<GameObject>("LoadingPanel");
        await handle.Task;

        GameObject prefab = handle.AssetObject as GameObject;
        GameObject go = Instantiate(prefab);
        
        // 2. 设置为全局不销毁
        DontDestroyOnLoad(go);
        _instance = go.GetComponent<LoadingModule>();
        
        // 3. 这里的 Awake 会在实例化时自动执行
        return _instance;
    }

    private void Awake()
    {
        _instance = this;
        _root = GetComponent<UIDocument>().rootVisualElement;
        _progressBar = _root.Q<ProgressBar>("LoadProgressBar");
        _statusLabel = _root.Q<Label>("StatusLabel");
        
        // 初始隐藏
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (_root != null)
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public async Task LoadSceneWithProgress(string sceneName)
    {
        SetVisible(true);
        var package = YooAssets.GetPackage("DefaultPackage");
        var handle = package.LoadSceneAsync(sceneName);

        while (!handle.IsDone)
        {
            float progress = handle.Progress * 100;
            _progressBar.value = progress;
            _statusLabel.text = $"进入场景中... {(int)progress}%";
            await Task.Yield();
        }

        await handle.Task;
        await Task.Delay(200); // 稍微停顿平滑过渡
        SetVisible(false);
    }
}
