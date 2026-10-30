using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Input;
using System.Windows.Media.Effects;
using Path = System.IO.Path;

namespace CyberCacti;

public partial class MainWindow : Window
{
    private readonly string folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CyberCacti");

    private string username = "";
    private bool[] tasks = new bool[5];
    private int highScore;
    private bool registering;
    private string currentPage = "Home";

    private Grid? content;
    private Canvas? gameCanvas;
    private TextBlock? gameInfo;
    private TextBlock? gameScore;
    private TextBlock? gameLives;
    private TextBlock? gameBest;
    private DispatcherTimer? timer;

    private readonly List<GameObject> objects = new();
    private readonly List<Button> navButtons = new();

    private int lane = 1, score, lives = 3, ticks;
    private bool playing;
    private readonly Random random = new();

    private class GameObject
    {
        public FrameworkElement Shape = new Border();
        public int Lane;
        public double Y;
        public bool Apple;
    }

    private class Account
    {
        public string Username { get; set; } = "";
        public string Salt { get; set; } = "";
        public string Hash { get; set; } = "";
    }

    private class SavedData
    {
        public bool[] Tasks { get; set; } = new bool[5];
        public int HighScore { get; set; }
    }

    private string AccountsFile => Path.Combine(folder, "accounts.json");
    private string ProgressFile => Path.Combine(folder, "progress.json");

    // Colour palette
    private static SolidColorBrush B(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private readonly Brush forest = B("#244B3B");
    private readonly Brush deepForest = B("#18382D");
    private readonly Brush cream = B("#F6F5EF");
    private readonly Brush sage = B("#E7EEE3");
    private readonly Brush pale = B("#F0F5ED");
    private readonly Brush gold = B("#C69B59");
    private readonly Brush ink = B("#26372E");
    private readonly Brush muted = B("#7D8980");
    private readonly Brush line = B("#E7EBE3");
    private readonly Brush white = Brushes.White;
    private readonly Brush red = B("#C65D4C");

    public MainWindow()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        WindowState = WindowState.Maximized;
        MinWidth = 950;
        MinHeight = 650;
        Background = cream;
        Title = "Cyber Cacti";

        PreviewKeyDown += GameKeyDown;

        LoadProgress();
        ShowLogin();
    }

    // =========================================================
    // DESIGN SYSTEM
    // =========================================================

    private TextBlock T(
        string value,
        double size = 14,
        bool bold = false,
        Brush? colour = null)
    {
        return new TextBlock
        {
            Text = value,
            FontSize = size,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = colour ?? ink,
            TextWrapping = TextWrapping.Wrap,
            LineHeight = size * 1.42,
            FontFamily = new FontFamily("Segoe UI"),
            Margin = new Thickness(0, 2, 0, 5)
        };
    }

    private Border Card(
        UIElement child,
        Thickness? padding = null,
        Brush? background = null)
    {
        return new Border
        {
            Background = background ?? white,
            CornerRadius = new CornerRadius(22),
            Padding = padding ?? new Thickness(24),
            Margin = new Thickness(0, 0, 0, 17),
            BorderBrush = line,
            BorderThickness = new Thickness(1),
            Child = child,
            Effect = new DropShadowEffect
            {
                BlurRadius = 22,
                ShadowDepth = 3,
                Opacity = 0.055,
                Color = Colors.Black
            }
        };
    }

    private Button Btn(
        string label,
        Action action,
        bool primary = false,
        bool compact = false)
    {
        var button = new Button
        {
            Content = label,
            Padding = compact
                ? new Thickness(15, 9, 15, 9)
                : new Thickness(21, 13, 21, 13),
            Margin = new Thickness(0, 5, 9, 5),
            FontSize = compact ? 12 : 14,
            FontWeight = FontWeights.SemiBold,
            Background = primary ? forest : sage,
            Foreground = primary ? white : forest,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            FontFamily = new FontFamily("Segoe UI"),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));

        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(15));
        border.SetValue(
            Border.BackgroundProperty,
            new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(
            Border.PaddingProperty,
            new TemplateBindingExtension(Control.PaddingProperty));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            HorizontalAlignment.Center);
        presenter.SetValue(
            ContentPresenter.VerticalAlignmentProperty,
            VerticalAlignment.Center);

        border.AppendChild(presenter);
        template.VisualTree = border;
        button.Template = template;

        button.MouseEnter += (_, _) => button.Opacity = 0.82;
        button.MouseLeave += (_, _) => button.Opacity = 1;
        button.Click += (_, _) => action();

        return button;
    }

    private TextBox MakeTextBox(string tip = "")
    {
        return new TextBox
        {
            FontSize = 15,
            Padding = new Thickness(15, 12, 15, 12),
            Margin = new Thickness(0, 6, 0, 14),
            Background = B("#FAFBF8"),
            Foreground = ink,
            BorderBrush = B("#E0E7DD"),
            BorderThickness = new Thickness(1),
            FontFamily = new FontFamily("Segoe UI"),
            Height = 48,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = tip
        };
    }

    private PasswordBox MakePasswordBox()
    {
        return new PasswordBox
        {
            FontSize = 15,
            Padding = new Thickness(15, 12, 15, 12),
            Margin = new Thickness(0, 6, 0, 14),
            Background = B("#FAFBF8"),
            Foreground = ink,
            BorderBrush = B("#E0E7DD"),
            BorderThickness = new Thickness(1),
            FontFamily = new FontFamily("Segoe UI"),
            Height = 48,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private Border Pill(string label, Brush? bg = null, Brush? fg = null)
    {
        return new Border
        {
            Background = bg ?? sage,
            CornerRadius = new CornerRadius(30),
            Padding = new Thickness(12, 6, 12, 6),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = T(label, 10, true, fg ?? forest)
        };
    }

    private void AddHeading(Panel panel, string title, string subtitle)
    {
        panel.Children.Add(T(title, 31, true, forest));
        panel.Children.Add(T(subtitle, 15, false, muted));

        panel.Children.Add(new Border
        {
            Width = 48,
            Height = 3,
            Background = gold,
            HorizontalAlignment = HorizontalAlignment.Left,
            CornerRadius = new CornerRadius(2),
            Margin = new Thickness(0, 13, 0, 22)
        });
    }

    private ProgressBar MakeProgress(double value)
    {
        return new ProgressBar
        {
            Minimum = 0,
            Maximum = 5,
            Value = Math.Clamp(value, 0, 5),
            Height = 10,
            Background = sage,
            Foreground = forest,
            BorderThickness = new Thickness(0)
        };
    }

    private Grid TwoColumn(UIElement left, UIElement right, double gap = 14)
    {
        var grid = new Grid();

        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        left.SetValue(
            FrameworkElement.MarginProperty,
            new Thickness(0, 0, gap / 2, 0));

        right.SetValue(
            FrameworkElement.MarginProperty,
            new Thickness(gap / 2, 0, 0, 0));

        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);

        grid.Children.Add(left);
        grid.Children.Add(right);

        return grid;
    }

    // =========================================================
    // CUSTOM ILLUSTRATIONS
    // =========================================================

    private void AddHedgehog(Canvas canvas, double x, double y, double scale = 1)
    {
        var spikes = new Ellipse
        {
            Width = 94 * scale,
            Height = 65 * scale,
            Fill = B("#665348"),
            Stroke = B("#514237"),
            StrokeThickness = 1
        };

        Canvas.SetLeft(spikes, x);
        Canvas.SetTop(spikes, y);
        canvas.Children.Add(spikes);

        for (int i = 0; i < 9; i++)
        {
            var spike = new Line
            {
                X1 = x + (10 + i * 9) * scale,
                Y1 = y + 13 * scale,
                X2 = x + (5 + i * 10) * scale,
                Y2 = y - (3 + (i % 3) * 5) * scale,
                Stroke = B("#493A32"),
                StrokeThickness = 3 * scale,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            canvas.Children.Add(spike);
        }

        var face = new Ellipse
        {
            Width = 54 * scale,
            Height = 43 * scale,
            Fill = B("#E6B98D")
        };

        Canvas.SetLeft(face, x + 53 * scale);
        Canvas.SetTop(face, y + 21 * scale);
        canvas.Children.Add(face);

        var ear = new Ellipse
        {
            Width = 15 * scale,
            Height = 15 * scale,
            Fill = B("#D99D7C")
        };

        Canvas.SetLeft(ear, x + 65 * scale);
        Canvas.SetTop(ear, y + 15 * scale);
        canvas.Children.Add(ear);

        var eye = new Ellipse
        {
            Width = 5 * scale,
            Height = 5 * scale,
            Fill = ink
        };

        Canvas.SetLeft(eye, x + 91 * scale);
        Canvas.SetTop(eye, y + 35 * scale);
        canvas.Children.Add(eye);

        var nose = new Ellipse
        {
            Width = 9 * scale,
            Height = 7 * scale,
            Fill = B("#493A32")
        };

        Canvas.SetLeft(nose, x + 104 * scale);
        Canvas.SetTop(nose, y + 46 * scale);
        canvas.Children.Add(nose);
    }

    private void AddCactus(Canvas canvas, double x, double y, double scale = 1)
    {
        Brush cactus = B("#78A27A");

        var pot = new Border
        {
            Width = 42 * scale,
            Height = 27 * scale,
            Background = B("#C78E67"),
            CornerRadius = new CornerRadius(4, 4, 9, 9)
        };

        Canvas.SetLeft(pot, x + 9 * scale);
        Canvas.SetTop(pot, y + 70 * scale);
        canvas.Children.Add(pot);

        var body = new Border
        {
            Width = 25 * scale,
            Height = 76 * scale,
            Background = cactus,
            CornerRadius = new CornerRadius(13 * scale)
        };

        Canvas.SetLeft(body, x + 17 * scale);
        Canvas.SetTop(body, y);
        canvas.Children.Add(body);

        var arm = new Border
        {
            Width = 30 * scale,
            Height = 13 * scale,
            Background = cactus,
            CornerRadius = new CornerRadius(8 * scale)
        };

        Canvas.SetLeft(arm, x - 1 * scale);
        Canvas.SetTop(arm, y + 35 * scale);
        canvas.Children.Add(arm);

        var armUp = new Border
        {
            Width = 13 * scale,
            Height = 27 * scale,
            Background = cactus,
            CornerRadius = new CornerRadius(7 * scale)
        };

        Canvas.SetLeft(armUp, x - 1 * scale);
        Canvas.SetTop(armUp, y + 16 * scale);
        canvas.Children.Add(armUp);
    }

    // =========================================================
    // LOCAL STORAGE AND ACCOUNTS
    // =========================================================

    private void LoadProgress()
    {
        try
        {
            Directory.CreateDirectory(folder);

            if (!File.Exists(ProgressFile))
                return;

            var data = JsonSerializer.Deserialize<SavedData>(
                File.ReadAllText(ProgressFile));

            if (data == null)
                return;

            tasks = data.Tasks != null && data.Tasks.Length == 5
                ? data.Tasks
                : new bool[5];

            highScore = Math.Max(0, data.HighScore);
        }
        catch
        {
            tasks = new bool[5];
            highScore = 0;
        }
    }

    private void SaveProgress()
    {
        try
        {
            Directory.CreateDirectory(folder);

            File.WriteAllText(
                ProgressFile,
                JsonSerializer.Serialize(new SavedData
                {
                    Tasks = tasks,
                    HighScore = highScore
                }));
        }
        catch
        {
            MessageBox.Show(
                "Your progress could not be saved. Please check your computer's storage permissions.",
                "Cyber Cacti",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private List<Account> GetAccounts()
    {
        try
        {
            Directory.CreateDirectory(folder);

            if (File.Exists(AccountsFile))
            {
                return JsonSerializer.Deserialize<List<Account>>(
                    File.ReadAllText(AccountsFile)) ?? new();
            }
        }
        catch
        {
            MessageBox.Show(
                "The account file could not be read.",
                "Cyber Cacti",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        return new();
    }

    private bool Authenticate(
        string name,
        string password,
        bool register,
        out string message)
    {
        name = name.Trim();

        if (register &&
            (name.Length < 3 || name.Length > 20 ||
             name.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-')))
        {
            message = "Your username must be 3–20 characters. Use letters, numbers, _ or -.";
            return false;
        }

        if (register && password.Length < 8)
        {
            message = "Your password must contain at least 8 characters.";
            return false;
        }

        var accounts = GetAccounts();

        var account = accounts.FirstOrDefault(a =>
            string.Equals(
                a.Username,
                name,
                StringComparison.OrdinalIgnoreCase));

        if (register)
        {
            if (account != null)
            {
                message = "That username is already taken. Please choose another.";
                return false;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                120000,
                HashAlgorithmName.SHA256,
                32);

            accounts.Add(new Account
            {
                Username = name,
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(hash)
            });

            try
            {
                Directory.CreateDirectory(folder);
                File.WriteAllText(
                    AccountsFile,
                    JsonSerializer.Serialize(accounts));
            }
            catch
            {
                message = "Your account could not be saved. Please try again.";
                return false;
            }

            username = name;
            message = "Your account has been created.";
            return true;
        }

        if (account == null)
        {
            message = "We couldn't find that account on this computer.";
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(account.Salt);
            byte[] stored = Convert.FromBase64String(account.Hash);

            byte[] entered = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                120000,
                HashAlgorithmName.SHA256,
                32);

            if (!CryptographicOperations.FixedTimeEquals(stored, entered))
            {
                message = "That password isn't correct. Please try again.";
                return false;
            }
        }
        catch
        {
            message = "This account's saved details could not be read.";
            return false;
        }

        username = account.Username;
        message = "Welcome back.";
        return true;
    }

    // =========================================================
    // LOGIN SCREEN
    // =========================================================

    private void ShowLogin()
    {
        StopGame();

        Root.Children.Clear();
        Root.Background = cream;

        var layout = new Grid { Margin = new Thickness(34) };

        layout.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });

        layout.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) });

        var left = new Border
        {
            Background = deepForest,
            CornerRadius = new CornerRadius(30),
            Padding = new Thickness(48),
            Margin = new Thickness(0, 0, 16, 0),
            ClipToBounds = true
        };

        var intro = new Grid();

        var introStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 490
        };

        introStack.Children.Add(T("🌵  CYBER CACTI", 14, true, B("#DCE8D8")));
        introStack.Children.Add(T("Small actions.\nBig impact.", 47, true, white));

        introStack.Children.Add(T(
            "Helping create a safer world for hedgehogs, one action at a time.",
            18,
            false,
            B("#D5E2D5")));

        introStack.Children.Add(new Border
        {
            Height = 3,
            Width = 54,
            Background = gold,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 22, 0, 20)
        });

        introStack.Children.Add(T(
            "LEARN  ·  PROTECT  ·  MAKE A DIFFERENCE",
            10,
            true,
            B("#D5E2D5")));

        intro.Children.Add(introStack);

        var art = new Canvas
        {
            Width = 240,
            Height = 160,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 4, 5),
            IsHitTestVisible = false
        };

        AddCactus(art, 30, 42, 1.1);
        AddHedgehog(art, 105, 76, 0.85);

        intro.Children.Add(art);
        left.Child = intro;

        Grid.SetColumn(left, 0);
        layout.Children.Add(left);

        var form = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        form.Children.Add(T("Welcome to Cyber Cacti", 29, true, forest));
        form.Children.Add(T(
            "Sign in to continue your journey.",
            14,
            false,
            muted));

        var modes = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 18, 0, 17)
        };

        modes.Children.Add(Btn(
            "Sign in",
            () => { registering = false; ShowLogin(); },
            !registering));

        modes.Children.Add(Btn(
            "Create account",
            () => { registering = true; ShowLogin(); },
            registering));

        form.Children.Add(modes);

        form.Children.Add(T("Username", 12, true));

        var name = MakeTextBox("Enter your username");
        form.Children.Add(name);

        form.Children.Add(T("Password", 12, true));

        var password = MakePasswordBox();
        form.Children.Add(password);

        PasswordBox? confirm = null;

        if (registering)
        {
            form.Children.Add(T("Confirm password", 12, true));

            confirm = MakePasswordBox();
            form.Children.Add(confirm);

            form.Children.Add(T(
                "Use at least 8 characters.",
                11,
                false,
                muted));
        }

        var feedback = T("", 12, true, red);
        form.Children.Add(feedback);

        var submit = Btn(
            registering ? "Create my account  →" : "Sign in  →",
            () =>
            {
                if (registering &&
                    confirm != null &&
                    password.Password != confirm.Password)
                {
                    feedback.Text = "Your passwords don't match.";
                    return;
                }

                if (Authenticate(
                    name.Text,
                    password.Password,
                    registering,
                    out string message))
                {
                    registering = false;
                    ShowApp();
                }
                else
                {
                    feedback.Text = message;
                }
            },
            true);

        submit.HorizontalAlignment = HorizontalAlignment.Stretch;
        form.Children.Add(submit);

        form.Children.Add(T(
            "Your account and progress are stored locally on this computer.",
            11,
            false,
            muted));

        var card = Card(form, new Thickness(34));
        card.Margin = new Thickness(16, 0, 0, 0);
        card.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetColumn(card, 1);
        layout.Children.Add(card);

        Root.Children.Add(layout);

        name.Focus();

        password.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                submit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        };
    }

    // =========================================================
    // MAIN APPLICATION AND NAVIGATION
    // =========================================================

    private void ShowApp(string page = "Home")
    {
        StopGame();

        Root.Children.Clear();
        Root.Background = cream;

        var shell = new Grid();

        shell.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(252) });

        shell.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var sidebar = new Grid { Background = deepForest };

        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        sidebar.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var brand = new StackPanel
        {
            Margin = new Thickness(25, 29, 20, 28)
        };

        brand.Children.Add(T("🌵  CYBER CACTI", 19, true, white));
        brand.Children.Add(T("HEDGEHOG AWARENESS", 9, true, B("#BFD0C1")));

        Grid.SetRow(brand, 0);
        sidebar.Children.Add(brand);

        var navScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var nav = new StackPanel
        {
            Margin = new Thickness(12, 0, 12, 10)
        };

        string[] pages =
        {
            "Home",
            "Dashboard",
            "About Us",
            "Hedgehogs",
            "How to Help",
            "More",
            "Innovation Project",
            "Contact Us",
            "Dash Game"
        };

        navButtons.Clear();

        foreach (string item in pages)
            nav.Children.Add(CreateNavButton(item));

        navScroll.Content = nav;

        Grid.SetRow(navScroll, 1);
        sidebar.Children.Add(navScroll);

        var userPanel = new Border
        {
            Background = B("#214536"),
            Padding = new Thickness(16),
            Margin = new Thickness(13, 10, 13, 18),
            CornerRadius = new CornerRadius(18)
        };

        var userStack = new StackPanel();

        userStack.Children.Add(T("SIGNED IN AS", 9, true, B("#BFD0C1")));
        userStack.Children.Add(T(username, 15, true, white));

        var signOut = Btn(
            "Sign out",
            () =>
            {
                username = "";
                ShowLogin();
            },
            false,
            true);

        signOut.Background = B("#DDE9D9");
        signOut.Margin = new Thickness(0, 10, 0, 0);

        userStack.Children.Add(signOut);
        userPanel.Child = userStack;

        Grid.SetRow(userPanel, 2);
        sidebar.Children.Add(userPanel);

        var main = new Grid { Background = cream };

        main.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        main.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var top = new Border
        {
            Background = white,
            BorderBrush = line,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(30, 17, 30, 17)
        };

        var topGrid = new Grid();

        topGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        topGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });

        var topText = new StackPanel();

        topText.Children.Add(T("YOUR HEDGEHOG CONSERVATION SPACE", 9, true, muted));
        topText.Children.Add(T("Welcome back, " + username, 17, true, forest));

        Grid.SetColumn(topText, 0);
        topGrid.Children.Add(topText);

        var profile = Pill("●  LOCAL PROFILE", pale, forest);
        profile.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetColumn(profile, 1);
        topGrid.Children.Add(profile);

        top.Child = topGrid;

        Grid.SetRow(top, 0);
        main.Children.Add(top);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(30, 27, 30, 25)
        };

        content = new Grid
        {
            MaxWidth = 1250,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        scroll.Content = content;

        Grid.SetRow(scroll, 1);
        main.Children.Add(scroll);

        Grid.SetColumn(sidebar, 0);
        Grid.SetColumn(main, 1);

        shell.Children.Add(sidebar);
        shell.Children.Add(main);

        Root.Children.Add(shell);

        ShowPage(page);
    }

    private Button CreateNavButton(string item)
    {
        string glyph = item switch
        {
            "Home" => "⌂",
            "Dashboard" => "▦",
            "About Us" => "○",
            "Hedgehogs" => "✿",
            "How to Help" => "✓",
            "More" => "···",
            "Innovation Project" => "✧",
            "Contact Us" => "✉",
            "Dash Game" => "▶",
            _ => "•"
        };

        var button = new Button
        {
            Content = glyph + "     " + item,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(16, 13, 12, 13),
            Margin = new Thickness(0, 3, 0, 3),
            Background = item == currentPage ? B("#315B47") : Brushes.Transparent,
            Foreground = item == currentPage ? white : B("#DCE7DC"),
            BorderThickness = new Thickness(0),
            FontSize = 13,
            FontWeight = item == currentPage
                ? FontWeights.SemiBold
                : FontWeights.Medium,
            Cursor = Cursors.Hand,
            FontFamily = new FontFamily("Segoe UI")
        };

        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));

        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(13));
        border.SetValue(
            Border.BackgroundProperty,
            new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(
            Border.PaddingProperty,
            new TemplateBindingExtension(Control.PaddingProperty));

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(
            ContentPresenter.HorizontalAlignmentProperty,
            HorizontalAlignment.Left);
        presenter.SetValue(
            ContentPresenter.VerticalAlignmentProperty,
            VerticalAlignment.Center);

        border.AppendChild(presenter);
        template.VisualTree = border;
        button.Template = template;

        button.Click += (_, _) => ShowPage(item);

        button.MouseEnter += (_, _) =>
        {
            if (currentPage != item)
                button.Background = B("#294C3B");
        };

        button.MouseLeave += (_, _) =>
        {
            if (currentPage != item)
                button.Background = Brushes.Transparent;
        };

        navButtons.Add(button);

        return button;
    }

    private void UpdateNavigation()
    {
        foreach (var button in navButtons)
        {
            string label = button.Content?.ToString() ?? "";
            bool selected = label.EndsWith(currentPage);

            button.Background = selected ? B("#315B47") : Brushes.Transparent;
            button.Foreground = selected ? white : B("#DCE7DC");
            button.FontWeight = selected
                ? FontWeights.SemiBold
                : FontWeights.Medium;
        }
    }

    private void ShowPage(string page)
    {
        if (content == null)
            return;

        StopGame();

        currentPage = page;
        UpdateNavigation();

        content.Children.Clear();

        var stack = new StackPanel
        {
            MaxWidth = 1150,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        content.Children.Add(stack);

        switch (page)
        {
            case "Home":
                BuildHome(stack);
                break;

            case "Dashboard":
                BuildDashboard(stack);
                break;

            case "About Us":
                BuildAbout(stack);
                break;

            case "Hedgehogs":
                BuildHedgehogs(stack);
                break;

            case "How to Help":
                BuildChecklist(stack);
                break;

            case "More":
                BuildMore(stack);
                break;

            case "Innovation Project":
                BuildInnovation(stack);
                break;

            case "Contact Us":
                BuildContact(stack);
                break;

            case "Dash Game":
                BuildGame(stack);
                break;

            default:
                BuildHome(stack);
                break;
        }
    }

    // =========================================================
    // HOME
    // =========================================================

    private void BuildHome(StackPanel stack)
    {
        var hero = new Border
        {
            Background = deepForest,
            CornerRadius = new CornerRadius(28),
            Padding = new Thickness(34),
            Margin = new Thickness(0, 0, 0, 22),
            ClipToBounds = true
        };

        var grid = new Grid();

        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1.25, GridUnitType.Star) });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(0.75, GridUnitType.Star) });

        var copy = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        copy.Children.Add(Pill("WELCOME TO CYBER CACTI", B("#DCE8D8"), forest));
        copy.Children.Add(T("Small actions.\nBig impact.", 39, true, white));

        copy.Children.Add(T(
            "Helping hedgehogs thrive by making everyday spaces safer for wildlife.",
            16,
            false,
            B("#D9E5D9")));

        copy.Children.Add(Btn(
            "Explore how to help  →",
            () => ShowPage("How to Help"),
            true));

        Grid.SetColumn(copy, 0);
        grid.Children.Add(copy);

        var artPanel = new Border
        {
            Background = B("#294F3D"),
            CornerRadius = new CornerRadius(24),
            Margin = new Thickness(14, 0, 0, 0)
        };

        var art = new Canvas
        {
            Height = 205,
            ClipToBounds = true
        };

        var sun = new Ellipse
        {
            Width = 100,
            Height = 100,
            Fill = B("#D9B778"),
            Opacity = 0.9
        };

        Canvas.SetLeft(sun, 140);
        Canvas.SetTop(sun, 17);
        art.Children.Add(sun);

        AddCactus(art, 42, 54, 1.25);
        AddHedgehog(art, 135, 115, 0.85);

        artPanel.Child = art;

        Grid.SetColumn(artPanel, 1);
        grid.Children.Add(artPanel);

        hero.Child = grid;
        stack.Children.Add(hero);

        AddHeading(
            stack,
            "Your conservation journey",
            "Every small step can make a difference.");

        int done = tasks.Count(x => x);

        var progress = new StackPanel();

        progress.Children.Add(T("YOUR PROGRESS", 10, true, muted));
        progress.Children.Add(T($"{done} of 5 actions completed", 23, true, forest));
        progress.Children.Add(MakeProgress(done));

        progress.Children.Add(T(
            done == 5
                ? "Fantastic work! You've completed every suggested action."
                : "Keep going. Each action helps make outdoor spaces safer.",
            13,
            false,
            muted));

        stack.Children.Add(Card(progress));

        var left = new StackPanel();

        left.Children.Add(T("Explore hedgehogs", 19, true, forest));
        left.Children.Add(T(
            "Discover their habits, habitats and the challenges they face.",
            13,
            false,
            muted));

        left.Children.Add(Btn(
            "Meet the hedgehogs  →",
            () => ShowPage("Hedgehogs")));

        var right = new StackPanel();

        right.Children.Add(T("Your dashboard", 19, true, forest));
        right.Children.Add(T(
            "See your completed actions and personal progress.",
            13,
            false,
            muted));

        right.Children.Add(Btn(
            "View dashboard  →",
            () => ShowPage("Dashboard")));

        stack.Children.Add(Card(TwoColumn(left, right)));
    }

    // =========================================================
    // DASHBOARD
    // =========================================================

    private void BuildDashboard(StackPanel stack)
    {
        AddHeading(
            stack,
            "Your Dashboard",
            "A snapshot of your Cyber Cacti journey.");

        int done = tasks.Count(x => x);

        var a = new StackPanel();
        a.Children.Add(T("ACTIONS COMPLETED", 10, true, muted));
        a.Children.Add(T(done.ToString(), 37, true, forest));
        a.Children.Add(T("Out of 5 suggested actions", 12, false, muted));

        var b = new StackPanel();
        b.Children.Add(T("GAME HIGH SCORE", 10, true, muted));
        b.Children.Add(T(highScore.ToString(), 37, true, forest));
        b.Children.Add(T("Your best Dash Game result", 12, false, muted));

        stack.Children.Add(Card(TwoColumn(a, b)));

        var progress = new StackPanel();

        progress.Children.Add(T("Your impact", 21, true, forest));
        progress.Children.Add(T($"{done} / 5 actions completed", 14, true));
        progress.Children.Add(MakeProgress(done));

        progress.Children.Add(T(
            done == 5
                ? "You've completed all five actions. Thank you for helping wildlife."
                : "There are still more ways to make a difference. Open your checklist to continue.",
            13,
            false,
            muted));

        progress.Children.Add(Btn(
            "Open my checklist  →",
            () => ShowPage("How to Help"),
            true));

        stack.Children.Add(Card(progress));

        var fact = new StackPanel();

        fact.Children.Add(Pill("DID YOU KNOW?", B("#F6EEDC"), gold));
        fact.Children.Add(T("Hedgehogs are night explorers", 21, true, forest));

        fact.Children.Add(T(
            "Hedgehogs are nocturnal mammals. They are generally most active at night, when they search for food and explore their surroundings.",
            14));

        stack.Children.Add(Card(fact));

        var mission = new StackPanel();

        mission.Children.Add(T("Your next mission", 20, true, forest));

        mission.Children.Add(T(
            "Choose one wildlife-friendly action to work on this week. Small changes in gardens and outdoor spaces can help hedgehogs.",
            14,
            false,
            muted));

        stack.Children.Add(Card(mission));
    }

    // =========================================================
    // ABOUT US
    // =========================================================

    private void BuildAbout(StackPanel stack)
    {
        AddHeading(stack, "About Us", "Why Cyber Cacti exists.");

        var intro = new StackPanel();

        intro.Children.Add(T("Who we are", 22, true, forest));

        intro.Children.Add(T(
            "Cyber Cacti is a project focused on hedgehog awareness, practical conservation and helping people understand how small changes can make outdoor spaces safer for wildlife.",
            15));

        stack.Children.Add(Card(intro));

        var values = new StackPanel();

        values.Children.Add(T("What we believe", 21, true, forest));

        values.Children.Add(T("01   Awareness", 16, true, forest));
        values.Children.Add(T(
            "Understanding wildlife is an important first step towards protecting it.",
            14,
            false,
            muted));

        values.Children.Add(T("02   Practical action", 16, true, forest));
        values.Children.Add(T(
            "Simple, thoughtful changes can help make gardens and green spaces safer.",
            14,
            false,
            muted));

        values.Children.Add(T("03   Wildlife protection", 16, true, forest));
        values.Children.Add(T(
            "We want to encourage people to care about the wildlife living around them.",
            14,
            false,
            muted));

        stack.Children.Add(Card(values));
    }

    // =========================================================
    // HEDGEHOGS
    // =========================================================

    private void BuildHedgehogs(StackPanel stack)
    {
        AddHeading(
            stack,
            "Meet the Hedgehogs",
            "Discover more about these fascinating visitors to our gardens.");

        var hero = new Border
        {
            Background = pale,
            CornerRadius = new CornerRadius(23),
            Padding = new Thickness(25),
            Margin = new Thickness(0, 0, 0, 17)
        };

        var grid = new Grid();

        grid.ColumnDefinitions.Add(new ColumnDefinition());

        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(170) });

        var copy = new StackPanel();

        copy.Children.Add(T(
            "Getting to know hedgehogs",
            22,
            true,
            forest));

        copy.Children.Add(T(
            "Hedgehogs are mostly active at night and use gardens, parks and hedgerows to find food and move between habitats.",
            14,
            false,
            muted));

        Grid.SetColumn(copy, 0);
        grid.Children.Add(copy);

        var art = new Canvas
        {
            Width = 150,
            Height = 105,
            VerticalAlignment = VerticalAlignment.Center
        };

        AddHedgehog(art, 7, 27, 0.82);

        Grid.SetColumn(art, 1);
        grid.Children.Add(art);

        hero.Child = grid;
        stack.Children.Add(hero);

        var habitat = new StackPanel();

        habitat.Children.Add(T("Where do they live?", 20, true, forest));

        habitat.Children.Add(T(
            "Hedgehogs use a range of habitats, including gardens, parks, woodland edges and hedgerows. Connected green spaces can help them move between places.",
            14));

        stack.Children.Add(Card(habitat));

        var dangers = new StackPanel();

        dangers.Children.Add(T("What dangers do they face?", 20, true, forest));

        dangers.Children.Add(T(
            "Some everyday garden features can create risks for hedgehogs:",
            14));

        dangers.Children.Add(T(
            "• Uncovered drains and holes\n" +
            "• Loose netting and litter\n" +
            "• Garden machinery\n" +
            "• Barriers between gardens\n" +
            "• Lack of safe places to shelter",
            14));

        stack.Children.Add(Card(dangers));

        var protect = new StackPanel();

        protect.Children.Add(T("How can we protect them?", 20, true, forest));

        protect.Children.Add(T(
            "Making small changes to outdoor spaces can help. Check for wildlife before gardening, keep loose materials safely stored and consider providing a shallow dish of fresh water.",
            14));

        protect.Children.Add(Btn(
            "See the full action checklist  →",
            () => ShowPage("How to Help"),
            true));

        stack.Children.Add(Card(protect));
    }

    // =========================================================
    // CHECKLIST
    // =========================================================

    private void BuildChecklist(StackPanel stack)
    {
        AddHeading(
            stack,
            "How to Help",
            "Five practical ways to make outdoor spaces safer.");

        int done = tasks.Count(x => x);

        var progress = new StackPanel();

        progress.Children.Add(T("YOUR CHECKLIST PROGRESS", 10, true, muted));
        progress.Children.Add(T($"{done} / 5 completed", 24, true, forest));
        progress.Children.Add(MakeProgress(done));

        progress.Children.Add(T(
            done == 5
                ? "All done! Thank you for making a difference."
                : "Tick an action when you've completed it.",
            13,
            false,
            muted));

        stack.Children.Add(Card(progress));

        string[] items =
        {
            "Create a small gap in a fence between gardens.",
            "Check for hedgehogs before using a mower or strimmer.",
            "Store garden litter and loose netting safely.",
            "Provide a shallow dish of fresh water.",
            "Leave a quiet, sheltered area for wildlife."
        };

        var list = new StackPanel();

        for (int i = 0; i < items.Length; i++)
        {
            int index = i;

            var row = new Border
            {
                Background = B("#FAFBF8"),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 5, 0, 5),
                BorderBrush = line,
                BorderThickness = new Thickness(1)
            };

            var check = new CheckBox
            {
                Content = items[i],
                IsChecked = tasks[i],
                FontSize = 14,
                Foreground = ink,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5),
                Margin = new Thickness(2)
            };

            check.Checked += (_, _) =>
            {
                tasks[index] = true;
                SaveProgress();
                ShowPage("How to Help");
            };

            check.Unchecked += (_, _) =>
            {
                tasks[index] = false;
                SaveProgress();
                ShowPage("How to Help");
            };

            row.Child = check;
            list.Children.Add(row);
        }

        list.Children.Add(T(
            "Only tick an action once you've actually completed it.",
            11,
            false,
            muted));

        list.Children.Add(Btn("Reset checklist", () =>
        {
            if (MessageBox.Show(
                "Are you sure you want to reset all five actions?",
                "Reset progress",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                tasks = new bool[5];
                SaveProgress();
                ShowPage("How to Help");
            }
        }));

        stack.Children.Add(Card(list));
    }

    // =========================================================
    // MORE
    // =========================================================

    private void BuildMore(StackPanel stack)
    {
        AddHeading(stack, "More", "Explore other areas of Cyber Cacti.");

        var innovation = new StackPanel();

        innovation.Children.Add(T("Innovation Project", 21, true, forest));
        innovation.Children.Add(T(
            "Find out more about the Cyber Cacti innovation project.",
            14,
            false,
            muted));

        innovation.Children.Add(Btn(
            "Explore project  →",
            () => ShowPage("Innovation Project")));

        stack.Children.Add(Card(innovation));

        var contact = new StackPanel();

        contact.Children.Add(T("Contact Us", 21, true, forest));
        contact.Children.Add(T(
            "Have a question or an idea? Find our contact information here.",
            14,
            false,
            muted));

        contact.Children.Add(Btn(
            "Contact information  →",
            () => ShowPage("Contact Us")));

        stack.Children.Add(Card(contact));

        var account = new StackPanel();

        account.Children.Add(T("Your account", 21, true, forest));
        account.Children.Add(T("Signed in as " + username, 14, false, muted));

        account.Children.Add(Btn(
            "Sign out",
            () =>
            {
                username = "";
                ShowLogin();
            }));

        stack.Children.Add(Card(account));
    }

    // =========================================================
    // INNOVATION PROJECT
    // =========================================================

    private void BuildInnovation(StackPanel stack)
    {
        AddHeading(
            stack,
            "Innovation Project",
            "The next chapter of Cyber Cacti.");

        var panel = new StackPanel();

        panel.Children.Add(T("Our innovation project", 22, true, forest));

        panel.Children.Add(T(
            "This page is reserved for the Cyber Cacti innovation project. More information will be added here.",
            15));

        stack.Children.Add(Card(panel));
    }

    // =========================================================
    // CONTACT
    // =========================================================

    private void BuildContact(StackPanel stack)
    {
        AddHeading(
            stack,
            "Contact Us",
            "We'd love to hear from you.");

        var panel = new StackPanel();

        panel.Children.Add(T("Get in touch", 22, true, forest));

        panel.Children.Add(T(
            "Questions or ideas about Cyber Cacti? You can contact us using the email address below.",
            14));

        var email = new Border
        {
            Background = pale,
            CornerRadius = new CornerRadius(15),
            Padding = new Thickness(17),
            Margin = new Thickness(0, 14, 0, 8),
            Child = T("cybercactirobotics@gmail.com", 16, true, forest)
        };

        panel.Children.Add(email);
        stack.Children.Add(Card(panel));
    }

    // =========================================================
    // HEDGEHOG DASH GAME
    // =========================================================

    private void BuildGame(StackPanel stack)
    {
        AddHeading(
            stack,
            "Hedgehog Dash",
            "Collect apples, dodge logs and see how high you can score.");

        var stats = new Grid
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        for (int i = 0; i < 3; i++)
        {
            stats.ColumnDefinitions.Add(new ColumnDefinition());
        }

        gameScore = T("0", 25, true, forest);
        gameLives = T("♥ ♥ ♥", 23, true, red);
        gameBest = T(highScore.ToString(), 25, true, forest);

        var s1 = StatBlock("SCORE", gameScore);
        var s2 = StatBlock("LIVES", gameLives);
        var s3 = StatBlock("PERSONAL BEST", gameBest);

        Grid.SetColumn(s1, 0);
        Grid.SetColumn(s2, 1);
        Grid.SetColumn(s3, 2);

        stats.Children.Add(s1);
        stats.Children.Add(s2);
        stats.Children.Add(s3);

        stack.Children.Add(Card(stats));

        gameInfo = T(
            "Ready when you are — press start to play.",
            13,
            true,
            muted);

        stack.Children.Add(gameInfo);

        gameCanvas = new Canvas
        {
            Height = 405,
            Background = B("#EAF1E6"),
            ClipToBounds = true,
            Focusable = true
        };

        var border = new Border
        {
            Background = B("#EAF1E6"),
            CornerRadius = new CornerRadius(22),
            BorderBrush = B("#DDE7D9"),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Child = gameCanvas,
            Margin = new Thickness(0, 0, 0, 12)
        };

        stack.Children.Add(border);

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        controls.Children.Add(Btn("←  LEFT", () => MovePlayer(-1)));
        controls.Children.Add(Btn("START / RESTART", StartGame, true));
        controls.Children.Add(Btn("RIGHT  →", () => MovePlayer(1)));

        stack.Children.Add(controls);

        stack.Children.Add(T(
            "Use the left and right arrow keys, or the buttons, to change lanes. Collect apples for points and avoid logs.",
            12,
            false,
            muted));

        DrawGameBackground();
        DrawPlayer();
    }

    private StackPanel StatBlock(string label, TextBlock value)
    {
        var panel = new StackPanel();

        panel.Children.Add(T(label, 9, true, muted));
        panel.Children.Add(value);

        return panel;
    }

    private void MovePlayer(int direction)
    {
        if (!playing)
            return;

        lane = Math.Clamp(lane + direction, 0, 2);
        DrawPlayer();
    }

    private void GameKeyDown(object sender, KeyEventArgs e)
    {
        if (!playing)
            return;

        if (e.Key == Key.Left || e.Key == Key.A)
        {
            MovePlayer(-1);
            e.Handled = true;
        }
        else if (e.Key == Key.Right || e.Key == Key.D)
        {
            MovePlayer(1);
            e.Handled = true;
        }
    }

    private void StartGame()
    {
        StopGame();

        if (gameCanvas == null)
            return;

        gameCanvas.Children.Clear();
        objects.Clear();

        score = 0;
        lives = 3;
        ticks = 0;
        lane = 1;
        playing = true;

        DrawGameBackground();
        DrawPlayer();
        UpdateGameInfo();

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(35)
        };

        timer.Tick += (_, _) => GameTick();
        timer.Start();

        gameCanvas.Focus();
        Focus();
    }

    private double GameWidth()
    {
        return gameCanvas != null && gameCanvas.ActualWidth > 0
            ? gameCanvas.ActualWidth
            : 600;
    }

    private double LaneX(int which)
    {
        return GameWidth() / 2 +
            (which - 1) * (GameWidth() * 0.65 / 3);
    }

    private void GameTick()
    {
        if (!playing || gameCanvas == null)
            return;

        ticks++;

        if (ticks % 27 == 0)
            SpawnObject();

        double speed = 3.1 + score / 12.0;
        double playerY = gameCanvas.ActualHeight - 58;

        foreach (var obj in objects.ToList())
        {
            obj.Y += speed;
            Canvas.SetTop(obj.Shape, obj.Y);

            bool collision =
                obj.Y > playerY - 21 &&
                obj.Y < playerY + 27 &&
                obj.Lane == lane;

            if (collision)
            {
                if (obj.Apple)
                    score++;
                else
                    lives--;

                gameCanvas.Children.Remove(obj.Shape);
                objects.Remove(obj);
                continue;
            }

            if (obj.Y > gameCanvas.ActualHeight)
            {
                gameCanvas.Children.Remove(obj.Shape);
                objects.Remove(obj);
            }
        }

        DrawPlayer();
        UpdateGameInfo();

        if (lives <= 0)
            EndGame();
    }

    private void DrawGameBackground()
    {
        if (gameCanvas == null)
            return;

        gameCanvas.Children.Clear();

        double width = GameWidth();
        double height = 405;

        var ground = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = B("#EAF1E6")
        };

        gameCanvas.Children.Add(ground);

        var road = new Border
        {
            Width = width * 0.67,
            Height = height,
            Background = B("#DDE8D9"),
            CornerRadius = new CornerRadius(20)
        };

        Canvas.SetLeft(road, width * 0.165);
        gameCanvas.Children.Add(road);

        for (int i = 1; i <= 2; i++)
        {
            var divider = new Rectangle
            {
                Width = 2,
                Height = height,
                Fill = B("#C2D2BE"),
                Opacity = 0.9
            };

            Canvas.SetLeft(
                divider,
                width / 2 + (i - 1.5) * (width * 0.67 / 3));

            gameCanvas.Children.Add(divider);
        }

        for (int i = 0; i < 7; i++)
        {
            var leaf = new Ellipse
            {
                Width = 13,
                Height = 7,
                Fill = B("#B8CDB0"),
                RenderTransform = new RotateTransform(i * 23)
            };

            Canvas.SetLeft(
                leaf,
                18 + (i * 71) % Math.Max(1, width * 0.14));

            Canvas.SetTop(leaf, 35 + i * 48);

            gameCanvas.Children.Add(leaf);
        }

        var title = T("HEDGEHOG DASH", 10, true, B("#8DA18A"));

        Canvas.SetLeft(title, 17);
        Canvas.SetTop(title, 13);

        gameCanvas.Children.Add(title);
    }

    private void SpawnObject()
    {
        if (gameCanvas == null)
            return;

        bool apple = random.Next(100) < 58;
        int which = random.Next(3);

        FrameworkElement shape;

        if (apple)
        {
            var applePanel = new Grid
            {
                Width = 31,
                Height = 34
            };

            var fruit = new Ellipse
            {
                Width = 27,
                Height = 27,
                Fill = B("#D96A4E"),
                Stroke = white,
                StrokeThickness = 1.5
            };

            var leaf = new Ellipse
            {
                Width = 10,
                Height = 5,
                Fill = B("#6F9B68"),
                RenderTransform = new RotateTransform(-30)
            };

            Canvas.SetLeft(leaf, 17);
            Canvas.SetTop(leaf, 0);

            applePanel.Children.Add(fruit);
            applePanel.Children.Add(leaf);

            shape = applePanel;
        }
        else
        {
            shape = new Border
            {
                Width = 38,
                Height = 23,
                Background = B("#795B48"),
                CornerRadius = new CornerRadius(10),
                BorderBrush = B("#5F4437"),
                BorderThickness = new Thickness(1)
            };
        }

        Canvas.SetLeft(
            shape,
            LaneX(which) - (apple ? 15 : 19));

        Canvas.SetTop(shape, -40);

        gameCanvas.Children.Add(shape);

        objects.Add(new GameObject
        {
            Shape = shape,
            Lane = which,
            Y = -40,
            Apple = apple
        });
    }

    private void DrawPlayer()
    {
        if (gameCanvas == null)
            return;

        var old = gameCanvas.Children
            .OfType<Border>()
            .FirstOrDefault(x => x.Tag?.ToString() == "player");

        if (old != null)
            gameCanvas.Children.Remove(old);

        var player = new Border
        {
            Tag = "player",
            Width = 52,
            Height = 48,
            CornerRadius = new CornerRadius(24),
            Background = B("#F5E5D3"),
            BorderBrush = B("#D8B99A"),
            BorderThickness = new Thickness(1.5),
            Child = new Canvas { ClipToBounds = true }
        };

        var art = (Canvas)player.Child;

        AddHedgehog(art, -4, 4, 0.43);

        Canvas.SetLeft(player, LaneX(lane) - 26);

        Canvas.SetTop(
            player,
            Math.Max(0, gameCanvas.ActualHeight - 58));

        gameCanvas.Children.Add(player);
    }

    private void UpdateGameInfo()
    {
        if (gameScore != null)
            gameScore.Text = score.ToString();

        if (gameLives != null)
            gameLives.Text = lives > 0
                ? string.Join(" ", Enumerable.Repeat("♥", lives))
                : "0";

        if (gameBest != null)
            gameBest.Text = highScore.ToString();

        if (gameInfo != null)
        {
            gameInfo.Text = playing
                ? "Collect apples and keep moving — the pace increases as you score."
                : "Ready when you are — press start to play.";
        }
    }

    private void EndGame()
    {
        StopGame();

        if (score > highScore)
        {
            highScore = score;
            SaveProgress();
        }

        UpdateGameInfo();

        if (gameInfo != null)
        {
            gameInfo.Text =
                $"GAME OVER  ·  You scored {score}  ·  Personal best {highScore}";
        }

        if (gameCanvas != null)
        {
            var panel = new Border
            {
                Background = B("#F7F7F1"),
                CornerRadius = new CornerRadius(22),
                Padding = new Thickness(25),
                Width = 270,
                BorderBrush = line,
                BorderThickness = new Thickness(1),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        T("RUN COMPLETE", 22, true, forest),
                        T($"You scored {score} points.", 14, false, ink),
                        T($"Personal best: {highScore}", 12, true, muted)
                    }
                }
            };

            Canvas.SetLeft(
                panel,
                Math.Max(10, (GameWidth() - 270) / 2));

            Canvas.SetTop(panel, 105);

            gameCanvas.Children.Add(panel);
        }
    }

    private void StopGame()
    {
        playing = false;
        timer?.Stop();
        timer = null;
    }

    // =========================================================
    // CLOSE
    // =========================================================

    protected override void OnClosed(EventArgs e)
    {
        StopGame();
        SaveProgress();

        PreviewKeyDown -= GameKeyDown;

        base.OnClosed(e);
    }
}
