namespace Cube.Shared.Models;

/// <summary>
/// 角色模型
/// </summary>
public class Character
{
    public long CharacterId { get; set; }
    public long UserId { get; set; }
    public string CharacterType { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }
    public string Stats { get; set; } = "{}"; // JSON格式存储属性
}
