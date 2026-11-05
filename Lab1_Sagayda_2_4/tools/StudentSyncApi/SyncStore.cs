using System.Text.Json;

namespace StudentSyncApi;

public class SyncStore
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        // Preserve the canonical payload of operations accepted before this optional field existed.
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ServerState _state = new(new(), new());

    public SyncStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, "sync-store.json");
    }

    public async Task InitializeAsync()
    {
        if (!File.Exists(_path))
            return;

        _state = JsonSerializer.Deserialize<ServerState>(await File.ReadAllTextAsync(_path), JsonOptions)
            ?? throw new InvalidDataException("Server data file is invalid.");
        if (_state.Students is null || _state.Operations is null)
            throw new InvalidDataException("Server data file is incomplete.");
    }

    public async Task<StudentData[]> GetStudentsAsync()
    {
        await _gate.WaitAsync();
        try
        {
            return _state.Students.Values.Where(row => !row.IsDeleted)
                .Select(row => row.Student!).OrderBy(student => student.FullName).ToArray();
        }
        finally { _gate.Release(); }
    }

    public async Task<StudentData?> GetStudentAsync(Guid id)
    {
        await _gate.WaitAsync();
        try
        {
            return _state.Students.TryGetValue(id, out var row) && !row.IsDeleted ? row.Student : null;
        }
        finally { _gate.Release(); }
    }

    public async Task<SyncOperation[]> GetOperationsAsync()
    {
        await _gate.WaitAsync();
        try { return _state.Operations.Values.OrderBy(op => op.CreatedAtUtc).ToArray(); }
        finally { _gate.Release(); }
    }

    public async Task<SyncReceipt> ApplyAsync(SyncOperation operation, StudentData student)
    {
        // Normalize JSON casing and timestamps before checking operation identity.
        operation = operation with
        {
            PayloadJson = JsonSerializer.Serialize(student, JsonOptions),
            CreatedAtUtc = operation.CreatedAtUtc.ToUniversalTime()
        };
        await _gate.WaitAsync();
        try
        {
            if (_state.Operations.TryGetValue(operation.OperationId, out var previous))
            {
                if (previous != operation)
                    throw new OperationConflictException();
                return new(operation.OperationId, operation.StudentId, "Duplicate", false, true);
            }

            var next = new ServerState(new(_state.Students), new(_state.Operations));
            next.Students.TryGetValue(operation.StudentId, out var current);
            var wins = current is null || operation.CreatedAtUtc > current.UpdatedAtUtc
                || (operation.CreatedAtUtc == current.UpdatedAtUtc
                    && operation.OperationId.CompareTo(current.LastOperationId) > 0);
            if (wins)
            {
                var deleted = operation.Operation == "Delete";
                next.Students[operation.StudentId] = new(operation.StudentId,
                    deleted ? null : student, operation.CreatedAtUtc, operation.OperationId, deleted);
            }
            next.Operations.Add(operation.OperationId, operation);

            // A failed disk write must not acknowledge or apply a change in memory.
            var temporaryPath = _path + ".tmp";
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(next, JsonOptions));
            File.Move(temporaryPath, _path, overwrite: true);
            _state = next;
            return new(operation.OperationId, operation.StudentId,
                wins ? "Applied" : "IgnoredOlder", wins, false);
        }
        finally { _gate.Release(); }
    }
}
