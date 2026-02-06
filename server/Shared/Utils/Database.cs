using MySqlConnector;
using System.Data;
using System.Collections.Concurrent;

namespace Cube.Shared.Utils;

/// <summary>
/// MariaDB数据库连接工具类
/// 提供连接池管理、SQL执行、事务处理等功能
/// </summary>
public class Database : IDisposable
{
    private readonly string _connectionString;
    private MySqlConnection? _connection;
    private bool _disposed = false;
    
    // 连接池相关静态成员
    private static readonly ConcurrentDictionary<string, ConnectionPool> _connectionPools = new();
    private static readonly object _poolLock = new();
    private ConnectionPool? _currentPool;
    private bool _useConnectionPooling = true;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionString">数据库连接字符串</param>
    /// <param name="useConnectionPooling">是否使用连接池</param>
    public Database(string connectionString, bool useConnectionPooling = true)
    {
        _connectionString = connectionString;
        _useConnectionPooling = useConnectionPooling;
        
        if (_useConnectionPooling)
        {
            // 获取或创建连接池
            _currentPool = _connectionPools.GetOrAdd(connectionString, cs => new ConnectionPool(cs));
        }
    }

    /// <summary>
    /// 创建数据库实例的静态方法
    /// </summary>
    public static Database Create(string host, string database, string username, string password, int port = 3306, bool useConnectionPooling = true)
    {
        var connectionString = $"Server={host};Port={port};Database={database};Uid={username};Pwd={password};SslMode=None;";
        return new Database(connectionString, useConnectionPooling);
    }
    
    /// <summary>
    /// 创建不使用连接池的数据库实例
    /// </summary>
    public static Database CreateWithoutPooling(string host, string database, string username, string password, int port = 3306)
    {
        return Create(host, database, username, password, port, false);
    }

    /// <summary>
    /// 打开数据库连接
    /// </summary>
    public async Task OpenAsync()
    {
        if (_useConnectionPooling && _currentPool != null)
        {
            // 从连接池获取连接
            _connection = await _currentPool.GetConnectionAsync();
            LogUtils.Debug("Acquired connection from pool");
        }
        else
        {
            // 不使用连接池，创建新连接
            if (_connection == null || _connection.State != ConnectionState.Open)
            {
                _connection = new MySqlConnection(_connectionString);
                await _connection.OpenAsync();
                LogUtils.Info("Database connection opened successfully (no pooling)");
            }
        }
    }

    /// <summary>
    /// 关闭数据库连接
    /// </summary>
    public async Task CloseAsync()
    {
        if (_useConnectionPooling && _currentPool != null && _connection != null)
        {
            // 将连接归还到连接池
            await _currentPool.ReturnConnectionAsync(_connection);
            _connection = null;
            LogUtils.Debug("Returned connection to pool");
        }
        else if (_connection != null && _connection.State == ConnectionState.Open)
        {
            // 不使用连接池，直接关闭连接
            await _connection.CloseAsync();
            LogUtils.Info("Database connection closed (no pooling)");
        }
    }

    /// <summary>
    /// 执行非查询SQL语句（INSERT, UPDATE, DELETE等）
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>受影响的行数</returns>
    public async Task<int> ExecuteNonQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        await EnsureConnectionOpen();
        
        using var command = new MySqlCommand(sql, _connection);
        AddParameters(command, parameters);
        
        try
        {
            int result = await command.ExecuteNonQueryAsync();
            LogUtils.Debug($"Executed non-query SQL: {sql}, Affected rows: {result}");
            return result;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to execute non-query SQL: {sql}", ex);
            throw;
        }
    }

    /// <summary>
    /// 执行查询SQL语句并返回单个值
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>查询结果的第一行第一列</returns>
    public async Task<object?> ExecuteScalarAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        await EnsureConnectionOpen();
        
        using var command = new MySqlCommand(sql, _connection);
        AddParameters(command, parameters);
        
        try
        {
            var result = await command.ExecuteScalarAsync();
            LogUtils.Debug($"Executed scalar SQL: {sql}");
            return result;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to execute scalar SQL: {sql}", ex);
            throw;
        }
    }

    /// <summary>
    /// 执行查询SQL语句并返回DataReader
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>MySqlDataReader</returns>
    public async Task<MySqlDataReader> ExecuteReaderAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        await EnsureConnectionOpen();
        
        using var command = new MySqlCommand(sql, _connection);
        AddParameters(command, parameters);
        
        try
        {
            var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            LogUtils.Debug($"Executed reader SQL: {sql}");
            return reader;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to execute reader SQL: {sql}", ex);
            throw;
        }
    }

    /// <summary>
    /// 执行查询并返回DataTable
    /// </summary>
    /// <param name="sql">SQL语句</param>
    /// <param name="parameters">参数字典</param>
    /// <returns>DataTable</returns>
    public async Task<DataTable> ExecuteQueryAsync(string sql, Dictionary<string, object>? parameters = null)
    {
        await EnsureConnectionOpen();
        
        using var command = new MySqlCommand(sql, _connection);
        AddParameters(command, parameters);
        
        var dataTable = new DataTable();
        
        try
        {
            using var adapter = new MySqlDataAdapter(command);
            adapter.Fill(dataTable);
            LogUtils.Debug($"Executed query SQL: {sql}, Returned {dataTable.Rows.Count} rows");
            return dataTable;
        }
        catch (Exception ex)
        {
            LogUtils.Error($"Failed to execute query SQL: {sql}", ex);
            throw;
        }
    }

    /// <summary>
    /// 开始事务
    /// </summary>
    public async Task<MySqlTransaction> BeginTransactionAsync()
    {
        await EnsureConnectionOpen();
        var transaction = await _connection!.BeginTransactionAsync();
        LogUtils.Info("Database transaction started");
        return transaction;
    }

    /// <summary>
    /// 执行带事务的SQL操作
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(Func<MySqlTransaction, Task<T>> operation)
    {
        await EnsureConnectionOpen();
        
        using var transaction = await _connection!.BeginTransactionAsync();
        try
        {
            var result = await operation(transaction);
            await transaction.CommitAsync();
            LogUtils.Info("Database transaction committed");
            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            LogUtils.Error("Database transaction rolled back", ex);
            throw;
        }
    }

    /// <summary>
    /// 检查表是否存在
    /// </summary>
    public async Task<bool> TableExistsAsync(string tableName)
    {
        var sql = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @tableName";
        var parameters = new Dictionary<string, object> { { "@tableName", tableName } };
        
        var result = await ExecuteScalarAsync(sql, parameters);
        return Convert.ToInt32(result) > 0;
    }

    /// <summary>
    /// 创建表（如果不存在）
    /// </summary>
    public async Task CreateTableIfNotExistsAsync(string tableName, string createTableSql)
    {
        if (!await TableExistsAsync(tableName))
        {
            await ExecuteNonQueryAsync(createTableSql);
            LogUtils.Info($"Created table: {tableName}");
        }
    }

    /// <summary>
    /// 获取数据库统计信息
    /// </summary>
    public async Task<Dictionary<string, object>> GetDatabaseStatsAsync()
    {
        var stats = new Dictionary<string, object>();
        
        // 获取表数量
        var tableCount = await ExecuteScalarAsync("SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = DATABASE()");
        stats["TableCount"] = tableCount ?? 0;
        
        // 获取当前连接数
        var connectionCount = await ExecuteScalarAsync("SHOW STATUS LIKE 'Threads_connected'");
        stats["CurrentConnections"] = connectionCount ?? 0;
        
        return stats;
    }

    /// <summary>
    /// 确保连接已打开
    /// </summary>
    private async Task EnsureConnectionOpen()
    {
        if (_connection == null || _connection.State != ConnectionState.Open)
        {
            await OpenAsync();
        }
    }

    /// <summary>
    /// 添加参数到命令
    /// </summary>
    private static void AddParameters(MySqlCommand command, Dictionary<string, object>? parameters)
    {
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                command.Parameters.AddWithValue(param.Key, param.Value);
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await CloseAsync();
            _connection?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// 释放资源（同步版本）
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().Wait();
    }

    /// <summary>
    /// 获取连接字符串（用于调试，隐藏密码）
    /// </summary>
    public string GetSafeConnectionString()
    {
        var builder = new MySqlConnectionStringBuilder(_connectionString);
        return $"Server={builder.Server};Port={builder.Port};Database={builder.Database};Uid={builder.UserID};...";
    }
}

/// <summary>
/// 数据库连接池实现
/// </summary>
internal class ConnectionPool
{
    private readonly string _connectionString;
    private readonly ConcurrentQueue<MySqlConnection> _availableConnections;
    private readonly SemaphoreSlim _semaphore;
    private readonly object _lock = new();
    private int _totalConnections = 0;
    
    // 连接池配置
    private const int MAX_POOL_SIZE = 20;
    private const int MIN_POOL_SIZE = 5;
    private const int CONNECTION_TIMEOUT_MS = 30000;
    
    public ConnectionPool(string connectionString)
    {
        _connectionString = connectionString;
        _availableConnections = new ConcurrentQueue<MySqlConnection>();
        _semaphore = new SemaphoreSlim(MAX_POOL_SIZE, MAX_POOL_SIZE);
        
        // 预创建最小连接数
        PreCreateConnectionsAsync().Forget();
    }
    
    /// <summary>
    /// 从连接池获取连接
    /// </summary>
    public async Task<MySqlConnection> GetConnectionAsync()
    {
        // 等待可用连接许可
        if (await _semaphore.WaitAsync(CONNECTION_TIMEOUT_MS))
        {
            // 尝试从队列获取现有连接
            if (_availableConnections.TryDequeue(out var connection))
            {
                // 检查连接是否仍然有效
                if (connection.State == ConnectionState.Open)
                {
                    LogUtils.Debug($"Reused existing connection from pool. Pool size: {_availableConnections.Count}");
                    return connection;
                }
                else
                {
                    // 连接已失效，关闭并创建新连接
                    try { await connection.CloseAsync(); } catch { }
                    connection.Dispose();
                }
            }
            
            // 创建新连接
            connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            lock (_lock)
            {
                _totalConnections++;
            }
            
            LogUtils.Debug($"Created new connection. Total connections: {_totalConnections}, Pool size: {_availableConnections.Count}");
            return connection;
        }
        
        throw new TimeoutException("Timeout waiting for database connection from pool");
    }
    
    /// <summary>
    /// 将连接归还到连接池
    /// </summary>
    public async Task ReturnConnectionAsync(MySqlConnection connection)
    {
        if (connection.State == ConnectionState.Open)
        {
            // 清理连接状态
            if (connection.HasActiveResult())
            {
                await connection.CloseAsync();
            }
            
            // 检查是否超过最大池大小
            if (_availableConnections.Count < MAX_POOL_SIZE)
            {
                _availableConnections.Enqueue(connection);
                LogUtils.Debug($"Returned connection to pool. Pool size: {_availableConnections.Count}");
            }
            else
            {
                // 池已满，直接关闭连接
                await connection.CloseAsync();
                connection.Dispose();
                lock (_lock)
                {
                    _totalConnections--;
                }
                LogUtils.Debug($"Pool full, closed excess connection. Total connections: {_totalConnections}");
            }
        }
        else
        {
            // 连接已关闭，直接销毁
            connection.Dispose();
            lock (_lock)
            {
                _totalConnections--;
            }
            LogUtils.Debug($"Disposed closed connection. Total connections: {_totalConnections}");
        }
        
        // 释放信号量
        _semaphore.Release();
    }
    
    /// <summary>
    /// 预创建最小连接数
    /// </summary>
    private async Task PreCreateConnectionsAsync()
    {
        try
        {
            for (int i = 0; i < MIN_POOL_SIZE; i++)
            {
                if (await _semaphore.WaitAsync(1000))
                {
                    var connection = new MySqlConnection(_connectionString);
                    await connection.OpenAsync();
                    _availableConnections.Enqueue(connection);
                    lock (_lock)
                    {
                        _totalConnections++;
                    }
                }
            }
            LogUtils.Info($"Pre-created {MIN_POOL_SIZE} connections for pool");
        }
        catch (Exception ex)
        {
            LogUtils.Error("Failed to pre-create pool connections", ex);
        }
    }
    
    /// <summary>
    /// 获取池统计信息
    /// </summary>
    public (int Available, int Total, int MaxSize) GetStats()
    {
        return (_availableConnections.Count, _totalConnections, MAX_POOL_SIZE);
    }
    
    /// <summary>
    /// 清理连接池
    /// </summary>
    public async Task ClearAsync()
    {
        while (_availableConnections.TryDequeue(out var connection))
        {
            try
            {
                await connection.CloseAsync();
                connection.Dispose();
            }
            catch { }
        }
        
        lock (_lock)
        {
            _totalConnections = 0;
        }
        LogUtils.Info("Connection pool cleared");
    }
}

/// <summary>
/// 扩展方法
/// </summary>
internal static class DatabaseExtensions
{
    /// <summary>
    /// 忽略任务结果（用于fire-and-forget模式）
    /// </summary>
    public static void Forget(this Task task)
    {
        if (task.IsCompleted)
        {
            var ignored = task.Exception;
        }
        else
        {
            _ = ForgetAwaited(task);
        }
        
        static async Task ForgetAwaited(Task task)
        {
            try
            {
                await task;
            }
            catch
            {
                // 忽略异常
            }
        }
    }
    
    /// <summary>
    /// 检查连接是否有活动结果
    /// </summary>
    public static bool HasActiveResult(this MySqlConnection connection)
    {
        // 这里可以根据具体需求实现检查逻辑
        // MySqlConnector通常会在连接关闭时自动清理结果
        return false;
    }
}