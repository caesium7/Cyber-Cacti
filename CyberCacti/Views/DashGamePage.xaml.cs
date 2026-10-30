using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CyberCacti.Controls;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class DashGamePage : UserControl
{
    private readonly IAppHost _host;
    private readonly List<GameObject> _objects = new();
    private readonly Random _random = new();
    private DispatcherTimer? _timer;
    private int _lane = 1;
    private int _score;
    private int _lives = 3;
    private int _ticks;
    private bool _playing;
    private Window? _window;

    private sealed class GameObject
    {
        public required FrameworkElement Shape { get; init; }
        public int Lane { get; init; }
        public double Y { get; set; }
        public bool Apple { get; init; }
    }

    public DashGamePage(IAppHost host)
    {
        _host = host;
        InitializeComponent();
        BestText.Text = host.Session.HighScore.ToString();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _window = Window.GetWindow(this);
        if (_window != null)
            _window.PreviewKeyDown += GameKeyDown;

        DrawPlayfield();
        DrawPlayer();
        UpdateHud();

        GameCanvas.SizeChanged += (_, _) =>
        {
            if (_playing)
                return;

            DrawPlayfield();
            DrawPlayer();
        };
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopGame();
        if (_window != null)
            _window.PreviewKeyDown -= GameKeyDown;
    }

    private void Left_Click(object sender, RoutedEventArgs e) => MovePlayer(-1);

    private void Right_Click(object sender, RoutedEventArgs e) => MovePlayer(1);

    private void Start_Click(object sender, RoutedEventArgs e) => StartGame();

    private void MovePlayer(int direction)
    {
        if (!_playing)
            return;

        _lane = Math.Clamp(_lane + direction, 0, 2);
        DrawPlayer();
    }

    private void GameKeyDown(object sender, KeyEventArgs e)
    {
        if (!_playing)
            return;

        if (e.Key is Key.Left or Key.A)
        {
            MovePlayer(-1);
            e.Handled = true;
        }
        else if (e.Key is Key.Right or Key.D)
        {
            MovePlayer(1);
            e.Handled = true;
        }
    }

    private void StartGame()
    {
        StopGame();

        _objects.Clear();
        _score = 0;
        _lives = 3;
        _ticks = 0;
        _lane = 1;
        _playing = true;

        DrawPlayfield();
        DrawPlayer();
        UpdateHud();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(35) };
        _timer.Tick += (_, _) => GameTick();
        _timer.Start();
        GameCanvas.Focus();
    }

    private double GameWidth() =>
        GameCanvas.ActualWidth > 0 ? GameCanvas.ActualWidth : 720;

    private double LaneX(int which) =>
        GameWidth() / 2 + (which - 1) * (GameWidth() * 0.58 / 3);

    private void GameTick()
    {
        if (!_playing)
            return;

        _ticks++;
        if (_ticks % 27 == 0)
            SpawnObject();

        double speed = 3.1 + _score / 12.0;
        double playerY = GameCanvas.ActualHeight - 70;

        foreach (var obj in _objects.ToList())
        {
            obj.Y += speed;
            Canvas.SetTop(obj.Shape, obj.Y);

            bool collision = obj.Y > playerY - 18 &&
                             obj.Y < playerY + 34 &&
                             obj.Lane == _lane;

            if (collision)
            {
                if (obj.Apple)
                    _score++;
                else
                    _lives--;

                GameCanvas.Children.Remove(obj.Shape);
                _objects.Remove(obj);
                continue;
            }

            if (obj.Y > GameCanvas.ActualHeight)
            {
                GameCanvas.Children.Remove(obj.Shape);
                _objects.Remove(obj);
            }
        }

        DrawPlayer();
        UpdateHud();

        if (_lives <= 0)
            EndGame();
    }

    private void DrawPlayfield()
    {
        GameCanvas.Children.Clear();

        double width = GameWidth();
        double height = 420;

        var grass = new Rectangle
        {
            Width = width,
            Height = height,
            Fill = new SolidColorBrush(Color.FromRgb(0x7F, 0xA8, 0x68))
        };
        GameCanvas.Children.Add(grass);

        var path = new Border
        {
            Width = width * 0.62,
            Height = height,
            Background = new SolidColorBrush(Color.FromRgb(0xC4, 0xA4, 0x72)),
            CornerRadius = new CornerRadius(8)
        };
        Canvas.SetLeft(path, width * 0.19);
        GameCanvas.Children.Add(path);

        var inner = new Border
        {
            Width = width * 0.56,
            Height = height,
            Background = new SolidColorBrush(Color.FromRgb(0xD4, 0xB4, 0x82)),
            CornerRadius = new CornerRadius(6)
        };
        Canvas.SetLeft(inner, width * 0.22);
        GameCanvas.Children.Add(inner);

        for (int i = 1; i <= 2; i++)
        {
            var divider = new Rectangle
            {
                Width = 3,
                Height = height,
                Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0xD6, 0xB0)),
                Opacity = 0.85
            };
            Canvas.SetLeft(divider, width / 2 + (i - 1.5) * (width * 0.56 / 3));
            GameCanvas.Children.Add(divider);
        }

        for (int i = 0; i < 4; i++)
        {
            var cactus = new CactusArt
            {
                Width = 36,
                Height = 56,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(cactus, i % 2 == 0 ? 18 : width - 54);
            Canvas.SetTop(cactus, 40 + i * 90);
            GameCanvas.Children.Add(cactus);
        }
    }

    private void SpawnObject()
    {
        bool apple = _random.Next(100) < 58;
        int which = _random.Next(3);

        FrameworkElement shape = apple
            ? new AppleArt { Width = 34, Height = 38 }
            : new LogArt { Width = 46, Height = 28 };

        Canvas.SetLeft(shape, LaneX(which) - (apple ? 17 : 23));
        Canvas.SetTop(shape, -40);
        GameCanvas.Children.Add(shape);

        _objects.Add(new GameObject
        {
            Shape = shape,
            Lane = which,
            Y = -40,
            Apple = apple
        });
    }

    private void DrawPlayer()
    {
        var old = GameCanvas.Children
            .OfType<FrameworkElement>()
            .FirstOrDefault(x => x.Tag as string == "player");

        if (old != null)
            GameCanvas.Children.Remove(old);

        var player = new HedgehogArt
        {
            Tag = "player",
            Width = 78,
            Height = 52
        };

        Canvas.SetLeft(player, LaneX(_lane) - 39);
        Canvas.SetTop(player, Math.Max(0, GameCanvas.ActualHeight - 64));
        GameCanvas.Children.Add(player);
    }

    private void UpdateHud()
    {
        ScoreText.Text = _score.ToString();
        BestText.Text = _host.Session.HighScore.ToString();

        LivesPanel.Children.Clear();
        for (int i = 0; i < Math.Max(0, _lives); i++)
        {
            LivesPanel.Children.Add(new LifeHeart { Margin = new Thickness(0, 0, 6, 0) });
        }

        InfoText.Text = _playing
            ? "Collect apples and keep moving — the pace increases as you score."
            : "Ready when you are — press start to play.";
    }

    private void EndGame()
    {
        StopGame();

        if (_score > _host.Session.HighScore)
        {
            _host.Session.HighScore = _score;
            _host.SaveProgress();
        }

        UpdateHud();
        InfoText.Text = $"Run complete. You scored {_score}. Personal best {_host.Session.HighScore}.";

        var panel = new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(22),
            Width = 280,
            BorderBrush = (Brush)FindResource("LineBrush"),
            BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Run complete",
                        FontSize = 22,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)FindResource("ForestBrush"),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"You scored {_score} points.",
                        FontSize = 14,
                        Margin = new Thickness(0, 8, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    },
                    new TextBlock
                    {
                        Text = $"Personal best: {_host.Session.HighScore}",
                        FontSize = 12,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = (Brush)FindResource("MutedBrush"),
                        Margin = new Thickness(0, 4, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    }
                }
            }
        };

        Canvas.SetLeft(panel, Math.Max(10, (GameWidth() - 280) / 2));
        Canvas.SetTop(panel, 130);
        GameCanvas.Children.Add(panel);
    }

    private void StopGame()
    {
        _playing = false;
        _timer?.Stop();
        _timer = null;
    }
}
