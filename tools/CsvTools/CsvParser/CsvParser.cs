// 核心逻辑：根据 CSV 生成 C# 类字符串
using CsvHelper;
using CsvHelper.Configuration;
using SQLite;
using System;
using System.Globalization;
using System.Text;
public static class CsvParser
{
    // 配置路径（根据实际情况修改）
    const string INPUT_ROOT = "./Generated_L10N";
    const string OUTPUT_ENTITIES = "./Generated_Entities";
    const string OUTPUT_DBS = "./Generated_Dbs";
    const string SCHEMA_LOCALE = "en_US"; // 以英文版作为生成代码的基准
    public static void GenerateEntityFiles()
    {
        Console.WriteLine("🚀 开始处理配置表...");

        // 1. Init: 清理并创建目录
        if (Directory.Exists(OUTPUT_ENTITIES)) Directory.Delete(OUTPUT_ENTITIES, true);
        if (Directory.Exists(OUTPUT_DBS)) Directory.Delete(OUTPUT_DBS, true);
        Directory.CreateDirectory(OUTPUT_ENTITIES);
        Directory.CreateDirectory(OUTPUT_DBS);

        var localeDirs = Directory.GetDirectories(INPUT_ROOT);
        var schemaPath = Path.Combine(INPUT_ROOT, SCHEMA_LOCALE);

        // 2. Step 1: 遍历基准目录生成 Entity_csv.cs
        GenerateAllEntities(schemaPath);

        // 3. Step 2 & 3: 遍历语言文件夹生成 .db
        foreach (var dir in localeDirs)
        {
            string localeName = Path.GetFileName(dir);
            string dbPath = Path.Combine(OUTPUT_DBS, $"{localeName}.db");
            Console.WriteLine($"\n📦 正在生成数据库: {localeName}.db");

            ProcessLocale(dir, dbPath);
        }

        Console.WriteLine("\n✅ 所有任务完成！");
    }
    static void GenerateAllEntities(string schemaPath)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// 自动生成，请勿手动修改\nusing System;\nusing SQLite;\n\nnamespace ConfigEntities\n{");

        foreach (var file in Directory.GetFiles(schemaPath, "*.csv"))
        {
            string tableName = Path.GetFileNameWithoutExtension(file);
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read(); string[] names = csv.Context.Parser.Record;
            csv.Read(); string[] types = csv.Context.Parser.Record;
            sb.AppendLine($"    [Table(\"{tableName}\")] // 强制映射到 CSV 的原始表名");
            sb.AppendLine($"    public class {tableName}Entity : IConfigEntity\n    {{");
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i].ToLower() == "id") sb.AppendLine("        [PrimaryKey]");
                sb.AppendLine($"        public {MapType(types[i])} {names[i]} {{ get; set; }}");
            }
            sb.AppendLine("    }\n");
        }

        sb.AppendLine("}");
        File.WriteAllText(Path.Combine(OUTPUT_ENTITIES, "Entity_csv.cs"), sb.ToString());
        Console.WriteLine("📝 已生成 Entity_csv.cs");
    }

    static void ProcessLocale(string sourceDir, string dbPath)
    {
        using var db = new SQLiteConnection(dbPath);

        foreach (var file in Directory.GetFiles(sourceDir, "*.csv"))
        {
            string tableName = Path.GetFileNameWithoutExtension(file);
            using var reader = new StreamReader(file);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = false });

            // 解析结构
            csv.Read(); string[] names = csv.Context.Parser.Record;
            csv.Read(); string[] types = csv.Context.Parser.Record;

            // 动态创建表结构 (SQLite 简单建表)
            string createTableSql = $"CREATE TABLE \"{tableName}\" (" +
                string.Join(", ", names.Select((n, i) => $"\"{n}\" {MapToSqlType(types[i])} {(n.ToLower() == "id" ? "PRIMARY KEY" : "")}")) + ")";
            db.Execute(createTableSql);

            // 批量插入
            db.BeginTransaction();
            int rowIdx = 3; // 数据从第3行开始
            try
            {
                string insertSql = $"INSERT INTO \"{tableName}\" ({string.Join(",", names.Select(n => $"\"{n}\""))}) VALUES ({string.Join(",", names.Select(_ => "?"))})";

                while (csv.Read())
                {
                    var row = csv.Context.Parser.Record;
                    // 数据校验
                    for (int j = 0; j < types.Length; j++)
                    {
                        if (!Validate(row[j], types[j]))
                            Console.WriteLine($"❌ [校验错误] {tableName}-{rowIdx}-{names[j]} : 值 \"{row[j]}\" 不是有效的 {types[j]}");
                    }
                    db.Execute(insertSql, row);
                    rowIdx++;
                }
                db.Commit();
            }
            catch (Exception ex)
            {
                db.Rollback();
                Console.WriteLine($"💥 {tableName} 导出失败: {ex.Message}");
            }
        }

        // Step 3: 优化
        db.Execute("VACUUM");
        Console.WriteLine($"✨ {Path.GetFileName(dbPath)} 优化完成 (VACUUM)");
    }

    static string MapType(string csvType) => csvType.ToLower() switch
    {
        "int" => "int",
        "int64" => "long",
        "float" => "float",
        _ => "string"
    };

    static string MapToSqlType(string csvType) => csvType.ToLower() switch
    {
        "int" or "int64" => "INTEGER",
        "float" => "REAL",
        _ => "TEXT"
    };
     
    static bool Validate(string val, string type) => type.ToLower() switch
    {
        "int" => int.TryParse(val, out _),
        "int64" => long.TryParse(val, out _),
        "float" => float.TryParse(val, out _),
        _ => true
    };
}