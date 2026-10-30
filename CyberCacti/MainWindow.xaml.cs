
using System.Windows;
using System.Windows.Controls;

namespace CyberCacti
{
    public partial class MainWindow : Window
    {
        private bool _loadingChecklist = false;

        private CheckBox[] Checklist =>
            new CheckBox[]
            {
                Action1,
                Action2,
                Action3,
                Action4,
                Action5
            };

        public MainWindow()
        {
            InitializeComponent();
            LoadChecklist();
        }

        private void LoadChecklist()
        {
            _loadingChecklist = true;

            bool[] savedProgress = ProgressStore.Load();

            for (int i = 0; i < Checklist.Length; i++)
            {
                Checklist[i].IsChecked = savedProgress[i];
            }

            _loadingChecklist = false;
            UpdateProgress();
        }

        private void Checklist_Changed(object sender, RoutedEventArgs e)
        {
            if (_loadingChecklist)
                return;

            bool[] progress = new bool[Checklist.Length];

            for (int i = 0; i < Checklist.Length; i++)
            {
                progress[i] = Checklist[i].IsChecked == true;
            }

            ProgressStore.Save(progress);
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            int completed = 0;

            foreach (CheckBox item in Checklist)
            {
                if (item.IsChecked == true)
                    completed++;
            }

            ProgressBarControl.Value = completed;
            ProgressText.Text =
                $"{completed} out of 5 actions completed";

            DashboardCount.Text = $"{completed} / 5";
            DashboardProgress.Value = completed;
            DashboardPercentage.Text =
                $"{completed * 20}% completed";
        }

        private void ResetProgress_Click(object sender, RoutedEventArgs e)
        {
            _loadingChecklist = true;

            foreach (CheckBox item in Checklist)
            {
                item.IsChecked = false;
            }

            _loadingChecklist = false;

            ProgressStore.Reset();
            UpdateProgress();
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button ||
                button.Tag is not string page)
                return;

            PageTitle.Text = page;

            bool showHelp = page == "How to Help";
            bool showDashboard = page == "Dashboard";

            HelpPanel.Visibility = showHelp
                ? Visibility.Visible
                : Visibility.Collapsed;

            DashboardPanel.Visibility = showDashboard
                ? Visibility.Visible
                : Visibility.Collapsed;

            StandardPanel.Visibility =
                (!showHelp && !showDashboard)
                ? Visibility.Visible
                : Visibility.Collapsed;

            switch (page)
            {
                case "Home":
                    PageHeading.Text =
                        "Protecting hedgehogs, one step at a time.";
                    PageDescription.Text =
                        "Welcome to Cyber Cacti! Explore our project, discover fascinating hedgehog facts and learn how you can help protect these wonderful animals.";
                    break;

                case "Dashboard":
                    UpdateProgress();
                    break;

                case "About Us":
                    PageHeading.Text = "Meet the Cyber Cacti team";
                    PageDescription.Text =
                        "We are the team behind Cyber Cacti. Our aim is to use creativity, technology and teamwork to help protect hedgehogs.";
                    break;

                case "Hedgehogs":
                    PageHeading.Text = "Discover Hedgehogs";
                    PageDescription.Text =
                        "Hedgehogs are fascinating animals. Explore this page to learn more about their lives, habitats and the challenges they face.";
                    break;

                case "How to Help":
                    break;

                case "More":
                    PageHeading.Text = "More from Cyber Cacti";
                    PageDescription.Text =
                        "Explore our Innovation Project, get in touch with the team or try the Dash game.";
                    break;

                case "Innovation Project":
                    PageHeading.Text = "Innovation Project";
                    PageDescription.Text =
                        "Placeholder text. Information about our Innovation Project will be added here.";
                    break;

                case "Contact Us":
                    PageHeading.Text = "Contact Us";
                    PageDescription.Text =
                        "You can contact the Cyber Cacti team at cybercactirobotics@gmail.com.";
                    break;

                case "Dash":
                    PageHeading.Text = "Dash";
                    PageDescription.Text =
                        "Welcome to Dash! The game will be added in a later development step.";
                    break;
            }
        }

        private void Explore_Click(object sender, RoutedEventArgs e)
        {
            PageTitle.Text = "How to Help";

            StandardPanel.Visibility = Visibility.Collapsed;
            DashboardPanel.Visibility = Visibility.Collapsed;
            HelpPanel.Visibility = Visibility.Visible;
        }
    }
}
