using System.Diagnostics;
using System.Text.Json;
using Lab1_Sagayda_2_4.Models;
using SQLite;
using Lab1_Sagayda_2_4.Services;

namespace Lab1_Sagayda_2_4.Repositories;

public class StudentRepository : IStudentRepository
{
    private const int CurrentSchemaVersion = 4;
    private readonly IStudentCardProtection _cardProtection;
    private readonly SQLiteAsyncConnection _database;
    private readonly string _databasePath;
    private readonly string _legacyJsonPath;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private bool _initialized;

    public StudentRepository(string databasePath, IStudentCardProtection cardProtection)
    {
        _databasePath = databasePath;
        _cardProtection = cardProtection;
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

                if (version < 4)
                {
                    connection.CreateTable<Student>();
                }

                if (version < 2)
                {
                    connection.Execute("CREATE TABLE IF NOT EXISTS AppMetadata (Key TEXT PRIMARY KEY NOT NULL, Value TEXT NOT NULL)");
                    ImportLegacyStudents(connection);
                    connection.Execute("PRAGMA user_version = 2");
                }

                if (version < 3)
                {
                    connection.CreateTable<SyncQueueItem>();
                    // Existing local students also need an initial upload to the future server.
                    foreach (var student in connection.Table<Student>().ToList())
                        EnqueueChange(connection, student, "Insert");
                    connection.Execute("PRAGMA user_version = 3");
                }

                if (version < 4)
                {
                    connection.Execute("PRAGMA user_version = 4");
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
        var students = await _database.Table<Student>().OrderBy(student => student.FullName).ToListAsync();
        foreach (var student in students)
            student.StudentCardNumber = await _cardProtection.DecryptAsync(student.StudentCardNumberEncrypted, student.Id);
        return students;
    }

    public async Task<Student?> GetByIdAsync(Guid id)
    {
        await InitializeAsync();
        var student = await _database.FindAsync<Student>(id);
        if (student is not null)
            student.StudentCardNumber = await _cardProtection.DecryptAsync(student.StudentCardNumberEncrypted, student.Id);
        return student;
    }

    public async Task<int> SaveAsync(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        if (student.Id == Guid.Empty)
            throw new ArgumentException("Ідентифікатор студента не може бути порожнім.", nameof(student));
        if (!double.IsFinite(student.AverageScore) || student.AverageScore < 0 || student.AverageScore > 5)
            throw new ArgumentOutOfRangeException(nameof(student), "Середній бал має бути в межах від 0 до 5.");

        await InitializeAsync();
        var storedStudent = new Student
        {
            Id = student.Id,
            FullName = student.FullName,
            Group = student.Group,
            AverageScore = student.AverageScore,
            StudentCardNumberEncrypted = await _cardProtection.EncryptAsync(student.StudentCardNumber, student.Id)
        };
        var affectedRows = 0;
        await _database.RunInTransactionAsync(connection =>
        {
            var isNew = connection.Find<Student>(student.Id) is null;
            affectedRows = isNew
                ? connection.Insert(storedStudent)
                : connection.Update(storedStudent);
            EnqueueChange(connection, storedStudent, isNew ? "Insert" : "Update");
        });
        return affectedRows;
    }

    public async Task<int> DeleteAsync(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        await InitializeAsync();
        var affectedRows = 0;
        await _database.RunInTransactionAsync(connection =>
        {
            var existing = connection.Find<Student>(student.Id);
            if (existing is null)
                return;

            affectedRows = connection.Delete<Student>(student.Id);
            EnqueueChange(connection, existing, "Delete");
        });
        return affectedRows;
    }

    public async Task<int> GetPendingChangeCountAsync()
    {
        await InitializeAsync();
        return await _database.Table<SyncQueueItem>().CountAsync();
    }

    public async Task<List<SyncQueueItem>> GetPendingChangesAsync()
    {
        await InitializeAsync();
        return await _database.Table<SyncQueueItem>().OrderBy(item => item.Sequence).ToListAsync();
    }

    public async Task<bool> AcknowledgeChangeAsync(Guid operationId)
    {
        await InitializeAsync();
        return await _database.ExecuteAsync("DELETE FROM SyncQueue WHERE OperationId = ?", operationId) == 1;
    }

    private static void EnqueueChange(SQLiteConnection connection, Student student, string operation)
    {
        connection.Insert(new SyncQueueItem
        {
            StudentId = student.Id,
            Operation = operation,
            PayloadJson = JsonSerializer.Serialize(student)
        });
    }
}
