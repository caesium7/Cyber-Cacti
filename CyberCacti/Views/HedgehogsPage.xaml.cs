using System.Windows;
using System.Windows.Controls;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class HedgehogsPage : UserControl
{
    private readonly IAppHost _host;

    public HedgehogsPage(IAppHost host)
    {
        _host = host;
        InitializeComponent();
    }

    private void Checklist_Click(object sender, RoutedEventArgs e) => _host.Navigate("How to Help");
}
