using System;
using System.Windows;

namespace crochet_store
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// Welcome Screen for Crochet Craft Store Management System
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Open Admin Login window
                AdminLogin loginWindow = new AdminLogin();
                loginWindow.Show();

                // Hide the welcome screen
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error opening login screen: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }
}