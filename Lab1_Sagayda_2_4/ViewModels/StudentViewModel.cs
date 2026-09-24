using System.ComponentModel;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.ViewModels;

public class StudentViewModel : INotifyPropertyChanged
{
    private readonly Student _student = new();

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}