
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
using Path = System.IO.Path;

namespace CyberCacti;

public partial class MainWindow : Window
{
    private readonly string folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CyberCacti");

    private string username = "";
    private bool[] tasks = new bool[5];
    private int highScore = 0;
    private Grid? content;
    private Canvas? gameCanvas;
    private DispatcherTimer? timer;
    private readonly List<GameObject> objects = new();
    private int lane = 1, score = 0, lives = 3, ticks = 0;
    private bool playing;

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

    private static SolidColorBrush B(string colour) =>
        new((Color)ColorConverter.ConvertFromString(colour));

    private readonly Brush forest = B("#254B3B");
    private readonly Brush cream = B("#F7F5EC");
    private readonly Brush sage = B("#DCE8D8");
    private readonly Brush muted = B("#718078");
    private readonly Brush ink = B("#26352D");

    public MainWindow()
    {
        InitializeComponent();
        LoadProgress();
        ShowLogin();
    }

    private TextBlock T(string value, double size = 15,
        bool bold = false, Brush? colour = null)
    {
        return new TextBlock
        {
            Text = value,
            FontSize = size,
            FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
            Foreground = colour ?? ink,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 5)
        };
    }

    private Border Card(UIElement child)
    {
        return new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(24),
            Margin = new Thickness(0, 10, 0, 10),
            BorderBrush = B("#E7E9E1"),
            BorderThickness = new Thickness(1),
            Child = child
        };
    }

    private Button Btn(string label, Action action, bool primary = false)
    {
        var button = new Button
        {
            Content = label,
            Padding = new Thickness(17, 11, 17, 11),
            Margin = new Thickness(0, 5, 8, 5),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Background = primary ? forest : sage,
            Foreground = primary ? Brushes.White : forest,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        button.Click += (_, _) => action();
        return button;
    }

    private void LoadProgress()
    {
        try
        {
            Directory.CreateDirectory(folder);
            if (File.Exists(ProgressFile))
            {
                var data = JsonSerializer.Deserialize<SavedData>(
                    File.ReadAllText(ProgressFile));

                if (data != null)
                {
                    tasks = data.Tasks.Length == 5
                        ? data.Tasks : new bool[5];
                    highScore = data.HighScore;
                }
            }
        }
        catch { }
    }

    private void SaveProgress()
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(ProgressFile,
            JsonSerializer.Serialize(new SavedData
            {
                Tasks = tasks,
                HighScore = highScore
            }));
    }

    private List<Account> GetAccounts()
    {
        try
        {
            Directory.CreateDirectory(folder);
            if (File.Exists(AccountsFile))
                return JsonSerializer.Deserialize<List<Account>>(
                    File.ReadAllText(AccountsFile)) ?? new();
        }
        catch { }

        return new();
    }

    private bool Authenticate(string name, string password,
        bool register, out string message)
    {
        name = name.Trim();

        if (register &&
            (name.Length < 3 || name.Length > 20 ||
             name.Any(c => !char.IsLetterOrDigit(c) &&
                           c != '_' && c != '-')))
        {
            message = "Username must be 3–20 characters. Use letters, numbers, _ or -.";
            return false;
        }

        if (register && password.Length < 8)
        {
            message = "Password must contain at least 8 characters.";
            return false;
        }

        var accounts = GetAccounts();

        var account = accounts.FirstOrDefault(a =>
            string.Equals(a.Username, name,
                StringComparison.OrdinalIgnoreCase));

        if (register)
        {
            if (account != null)
            {
                message = "That username already exists.";
                return false;
            }

            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password, salt, 120000,
                HashAlgorithmName.SHA256, 32);

            accounts.Add(new Account
            {
                Username = name,
                Salt = Convert.ToBase64String(salt),
                Hash = Convert.ToBase64String(hash)
            });

            Directory.CreateDirectory(folder);
            File.WriteAllText(AccountsFile,
                JsonSerializer.Serialize(accounts));

            username = name;
            message = "Account created!";
            return true;
        }

        if (account == null)
        {
            message = "Account not found on this computer.";
            return false;
        }

        byte[] storedSalt = Convert.FromBase64String(account.Salt);
        byte[] storedHash = Convert.FromBase64String(account.Hash);

        byte[] enteredHash = Rfc2898DeriveBytes.Pbkdf2(
            password, storedSalt, 120000,
            HashAlgorithmName.SHA256, 32);

        if (!CryptographicOperations.FixedTimeEquals(
            storedHash, enteredHash))
        {
            message = "Incorrect password.";
            return false;
        }

        username = account.Username;
        message = "Welcome back!";
        return true;
    }

    private void ShowLogin()
    {
        Root.Children.Clear();
        Root.Background = cream;

        var layout = new Grid { Margin = new Thickness(35) };
        layout.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(1.1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(0.9, GridUnitType.Star) });

        var intro = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(40)
        };

        intro.Children.Add(T("🌵 CYBER CACTI", 17, true, forest));
        intro.Children.Add(T("Small actions.\nBig impact.", 42, true, forest));
        intro.Children.Add(T(
            "Helping create a safer world for hedgehogs, one action at a time.",
            18, false, muted));

        Grid.SetColumn(intro, 0);
        layout.Children.Add(intro);

        var form = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        form.Children.Add(T("Welcome to Cyber Cacti", 27, true, forest));
        form.Children.Add(T("Sign in or create your account.", 15, false, muted));

        var name = new TextBox
        {
            Padding = new Thickness(12),
            FontSize = 16,
            Margin = new Thickness(0, 12, 0, 8)
        };

        var password = new PasswordBox
        {
            Padding = new Thickness(12),
            FontSize = 16,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var feedback = T("", 13, false, muted);

        form.Children.Add(T("Username", 13, true));
        form.Children.Add(name);
        form.Children.Add(T("Password", 13, true));
        form.Children.Add(password);

        form.Children.Add(Btn("Sign in", () =>
        {
            if (Authenticate(name.Text, password.Password,
                false, out string message))
                ShowApp();
            else
                feedback.Text = message;
        }, true));

        form.Children.Add(Btn("Create account", () =>
        {
            if (Authenticate(name.Text, password.Password,
                true, out string message))
                ShowApp();
            else
                feedback.Text = message;
        }));

        form.Children.Add(feedback);
        form.Children.Add(T(
            "Your account is stored locally on this computer.",
            11, false, muted));

        var panel = Card(form);
        panel.Padding = new Thickness(30);
        panel.Margin = new Thickness(25);

        Grid.SetColumn(panel, 1);
        layout.Children.Add(panel);

        Root.Children.Add(layout);
    }

    private void ShowApp(string page = "Home")
    {
        StopGame();
        Root.Children.Clear();

        var shell = new Grid();
        shell.ColumnDefinitions.Add(
            new ColumnDefinition { Width = new GridLength(235) });
        shell.ColumnDefinitions.Add(new ColumnDefinition());

        var sidebar = new DockPanel { Background = forest };

        var brand = new StackPanel { Margin = new Thickness(20, 28, 15, 25) };
        brand.Children.Add(T("🌵 CYBER CACTI", 19, true, Brushes.White));
        brand.Children.Add(T("Hedgehog awareness project", 11, false, sage));
        DockPanel.SetDock(brand, Dock.Top);
        sidebar.Children.Add(brand);

        var nav = new StackPanel { Margin = new Thickness(12) };

        string[] pages =
        {
            "Home", "Dashboard", "About Us", "Hedgehogs",
            "How to Help", "More", "Innovation Project",
            "Contact Us", "Dash Game"
        };

        foreach (string item in pages)
        {
            var button = new Button
            {
                Content = item,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(14, 11, 10, 11),
                Margin = new Thickness(0, 3, 0, 3),
                Background = Brushes.Transparent,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontSize = 14
            };

            button.Click += (_, _) => ShowPage(item);
            nav.Children.Add(button);
        }

        sidebar.Children.Add(nav);

        var main = new DockPanel { Margin = new Thickness(28) };

        var heading = T("CYBER CACTI  •  HEDGEHOG AWARENESS",
            11, true, muted);

        DockPanel.SetDock(heading, Dock.Top);
        main.Children.Add(heading);

        content = new Grid { Margin = new Thickness(0, 20, 0, 0) };
        main.Children.Add(content);

        Grid.SetColumn(sidebar, 0);
        Grid.SetColumn(main, 1);

        shell.Children.Add(sidebar);
        shell.Children.Add(main);
        Root.Children.Add(shell);

        ShowPage(page);
    }

    private void ShowPage(string page)
    {
        if (content == null) return;

        StopGame();
        content.Children.Clear();

        var stack = new StackPanel();
        content.Children.Add(stack);

        switch (page)
        {
            case "Home":
                stack.Children.Add(T("Welcome to Cyber Cacti 🌿", 31, true, forest));
                stack.Children.Add(T(
                    "Helping hedgehogs, one small action at a time.",
                    17, false, muted));

                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Our mission", 21, true, forest),
                        T("Cyber Cacti encourages people to make everyday spaces safer for hedgehogs through practical, positive action.", 15),
                        Btn("Explore how to help", () => ShowPage("How to Help"), true)
                    }
                }));

                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Your impact starts here", 20, true, forest),
                        T($"You've completed {tasks.Count(x => x)} of 5 suggested actions.", 15),
                        Btn("View dashboard", () => ShowPage("Dashboard"))
                    }
                }));
                break;

            case "Dashboard":
                stack.Children.Add(T("Your Dashboard", 30, true, forest));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Actions completed", 17, true, muted),
                        T($"{tasks.Count(x => x)} / 5", 40, true, forest),
                        T("Every helpful action makes a difference.", 15)
                    }
                }));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Hedgehog fact", 20, true, forest),
                        T("Hedgehogs are nocturnal mammals. Connected gardens and green spaces can help them move around safely.", 15)
                    }
                }));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Your mission", 20, true, forest),
                        T("Choose one new hedgehog-friendly action to try this week.", 15),
                        Btn("Open checklist", () => ShowPage("How to Help"), true)
                    }
                }));
                break;

            case "About Us":
                stack.Children.Add(T("About Us", 30, true, forest));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Who we are", 21, true, forest),
                        T("Cyber Cacti is a project focused on hedgehog awareness, practical conservation and helping people understand how small changes can make outdoor spaces safer for wildlife.", 16)
                    }
                }));
                break;

            case "Hedgehogs":
                stack.Children.Add(T("Hedgehogs", 30, true, forest));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Getting to know hedgehogs", 21, true, forest),
                        T("Hedgehogs are mostly active at night and use gardens, parks and hedgerows to find food and move between habitats.", 15),
                        T("A few useful things to know", 18, true, forest),
                        T("• They need connected routes between green spaces.\n• Garden hazards can include uncovered drains, netting and litter.\n• A quiet, sheltered space can provide somewhere to rest.\n• If you find an injured hedgehog, contact a local wildlife rescue or vet for advice.", 15)
                    }
                }));
                break;

            case "How to Help":
                stack.Children.Add(T("How to Help", 30, true, forest));
                stack.Children.Add(T(
                    "Tick off actions as you complete them. Your progress is saved.",
                    14, false, muted));

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
                        Margin = new Thickness(0, 10, 0, 10),
                        Foreground = ink
                    };

                    check.Checked += (_, _) =>
                    {
                        tasks[index] = true;
                        SaveProgress();
                    };

                    check.Unchecked += (_, _) =>
                    {
                        tasks[index] = false;
                        SaveProgress();
                    };

                    list.Children.Add(check);
                }

                list.Children.Add(T(
                    $"Completed: {tasks.Count(x => x)} / 5",
                    16, true, forest));

                list.Children.Add(Btn("Reset checklist", () =>
                {
                    tasks = new bool[5];
                    SaveProgress();
                    ShowPage("How to Help");
                }));

                stack.Children.Add(Card(list));
                break;

            case "More":
                stack.Children.Add(T("More", 30, true, forest));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Explore Cyber Cacti", 21, true, forest),
                        Btn("Innovation Project", () => ShowPage("Innovation Project")),
                        Btn("Contact Us", () => ShowPage("Contact Us")),
                        Btn("Sign out", () =>
                        {
                            username = "";
                            content = null;
                            ShowLogin();
                        })
                    }
                }));
                break;

            case "Innovation Project":
                stack.Children.Add(T("Innovation Project", 30, true, forest));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Our innovation project", 21, true, forest),
                        T("This page is reserved for the Cyber Cacti innovation project. More information will be added here.", 15)
                    }
                }));
                break;

            case "Contact Us":
                stack.Children.Add(T("Contact Us", 30, true, forest));
                stack.Children.Add(Card(new StackPanel
                {
                    Children =
                    {
                        T("Get in touch", 21, true, forest),
                        T("Questions or ideas about Cyber Cacti? We'd be happy to hear from you.", 15),
                        T("cybercactirobotics@gmail.com", 17, true, forest)
                    }
                }));
                break;

            case "Dash Game":
                BuildGame(stack);
                break;
        }
    }

    private void BuildGame(StackPanel stack)
    {
        stack.Children.Add(T("Dash Game", 30, true, forest));
        stack.Children.Add(T(
            "Move between three lanes, collect apples and avoid logs.",
            14, false, muted));

        var info = T("Score: 0    Lives: 3", 16, true, forest);
        stack.Children.Add(info);

        gameCanvas = new Canvas
        {
            Height = 390,
            Background = B("#EAF0E5"),
            ClipToBounds = true
        };

        stack.Children.Add(Card(gameCanvas));

        var controls = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };

        controls.Children.Add(Btn("◀ Left", () =>
            lane = Math.Max(0, lane - 1)));

        controls.Children.Add(Btn("Right ▶", () =>
            lane = Math.Min(2, lane + 1)));

        controls.Children.Add(Btn("Start / Restart",
            () => StartGame(info), true));

        stack.Children.Add(controls);
        stack.Children.Add(T(
            "You can also use the keyboard arrow keys.",
            12, false, muted));

        KeyDown -= GameKeyDown;
        KeyDown += GameKeyDown;
        Focusable = true;
        Focus();
    }

    private void GameKeyDown(object sender,
        System.Windows.Input.KeyEventArgs e)
    {
        if (!playing) return;

        if (e.Key == System.Windows.Input.Key.Left)
            lane = Math.Max(0, lane - 1);

        if (e.Key == System.Windows.Input.Key.Right)
            lane = Math.Min(2, lane + 1);
    }

    private void StartGame(TextBlock info)
    {
        StopGame();

        if (gameCanvas == null) return;

        gameCanvas.Children.Clear();
        objects.Clear();

        score = 0;
        lives = 3;
        ticks = 0;
        lane = 1;
        playing = true;

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(35)
        };

        timer.Tick += (_, _) =>
        {
            if (!playing || gameCanvas == null) return;

            ticks++;

            if (ticks % 28 == 0)
                SpawnObject();

            double speed = 3.5 + score / 15.0;
            double playerY = gameCanvas.ActualHeight - 58;

            foreach (var obj in objects.ToList())
            {
                obj.Y += speed;
                Canvas.SetTop(obj.Shape, obj.Y);

                if (obj.Y > playerY - 20 &&
                    obj.Y < playerY + 26 &&
                    obj.Lane == lane)
                {
                    if (obj.Apple) score++;
                    else lives--;

                    gameCanvas.Children.Remove(obj.Shape);
                    objects.Remove(obj);
                }
                else if (obj.Y > gameCanvas.ActualHeight)
                {
                    gameCanvas.Children.Remove(obj.Shape);
                    objects.Remove(obj);
                }
            }

            DrawPlayer();

            info.Text = $"Score: {score}    Lives: {lives}";

            if (lives <= 0)
            {
                StopGame();

                if (score > highScore)
                {
                    highScore = score;
                    SaveProgress();
                }

                info.Text = $"Game over! Score: {score}    Best: {highScore}";
            }
        };

        timer.Start();
    }

    private void SpawnObject()
    {
        if (gameCanvas == null) return;

        bool apple = Random.Shared.Next(100) < 58;
        int objectLane = Random.Shared.Next(3);

        var shape = new Ellipse
        {
            Width = 28,
            Height = 28,
            Fill = apple ? B("#D95D39") : B("#795548"),
            Stroke = Brushes.White,
            StrokeThickness = 2
        };

        double width = gameCanvas.ActualWidth > 0
            ? gameCanvas.ActualWidth : 600;

        Canvas.SetLeft(shape,
            width / 2 + (objectLane - 1) * 145 - 14);

        Canvas.SetTop(shape, -30);
        gameCanvas.Children.Add(shape);

        objects.Add(new GameObject
        {
            Shape = shape,
            Lane = objectLane,
            Y = -30,
            Apple = apple
        });
    }

    private void DrawPlayer()
    {
        if (gameCanvas == null) return;

        var old = gameCanvas.Children
            .OfType<Border>()
            .FirstOrDefault(x => x.Tag?.ToString() == "player");

        if (old != null)
            gameCanvas.Children.Remove(old);

        var player = new Border
        {
            Tag = "player",
            Width = 42,
            Height = 42,
            CornerRadius = new CornerRadius(20),
            Background = forest,
            Child = new TextBlock
            {
                Text = "🦔",
                FontSize = 26,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        double width = gameCanvas.ActualWidth > 0
            ? gameCanvas.ActualWidth : 600;

        Canvas.SetLeft(player,
            width / 2 + (lane - 1) * 145 - 21);

        Canvas.SetTop(player,
            Math.Max(0, gameCanvas.ActualHeight - 55));

        gameCanvas.Children.Add(player);
    }

    private void StopGame()
    {
        playing = false;
        timer?.Stop();
        timer = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopGame();
        base.OnClosed(e);
    }
}
