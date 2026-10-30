using System.Windows;
using CyberCacti.Services;
using CyberCacti.Views;

namespace CyberCacti;

public partial class MainWindow : Window, IAppHost
{
    private AppShell? _shell;

    public AppSession Session { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ProgressService.Load(Session);
        ShowLogin();
    }

    public void ShowLogin()
    {
        _shell = null;
        Root.Children.Clear();
        Root.Children.Add(new LoginView(this));
    }

    public void ShowApp(string page = "Home")
    {
        Root.Children.Clear();
        _shell = new AppShell(this);
        Root.Children.Add(_shell);
        _shell.Navigate(page);
    }

    public void Navigate(string page) => _shell?.Navigate(page);

    public void SaveProgress()
    {
        if (ProgressService.Save(Session))
            return;

        MessageBox.Show(
            "Your progress could not be saved. Please check your computer's storage permissions.",
            "Cyber Cacti",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    protected override void OnClosed(EventArgs e)
    {
        SaveProgress();
        base.OnClosed(e);
    }
}
