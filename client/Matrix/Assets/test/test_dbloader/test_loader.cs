using ConfigEntities;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
public class test_loader : MonoBehaviour
{
    public string tablename;
    public int row;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            ConfigService.Instance.Init("zh_CN");
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            RunStressTest();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            RunStressTest2();
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            ConfigService.Instance.SwitchLanguage("en_US");
        }
        if(Input.GetKeyDown(KeyCode.E))
        {
            var data = ConfigService.Instance.GetById<TestTable_001Entity>(row);
            if (data != null)
            {
                Debug.Log($"查询 {tablename} 表，ID={row} 的结果: {Newtonsoft.Json.JsonConvert.SerializeObject(data)}");
            }
            else
            {
                Debug.LogWarning($"未找到 {tablename} 表中 ID={row} 的数据");
            }
        }
    }

    void RunStressTest()
    {
        int testCount = 10000;
        // 假设你的大表 ID 范围是 1000 - 21000
        int minId = 1000;
        int maxId = 21000;

        Debug.Log($"<color=cyan>=== 开始 SQL 查询压测 (次数: {testCount}) ===</color>");

        Stopwatch sw = new Stopwatch();

        // --- 测试 1: 随机主键查询 (最模拟真实环境) ---
        sw.Start();
        for (int i = 0; i < testCount; i++)
        {
            int randomId = Random.Range(minId, maxId);
            // 替换为你的一张 20000 行的大表 Entity 名
            var data = ConfigService.Instance.GetById<TestTable_199Entity>(randomId);

            if (data == null)
            {
                // 仅用于调试，正式测试建议注销此行防止 Log 影响性能
                // Debug.LogWarning($"未找到 ID: {randomId}");
            }
        }
        sw.Stop();

        long totalMs = sw.ElapsedMilliseconds;
        float avgMs = (float)totalMs / testCount;

        Debug.Log($"<color=yellow>【随机查询结果】</color>");
        Debug.Log($"总耗时: {totalMs} ms");
        Debug.Log($"单次平均: {avgMs:F4} ms");
        Debug.Log($"每秒可处理 (TPS): {(1000f / avgMs):F0} 次");

        // --- 测试 2: 内存缓存对比 (如果你做了 Dictionary 缓存) ---
        // 这里可以对比测试你的 GetTable001(id) 方法

        sw.Reset();
    }
    void RunStressTest2()
    {
        int testCount = 10000;
        // 假设你的大表 ID 范围是 1000 - 21000
        int minId = 1000;
        int maxId = 1200;

        Debug.Log($"<color=cyan>=== 开始 SQL 查询压测 (次数: {testCount}) ===</color>");

        Stopwatch sw = new Stopwatch();

        // --- 测试 1: 随机主键查询 (最模拟真实环境) ---
        sw.Start();
        for (int i = 0; i < testCount; i++)
        {
            int randomId = Random.Range(minId, maxId);
            // 替换为你的一张 20000 行的大表 Entity 名
            var data = ConfigService.Instance.GetById<TestTable_001Entity>(randomId);

            if (data == null)
            {
                // 仅用于调试，正式测试建议注销此行防止 Log 影响性能
                // Debug.LogWarning($"未找到 ID: {randomId}");
            }
        }
        sw.Stop();

        long totalMs = sw.ElapsedMilliseconds;
        float avgMs = (float)totalMs / testCount;

        Debug.Log($"<color=yellow>【随机查询结果】</color>");
        Debug.Log($"总耗时: {totalMs} ms");
        Debug.Log($"单次平均: {avgMs:F4} ms");
        Debug.Log($"每秒可处理 (TPS): {(1000f / avgMs):F0} 次");

        // --- 测试 2: 内存缓存对比 (如果你做了 Dictionary 缓存) ---
        // 这里可以对比测试你的 GetTable001(id) 方法

        sw.Reset();
    }
}
