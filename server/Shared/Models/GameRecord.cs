namespace Cube.Shared.Models;

/// <summary>
/// 游戏记录模型
/// </summary>
public class GameRecord
{
    public long RecordId { get; set; }
    public string RoomId { get; set; } = string.Empty;
    public List<long> PlayerIds { get; set; } = new();
    public string GameMode { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public int Duration { get; set; } // 游戏时长（秒）
    public DateTime CreatedAt { get; set; }
}
