namespace CyberCacti.Services;

public interface IAppHost
{
    AppSession Session { get; }

    void ShowLogin();

    void ShowApp(string page = "Home");

    void Navigate(string page);

    void SaveProgress();
}
