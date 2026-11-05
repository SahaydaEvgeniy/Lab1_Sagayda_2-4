using System.ComponentModel;
using System.Windows.Input;
using Lab1_Sagayda_2_4.Models;
using Lab1_Sagayda_2_4.Services;
using Lab1_Sagayda_2_4.Repositories;
using System.Diagnostics;

namespace Lab1_Sagayda_2_4.ViewModels;

public class StudentDetailViewModel : IQueryAttributable, INotifyPropertyChanged
{
    private Student? _originalStudent;
    private readonly IAppStateService _appStateService;
    private double _averageScore;
    private string _studentCardNumber = string.Empty;
    private readonly IStudentRepository _repository;
    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            ((Command)SaveCommand).ChangeCanExecute();
            ((Command)GoBackCommand).ChangeCanExecute();
            ((Command)DecreaseScoreCommand).ChangeCanExecute();
            ((Command)IncreaseScoreCommand).ChangeCanExecute();
        }
    }

    public string ErrorMessage { get; private set; } = string.Empty;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ICommand GoBackCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand DecreaseScoreCommand { get; }

    public ICommand IncreaseScoreCommand { get; }

    public ICommand CopyLinkCommand { get; }

    public StudentDetailViewModel(IAppStateService appStateService, IStudentRepository repository)
    {
        _appStateService = appStateService;
        _repository = repository;
        CopyLinkCommand = new Command(async () =>
            await Clipboard.Default.SetTextAsync(StudentLink),
            () => _originalStudent is not null);
        DecreaseScoreCommand = new Command(
            () => AverageScore = Math.Max(0, Math.Round(AverageScore - 0.1, 2)),
            () => !IsBusy && AverageScore > 0);
        IncreaseScoreCommand = new Command(
            () => AverageScore = Math.Min(5, Math.Round(AverageScore + 0.1, 2)),
            () => !IsBusy && AverageScore < 5);
        GoBackCommand = new Command(async () =>
            await RunOperationAsync(() => Shell.Current.GoToAsync(".."),
                "Не вдалося повернутися до списку."), () => !IsBusy);
        SaveCommand = new Command(async () => await SaveAsync(),
            () => !IsBusy && _originalStudent is not null);
    }

    public string FullName { get; private set; } = string.Empty;

    public Guid StudentId => _originalStudent?.Id ?? Guid.Empty;

    public string StudentLink => _originalStudent is null
        ? string.Empty
        : StudentDeepLink.Create(StudentId);

    public string Group { get; private set; } = string.Empty;

    public string StudentCardNumber
    {
        get => _studentCardNumber;
        set
        {
            if (_studentCardNumber == value) return;
            _studentCardNumber = value;
            OnPropertyChanged(nameof(StudentCardNumber));
        }
    }

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
                ((Command)DecreaseScoreCommand).ChangeCanExecute();
                ((Command)IncreaseScoreCommand).ChangeCanExecute();
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
            StudentCardNumber = student.StudentCardNumber;

            OnPropertyChanged(nameof(FullName));
            OnPropertyChanged(nameof(StudentId));
            OnPropertyChanged(nameof(StudentLink));
            ((Command)CopyLinkCommand).ChangeCanExecute();
            OnPropertyChanged(nameof(Group));
            RestoreDraft();
            ((Command)SaveCommand).ChangeCanExecute();
        }
    }

    public void SaveDraft()
    {
        if (_originalStudent is null)
            return;

        _appStateService.StudentDraft = new StudentEditDraft(_originalStudent, AverageScore, StudentCardNumber);
        Debug.WriteLine("[Student Draft] Saved: чернетку форми збережено в стані сесії.");
    }

    public void RestoreDraft()
    {
        var draft = _appStateService.StudentDraft;
        if (draft is not null && _originalStudent?.Id == draft.OriginalStudent.Id)
        {
            AverageScore = draft.AverageScore;
            StudentCardNumber = draft.StudentCardNumber;
            Debug.WriteLine("[Student Draft] Restored: чернетку форми відновлено.");
        }
    }

    public void ClearDraft()
    {
        if (_originalStudent is not null
            && _appStateService.StudentDraft?.OriginalStudent.Id == _originalStudent.Id)
            _appStateService.StudentDraft = null;
    }

    private async Task SaveAsync()
    {
        await RunOperationAsync(SaveStudentAsync,
            "Не вдалося завершити збереження. Залишайтеся на цій сторінці та спробуйте ще раз.");
    }

    private async Task SaveStudentAsync()
    {
        if (_originalStudent is null)
            return;

        var updatedStudent = new Student
        {
            Id = _originalStudent.Id,
            FullName = FullName,
            Group = Group,
            AverageScore = AverageScore,
            StudentCardNumber = StudentCardNumber
        };

        await _repository.SaveAsync(updatedStudent);

        var parameters = new ShellNavigationQueryParameters
        {
            { "OriginalStudent", _originalStudent },
            { "UpdatedStudent", updatedStudent }
        };

        await Shell.Current.GoToAsync("..", parameters);
    }

    private async Task RunOperationAsync(Func<Task> operation, string errorMessage)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        SetError(string.Empty);
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Student Details] Error: {ex}");
            SetError(errorMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetError(string message)
    {
        ErrorMessage = message;
        OnPropertyChanged(nameof(ErrorMessage));
        OnPropertyChanged(nameof(HasError));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}
