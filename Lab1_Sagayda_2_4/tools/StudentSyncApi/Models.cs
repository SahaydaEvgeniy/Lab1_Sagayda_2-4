namespace StudentSyncApi;

public record StudentData(Guid Id, string FullName, string Group, double AverageScore);

public record SyncOperation(Guid OperationId, Guid StudentId, string EntityType,
    string Operation, string PayloadJson, DateTimeOffset CreatedAtUtc);

public record SyncReceipt(Guid OperationId, Guid StudentId, string Status, bool Applied, bool Duplicate);

public record StoredStudent(Guid Id, StudentData? Student, DateTimeOffset UpdatedAtUtc,
    Guid LastOperationId, bool IsDeleted);

public record ServerState(Dictionary<Guid, StoredStudent> Students,
    Dictionary<Guid, SyncOperation> Operations);

public sealed class OperationConflictException : Exception
{
    public OperationConflictException() : base("OperationId already belongs to a different operation.") { }
}
