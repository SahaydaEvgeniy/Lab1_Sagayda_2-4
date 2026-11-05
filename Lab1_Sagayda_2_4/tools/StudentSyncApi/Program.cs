using System.Text.Json;
using StudentSyncApi;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["urls"] ?? "http://127.0.0.1:5080");
builder.Services.AddSingleton(new SyncStore(builder.Configuration["DataDirectory"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "data")));
var app = builder.Build();
await app.Services.GetRequiredService<SyncStore>().InitializeAsync();

app.MapGet("/", () => Results.Ok(new
{
    service = "Student Sync API",
    endpoints = new[] { "GET /health", "GET /students", "GET /students/{id}",
        "GET /sync/operations", "POST /sync/operations" }
}));
app.MapGet("/health", () => Results.Ok(new { status = "ok", supportsEncryptedStudentCards = true }));
app.MapGet("/students", async (SyncStore store) => Results.Ok(await store.GetStudentsAsync()));
app.MapGet("/students/{id:guid}", async Task<IResult> (Guid id, SyncStore store) =>
    await store.GetStudentAsync(id) is { } student ? Results.Ok(student) : Results.NotFound());
app.MapGet("/sync/operations", async (SyncStore store) => Results.Ok(await store.GetOperationsAsync()));
app.MapPost("/sync/operations", async Task<IResult> (SyncOperation request, SyncStore store,
    ILogger<SyncStore> logger) =>
{
    if (request.OperationId == Guid.Empty || request.StudentId == Guid.Empty
        || request.EntityType != "Student" || request.Operation is not ("Insert" or "Update" or "Delete")
        || request.CreatedAtUtc == default || string.IsNullOrWhiteSpace(request.PayloadJson))
        return Results.BadRequest(new { error = "Invalid operation metadata." });

    StudentData? student;
    try { student = JsonSerializer.Deserialize<StudentData>(request.PayloadJson, SyncStore.JsonOptions); }
    catch (JsonException) { return Results.BadRequest(new { error = "PayloadJson is not a valid student." }); }
    if (student is null || student.Id != request.StudentId
        || string.IsNullOrWhiteSpace(student.FullName) || student.Group is null
        || !double.IsFinite(student.AverageScore) || student.AverageScore < 0 || student.AverageScore > 5)
        return Results.BadRequest(new { error = "Invalid student payload or mismatched StudentId." });

    try { return Results.Ok(await store.ApplyAsync(request, student)); }
    catch (OperationConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
    {
        logger.LogError(ex, "Could not persist operation {OperationId}", request.OperationId);
        return Results.Problem("The operation could not be saved. Retry it later.", statusCode: 503);
    }
});
await app.RunAsync();
