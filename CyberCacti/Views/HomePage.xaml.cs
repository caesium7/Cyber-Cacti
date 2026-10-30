using System.Windows;
using System.Windows.Controls;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class HomePage : UserControl
{
    private readonly IAppHost _host;

    public HomePage(IAppHost host)
    {
        _host = host;
        InitializeComponent();

        int done = host.Session.Tasks.Count(x => x);
        ProgressLabel.Text = $"{done} of 5 actions completed";
        ProgressBar.Value = done;
        ProgressHint.Text = done == 5
            ? "Fantastic work. You have completed every suggested action."
            : "Keep going. Each action helps make outdoor spaces safer.";
    }

    private void Help_Click(object sender, RoutedEventArgs e) => _host.Navigate("How to Help");

    private void Hedgehogs_Click(object sender, RoutedEventArgs e) => _host.Navigate("Hedgehogs");

    private void Dashboard_Click(object sender, RoutedEventArgs e) => _host.Navigate("Dashboard");
}
