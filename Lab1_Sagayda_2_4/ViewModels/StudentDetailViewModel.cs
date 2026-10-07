using System.ComponentModel;
using System.Windows.Input;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.ViewModels;

public class StudentDetailViewModel : IQueryAttributable, INotifyPropertyChanged
{
    private Student? _originalStudent;
    private double _averageScore;

    public ICommand GoBackCommand { get; }

    public ICommand SaveCommand { get; }

    public StudentDetailViewModel()
    {
        GoBackCommand = new Command(async () =>
            await Shell.Current.GoToAsync(".."));
        SaveCommand = new Command(async () => await SaveAsync(),
            () => _originalStudent is not null);
    }

    public string FullName { get; private set; } = string.Empty;

    public string Group { get; private set; } = string.Empty;

    public double AverageScore
    {
        get => _averageScore;
        set
        {
            if (!double.IsFinite(value) || value < 0 || value > 5)
                return;

            if (_averageScore != value)
            {
                _averageScore = value;
                OnPropertyChanged(nameof(AverageScore));
            }
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("SelectedStudent", out var value)
            && value is Student student)
        {
            _originalStudent = student;
            FullName = student.FullName;
            Group = student.Group;
            AverageScore = student.AverageScore;

            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(Group));
            ((Command)SaveCommand).ChangeCanExecute();
        }
    }

    private async Task SaveAsync()
    {
        if (_originalStudent is null)
            return;

        var updatedStudent = new Student
        {
            FullName = FullName,
            Group = Group,
            AverageScore = AverageScore
        };

        var parameters = new ShellNavigationQueryParameters
        {
            { "OriginalStudent", _originalStudent },
            { "UpdatedStudent", updatedStudent }
        };

        await Shell.Current.GoToAsync("..", parameters);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
