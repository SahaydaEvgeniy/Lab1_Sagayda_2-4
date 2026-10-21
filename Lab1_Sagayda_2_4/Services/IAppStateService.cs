using System.Collections.ObjectModel;
using System.ComponentModel;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.Services;

public interface IAppStateService : INotifyPropertyChanged
{
    ObservableCollection<Student> Students { get; }

    StudentEditDraft? StudentDraft { get; set; }

    string StorageError { get; }

    bool HasStorageError { get; }

    Student? FindStudent(Guid id);
}
