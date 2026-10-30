
using System.Windows;
using System.Windows.Controls;

namespace CyberCacti
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Navigate_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string page)
                return;

            PageTitle.Text = page;

            switch (page)
            {
                case "Home":
                    PageHeading.Text = "Protecting hedgehogs, one step at a time.";
                    PageDescription.Text =
                        "Welcome to Cyber Cacti! Explore our project, discover fascinating hedgehog facts and learn how you can help protect these wonderful animals.";
                    break;

                case "Dashboard":
                    PageHeading.Text = "Your Dashboard";
                    PageDescription.Text =
                        "Track your progress, discover hedgehog facts and see how your actions can make a difference.";
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
                    PageHeading.Text = "How You Can Help";
                    PageDescription.Text =
                        "Small actions can make a difference. Explore practical ways to make your garden and local environment safer for hedgehogs.";
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
            PageHeading.Text = "Make a difference for hedgehogs";
            PageDescription.Text =
                "Discover practical actions you can take to help hedgehogs and protect their habitats.";
        }
    }
}
