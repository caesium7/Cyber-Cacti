namespace CyberCacti.Services;

public sealed class AppSession
{
    public string Username { get; set; } = "";
    public bool[] Tasks { get; set; } = new bool[5];
    public int HighScore { get; set; }
}
