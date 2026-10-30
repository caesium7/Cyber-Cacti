using System.Windows;
using System.Windows.Controls;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class DashboardPage : UserControl
{
    private readonly IAppHost _host;

    public DashboardPage(IAppHost host)
    {
        _host = host;
        InitializeComponent();

        int done = host.Session.Tasks.Count(x => x);
        ActionsValue.Text = done.ToString();
        ScoreValue.Text = host.Session.HighScore.ToString();
        ImpactLabel.Text = $"{done} / 5 actions completed";
        ProgressBar.Value = done;
        ImpactHint.Text = done == 5
            ? "You have completed all five actions. Thank you for helping wildlife."
            : "There are still more ways to make a difference. Open your checklist to continue.";
    }

    private void Checklist_Click(object sender, RoutedEventArgs e) => _host.Navigate("How to Help");
}
