using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using Lab1_Sagayda_2_4.Repositories;
using Microsoft.Maui.Networking;

namespace Lab1_Sagayda_2_4.Services;

public sealed class SyncService : INotifyPropertyChanged, IDisposable
{
    private readonly IStudentRepository _repository;
    private readonly HttpClient _httpClient;
    private readonly IConnectivity _connectivity;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private CancellationTokenSource _lifetime = new();
    private bool _started;
    private int _countRevision;

    public SyncService(IStudentRepository repository, HttpClient httpClient, IConnectivity connectivity)
    {
        _repository = repository;
        _httpClient = httpClient;
        _connectivity = connectivity;
    }

    public bool IsSynchronizing { get; private set; }
    public int? PendingChangeCount { get; private set; }
    public string StatusMessage { get; private set; } = "Ще не синхронізовано.";
    public string PendingSyncSummary => PendingChangeCount is int count
        ? $"Очікують синхронізації: {count}"
        : "Черга синхронізації: кількість невідома";

    public void Start()
    {
        if (_started)
            return;
        if (_lifetime.IsCancellationRequested)
            _lifetime = new CancellationTokenSource();
        _started = true;
        _connectivity.ConnectivityChanged += OnConnectivityChanged;
        _ = SynchronizeAsync();
    }

    public void Stop()
    {
        if (_started)
            _connectivity.ConnectivityChanged -= OnConnectivityChanged;
        _started = false;
        _lifetime.Cancel();
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs args)
    {
        if (args.NetworkAccess is NetworkAccess.Internet or NetworkAccess.Local)
            _ = SynchronizeAsync();
    }

    public async Task RefreshPendingCountAsync()
    {
        var revision = Interlocked.Increment(ref _countRevision);
        int? count;
        try { count = await _repository.GetPendingChangeCountAsync(); }
        catch (Exception ex)
        {
            count = null;
            Debug.WriteLine($"[Sync] Count error: {ex}");
        }
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (revision != Volatile.Read(ref _countRevision))
                return;
            PendingChangeCount = count;
            Notify(nameof(PendingChangeCount));
            Notify(nameof(PendingSyncSummary));
        });
    }

    public async Task SynchronizeAsync()
    {
        var token = _lifetime.Token;
        try { await _syncLock.WaitAsync(token); }
        catch (OperationCanceledException) { return; }

        var acknowledged = 0;
        var ignoredOlder = 0;
        var encryptedFieldSupported = false;
        SetState(true, "Виконується синхронізація…");
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var changes = await _repository.GetPendingChangesAsync();
                if (changes.Count == 0)
                    break;

                foreach (var change in changes)
                {
                    token.ThrowIfCancellationRequested();
                    using var payload = JsonDocument.Parse(change.PayloadJson);
                    if (!encryptedFieldSupported
                        && payload.RootElement.TryGetProperty("StudentCardNumberEncrypted", out var encryptedCard)
                        && encryptedCard.ValueKind == JsonValueKind.String
                        && !string.IsNullOrEmpty(encryptedCard.GetString()))
                    {
                        var health = await _httpClient.GetFromJsonAsync<ApiHealth>("health", token);
                        if (health?.SupportsEncryptedStudentCards != true)
                            throw new NotSupportedException("The API must support encrypted student cards.");
                        encryptedFieldSupported = true;
                    }
                    // sqlite-net restores DateTime ticks without the UTC Kind flag.
                    var timestamp = new DateTimeOffset(DateTime.SpecifyKind(change.CreatedAtUtc, DateTimeKind.Utc));
                    var request = new SyncRequest(change.OperationId, change.StudentId, change.EntityType,
                        change.Operation, change.PayloadJson, timestamp);
                    using var response = await _httpClient.PostAsJsonAsync("sync/operations", request, token);
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"Server returned HTTP {(int)response.StatusCode}.", null, response.StatusCode);

                    var receipt = await response.Content.ReadFromJsonAsync<SyncReceipt>(cancellationToken: token);
                    if (receipt is null || receipt.OperationId != change.OperationId
                        || receipt.StudentId != change.StudentId || !IsValidReceipt(receipt))
                        throw new InvalidDataException("Server returned an invalid acknowledgement.");

                    // Delete only the specifically acknowledged operation, never the whole queue.
                    if (!await _repository.AcknowledgeChangeAsync(change.OperationId))
                        throw new InvalidOperationException("Acknowledged queue record is missing.");
                    acknowledged++;
                    if (receipt.Status == "IgnoredOlder") ignoredOlder++;
                    await RefreshPendingCountAsync();
                    Debug.WriteLine($"[Sync] Acknowledged {change.OperationId}: {receipt.Status}.");
                }
            }

            SetState(true, acknowledged == 0 ? "Немає змін для надсилання."
                : $"Підтверджено операцій: {acknowledged}."
                    + (ignoredOlder > 0 ? $" Сервер уже мав новіші дані для {ignoredOlder} операцій." : ""));
        }
        catch (OperationCanceledException)
        {
            SetState(true, token.IsCancellationRequested ? "Синхронізацію зупинено."
                : "Час очікування сервера минув. Зміни залишилися в черзі.");
        }
        catch (NotSupportedException)
        {
            SetState(true, "Перезапустіть локальний API з оновленим кодом. Зміни залишилися в черзі.");
        }
        catch (HttpRequestException ex)
        {
            Debug.WriteLine($"[Sync] HTTP error: {ex}");
            SetState(true, ex.StatusCode is { } code
                ? $"Сервер повернув HTTP {(int)code}. Непідтверджені зміни залишилися в черзі."
                : "Сервер недоступний. Зміни залишилися в черзі; спробуйте синхронізувати ще раз.");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Sync] Error: {ex}");
            SetState(true, "Синхронізацію перервано. Непідтверджені зміни залишилися в черзі.");
        }
        finally
        {
            await RefreshPendingCountAsync();
            SetState(false, null);
            _syncLock.Release();
        }
    }

    private static bool IsValidReceipt(SyncReceipt receipt) => receipt.Status switch
    {
        "Applied" => receipt.Applied && !receipt.Duplicate,
        "IgnoredOlder" => !receipt.Applied && !receipt.Duplicate,
        "Duplicate" => !receipt.Applied && receipt.Duplicate,
        _ => false
    };

    private void SetState(bool synchronizing, string? message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsSynchronizing = synchronizing;
            if (message is not null) StatusMessage = message;
            Notify(nameof(IsSynchronizing));
            Notify(nameof(StatusMessage));
        });
    }

    private void Notify(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose() { Stop(); _lifetime.Dispose(); }

    private record SyncRequest(Guid OperationId, Guid StudentId, string EntityType,
        string Operation, string PayloadJson, DateTimeOffset CreatedAtUtc);
    private record SyncReceipt(Guid OperationId, Guid StudentId, string Status, bool Applied, bool Duplicate);
    private record ApiHealth(bool SupportsEncryptedStudentCards);
}
