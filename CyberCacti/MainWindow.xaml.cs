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
using Path = System.IO.Path;

namespace CyberCacti;

public partial class MainWindow : Window
{
    // ------------------------------------------------------------
    // DATA
    // ------------------------------------------------------------

    private readonly string folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CyberCacti");

    private string username = "";
    private bool[] tasks = new bool[5];
    private int highScore;

    private Grid? content;
    private Border? activeNav;
    private Canvas? gameCanvas;
    private TextBlock? gameInfo;
    private DispatcherTimer? timer;

    private readonly List<GameObject> objects = new();

    private int lane = 1;
    private int score;
    private int lives = 3;
    private int ticks;
    private bool playing;
    private string currentPage = "Home";

    private bool registering;

    private class GameObject
    {
        public Ellipse Shape = new();
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

    // ------------------------------------------------------------
    // COLOUR PALETTE
    // ------------------------------------------------------------

    private static SolidColorBrush B(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private readonly Brush forest = B("#234B3B");
    private readonly Brush deepForest = B("#18382D");
    private readonly Brush cream = B("#F5F4EC");
    private readonly Brush paleSage = B("#E9EFE5");
    private readonly Brush sage = B("#D8E5D4");
    private readonly Brush gold = B("#C49A55");
    private readonly Brush ink = B("#29372F");
    private readonly Brush muted = B("#78847B");
    private readonly Brush line = B("#E5E9E0");
    private readonly Brush white = Brushes.White;
    private readonly Brush softGold = B("#F5EBD7");
    private readonly Brush paleGreen = B("#F0F5ED");
    private readonly Brush danger = B("#A94D42");

    // ------------------------------------------------------------
    // STARTUP
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // GENERAL UI HELPERS
    // ------------------------------------------------------------

    private TextBlock T(
        string value,
        double size = 15,
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
            LineHeight = size * 1.45,
            Margin = new Thickness(0, 3, 0, 5),
            FontFamily = new FontFamily("Segoe UI")
        };
    }

    private Border Card(UIElement child, Thickness? padding = null)
    {
        return new Border
        {
            Background = white,
            CornerRadius = new CornerRadius(18),
            Padding = padding ?? new Thickness(25),
            Margin = new Thickness(0, 0, 0, 17),
            BorderBrush = line,
            BorderThickness = new Thickness(1),
            Child = child,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 15,
                ShadowDepth = 2,
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
                ? new Thickness(13, 8, 13, 8)
                : new Thickness(19, 12, 19, 12),
            Margin = new Thickness(0, 5, 9, 5),
            FontSize = compact ? 12 : 14,
            FontWeight = FontWeights.SemiBold,
            Background = primary ? forest : paleSage,
            Foreground = primary ? white : forest,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            FontFamily = new FontFamily("Segoe UI"),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center
        };

        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(11));
        border.SetValue(Border.BackgroundProperty,
            new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.PaddingProperty,
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

        button.MouseEnter += (_, _) =>
        {
            button.Opacity = 0.83;
        };

        button.MouseLeave += (_, _) =>
        {
            button.Opacity = 1;
        };

        button.Click += (_, _) => action();

        return button;
    }

    private TextBox MakeTextBox(string placeholder = "")
    {
        return new TextBox
        {
            FontSize = 15,
            Padding = new Thickness(13),
            Margin = new Thickness(0, 5, 0, 13),
            Background = white,
            Foreground = ink,
            BorderBrush = B("#DCE3D9"),
            BorderThickness = new Thickness(1),
            FontFamily = new FontFamily("Segoe UI"),
            Height = 45,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = placeholder
        };
    }

    private PasswordBox MakePasswordBox()
    {
        return new PasswordBox
        {
            FontSize = 15,
            Padding = new Thickness(13),
            Margin = new Thickness(0, 5, 0, 13),
            Background = white,
            Foreground = ink,
            BorderBrush = B("#DCE3D9"),
            BorderThickness = new Thickness(1),
            FontFamily = new FontFamily("Segoe UI"),
            Height = 45,
            VerticalContentAlignment = VerticalAlignment.Center
        };
    }

    private StackPanel VerticalStack(double spacing = 0)
    {
        return new StackPanel
        {
            Orientation = Orientation.Vertical,
            Margin = new Thickness(0),
        };
    }

    private void AddPageHeading(
        Panel panel,
        string title,
        string subtitle)
    {
        panel.Children.Add(T(title, 31, true, forest));
        panel.Children.Add(T(subtitle, 15, false, muted));

        panel.Children.Add(new Border
        {
            Height = 2,
            Width = 55,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = gold,
            Margin = new Thickness(0, 12, 0, 22)
        });
    }

    private Border Pill(string text, Brush background, Brush foreground)
    {
        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(12, 5, 12, 5),
            Child = T(text, 11, true, foreground),
            HorizontalAlignment = HorizontalAlignment.Left
        };
    }

    private ProgressBar MakeProgress(double value, double maximum = 5)
    {
        return new ProgressBar
        {
            Minimum = 0,
            Maximum = maximum,
            Value = Math.Clamp(value, 0, maximum),
            Height = 9,
            Background = paleSage,
            Foreground = forest,
            BorderThickness = new Thickness(0)
        };
    }

    private Grid TwoColumn(UIElement left, UIElement right)
    {
        var grid = new Grid();

        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        grid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);

        grid.Children.Add(left);
        grid.Children.Add(right);

        return grid;
    }

    // ------------------------------------------------------------
    // LOCAL STORAGE
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // ACCOUNT SYSTEM
    // ------------------------------------------------------------

    private bool Authenticate(
        string name,
        string password,
        bool register,
        out string message)
    {
        name = name.Trim();

        if (register &&
            (name.Length < 3 || name.Length > 20 ||
             name.Any(c => !char.IsLetterOrDigit(c) &&
                           c != '_' && c != '-')))
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
            byte[] storedSalt = Convert.FromBase64String(account.Salt);
            byte[] storedHash = Convert.FromBase64String(account.Hash);

            byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                storedSalt,
                120000,
                HashAlgorithmName.SHA256,
                32);

            if (!CryptographicOperations.FixedTimeEquals(
                storedHash,
                enteredHash))
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

    private void ShowLogin()
    {
        StopGame();

        Root.Children.Clear();
        Root.Background = cream;

        var outer = new Grid
        {
            Margin = new Thickness(35)
        };

        outer.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1.05, GridUnitType.Star) });

        outer.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(0.95, GridUnitType.Star) });

        // LEFT INTRODUCTION PANEL

        var intro = new Border
        {
            Background = forest,
            CornerRadius = new CornerRadius(26),
            Padding = new Thickness(45),
            Margin = new Thickness(0, 0, 22, 0)
        };

        var introStack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        introStack.Children.Add(
            T("🌵  CYBER CACTI", 17, true, B("#E7EEDB")));

        introStack.Children.Add(
            T("Small actions.\nBig impact.", 43, true, white));

        introStack.Children.Add(
            T(
                "Helping create a safer world for hedgehogs, one action at a time.",
                19,
                false,
                B("#D7E2D5")));

        introStack.Children.Add(new Border
        {
            Width = 65,
            Height = 3,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = gold,
            Margin = new Thickness(0, 20, 0, 20)
        });

        introStack.Children.Add(
            T("LEARN  •  PROTECT  •  MAKE A DIFFERENCE",
                11, true, B("#D7E2D5")));

        intro.Child = introStack;

        Grid.SetColumn(intro, 0);
        outer.Children.Add(intro);

        // RIGHT ACCOUNT PANEL

        var form = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var title = T("Welcome to Cyber Cacti", 28, true, forest);
        var subtitle = T("Sign in to continue your journey.", 14, false, muted);

        form.Children.Add(title);
        form.Children.Add(subtitle);

        var modeButtons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 17, 0, 16)
        };

        var signInMode = Btn("Sign in", () =>
        {
            registering = false;
            ShowLogin();
        }, !registering);

        var createMode = Btn("Create account", () =>
        {
            registering = true;
            ShowLogin();
        }, registering);

        modeButtons.Children.Add(signInMode);
        modeButtons.Children.Add(createMode);
        form.Children.Add(modeButtons);

        form.Children.Add(T("Username", 13, true, ink));

        var name = MakeTextBox("Enter your username");
        form.Children.Add(name);

        form.Children.Add(T("Password", 13, true, ink));

        var password = MakePasswordBox();
        form.Children.Add(password);

        PasswordBox? confirm = null;

        if (registering)
        {
            form.Children.Add(T("Confirm password", 13, true, ink));
            confirm = MakePasswordBox();
            form.Children.Add(confirm);

            form.Children.Add(
                T("Use at least 8 characters for your password.",
                    11, false, muted));
        }

        var feedback = T("", 12, true, danger);
        feedback.Margin = new Thickness(0, 8, 0, 8);
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

        form.Children.Add(
            T(
                "Your account and progress are stored locally on this computer.",
                11,
                false,
                muted));

        var panel = Card(form, new Thickness(30));
        panel.Margin = new Thickness(22, 0, 0, 0);
        panel.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetColumn(panel, 1);
        outer.Children.Add(panel);

        Root.Children.Add(outer);

        name.Focus();

        password.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
                submit.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        };
    }

    // ------------------------------------------------------------
    // MAIN APP SHELL
    // ------------------------------------------------------------

    private void ShowApp(string page = "Home")
    {
        StopGame();

        Root.Children.Clear();
        Root.Background = cream;

        var shell = new Grid();

        shell.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(245) });

        shell.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // SIDEBAR

        var sidebar = new Grid
        {
            Background = deepForest
        };

        sidebar.RowDefinitions.Add(new RowDefinition
        {
            Height = GridLength.Auto
        });

        sidebar.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star)
        });

        sidebar.RowDefinitions.Add(new RowDefinition
        {
            Height = GridLength.Auto
        });

        var brand = new StackPanel
        {
            Margin = new Thickness(24, 30, 20, 27)
        };

        brand.Children.Add(T("🌵  CYBER CACTI", 20, true, white));
        brand.Children.Add(
            T("HEDGEHOG AWARENESS PROJECT", 9, true, B("#C6D5C6")));

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

        foreach (string item in pages)
            nav.Children.Add(CreateNavButton(item));

        navScroll.Content = nav;

        Grid.SetRow(navScroll, 1);
        sidebar.Children.Add(navScroll);

        var userPanel = new Border
        {
            Background = B("#1E4435"),
            Padding = new Thickness(17),
            Margin = new Thickness(13, 10, 13, 18),
            CornerRadius = new CornerRadius(13)
        };

        var userStack = new StackPanel();

        userStack.Children.Add(T("SIGNED IN AS", 9, true, B("#B9CABB")));
        userStack.Children.Add(T(username, 15, true, white));

        var signOut = Btn("Sign out", () =>
        {
            username = "";
            content = null;
            ShowLogin();
        }, false, true);

        signOut.Background = B("#DCE8D8");
        signOut.Margin = new Thickness(0, 9, 0, 0);

        userStack.Children.Add(signOut);
        userPanel.Child = userStack;

        Grid.SetRow(userPanel, 2);
        sidebar.Children.Add(userPanel);

        // MAIN CONTENT AREA

        var main = new Grid
        {
            Background = cream
        };

        main.RowDefinitions.Add(new RowDefinition
        {
            Height = GridLength.Auto
        });

        main.RowDefinitions.Add(new RowDefinition
        {
            Height = new GridLength(1, GridUnitType.Star)
        });

        var topBar = new Border
        {
            Background = white,
            BorderBrush = line,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(28, 16, 28, 16)
        };

        var topGrid = new Grid();

        topGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        topGrid.ColumnDefinitions.Add(
            new ColumnDefinition { Width = GridLength.Auto });

        var topLeft = new StackPanel();

        topLeft.Children.Add(
            T("YOUR HEDGEHOG CONSERVATION SPACE", 10, true, muted));

        topLeft.Children.Add(
            T("Welcome, " + username, 17, true, forest));

        Grid.SetColumn(topLeft, 0);
        topGrid.Children.Add(topLeft);

        var topRight = Pill(
            "●  LOCAL PROFILE",
            paleGreen,
            forest);

        topRight.VerticalAlignment = VerticalAlignment.Center;

        Grid.SetColumn(topRight, 1);
        topGrid.Children.Add(topRight);

        topBar.Child = topGrid;

        Grid.SetRow(topBar, 0);
        main.Children.Add(topBar);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Padding = new Thickness(30, 25, 30, 25)
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
        var button = new Button
        {
            Content = item,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(16, 12, 12, 12),
            Margin = new Thickness(0, 3, 0, 3),
            Background = Brushes.Transparent,
            Foreground = B("#E3EBE2"),
            BorderThickness = new Thickness(0),
            FontSize = 13,
            FontWeight = FontWeights.Medium,
            Cursor = Cursors.Hand,
            FontFamily = new FontFamily("Segoe UI")
        };

        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
        border.SetValue(Border.BackgroundProperty,
            new TemplateBindingExtension(Control.BackgroundProperty));
        border.SetValue(Border.PaddingProperty,
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
                button.Background = B("#315847");
        };

        button.MouseLeave += (_, _) =>
        {
            if (currentPage != item)
                button.Background = Brushes.Transparent;
        };

        return button;
    }

    // ------------------------------------------------------------
    // PAGE NAVIGATION
    // ------------------------------------------------------------

    private void ShowPage(string page)
    {
        if (content == null)
            return;

        StopGame();

        currentPage = page;
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

    // ------------------------------------------------------------
    // HOME PAGE
    // ------------------------------------------------------------

    private void BuildHome(StackPanel stack)
    {
        var hero = new Border
        {
            Background = forest,
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(34),
            Margin = new Thickness(0, 0, 0, 20)
        };

        var heroContent = new StackPanel();

        heroContent.Children.Add(
            Pill("WELCOME TO CYBER CACTI", B("#DCE8D8"), forest));

        heroContent.Children.Add(
            T("Small actions.\nBig impact.", 38, true, white));

        heroContent.Children.Add(
            T(
                "Helping hedgehogs thrive by making everyday spaces safer for wildlife.",
                17,
                false,
                B("#E0E9DF")));

        heroContent.Children.Add(
            Btn("Discover how to help  →",
                () => ShowPage("How to Help"),
                true));

        hero.Child = heroContent;
        stack.Children.Add(hero);

        AddPageHeading(
            stack,
            "Your conservation journey",
            "Every small step can make a difference.");

        int completed = tasks.Count(x => x);

        var progressCard = new StackPanel();

        progressCard.Children.Add(
            T("YOUR PROGRESS", 11, true, muted));

        progressCard.Children.Add(
            T($"{completed} out of 5 actions completed", 22, true, forest));

        progressCard.Children.Add(MakeProgress(completed));

        progressCard.Children.Add(
            T(
                completed == 5
                    ? "Fantastic work! You've completed every suggested action."
                    : "Keep going. Each action helps make outdoor spaces safer.",
                13,
                false,
                muted));

        stack.Children.Add(Card(progressCard));

        var left = new StackPanel();
        left.Children.Add(T("Explore hedgehogs", 19, true, forest));
        left.Children.Add(
            T("Discover their habits, habitats and the challenges they face.",
                14, false, muted));
        left.Children.Add(
            Btn("Meet the hedgehogs  →",
                () => ShowPage("Hedgehogs")));

        var right = new StackPanel();
        right.Children.Add(T("Your dashboard", 19, true, forest));
        right.Children.Add(
            T("See your completed actions and your personal progress.",
                14, false, muted));
        right.Children.Add(
            Btn("View dashboard  →",
                () => ShowPage("Dashboard")));

        stack.Children.Add(Card(TwoColumn(left, right)));
    }

    // ------------------------------------------------------------
    // DASHBOARD
    // ------------------------------------------------------------

    private void BuildDashboard(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "Your Dashboard",
            "A snapshot of your Cyber Cacti journey.");

        int completed = tasks.Count(x => x);

        var stat1 = new StackPanel();
        stat1.Children.Add(T("ACTIONS COMPLETED", 10, true, muted));
        stat1.Children.Add(T(completed.ToString(), 36, true, forest));
        stat1.Children.Add(T("Out of 5 suggested actions", 12, false, muted));

        var stat2 = new StackPanel();
        stat2.Children.Add(T("GAME HIGH SCORE", 10, true, muted));
        stat2.Children.Add(T(highScore.ToString(), 36, true, forest));
        stat2.Children.Add(T("Your best Dash Game result", 12, false, muted));

        stack.Children.Add(
            Card(TwoColumn(stat1, stat2)));

        var progress = new StackPanel();

        progress.Children.Add(T("Your impact", 21, true, forest));
        progress.Children.Add(
            T($"{completed} / 5 actions completed", 15, true, ink));

        progress.Children.Add(MakeProgress(completed));

        progress.Children.Add(
            T(
                completed == 5
                    ? "You've completed all five actions. Thank you for helping wildlife."
                    : "There are still more ways to make a difference. Open your checklist to continue.",
                14,
                false,
                muted));

        progress.Children.Add(
            Btn("Open my checklist  →",
                () => ShowPage("How to Help"),
                true));

        stack.Children.Add(Card(progress));

        var fact = new StackPanel();

        fact.Children.Add(T("DID YOU KNOW?", 10, true, gold));
        fact.Children.Add(T("Hedgehog fact", 21, true, forest));
        fact.Children.Add(
            T(
                "Hedgehogs are nocturnal mammals. They are generally most active at night, when they search for food and explore their surroundings.",
                15));

        stack.Children.Add(Card(fact));

        var mission = new StackPanel();

        mission.Children.Add(T("Your next mission", 20, true, forest));
        mission.Children.Add(
            T(
                "Choose one wildlife-friendly action to work on this week. Small changes in gardens and outdoor spaces can help hedgehogs.",
                14,
                false,
                muted));

        stack.Children.Add(Card(mission));
    }

    // ------------------------------------------------------------
    // ABOUT US
    // ------------------------------------------------------------

    private void BuildAbout(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "About Us",
            "Why Cyber Cacti exists.");

        var intro = new StackPanel();

        intro.Children.Add(T("Who we are", 22, true, forest));

        intro.Children.Add(
            T(
                "Cyber Cacti is a project focused on hedgehog awareness, practical conservation and helping people understand how small changes can make outdoor spaces safer for wildlife.",
                16));

        stack.Children.Add(Card(intro));

        var values = new StackPanel();

        values.Children.Add(T("What we believe", 21, true, forest));

        values.Children.Add(
            T("🌿  Awareness", 16, true, forest));

        values.Children.Add(
            T("Understanding wildlife is an important first step towards protecting it.",
                14));

        values.Children.Add(
            T("🌱  Practical action", 16, true, forest));

        values.Children.Add(
            T("Simple, thoughtful changes can help make gardens and green spaces safer.",
                14));

        values.Children.Add(
            T("🦔  Wildlife protection", 16, true, forest));

        values.Children.Add(
            T("We want to encourage people to care about the wildlife living around them.",
                14));

        stack.Children.Add(Card(values));
    }

    // ------------------------------------------------------------
    // HEDGEHOGS
    // ------------------------------------------------------------

    private void BuildHedgehogs(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "Meet the Hedgehogs",
            "Discover more about these fascinating visitors to our gardens.");

        var intro = new Border
        {
            Background = paleGreen,
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(28),
            Margin = new Thickness(0, 0, 0, 18)
        };

        var introStack = new StackPanel();

        introStack.Children.Add(T("🦔", 42, false, forest));
        introStack.Children.Add(
            T("Getting to know hedgehogs", 22, true, forest));

        introStack.Children.Add(
            T(
                "Hedgehogs are mostly active at night and use gardens, parks and hedgerows to find food and move between habitats.",
                15));

        intro.Child = introStack;
        stack.Children.Add(intro);

        var habitats = new StackPanel();

        habitats.Children.Add(T("Where do they live?", 20, true, forest));

        habitats.Children.Add(
            T(
                "Hedgehogs use a range of habitats, including gardens, parks, woodland edges and hedgerows. Connected green spaces can help them move between places.",
                15));

        stack.Children.Add(Card(habitats));

        var hazards = new StackPanel();

        hazards.Children.Add(T("What dangers do they face?", 20, true, forest));

        hazards.Children.Add(
            T(
                "Some everyday garden features can create risks for hedgehogs:",
                14));

        hazards.Children.Add(
            T(
                "• Uncovered drains and holes.\n• Loose netting and litter.\n• Garden machinery.\n• Barriers between gardens.\n• Lack of safe places to shelter.",
                15));

        stack.Children.Add(Card(hazards));

        var protection = new StackPanel();

        protection.Children.Add(T("How can we protect them?", 20, true, forest));

        protection.Children.Add(
            T(
                "Making small changes to outdoor spaces can help. Check for wildlife before gardening, keep loose materials safely stored and consider providing a shallow dish of fresh water.",
                15));

        protection.Children.Add(
            Btn("See the full action checklist  →",
                () => ShowPage("How to Help"),
                true));

        stack.Children.Add(Card(protection));
    }

    // ------------------------------------------------------------
    // HOW TO HELP CHECKLIST
    // ------------------------------------------------------------

    private void BuildChecklist(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "How to Help",
            "Five practical ways to make outdoor spaces safer.");

        int completed = tasks.Count(x => x);

        var progress = new StackPanel();

        progress.Children.Add(T("YOUR CHECKLIST PROGRESS", 10, true, muted));
        progress.Children.Add(
            T($"{completed} / 5 completed", 24, true, forest));

        progress.Children.Add(MakeProgress(completed));

        progress.Children.Add(
            T(
                completed == 5
                    ? "All done! Thank you for making a difference."
                    : "Tick an action when you've completed it.",
                13,
                false,
                muted));

        stack.Children.Add(Card(progress));

        string[] suggestions =
        {
            "Create a small gap in a fence between gardens.",
            "Check for hedgehogs before using a mower or strimmer.",
            "Store garden litter and loose netting safely.",
            "Provide a shallow dish of fresh water.",
            "Leave a quiet, sheltered area for wildlife."
        };

        var list = new StackPanel();

        for (int i = 0; i < suggestions.Length; i++)
        {
            int index = i;

            var check = new CheckBox
            {
                Content = suggestions[i],
                IsChecked = tasks[i],
                FontSize = 15,
                Margin = new Thickness(4, 12, 4, 12),
                Foreground = ink,
                FontFamily = new FontFamily("Segoe UI"),
                VerticalContentAlignment = VerticalAlignment.Center
            };

            check.Checked += (_, _) =>
            {
                tasks[index] = true;
                SaveProgress();
                RefreshChecklistProgress(list);
            };

            check.Unchecked += (_, _) =>
            {
                tasks[index] = false;
                SaveProgress();
                RefreshChecklistProgress(list);
            };

            list.Children.Add(check);
        }

        list.Children.Add(
            T(
                "Only tick an action once you've actually completed it.",
                12,
                false,
                muted));

        list.Children.Add(
            Btn("Reset checklist",
                () =>
                {
                    var result = MessageBox.Show(
                        "Are you sure you want to reset all five actions?",
                        "Reset progress",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        tasks = new bool[5];
                        SaveProgress();
                        ShowPage("How to Help");
                    }
                }));

        stack.Children.Add(Card(list));
    }

    private void RefreshChecklistProgress(Panel panel)
    {
        // The saved task state is updated immediately.
        // Rebuilding the page updates its progress display.
        ShowPage("How to Help");
    }

    // ------------------------------------------------------------
    // MORE
    // ------------------------------------------------------------

    private void BuildMore(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "More",
            "Explore other areas of Cyber Cacti.");

        var innovation = new StackPanel();

        innovation.Children.Add(T("Innovation Project", 21, true, forest));

        innovation.Children.Add(
            T(
                "Find out more about the Cyber Cacti innovation project.",
                14,
                false,
                muted));

        innovation.Children.Add(
            Btn("Explore project  →",
                () => ShowPage("Innovation Project")));

        stack.Children.Add(Card(innovation));

        var contact = new StackPanel();

        contact.Children.Add(T("Contact Us", 21, true, forest));

        contact.Children.Add(
            T(
                "Have a question or an idea? Find our contact information here.",
                14,
                false,
                muted));

        contact.Children.Add(
            Btn("Contact information  →",
                () => ShowPage("Contact Us")));

        stack.Children.Add(Card(contact));

        var account = new StackPanel();

        account.Children.Add(T("Your account", 21, true, forest));
        account.Children.Add(T("Signed in as " + username, 14, false, muted));

        account.Children.Add(
            Btn("Sign out",
                () =>
                {
                    username = "";
                    content = null;
                    ShowLogin();
                }));

        stack.Children.Add(Card(account));
    }

    // ------------------------------------------------------------
    // INNOVATION PROJECT
    // ------------------------------------------------------------

    private void BuildInnovation(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "Innovation Project",
            "The next chapter of Cyber Cacti.");

        var panel = new StackPanel();

        panel.Children.Add(T("Our innovation project", 22, true, forest));

        panel.Children.Add(
            T(
                "This page is reserved for the Cyber Cacti innovation project. More information will be added here.",
                15));

        stack.Children.Add(Card(panel));
    }

    // ------------------------------------------------------------
    // CONTACT
    // ------------------------------------------------------------

    private void BuildContact(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "Contact Us",
            "We'd love to hear from you.");

        var panel = new StackPanel();

        panel.Children.Add(T("Get in touch", 22, true, forest));

        panel.Children.Add(
            T(
                "Questions or ideas about Cyber Cacti? You can contact us using the email address below.",
                15));

        var email = new Border
        {
            Background = paleGreen,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(17),
            Margin = new Thickness(0, 15, 0, 10),
            Child = T("cybercactirobotics@gmail.com", 16, true, forest)
        };

        panel.Children.Add(email);

        stack.Children.Add(Card(panel));
    }

    // ------------------------------------------------------------
    // DASH GAME
    // ------------------------------------------------------------

    private void BuildGame(StackPanel stack)
    {
        AddPageHeading(
            stack,
            "Hedgehog Dash",
            "Collect apples, dodge logs and see how high you can score.");

        var stats = new Grid
        {
            Margin = new Thickness(0, 0, 0, 12)
        };

        stats.ColumnDefinitions.Add(new ColumnDefinition());
        stats.ColumnDefinitions.Add(new ColumnDefinition());
        stats.ColumnDefinitions.Add(new ColumnDefinition());

        var scorePanel = new StackPanel();
        scorePanel.Children.Add(T("SCORE", 10, true, muted));
        scorePanel.Children.Add(T("0", 22, true, forest));

        var livesPanel = new StackPanel();
        livesPanel.Children.Add(T("LIVES", 10, true, muted));
        livesPanel.Children.Add(T("♥ ♥ ♥", 22, true, danger));

        var bestPanel = new StackPanel();
        bestPanel.Children.Add(T("PERSONAL BEST", 10, true, muted));
        bestPanel.Children.Add(T(highScore.ToString(), 22, true, forest));

        Grid.SetColumn(scorePanel, 0);
        Grid.SetColumn(livesPanel, 1);
        Grid.SetColumn(bestPanel, 2);

        stats.Children.Add(scorePanel);
        stats.Children.Add(livesPanel);
        stats.Children.Add(bestPanel);

        gameInfo = T(
            $"Score: 0     Lives: 3     Best: {highScore}",
            15,
            true,
            forest);

        stack.Children.Add(Card(stats));
        stack.Children.Add(gameInfo);

        gameCanvas = new Canvas
        {
            Height = 390,
            Background = B("#EAF0E5"),
            ClipToBounds = true,
            Focusable = true
        };

        var gameBorder = new Border
        {
            Background = B("#EAF0E5"),
            CornerRadius = new CornerRadius(18),
            BorderBrush = B("#DCE5D8"),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Child = gameCanvas,
            Margin = new Thickness(0, 0, 0, 12)
        };

        stack.Children.Add(gameBorder);

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        controls.Children.Add(
            Btn("◀  LEFT",
                () => MovePlayer(-1)));

        controls.Children.Add(
            Btn("START / RESTART",
                StartGame,
                true));

        controls.Children.Add(
            Btn("RIGHT  ▶",
                () => MovePlayer(1)));

        stack.Children.Add(controls);

        stack.Children.Add(
            T(
                "Use the left and right arrow keys, or the buttons above, to change lanes.",
                12,
                false,
                muted));

        stack.Children.Add(
            T(
                "Apples give you points. Logs cost you a life. The game gets faster as your score increases.",
                13,
                false,
                muted));

        DrawGameBackground();
        DrawPlayer();
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

    private void GameTick()
    {
        if (!playing || gameCanvas == null)
            return;

        ticks++;

        if (ticks % 28 == 0)
            SpawnObject();

        double speed = 3.3 + score / 12.0;
        double playerY = gameCanvas.ActualHeight - 58;

        foreach (var obj in objects.ToList())
        {
            obj.Y += speed;
            Canvas.SetTop(obj.Shape, obj.Y);

            bool collision =
                obj.Y > playerY - 20 &&
                obj.Y < playerY + 27 &&
                obj.Lane == lane;

            if (collision)
            {
                if (obj.Apple)
                {
                    score++;
                }
                else
                {
                    lives--;
                }

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

        double width = ActualGameWidth();
        double height = gameCanvas.Height;

        var road = new Rectangle
        {
            Width = width * 0.65,
            Height = height,
            Fill = B("#DDE8D8")
        };

        Canvas.SetLeft(road, width * 0.175);
        Canvas.SetTop(road, 0);
        gameCanvas.Children.Add(road);

        for (int i = 1; i <= 2; i++)
        {
            var divider = new Rectangle
            {
                Width = 2,
                Height = height,
                Fill = B("#C4D3BF"),
                Opacity = 0.9
            };

            Canvas.SetLeft(divider,
                width / 2 + (i - 1.5) * (width * 0.65 / 3));

            Canvas.SetTop(divider, 0);
            gameCanvas.Children.Add(divider);
        }

        var title = T("HEDGEHOG DASH", 11, true, B("#91A58D"));
        Canvas.SetLeft(title, 15);
        Canvas.SetTop(title, 12);
        gameCanvas.Children.Add(title);
    }

    private double ActualGameWidth()
    {
        if (gameCanvas == null)
            return 600;

        return gameCanvas.ActualWidth > 0
            ? gameCanvas.ActualWidth
            : 600;
    }

    private void SpawnObject()
    {
        if (gameCanvas == null)
            return;

        bool apple = Random.Shared.Next(100) < 58;
        int objectLane = Random.Shared.Next(3);

        var shape = new Ellipse
        {
            Width = apple ? 29 : 34,
            Height = apple ? 29 : 22,
            Fill = apple ? B("#D95D39") : B("#795548"),
            Stroke = white,
            StrokeThickness = 2
        };

        double width = ActualGameWidth();

        Canvas.SetLeft(
            shape,
            width / 2 + (objectLane - 1) * (width * 0.65 / 3) - shape.Width / 2);

        Canvas.SetTop(shape, -35);

        gameCanvas.Children.Add(shape);

        objects.Add(new GameObject
        {
            Shape = shape,
            Lane = objectLane,
            Y = -35,
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
            Width = 46,
            Height = 46,
            CornerRadius = new CornerRadius(23),
            Background = forest,
            BorderBrush = B("#D8E5D4"),
            BorderThickness = new Thickness(2),
            Child = new TextBlock
            {
                Text = "🦔",
                FontSize = 27,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        double width = ActualGameWidth();

        Canvas.SetLeft(
            player,
            width / 2 + (lane - 1) * (width * 0.65 / 3) - 23);

        Canvas.SetTop(
            player,
            Math.Max(0, gameCanvas.ActualHeight - 55));

        gameCanvas.Children.Add(player);
    }

    private void UpdateGameInfo()
    {
        if (gameInfo == null)
            return;

        string hearts = new string('♥', Math.Max(0, lives));

        gameInfo.Text =
            $"Score: {score}     Lives: {hearts}     Best: {highScore}";
    }

    private void EndGame()
    {
        StopGame();

        if (score > highScore)
        {
            highScore = score;
            SaveProgress();
        }

        if (gameInfo != null)
        {
            gameInfo.Text =
                $"GAME OVER     Score: {score}     Personal best: {highScore}";
        }

        if (gameCanvas != null)
        {
            var overlay = new Border
            {
                Background = B("#F5F4EC"),
                CornerRadius = new CornerRadius(15),
                Padding = new Thickness(25),
                Width = 260,
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        T("GAME OVER", 23, true, forest),
                        T($"You scored {score} points.", 15, false, ink),
                        T($"Personal best: {highScore}", 13, true, muted)
                    }
                }
            };

            Canvas.SetLeft(overlay,
                Math.Max(10, (ActualGameWidth() - 260) / 2));

            Canvas.SetTop(overlay, 95);

            gameCanvas.Children.Add(overlay);
        }
    }

    private void StopGame()
    {
        playing = false;

        timer?.Stop();
        timer = null;
    }

    // ------------------------------------------------------------
    // CLOSE
    // ------------------------------------------------------------

    protected override void OnClosed(EventArgs e)
    {
        StopGame();
        SaveProgress();

        PreviewKeyDown -= GameKeyDown;

        base.OnClosed(e);
    }
}
