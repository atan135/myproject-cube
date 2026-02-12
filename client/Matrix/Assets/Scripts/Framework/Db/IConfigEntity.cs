using SQLite;

public interface IConfigEntity
{
    [PrimaryKey]
    int Id { get; set; }
}