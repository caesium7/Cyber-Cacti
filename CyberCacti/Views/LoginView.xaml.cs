using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class LoginView : UserControl
{
    private readonly IAppHost _host;
    private bool _registering;

    public LoginView(IAppHost host)
    {
        _host = host;
        InitializeComponent();
        Loaded += (_, _) => NameBox.Focus();
        UpdateMode();
    }

    private void SignInMode_Click(object sender, RoutedEventArgs e)
    {
        _registering = false;
        UpdateMode();
    }

    private void RegisterMode_Click(object sender, RoutedEventArgs e)
    {
        _registering = true;
        UpdateMode();
    }

    private void UpdateMode()
    {
        ConfirmPanel.Visibility = _registering ? Visibility.Visible : Visibility.Collapsed;
        WelcomeHint.Text = _registering
            ? "Create a local account to save your checklist and high score."
            : "Sign in to continue your journey.";
        SubmitButton.Content = _registering ? "Create my account" : "Sign in";
        SignInMode.Style = (Style)FindResource(_registering ? "DefaultButton" : "PrimaryButton");
        RegisterMode.Style = (Style)FindResource(_registering ? "PrimaryButton" : "DefaultButton");
        Feedback.Text = "";
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            Submit_Click(sender, e);
    }

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        if (_registering && PasswordBox.Password != ConfirmBox.Password)
        {
            Feedback.Text = "Your passwords don't match.";
            return;
        }

        if (AccountService.Authenticate(
                NameBox.Text,
                PasswordBox.Password,
                _registering,
                out string username,
                out string message))
        {
            _host.Session.Username = username;
            _host.ShowApp();
        }
        else
        {
            Feedback.Text = message;
        }
    }
}
