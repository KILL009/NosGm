// SPDX-License-Identifier: MIT

using System.Windows;
using System.Windows.Controls;

namespace NosGM.Launcher;

internal sealed record LauncherCredentials(string AccountName, string Password);

internal sealed class LauncherLoginDialog : Window
{
    private sealed record LoginText(
        string Title,
        string Description,
        string Account,
        string Password,
        string Login,
        string Cancel,
        string Required,
        string Authenticating,
        string StartedDetail);

    private static readonly IReadOnlyDictionary<string, LoginText> Catalog =
        new Dictionary<string, LoginText>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = new("Iniciar sesión", "Usa tu cuenta de NosGM. La contraseña no se guardará.", "Cuenta", "Contraseña", "Entrar", "Cancelar", "Escribe la cuenta y la contraseña.", "Autenticando cuenta...", "Autenticación moderna completada y cliente iniciado."),
            ["en"] = new("Sign in", "Use your NosGM account. The password will not be saved.", "Account", "Password", "Sign in", "Cancel", "Enter the account and password.", "Authenticating account...", "Modern authentication completed and the client was started."),
            ["de"] = new("Anmelden", "NosGM-Konto verwenden. Das Passwort wird nicht gespeichert.", "Konto", "Passwort", "Anmelden", "Abbrechen", "Konto und Passwort eingeben.", "Konto wird authentifiziert...", "Moderne Authentifizierung abgeschlossen und Client gestartet."),
            ["fr"] = new("Connexion", "Utilisez votre compte NosGM. Le mot de passe ne sera pas enregistré.", "Compte", "Mot de passe", "Connexion", "Annuler", "Saisissez le compte et le mot de passe.", "Authentification du compte...", "Authentification moderne terminée et client lancé."),
            ["it"] = new("Accedi", "Usa il tuo account NosGM. La password non verrà salvata.", "Account", "Password", "Accedi", "Annulla", "Inserisci account e password.", "Autenticazione account...", "Autenticazione moderna completata e client avviato."),
            ["pl"] = new("Logowanie", "Użyj konta NosGM. Hasło nie zostanie zapisane.", "Konto", "Hasło", "Zaloguj", "Anuluj", "Wpisz konto i hasło.", "Uwierzytelnianie konta...", "Nowoczesne uwierzytelnianie zakończone i klient uruchomiony."),
            ["cz"] = new("Přihlášení", "Použijte účet NosGM. Heslo nebude uloženo.", "Účet", "Heslo", "Přihlásit", "Zrušit", "Zadejte účet a heslo.", "Ověřování účtu...", "Moderní ověření dokončeno a klient spuštěn."),
            ["ru"] = new("Вход", "Используйте учетную запись NosGM. Пароль не будет сохранен.", "Учетная запись", "Пароль", "Войти", "Отмена", "Введите учетную запись и пароль.", "Проверка учетной записи...", "Современная авторизация завершена, клиент запущен."),
            ["jp"] = new("ログイン", "NosGMアカウントを使用します。パスワードは保存されません。", "アカウント", "パスワード", "ログイン", "キャンセル", "アカウントとパスワードを入力してください。", "アカウントを認証しています...", "最新認証が完了し、クライアントを起動しました。"),
            ["cn"] = new("登录", "使用你的 NosGM 账号。密码不会被保存。", "账号", "密码", "登录", "取消", "请输入账号和密码。", "正在验证账号...", "现代登录验证完成，客户端已启动。")
        };

    private readonly TextBox _accountTextBox;
    private readonly PasswordBox _passwordBox;
    private readonly LoginText _text;

    private LauncherLoginDialog(string language, string initialAccountName)
    {
        _text = GetText(language);
        Title = _text.Title;
        Width = 440;
        Height = 300;
        MinWidth = 400;
        MinHeight = 280;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;

        var root = new Grid { Margin = new Thickness(24) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var description = new TextBlock
        {
            Text = _text.Description,
            TextWrapping = TextWrapping.Wrap,
            Foreground = System.Windows.Media.Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 18)
        };
        Grid.SetRow(description, 0);
        root.Children.Add(description);

        var accountLabel = new TextBlock
        {
            Text = _text.Account,
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetRow(accountLabel, 1);
        root.Children.Add(accountLabel);

        _accountTextBox = new TextBox
        {
            Text = initialAccountName,
            Margin = new Thickness(0, 6, 0, 14),
            MaxLength = 255
        };
        Grid.SetRow(_accountTextBox, 2);
        root.Children.Add(_accountTextBox);

        var passwordLabel = new TextBlock
        {
            Text = _text.Password,
            FontWeight = FontWeights.SemiBold
        };
        Grid.SetRow(passwordLabel, 3);
        root.Children.Add(passwordLabel);

        _passwordBox = new PasswordBox
        {
            Margin = new Thickness(0, 6, 0, 16),
            MaxLength = 1024
        };
        Grid.SetRow(_passwordBox, 4);
        root.Children.Add(_passwordBox);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var cancelButton = new Button
        {
            Content = _text.Cancel,
            MinWidth = 92,
            IsCancel = true
        };
        var loginButton = new Button
        {
            Content = _text.Login,
            MinWidth = 92,
            Margin = new Thickness(8, 0, 0, 0),
            IsDefault = true
        };
        loginButton.Click += Login_Click;
        buttons.Children.Add(cancelButton);
        buttons.Children.Add(loginButton);
        Grid.SetRow(buttons, 5);
        root.Children.Add(buttons);

        Content = root;
        Loaded += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_accountTextBox.Text))
            {
                _accountTextBox.Focus();
            }
            else
            {
                _passwordBox.Focus();
            }
        };
    }

    public LauncherCredentials? Credentials { get; private set; }

    public static LauncherCredentials? Prompt(
        Window owner,
        string language,
        string initialAccountName)
    {
        var dialog = new LauncherLoginDialog(language, initialAccountName)
        {
            Owner = owner
        };
        return dialog.ShowDialog() == true ? dialog.Credentials : null;
    }

    public static string Authenticating(string language) => GetText(language).Authenticating;

    public static string StartedDetail(string language) => GetText(language).StartedDetail;

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var accountName = _accountTextBox.Text.Trim();
        var password = _passwordBox.Password;
        if (string.IsNullOrWhiteSpace(accountName) || string.IsNullOrEmpty(password))
        {
            MessageBox.Show(this, _text.Required, _text.Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Credentials = new LauncherCredentials(accountName, password);
        DialogResult = true;
    }

    private static LoginText GetText(string language)
        => Catalog.TryGetValue(language, out var text) ? text : Catalog["en"];
}
