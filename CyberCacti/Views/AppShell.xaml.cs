using System.Windows;
using System.Windows.Controls;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class AppShell : UserControl
{
    private readonly IAppHost _host;
    private string _current = "";
    private bool _suppressNav;

    public AppShell(IAppHost host)
    {
        _host = host;
        InitializeComponent();
        UserNameText.Text = host.Session.Username;
        WelcomeText.Text = "Welcome back, " + host.Session.Username;
    }

    public void Navigate(string page)
    {
        if (_current == page && PageHost.Content != null)
            return;

        _current = page;
        _suppressNav = true;

        foreach (var item in NavPanel.Children.OfType<RadioButton>())
            item.IsChecked = (item.Tag as string) == page;

        _suppressNav = false;

        PageHost.Content = page switch
        {
            "Dashboard" => new DashboardPage(_host),
            "About Us" => new AboutPage(),
            "Hedgehogs" => new HedgehogsPage(_host),
            "How to Help" => new ChecklistPage(_host),
            "More" => new MorePage(_host),
            "Innovation Project" => new InnovationPage(),
            "Contact Us" => new ContactPage(),
            "Dash Game" => new DashGamePage(_host),
            _ => new HomePage(_host)
        };
    }

    private void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (_suppressNav)
            return;

        if (sender is RadioButton { Tag: string page })
            Navigate(page);
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        _host.Session.Username = "";
        _host.ShowLogin();
    }
}
