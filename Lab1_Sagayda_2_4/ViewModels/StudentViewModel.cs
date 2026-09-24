using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.ViewModels;

public class StudentViewModel : INotifyPropertyChanged
{
    private readonly Student _student = new();

    public ObservableCollection<Student> Students { get; } = new();

    public ICommand AddStudentCommand { get; }

    public StudentViewModel()
    {
        AddStudentCommand = new Command(AddStudent, CanAddStudent);
    }

    public string FullName
    {
        get => _student.FullName;
        set
        {
            if (_student.FullName != value)
            {
                _student.FullName = value;

                OnPropertyChanged(nameof(FullName));
                OnPropertyChanged(nameof(StudentSummary));

                ((Command)AddStudentCommand).ChangeCanExecute();
            }
        }
    }

    public string Group
    {
        get => _student.Group;
        set
        {
            if (_student.Group != value)
            {
                _student.Group = value;

                OnPropertyChanged(nameof(Group));
                OnPropertyChanged(nameof(StudentSummary));
            }
        }
    }

    public double AverageScore
    {
        get => _student.AverageScore;
        set
        {
            if (_student.AverageScore != value)
            {
                _student.AverageScore = value;

                OnPropertyChanged(nameof(AverageScore));
                OnPropertyChanged(nameof(IsHighScore));
            }
        }
    }

    public string StudentSummary =>
        $"Студент: {FullName}, група {Group}";

    public bool IsHighScore =>
        AverageScore >= 4.0;

    private void AddStudent()
    {
        Students.Add(new Student
        {
            FullName = FullName,
            Group = Group,
            AverageScore = AverageScore
        });

        FullName = string.Empty;
        Group = string.Empty;
        AverageScore = 0;
    }

    private bool CanAddStudent()
    {
        return !string.IsNullOrWhiteSpace(FullName);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}