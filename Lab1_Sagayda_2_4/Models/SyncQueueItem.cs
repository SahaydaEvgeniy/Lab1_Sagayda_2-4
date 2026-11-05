namespace Lab1_Sagayda_2_4.Models;

[SQLite.Table("SyncQueue")]
public class SyncQueueItem
{
    [SQLite.PrimaryKey, SQLite.AutoIncrement]
    public long Sequence { get; set; }

    [SQLite.Indexed(Unique = true)]
    public Guid OperationId { get; set; } = Guid.NewGuid();

    [SQLite.Indexed]
    public Guid StudentId { get; set; }

    public string EntityType { get; set; } = "Student";

    public string Operation { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
