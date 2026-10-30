using System.Windows;
using System.Windows.Controls;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class MorePage : UserControl
{
    private readonly IAppHost _host;

    public MorePage(IAppHost host)
    {
        _host = host;
        InitializeComponent();
        AccountLabel.Text = "Signed in as " + host.Session.Username;
    }

    private void Innovation_Click(object sender, RoutedEventArgs e) => _host.Navigate("Innovation Project");

    private void Contact_Click(object sender, RoutedEventArgs e) => _host.Navigate("Contact Us");

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        _host.Session.Username = "";
        _host.ShowLogin();
    }
}
