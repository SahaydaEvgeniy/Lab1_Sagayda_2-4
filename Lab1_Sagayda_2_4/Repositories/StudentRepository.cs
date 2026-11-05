using System.Diagnostics;
using System.Text.Json;
using Lab1_Sagayda_2_4.Models;
using SQLite;

namespace Lab1_Sagayda_2_4.Repositories;

public class StudentRepository : IStudentRepository
{
    private const int CurrentSchemaVersion = 2;
    private readonly SQLiteAsyncConnection _database;
    private readonly string _databasePath;
    private readonly string _legacyJsonPath;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public StudentRepository(string databasePath)
    {
        _databasePath = databasePath;
        _legacyJsonPath = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(databasePath))!, "students.json");
        _database = new SQLiteAsyncConnection(databasePath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
    }

    public async Task InitializeAsync()
    {
        await _initializationLock.WaitAsync();
        try
        {
            if (_initialized)
                return;

            await _database.RunInTransactionAsync(connection =>
            {
                var version = connection.ExecuteScalar<int>("PRAGMA user_version");
                if (version > CurrentSchemaVersion)
                    throw new InvalidOperationException("Версія бази даних новіша за підтримувану застосунком.");

                if (version == 0)
                {
                    connection.CreateTable<Student>();
                }

                if (version < 2)
                {
                    connection.Execute("CREATE TABLE IF NOT EXISTS AppMetadata (Key TEXT PRIMARY KEY NOT NULL, Value TEXT NOT NULL)");
                    ImportLegacyStudents(connection);
                    connection.Execute("PRAGMA user_version = 2");
                }
            });

            _initialized = true;
            Debug.WriteLine($"[SQLite] Ready: {_databasePath}; schema v{CurrentSchemaVersion}; table Students.");
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    private void ImportLegacyStudents(SQLiteConnection connection)
    {
        if (connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM AppMetadata WHERE Key = ?", "LegacyJsonImported") > 0)
            return;

        var imported = 0;
        if (File.Exists(_legacyJsonPath))
        {
            var students = JsonSerializer.Deserialize<List<Student>>(File.ReadAllText(_legacyJsonPath))
                ?? throw new InvalidDataException("JSON-список студентів порожній або пошкоджений.");
            var ids = new HashSet<Guid>();
            foreach (var student in students)
            {
                if (student is null || student.Id == Guid.Empty || !ids.Add(student.Id)
                    || !double.IsFinite(student.AverageScore)
                    || student.AverageScore < 0 || student.AverageScore > 5)
                    throw new InvalidDataException("JSON-список містить некоректний запис або повторний Id.");
            }

            foreach (var student in students)
            {
                // Existing SQLite records take precedence over the old backup.
                if (connection.Find<Student>(student.Id) is null)
                    imported += connection.Insert(student);
            }
        }

        connection.Execute("INSERT INTO AppMetadata (Key, Value) VALUES (?, ?)", "LegacyJsonImported", "1");
        Debug.WriteLine($"[SQLite] JSON migration: imported {imported}; original file retained.");
    }

    public async Task<List<Student>> GetAllAsync()
    {
        await InitializeAsync();
        return await _database.Table<Student>().OrderBy(student => student.FullName).ToListAsync();
    }

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        await InitializeAsync();
        return await _database.FindAsync<Student>(id);
    }

    public async Task<int> SaveAsync(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        if (student.Id == Guid.Empty)
            throw new ArgumentException("Ідентифікатор студента не може бути порожнім.", nameof(student));
        if (!double.IsFinite(student.AverageScore) || student.AverageScore < 0 || student.AverageScore > 5)
            throw new ArgumentOutOfRangeException(nameof(student), "Середній бал має бути в межах від 0 до 5.");

        await InitializeAsync();
        var affectedRows = 0;
        await _database.RunInTransactionAsync(connection =>
        {
            affectedRows = connection.Find<Student>(student.Id) is null
                ? connection.Insert(student)
                : connection.Update(student);
        });
        return affectedRows;
    }

    public async Task<int> DeleteAsync(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        await InitializeAsync();
        return await _database.DeleteAsync<Student>(student.Id);
    }
}
