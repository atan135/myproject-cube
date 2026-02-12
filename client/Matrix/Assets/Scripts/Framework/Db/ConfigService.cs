using System.Collections.Generic;
using System.IO;
using SQLite;
using UnityEngine;
using ConfigEntities;
using System.Diagnostics; // 引用你生成的命名空间
using Debug = UnityEngine.Debug;

public class ConfigService
{
    private static ConfigService _instance;
    public static ConfigService Instance => _instance ??= new ConfigService();

    private SQLiteConnection _db;
    private string _currentLocale;


    // 使用嵌套字典：外层 Key 是类名(Type)，内层 Key 是数据的 Id
    private Dictionary<System.Type, object> _tableCaches = new Dictionary<System.Type, object>();

    // 用于标记哪些表需要存入内存（常用表列表）
    private readonly System.Type[] _preloadTables = {
        typeof(TestTable_001Entity),
        typeof(TestTable_002Entity) 
        // 将你认为需要高频访问的类放进来
    };

    // 针对常用小表的内存缓存
    private Dictionary<int, TestTable_001Entity> _cacheTable001; 

    public void Init(string locale = "zh_CN")
    {
        Stopwatch sw = new Stopwatch();
        sw.Start();
        _currentLocale = locale;
        string dbName = $"{locale}.db";
        
        // 移动端 StreamingAssets 路径处理
        string sourcePath = Path.Combine(Application.streamingAssetsPath, "Generated_Dbs", dbName);
        string destPath = Path.Combine(Application.persistentDataPath, dbName);

        // SQLite 在 Android 上不能直接读取压缩的 APK 包内文件，需拷贝到持久化目录
        if (Application.platform == RuntimePlatform.Android)
        {
            if (!File.Exists(destPath)) 
            {
                var loading = UnityEngine.Networking.UnityWebRequest.Get(sourcePath);
                loading.SendWebRequest();
                while (!loading.isDone) { } // 简单同步处理
                File.WriteAllBytes(destPath, loading.downloadHandler.data);
            }
        }
        else
        {
            destPath = sourcePath; // PC/iOS 直接读取
        }

        // 优化：使用只读模式和缓存配置
        _db = new SQLiteConnection(destPath, SQLiteOpenFlags.ReadOnly | SQLiteOpenFlags.FullMutex);
        _db.Execute("PRAGMA cache_size = -4000;"); // 4MB 缓存
        _db.Execute("PRAGMA mmap_size = 10485760;"); // 10MB 内存映射
        
        Debug.Log($"ConfigService 初始化完成: {locale}");

        Debug.Log("开始后处理，预加载常用表...");
        PreloadCommonTables();
        sw.Stop();

        long totalMs = sw.ElapsedMilliseconds;

        Debug.Log($"<color=yellow>【随机查询结果】</color>");
        Debug.Log($"总耗时: {totalMs} ms");

        // --- 测试 2: 内存缓存对比 (如果你做了 Dictionary 缓存) ---
        // 这里可以对比测试你的 GetTable001(id) 方法

        sw.Reset();
    }

    // --- 查询接口 A：针对大表（直接查磁盘，不占内存） ---
    public T GetById<T>(int id) where T : IConfigEntity, new()
    {
        var type = typeof(T);

        // 1. 尝试从内存缓存读取 (O(1))
        if (_tableCaches.TryGetValue(type, out var dictObj))
        {
            var dict = (Dictionary<int, T>)dictObj;
            if (dict.TryGetValue(id, out var val))
                return val;
            return default; // 内存表里没有，说明真的没有
        }
        // 获取表名（去掉 Entity 后缀）
        string tableName = typeof(T).Name.Replace("Entity", "");

        // 使用 Query 直接运行 SQL，速度最快
        // 占位符 '?' 会由底层 C 库直接绑定，无需 C# 解析表达式
        var list = _db.Query<T>($"SELECT * FROM \"{tableName}\" WHERE Id = ?", id);

        return list.Count > 0 ? list[0] : default;
    }


    public TestTable_001Entity GetTable001(int id)
    {
        return _cacheTable001.TryGetValue(id, out var val) ? val : null;
    }

    public void Close()
    {
        _db?.Close();
        _db = null;
    }

    public void SwitchLanguage(string newLocale)
    {
        ConfigService.Instance.Close();
        ConfigService.Instance.Init(newLocale);
        ConfigService.Instance.PreloadCommonTables();

        // 触发 UI 刷新事件
        // EventSystem.Trigger(LanguageChangedEvent);
    }

    public void PreloadCommonTables()
    {
        _tableCaches.Clear();

        foreach (var type in _preloadTables)
        {
            // 动态获取表名
            string tableName = type.Name.Replace("Entity", "");

            // 利用反射调用 SQLite 的 Query 方法获取全表数据
            // 注意：ToList 会立即触发磁盘 IO 并分配内存
            var method = typeof(SQLiteConnection).GetMethod("Query", new[] { typeof(string), typeof(object[]) });
            var genericMethod = method.MakeGenericMethod(type);
            var list = genericMethod.Invoke(_db, new object[] { $"SELECT * FROM \"{tableName}\"", new object[0] }) as System.Collections.IList;

            if (list != null)
            {
                // 构建 Dictionary<int, T>
                var dictType = typeof(Dictionary<,>).MakeGenericType(typeof(int), type);
                var dict = System.Activator.CreateInstance(dictType) as System.Collections.IDictionary;

                foreach (var item in list)
                {
                    // 假设你的 Entity 都有 Id 字段
                    var id = (int)type.GetProperty("Id").GetValue(item);
                    dict[id] = item;
                }

                _tableCaches[type] = dict;
                Debug.Log($"[Config] 预加载表 {tableName} 完成，条目数: {list.Count}");
            }
        }
    }
}