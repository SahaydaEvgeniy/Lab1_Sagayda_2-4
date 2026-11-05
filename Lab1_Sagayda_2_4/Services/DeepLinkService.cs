using System.Diagnostics;
using Lab1_Sagayda_2_4.ViewModels;

namespace Lab1_Sagayda_2_4.Services;

public class DeepLinkService
{
    private readonly IAppStateService _appStateService;
    private readonly Queue<Uri> _pendingLinks = new();
    private Shell? _shell;
    private bool _isProcessing;

    public DeepLinkService(IAppStateService appStateService)
    {
        _appStateService = appStateService;
    }

    public void ReceiveLink(Uri uri)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _pendingLinks.Enqueue(uri);
            _ = ProcessPendingLinksAsync();
        });
    }

    public void SetShellReady(Shell shell)
    {
        _shell = shell;
        _ = ProcessPendingLinksAsync();
    }

    private async Task ProcessPendingLinksAsync()
    {
        // Cold-start links wait until the main page and Shell navigation are ready.
        if (_shell is null || _isProcessing)
            return;

        _isProcessing = true;
        try
        {
            while (_pendingLinks.TryDequeue(out var uri))
            {
                try
                {
                    await OpenStudentAsync(uri);
                }
                catch (Exception ex)
                {
                    // External requests must not terminate the app via an unobserved task.
                    Debug.WriteLine($"[Deep Link] Error: {ex}");
                }
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    private async Task OpenStudentAsync(Uri uri)
    {
        var shell = _shell!;
        if (!StudentDeepLink.TryGetStudentId(uri, out var id))
        {
            await shell.DisplayAlertAsync("Некоректне посилання",
                "Посилання має містити коректний ідентифікатор студента.", "OK");
            return;
        }

        var student = _appStateService.FindStudent(id);
        if (student is null)
        {
            await shell.DisplayAlertAsync("Студента не знайдено",
                "Цього студента немає у списку на цьому пристрої. Можливо, його видалено.", "OK");
            return;
        }

        if (shell.CurrentPage?.BindingContext is StudentDetailViewModel current
            && current.StudentId == id)
            return;

        // Start from the list so Back and Save return to its ViewModel.
        await shell.GoToAsync("//MainPage");
        await shell.GoToAsync("studentdetail", new ShellNavigationQueryParameters
        {
            { "SelectedStudent", student }
        });
        Debug.WriteLine($"[Deep Link] Opened student: {id}.");
    }
}
