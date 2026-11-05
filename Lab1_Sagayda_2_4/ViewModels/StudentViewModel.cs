using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Lab1_Sagayda_2_4.Models;
using Lab1_Sagayda_2_4.Services;
using Lab1_Sagayda_2_4.Repositories;

namespace Lab1_Sagayda_2_4.ViewModels;

public class StudentViewModel : INotifyPropertyChanged, IQueryAttributable
{
    private readonly Student _student = new();

    private readonly IAppStateService _appStateService;

    private readonly IStudentRepository _repository;
    private bool _isBusy;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
            ((Command)LoadCommand).ChangeCanExecute();
            ((Command)AddStudentCommand).ChangeCanExecute();
            ((Command)DeleteStudentCommand).ChangeCanExecute();
            ((Command)OpenDetailsCommand).ChangeCanExecute();
            ((Command)OpenSettingsCommand).ChangeCanExecute();
            ((Command)SyncCommand).ChangeCanExecute();
        }
    }

    public string ErrorMessage { get; private set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public SyncService Sync { get; }
    public ICommand SyncCommand { get; }

    public ObservableCollection<Student> Students { get; private set; }

    public ICommand LoadCommand { get; }

    public ICommand OpenSettingsCommand { get; }

    public ICommand AddStudentCommand { get; }

    public ICommand OpenDetailsCommand { get; }

    public ICommand DeleteStudentCommand { get; }

    public StudentViewModel(IAppStateService appStateService, IStudentRepository repository, SyncService syncService)
    {
        _appStateService = appStateService;
        _repository = repository;
        Sync = syncService;
        Students = appStateService.Students;
        LoadCommand = new Command(async () => await RunOperationAsync(LoadStudentsAsync,
            "Не вдалося завантажити студентів. Натисніть «Оновити список», щоб повторити."), () => !IsBusy);
        OpenSettingsCommand = new Command(async () =>
            await RunOperationAsync(() => Shell.Current.GoToAsync("settings"),
                "Не вдалося відкрити налаштування."), () => !IsBusy);
        AddStudentCommand = new Command(async () => await RunOperationAsync(AddStudentAsync,
            "Не вдалося додати студента. Дані форми залишено для повторної спроби."), CanAddStudent);
        DeleteStudentCommand = new Command<Student>(async student =>
            await RunOperationAsync(() => DeleteStudentAsync(student),
                "Не вдалося видалити студента. Спробуйте ще раз."), student => student is not null && !IsBusy);
        OpenDetailsCommand = new Command<Student>(async student =>
            await RunOperationAsync(() => OpenDetailsAsync(student),
                "Не вдалося відкрити деталі студента."), student => student is not null && !IsBusy);
        SyncCommand = new Command(async () => await Sync.SynchronizeAsync(), () => !IsBusy && !Sync.IsSynchronizing);
    }

    public void ObserveSync() => Sync.PropertyChanged += OnSyncChanged;
    public void StopObservingSync() => Sync.PropertyChanged -= OnSyncChanged;

    private void OnSyncChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(SyncService.IsSynchronizing))
            ((Command)SyncCommand).ChangeCanExecute();
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

    public string StudentCardNumber
    {
        get => _student.StudentCardNumber;
        set
        {
            if (_student.StudentCardNumber == value) return;
            _student.StudentCardNumber = value;
            OnPropertyChanged(nameof(StudentCardNumber));
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

    private async Task LoadStudentsAsync()
    {
        var students = await _repository.GetAllAsync();
        // Replace the UI snapshot only after a successful read. It is not a storage trigger.
        Students.Clear();
        foreach (var student in students)
            Students.Add(student);
        Debug.WriteLine($"[Student List] Loaded from SQLite: {Students.Count}.");
        await RefreshPendingCountAsync();
    }

    private async Task AddStudentAsync()
    {
        var student = new Student
        {
            FullName = FullName,
            Group = Group,
            AverageScore = AverageScore,
            StudentCardNumber = StudentCardNumber
        };
        await _repository.SaveAsync(student);
        Students.Add(student);

        FullName = string.Empty;
        Group = string.Empty;
        AverageScore = 0;
        StudentCardNumber = string.Empty;
        await RefreshPendingCountAsync();
    }

    private async Task DeleteStudentAsync(Student? student)
    {
        if (student is not null)
        {
            await _repository.DeleteAsync(student);
            var current = _appStateService.FindStudent(student.Id);
            if (current is not null)
                Students.Remove(current);
            await RefreshPendingCountAsync();
        }
    }

    private async Task OpenDetailsAsync(Student? student)
    {
        if (student is null)
            return;

        var parameters = new ShellNavigationQueryParameters
        {
            { "SelectedStudent", student }
        };

        await Shell.Current.GoToAsync("studentdetail", parameters);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("OriginalStudent", out var originalValue)
            && originalValue is Student originalStudent
            && query.TryGetValue("UpdatedStudent", out var updatedValue)
            && updatedValue is Student updatedStudent)
        {
            var existingStudent = _appStateService.FindStudent(originalStudent.Id);
            var index = existingStudent is null ? -1 : Students.IndexOf(existingStudent);
            if (index >= 0)
                Students[index] = updatedStudent;
        }
    }

    private Task RefreshPendingCountAsync() => Sync.RefreshPendingCountAsync();

    private bool CanAddStudent()
    {
        return !IsBusy && !string.IsNullOrWhiteSpace(FullName);
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
            Debug.WriteLine($"[Student List] Error: {ex}");
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
