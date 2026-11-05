# Локальний API синхронізації студентів

Окремий ASP.NET Core сервер для лабораторної роботи 4. Потрібен .NET SDK 10.
У PowerShell із папки MAUI-проєкту виконайте:

```powershell
dotnet run --project tools/StudentSyncApi/StudentSyncApi.csproj
```

Залиште цей термінал відкритим. Для зупинки натисніть Ctrl+C.

## Адреси

- Windows і браузер комп'ютера: http://127.0.0.1:5080
- Android-емулятор: http://10.0.2.2:5080

Перевірка у браузері: `/health` повертає `status: ok`, `/students` показує список,
а `/sync/operations` показує прийняті операції. Порожній список `[]` до першого
надсилання є очікуваним. Починаючи з кроку 5, MAUI надсилає чергу при запуску,
відновленні мережі або натисканні «Синхронізувати».

`10.0.2.2` є адресою комп'ютера з погляду стандартного Android-емулятора.
Для фізичного телефона ця адреса не підходить. Сервер за замовчуванням слухає лише
локальний інтерфейс комп'ютера.

## Тестове надсилання

В іншому вікні PowerShell виконайте:

```powershell
$studentId = [guid]::NewGuid().ToString()
$student = @{ Id = $studentId; FullName = 'Тест API'; Group = '2-4'; AverageScore = 4.5 }
$operation = @{
    OperationId = [guid]::NewGuid().ToString()
    StudentId = $studentId
    EntityType = 'Student'
    Operation = 'Insert'
    PayloadJson = ($student | ConvertTo-Json -Compress)
    CreatedAtUtc = [DateTime]::UtcNow.ToString('o')
}
$body = [System.Text.Encoding]::UTF8.GetBytes(($operation | ConvertTo-Json -Compress))
Invoke-RestMethod -Uri 'http://127.0.0.1:5080/sync/operations' -Method Post -ContentType 'application/json; charset=utf-8' -Body $body
```

Після цього відкрийте http://127.0.0.1:5080/students і перевірте тестового студента.
Повторіть останню команду з тим самим `$body`: відповідь матиме `status: Duplicate`,
другий запис не з'явиться. Перезапустіть сервер: запис має зберегтися.

## Контракт

`POST /sync/operations` приймає JSON із полями `operationId`, `studentId`,
`entityType`, `operation`, `payloadJson`, `createdAtUtc`. Регістр назв полів
не має значення; значення `operation` - `Insert`, `Update` або `Delete`.
`payloadJson` є рядком JSON зі студентом, а `createdAtUtc` - часом операції з часовим поясом.

Відповідь містить `operationId`, `studentId`, `status`, `applied`, `duplicate`:

- `Applied`: прийнята зміна оновила серверний запис.
- `IgnoredOlder`: операція прийнята, але сервер уже має новішу версію.
- `Duplicate`: цю саму операцію вже прийнято; повторне надсилання безпечне.
- HTTP 400: некоректний запит.
- HTTP 409: той самий OperationId використано для іншого вмісту.
- HTTP 503: збереження не вдалося, підтвердження не видається; потрібна повторна спроба.

## Збереження та конфлікти

Дані зберігаються в `data/sync-store.json`, який не потрапляє в Git.
Для тестів можна задати іншу папку: `--DataDirectory <абсолютний-шлях>`.
Файл атомарно замінюється після успішного запису. Пошкоджений файл не очищається
автоматично: сервер зупиняє запуск, щоб не перезаписати дані.

Правило конфліктів: пізніший `CreatedAtUtc` перемагає, а при рівності часу
перемагає більший OperationId у порівнянні Guid. Застосовано саме час створення
зміни, а не час отримання сервером. Це навчальна стратегія, залежна від годинника пристрою.
Видалення залишає службовий запис із часом зміни, щоб старіше оновлення не відновило студента.
Insert і Update застосовуються як оновлення або створення за сталим Id.
Прийняті OperationId зберігаються між запусками для захисту від повторів.

Джерела:
- https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/webapplication?view=aspnetcore-10.0
- https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-10.0
