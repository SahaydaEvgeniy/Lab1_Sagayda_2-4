using System.Collections.ObjectModel;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.Services;

public class AppStateService : IAppStateService
{
    // Session state only. Persistent reads and writes go through IStudentRepository.
    public ObservableCollection<Student> Students { get; } = new();

    public StudentEditDraft? StudentDraft { get; set; }

    public Student? FindStudent(Guid id) => Students.FirstOrDefault(student => student.Id == id);
}