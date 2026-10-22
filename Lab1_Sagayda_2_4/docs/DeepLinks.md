# Перевірка deep link (Android)

Формат: `studentcabinet://student/<Id студента>`.

1. Виберіть Android-емулятор або телефон у Visual Studio й запустіть застосунок.
2. Додайте студента саме на Android: Windows та Android мають окремі локальні списки.
3. Відкрийте деталі й натисніть «Скопіювати посилання» або перепишіть відображений Id.
4. У PowerShell виконайте команду нижче, замінивши `<Id>` на Id цього студента.

```powershell
& 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe' shell am start -W -a android.intent.action.VIEW -d 'studentcabinet://student/<Id>' -p com.companyname.lab1_sagayda_2_4
```

Якщо підключено кілька пристроїв, додайте `-s <серійний-номер>` після шляху до `adb.exe`.

## Сценарії

- Відкритий застосунок: поверніться до списку й виконайте команду. Мають відкритися правильні деталі.
- Холодний запуск: закрийте застосунок командою нижче, потім повторіть відкриття посилання.
- Повернення: кнопка «Назад» веде до списку; «Зберегти» оновлює бал у списку.
- Повторне посилання: відкриті деталі того самого студента не дублюються в стеку навігації.
- Видалений студент: видаліть його та повторіть посилання. Має з'явитися повідомлення «Студента не знайдено».
- Некоректний Id: замініть Id у посиланні на `invalid`. Має з'явитися повідомлення «Некоректне посилання».

```powershell
& 'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe' shell am force-stop com.companyname.lab1_sagayda_2_4
```

Цей крок реєструє зовнішні посилання для Android. На Windows доступні відображення й копіювання посилання, але його запуск через Windows не налаштований.

Реалізація використовує Android Intent Filter, `OnCreate`/`OnNewIntent` та `OnAppLinkRequestReceived`.
Джерело: [Microsoft Learn: Android app links](https://learn.microsoft.com/en-us/dotnet/maui/android/app-links?view=net-maui-10.0).
