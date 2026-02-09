using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 资源注册")]
    [Tooltip("在这里配置 UI 名字与 UXML 文件的对应关系")]
    public List<UIScreenMapping> uiConfigs;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private Dictionary<string, VisualElement> _activeScreens = new Dictionary<string, VisualElement>();

    [System.Serializable]
    public struct UIScreenMapping {
        public string screenName;
        public VisualTreeAsset asset;
    }

    private void Awake() {
        Instance = this;
        _uiDocument = GetComponent<UIDocument>();
        _root = _uiDocument.rootVisualElement;
    }

    public VisualElement OpenScreen(string screenName) {
        if (_activeScreens.ContainsKey(screenName)) return _activeScreens[screenName];

        // 查找配置的资源
        var config = uiConfigs.Find(x => x.screenName == screenName);
        if (config.asset == null) {
            Debug.LogError($"[UIManager] 未在 Inspector 中注册 UI: {screenName}");
            return null;
        }

        VisualElement screen = config.asset.Instantiate();
        screen.style.flexGrow = 1; 
        _root.Add(screen);
        
        _activeScreens.Add(screenName, screen);
        return screen;
    }

    public void CloseScreen(string screenName) {
        if (_activeScreens.TryGetValue(screenName, out var screen)) {
            _root.Remove(screen);
            _activeScreens.Remove(screenName);
        }
    }
}
