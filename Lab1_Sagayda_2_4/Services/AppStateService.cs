using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Lab1_Sagayda_2_4.Models;

namespace Lab1_Sagayda_2_4.Services;

public class AppStateService : IAppStateService
{
    private readonly string _storagePath;
    private readonly bool _canSave;

    public AppStateService()
        : this(Path.Combine(FileSystem.Current.AppDataDirectory, "students.json"))
    {
    }

    internal AppStateService(string storagePath)
    {
        _storagePath = storagePath;
        _canSave = LoadStudents();
        Students.CollectionChanged += (_, _) => SaveStudents();
    }

    public ObservableCollection<Student> Students { get; } = new();

    public StudentEditDraft? StudentDraft { get; set; }

    public string StorageError { get; private set; } = string.Empty;

    public bool HasStorageError => !string.IsNullOrEmpty(StorageError);

    public Student? FindStudent(Guid id) => Students.FirstOrDefault(student => student.Id == id);

    private bool LoadStudents()
    {
        try
        {
            if (!File.Exists(_storagePath))
                return true;

            var students = JsonSerializer.Deserialize<List<Student>>(File.ReadAllText(_storagePath))
                ?? throw new JsonException("Student list is null.");
            var ids = new HashSet<Guid>();
            foreach (var student in students)
            {
                if (student is null || student.Id == Guid.Empty || !ids.Add(student.Id)
                    || !double.IsFinite(student.AverageScore)
                    || student.AverageScore < 0 || student.AverageScore > 5)
                {
                    throw new JsonException("Invalid student record.");
                }
            }

            foreach (var student in students)
                Students.Add(student);

            Debug.WriteLine($"[Student Storage] Loaded: {Students.Count}.");
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            SetStorageError("Не вдалося прочитати збережений список. Автозбереження вимкнено, щоб не перезаписати файл даних.", ex);
            return false;
        }
    }

    private void SaveStudents()
    {
        if (!_canSave)
            return;

        try
        {
            // Replace the saved file only after the complete new JSON has been written.
            var temporaryPath = _storagePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(Students));
            File.Move(temporaryPath, _storagePath, overwrite: true);
            StorageError = string.Empty;
            NotifyStorageErrorChanged();
            Debug.WriteLine($"[Student Storage] Saved: {Students.Count}.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            SetStorageError("Не вдалося зберегти список. Останні зміни залишилися лише в поточному сеансі.", ex);
        }
    }

    private void SetStorageError(string message, Exception exception)
    {
        StorageError = message;
        NotifyStorageErrorChanged();
        Debug.WriteLine($"[Student Storage] Error: {exception.Message}");
    }

    private void NotifyStorageErrorChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasStorageError)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
