using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CyberCacti.Services;

namespace CyberCacti.Views;

public partial class ChecklistPage : UserControl
{
    private readonly IAppHost _host;

    public ChecklistPage(IAppHost host)
    {
        _host = host;
        InitializeComponent();
        BuildTasks();
        RefreshProgress();
    }

    private void BuildTasks()
    {
        TaskList.Children.Clear();

        for (int i = 0; i < ConservationActions.Items.Length; i++)
        {
            int index = i;
            var row = new Border
            {
                Background = (Brush)FindResource("PaleBrush"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 8),
                BorderBrush = (Brush)FindResource("LineBrush"),
                BorderThickness = new Thickness(1)
            };

            var check = new CheckBox
            {
                IsChecked = _host.Session.Tasks[i],
                Content = new TextBlock
                {
                    Text = ConservationActions.Items[i],
                    TextWrapping = TextWrapping.Wrap
                }
            };

            check.Checked += (_, _) => SetTask(index, true);
            check.Unchecked += (_, _) => SetTask(index, false);

            row.Child = check;
            TaskList.Children.Add(row);
        }
    }

    private void SetTask(int index, bool value)
    {
        _host.Session.Tasks[index] = value;
        _host.SaveProgress();
        RefreshProgress();
    }

    private void RefreshProgress()
    {
        int done = _host.Session.Tasks.Count(x => x);
        ProgressLabel.Text = $"{done} / 5 completed";
        ProgressBar.Value = done;
        ProgressHint.Text = done == 5
            ? "All done. Thank you for making a difference."
            : "Tick an action when you have completed it.";
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(
                "Are you sure you want to reset all five actions?",
                "Reset progress",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _host.Session.Tasks = new bool[5];
        _host.SaveProgress();
        BuildTasks();
        RefreshProgress();
    }
}
