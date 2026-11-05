
namespace Lab1_Sagayda_2_4.Models;

[SQLite.Table("Students")]
public class Student
{
    [SQLite.PrimaryKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    [SQLite.Column("StudentGroup")]
    public string Group { get; set; } = string.Empty;

    public double AverageScore { get; set; }

    [SQLite.Ignore, System.Text.Json.Serialization.JsonIgnore]
    public string StudentCardNumber { get; set; } = string.Empty;

    public string StudentCardNumberEncrypted { get; set; } = string.Empty;
}
