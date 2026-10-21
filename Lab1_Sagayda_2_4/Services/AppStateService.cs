using System.Collections.ObjectModel;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.Services;

public class AppStateService : IAppStateService
{
    public ObservableCollection<Student> Students { get; } = new();

    public StudentEditDraft? StudentDraft { get; set; }
}
