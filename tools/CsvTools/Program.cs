using System.Text;

namespace CsvTools
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 初始化配置读取器
                InitializeConfiguration();
                
                // 显示应用程序信息
                DisplayAppInfo();
                
                // 显示配置信息
                DisplayConfiguration();
                
                // 执行主要业务逻辑
                ProcessCsvFiles();
                
                Console.WriteLine("\n程序执行完成！");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"程序执行出错: {ex.Message}");
                Environment.ExitCode = 1;
            }
            
            Console.WriteLine("按任意键退出...");
            Console.ReadLine();
        }
        
        /// <summary>
        /// 初始化配置系统
        /// </summary>
        private static void InitializeConfiguration()
        {
            Console.WriteLine("正在初始化配置系统...");
            ConfigReader.Initialize();
            Console.WriteLine("配置系统初始化完成");
        }
        
        /// <summary>
        /// 显示应用程序信息
        /// </summary>
        private static void DisplayAppInfo()
        {
            var appName = ConfigReader.GetString("AppName", "CsvTools");
            var version = ConfigReader.GetString("Version", "1.0.0");
            var environment = ConfigReader.GetString("Environment", "Unknown");
            
            Console.WriteLine("=========================");
            Console.WriteLine($"  {appName} v{version}");
            Console.WriteLine($"  运行环境: {environment}");
            Console.WriteLine("=========================");
        }
        
        /// <summary>
        /// 显示配置信息
        /// </summary>
        private static void DisplayConfiguration()
        {
            Console.WriteLine("\n当前配置信息:");
            
            // 基本配置
            Console.WriteLine($"  应用名称: {ConfigReader.GetString("AppName")}");
            Console.WriteLine($"  版本: {ConfigReader.GetString("Version")}");
            Console.WriteLine($"  环境: {ConfigReader.GetString("Environment")}");
            
            // CSV配置
            Console.WriteLine($"  输入目录: {ConfigReader.GetString("CsvSettings:InputDirectory")}");
            Console.WriteLine($"  输出目录: {ConfigReader.GetString("CsvSettings:OutputDirectory")}");
            Console.WriteLine($"  文件编码: {ConfigReader.GetString("CsvSettings:FileEncoding")}");
            Console.WriteLine($"  分隔符: {ConfigReader.GetString("CsvSettings:Delimiter")}");
            Console.WriteLine($"  批处理大小: {ConfigReader.GetInt("CsvSettings:BatchSize")}");
            
            // 性能配置
            Console.WriteLine($"  最大并发文件数: {ConfigReader.GetInt("Performance:MaxConcurrentFiles")}");
            Console.WriteLine($"  内存阈值(MB): {ConfigReader.GetInt("Performance:MemoryThresholdMB")}");
            
            // 数据库配置（隐藏敏感信息）
            var connStr = ConfigReader.GetString("Database:ConnectionString");
            if (!string.IsNullOrEmpty(connStr))
            {
                Console.WriteLine("  数据库连接: ******");
            }
        }
        
        /// <summary>
        /// 处理CSV文件的主要逻辑
        /// </summary>
        private static void ProcessCsvFiles()
        {
            Console.WriteLine("\n开始处理CSV文件...");
            CsvParser.GenerateEntityFiles();
            Console.WriteLine("CSV文件处理完成！");
        }
        

    }
}
