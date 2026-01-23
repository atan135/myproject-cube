namespace Cube.Shared.Protocols;

/// <summary>
/// 消息基类
/// </summary>
public class Message
{
    public int MessageType { get; set; }
    public long Timestamp { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();
}

/// <summary>
/// 消息类型枚举
/// </summary>
public enum MessageType
{
    // 连接相关
    Connect = 1000,
    Disconnect = 1001,
    Heartbeat = 1002,
    
    // 匹配相关
    MatchRequest = 2000,
    MatchSuccess = 2001,
    MatchCancel = 2002,
    
    // 游戏相关
    GameStart = 3000,
    PlayerMove = 3001,
    PlayerAction = 3002,
    GameState = 3003,
    GameEnd = 3004
}
