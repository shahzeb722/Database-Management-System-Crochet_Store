using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace crochet_store
{
    public partial class DeliveryMgnt : Window
    {

        private readonly string connectionString = DBConnection.ConnectionString;


        private bool isUpdateMode = false;

        public DeliveryMgnt()
        {
            InitializeComponent();
            _ = LoadAllDeliveriesAsync(); // Load deliveries on startup
        }

        // ================= VIEW SWITCHING =================
        private void HideAll()
        {
            DeliveryForm.Visibility = Visibility.Collapsed;
            dgDeliveries.Visibility = Visibility.Collapsed;
        }

        private void ShowAddDelivery(object sender, RoutedEventArgs e)
        {
            HideAll();
            DeliveryForm.Visibility = Visibility.Visible;
            isUpdateMode = false;

            txtPageTitle.Text = "Add New Delivery";
            txtPageSubtitle.Text = "Assign delivery to an employee";

            btnSave.Content = "Add Delivery";
            ClearForm();

            txtDeliveryId.IsReadOnly = true;
            txtDeliveryId.Background = System.Windows.Media.Brushes.LightGray;
            txtOrderId.IsReadOnly = false;
            txtOrderId.Background = System.Windows.Media.Brushes.White;
            txtEmployeeId.IsReadOnly = false;
            txtEmployeeId.Background = System.Windows.Media.Brushes.White;
            dpDeliveryDate.IsEnabled = true;

            cmbDeliveryStatus.IsEnabled = false;
            cmbDeliveryStatus.SelectedIndex = 0; // pending
        }

        private void ShowUpdateDelivery(object sender, RoutedEventArgs e)
        {
            HideAll();
            DeliveryForm.Visibility = Visibility.Visible;
            isUpdateMode = true;

            txtPageTitle.Text = "Update Delivery Status";
            txtPageSubtitle.Text = "Change status of pending deliveries only";

            btnSave.Content = "Update Delivery";
            ClearForm();

            txtDeliveryId.IsReadOnly = false;
            txtDeliveryId.Background = System.Windows.Media.Brushes.White;
            txtOrderId.IsReadOnly = true;
            txtOrderId.Background = System.Windows.Media.Brushes.LightGray;
            txtEmployeeId.IsReadOnly = true;
            txtEmployeeId.Background = System.Windows.Media.Brushes.LightGray;
            dpDeliveryDate.IsEnabled = false;
            cmbDeliveryStatus.IsEnabled = true;

            MessageBox.Show(
                "UPDATE DELIVERY RULES:\n\n" +
                "✓ Only PENDING deliveries can be updated\n" +
                "✓ You can only change the delivery status\n" +
                "✗ Order ID, Employee, and Date CANNOT be changed\n\n" +
                "Enter the Delivery ID to update its status.",
                "Update Delivery Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // ================= SAVE DELIVERY =================
        private async void SaveDelivery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isUpdateMode)
                {
                    if (string.IsNullOrWhiteSpace(txtDeliveryId.Text))
                    {
                        MessageBox.Show("Please enter the Delivery ID to update.", "Missing Delivery ID",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (cmbDeliveryStatus.SelectedItem == null)
                    {
                        MessageBox.Show("Please select a new delivery status.", "Missing Status",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int deliveryId = Convert.ToInt32(txtDeliveryId.Text);
                    string newStatus = (cmbDeliveryStatus.SelectedItem as ComboBoxItem).Content.ToString();

                    await ExecuteNonQueryAsync("update_delivery",
                        new SqlParameter("@del_id", deliveryId),
                        new SqlParameter("@del_status", newStatus));

                    MessageBox.Show($"Delivery #{deliveryId} status changed to '{newStatus}'!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(txtOrderId.Text) ||
                        string.IsNullOrWhiteSpace(txtEmployeeId.Text) ||
                        !dpDeliveryDate.SelectedDate.HasValue)
                    {
                        MessageBox.Show("Please fill in all required fields (Order ID, Employee ID, Delivery Date).",
                            "Missing Fields", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int orderId = Convert.ToInt32(txtOrderId.Text);
                    int employeeId = Convert.ToInt32(txtEmployeeId.Text);
                    DateTime deliveryDate = dpDeliveryDate.SelectedDate.Value;

                    await ExecuteNonQueryAsync("add_delivery",
                        new SqlParameter("@o_id", orderId),
                        new SqlParameter("@emp_id", employeeId),
                        new SqlParameter("@del_date", deliveryDate));

                    MessageBox.Show(
                        $"Delivery assigned successfully!\nOrder ID: {orderId}\nEmployee ID: {employeeId}\nStatus: pending",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                await LoadAllDeliveriesAsync();
                ClearForm();
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter valid numeric values for Delivery ID, Order ID, and Employee ID.",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show(sqlEx.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeliveriesByEmployee_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Deliveries by Employee",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock prompt = new TextBlock
            {
                Text = "Enter Employee ID:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            TextBox inputBox = new TextBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 20)
            };

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Button okButton = new Button
            {
                Content = "View Deliveries",
                Width = 130,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Blue,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = System.Windows.Media.Brushes.LightGray,
                FontWeight = FontWeights.Bold
            };

            okButton.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(inputBox.Text))
                {
                    MessageBox.Show("Please enter an Employee ID.", "Input Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int employeeId = Convert.ToInt32(inputBox.Text);
                    inputDialog.Close();

                    HideAll();
                    txtPageTitle.Text = $"Deliveries by Employee #{employeeId}";
                    txtPageSubtitle.Text = "All deliveries assigned to this employee";

                    DataTable dt = await ExecuteProcedureAsync("view_deliveries_by_employee",
                        new SqlParameter("@emp_id", employeeId));

                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show(
                            $"ℹ️ NO DELIVERIES FOUND\n\n" +
                            $"Employee #{employeeId} has no delivery assignments.",
                            "No Deliveries",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }

                    dgDeliveries.ItemsSource = dt.DefaultView;
                    dgDeliveries.Visibility = Visibility.Visible;
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Employee ID.",
                                  "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (s, ev) => inputDialog.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(prompt);
            panel.Children.Add(inputBox);
            panel.Children.Add(buttonPanel);
            inputDialog.Content = panel;
            inputDialog.ShowDialog();
        }


        // ================= CLEAR FORM =================
        private void ClearForm_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void ClearForm()
        {
            txtDeliveryId.Text = isUpdateMode ? "" : "Auto-generated";
            txtOrderId.Text = "";
            txtEmployeeId.Text = "";
            dpDeliveryDate.SelectedDate = DateTime.Today;
            cmbDeliveryStatus.SelectedIndex = 0;
        }

        // ================= VIEW ALL DELIVERIES =================
        private async void AllDeliveries_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "All Deliveries";
            txtPageSubtitle.Text = "Complete list of all deliveries with employee information";
            await LoadAllDeliveriesAsync();
        }

        // ================= VIEW PENDING DELIVERIES =================
        private async void PendingDeliveries_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Pending Deliveries";
            txtPageSubtitle.Text = "Deliveries waiting to be completed";

            DataTable dt = await Task.Run(() =>
            {
                DataTable temp = new DataTable();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT d.del_id, d.o_id, e.emp_name, d.del_date, d.del_status " +
                        "FROM delivery d JOIN employee e ON d.emp_id = e.emp_id " +
                        "WHERE d.del_status='pending' ORDER BY d.del_date ASC", con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(temp);
                    }
                }

                return temp;
            });

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show("No pending deliveries.", "No Pending Deliveries",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            dgDeliveries.ItemsSource = dt.DefaultView;
            dgDeliveries.Visibility = Visibility.Visible;
        }

        // ================= EXIT =================
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            adminDashboard dashboard = new adminDashboard();
            dashboard.Show();
            Close();
        }

        // ================= DATABASE HELPERS =================
        private async Task ExecuteNonQueryAsync(string procedure, params SqlParameter[] parameters)
        {
            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(procedure, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddRange(parameters);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        private async Task<DataTable> ExecuteProcedureAsync(string procedure, params SqlParameter[] parameters)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(procedure, con))
                    {
                        da.SelectCommand.CommandType = CommandType.StoredProcedure;
                        da.SelectCommand.Parameters.AddRange(parameters);
                        da.Fill(dt);
                    }
                }

                return dt;
            });
        }

        private async Task LoadAllDeliveriesAsync()
        {
            DataTable dt = await ExecuteProcedureAsync("view_all_deliveries");
            dgDeliveries.ItemsSource = dt.DefaultView;
            dgDeliveries.Visibility = Visibility.Visible;
        }
    }
}
