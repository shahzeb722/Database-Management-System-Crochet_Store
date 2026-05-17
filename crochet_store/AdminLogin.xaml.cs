// NOTE: You must have a reference to the Microsoft.VisualBasic assembly 
// in your project to use Interaction.InputBox.
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace crochet_store
{
    public partial class AdminLogin : Window
    {
        // *!!! CRITICAL: ENSURE THIS CONNECTION STRING IS 100% CORRECT !!!*
        private readonly string connectionString = DBConnection.ConnectionString;


        public AdminLogin()
        {
            InitializeComponent();
            TxtUsername.Focus();
        }

        // --- LOGIN LOGIC ---

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = PwbPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.",
                                "Login Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CheckAdminLogin(username, password))
            {
                MessageBox.Show("Login successful. Opening Dashboard...",
                                "Welcome",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                // NAVIGATION TO DASHBOARD
                adminDashboard dashboard = new adminDashboard();
                dashboard.Show();
                this.Close();
            }
        }

        private bool CheckAdminLogin(string username, string password)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // FIX: Explicitly referencing the dbo schema
                    using (SqlCommand cmd = new SqlCommand("dbo.admin_login_check", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                string errorMessage = sqlEx.Message.Split('\n')[0].Trim();
                MessageBox.Show($"Authentication Failed: {errorMessage}",
                                "Login Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A system or connection error occurred: {ex.Message}",
                                "Fatal Error",
                                MessageBoxButton.OK, MessageBoxImage.Stop);
                return false;
            }
        }

        // --- CHANGE PASSWORD LOGIC ---

        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string oldPassword = PwbPassword.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(oldPassword))
            {
                MessageBox.Show("Please enter your Username and Current Password first.",
                                "Missing Details",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Prompt the user for the New Password
            string newPassword = Interaction.InputBox(
                "Enter your NEW password:",
                "Change Password",
                "",
                -1, -1);

            if (string.IsNullOrEmpty(newPassword))
            {
                return;
            }

            if (newPassword.Length < 6)
            {
                MessageBox.Show("New password must be at least 6 characters long.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ChangeAdminPassword(username, oldPassword, newPassword);
        }

        private async void ChangeAdminPassword(string username, string oldPassword, string newPassword)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    // FIX: Explicitly referencing the dbo schema
                    using (SqlCommand cmd = new SqlCommand("dbo.admin_change_password", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@old_password", oldPassword);
                        cmd.Parameters.AddWithValue("@new_password", newPassword);

                        cmd.ExecuteNonQuery();

                        MessageBox.Show("Password updated successfully! You can now log in with your new password.",
                                        "Success",
                                        MessageBoxButton.OK, MessageBoxImage.Information);

                        PwbPassword.Password = string.Empty;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                string errorMessage = sqlEx.Message.Split('\n')[0].Trim();
                MessageBox.Show($"Password Change Failed: {errorMessage}",
                                "Update Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A system error occurred during password change: {ex.Message}",
                                "Fatal Error",
                                MessageBoxButton.OK, MessageBoxImage.Stop);
            }
        }
    }
}