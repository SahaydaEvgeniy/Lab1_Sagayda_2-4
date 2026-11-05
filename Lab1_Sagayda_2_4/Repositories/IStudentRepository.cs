using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.Repositories;

public interface IStudentRepository
{
    Task InitializeAsync();
    Task<List<Student>> GetAllAsync();
    Task<Student?> GetByIdAsync(Guid id);
    Task<int> SaveAsync(Student student);
    Task<int> DeleteAsync(Student student);
    Task<int> GetPendingChangeCountAsync();
    Task<List<SyncQueueItem>> GetPendingChangesAsync();
    Task<bool> AcknowledgeChangeAsync(Guid operationId);
}
