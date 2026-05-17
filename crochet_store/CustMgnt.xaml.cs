using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace crochet_store
{
    public partial class CustMgnt : Window
    {

        private readonly string connectionString = DBConnection.ConnectionString;


        private bool isUpdateMode = false;

        public CustMgnt()
        {
            InitializeComponent();
            AllCustomers_Click(null, null); // Load all customers on startup
        }

        // ================= VIEW SWITCHING =================
        private void HideAll()
        {
            CustomerForm.Visibility = Visibility.Collapsed;
            dgCustomers.Visibility = Visibility.Collapsed;
        }

        private void ShowAddCustomer(object sender, RoutedEventArgs e)
        {
            HideAll();
            CustomerForm.Visibility = Visibility.Visible;
            isUpdateMode = false;

            txtPageTitle.Text = "Add New Customer";
            txtPageSubtitle.Text = "Register a new customer in the system";

            btnSave.Content = "Add Customer";

            ClearForm_Click(null, null);

            txtCustomerId.IsReadOnly = true;
            txtCustomerId.Background = System.Windows.Media.Brushes.LightGray;
            txtCustomerName.IsReadOnly = false;
            txtCustomerName.Background = System.Windows.Media.Brushes.White;
        }

        private void ShowUpdateCustomer(object sender, RoutedEventArgs e)
        {
            HideAll();
            CustomerForm.Visibility = Visibility.Visible;
            isUpdateMode = true;

            txtPageTitle.Text = "Update Customer";
            txtPageSubtitle.Text = "Update customer contact details";

            btnSave.Content = "Update Customer";

            ClearForm_Click(null, null);

            txtCustomerId.IsReadOnly = false;
            txtCustomerId.Background = System.Windows.Media.Brushes.White;
            txtCustomerName.IsReadOnly = true;
            txtCustomerName.Background = System.Windows.Media.Brushes.LightGray;

            MessageBox.Show(
                "UPDATE RESTRICTIONS:\n\n" +
                "✓ You can update: Contact, Email, and Address\n" +
                "✗ You CANNOT update: Customer Name\n\n" +
                "Enter the Customer ID to update their information.",
                "Update Customer Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // ================= SAVE CUSTOMER =================
        private async void SaveCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isUpdateMode)
                {
                    // ========== UPDATE MODE ==========
                    if (string.IsNullOrWhiteSpace(txtCustomerId.Text))
                    {
                        MessageBox.Show(
                            "❌ VALIDATION ERROR\n\n" +
                            "Please enter the Customer ID to update.",
                            "Missing Customer ID",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    int customerId = Convert.ToInt32(txtCustomerId.Text);

                    // At least one field should be filled
                    if (string.IsNullOrWhiteSpace(txtContact.Text) &&
                        string.IsNullOrWhiteSpace(txtEmail.Text) &&
                        string.IsNullOrWhiteSpace(txtAddress.Text))
                    {
                        MessageBox.Show(
                            "❌ VALIDATION ERROR\n\n" +
                            "Please enter at least one field to update:\n" +
                            "• Contact Number\n" +
                            "• Email Address\n" +
                            "• Address",
                            "No Fields to Update",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    await ExecuteNonQueryAsync("update_customer",
                        new SqlParameter("@c_id", customerId),
                        new SqlParameter("@c_contact", string.IsNullOrWhiteSpace(txtContact.Text) ? (object)DBNull.Value : txtContact.Text),
                        new SqlParameter("@c_email", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text),
                        new SqlParameter("@c_address", string.IsNullOrWhiteSpace(txtAddress.Text) ? (object)DBNull.Value : txtAddress.Text)
                    );

                    MessageBox.Show(
                        "✓ UPDATE SUCCESSFUL\n\n" +
                        "Customer information updated successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    // ========== ADD MODE ==========
                    if (string.IsNullOrWhiteSpace(txtCustomerName.Text) ||
                        string.IsNullOrWhiteSpace(txtContact.Text) ||
                        string.IsNullOrWhiteSpace(txtEmail.Text) ||
                        string.IsNullOrWhiteSpace(txtAddress.Text))
                    {
                        MessageBox.Show(
                            "❌ VALIDATION ERROR\n\n" +
                            "Please fill in all required fields:\n" +
                            "• Customer Name\n" +
                            "• Contact Number\n" +
                            "• Email Address\n" +
                            "• Address",
                            "Missing Required Fields",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    await ExecuteNonQueryAsync("add_customer",
                        new SqlParameter("@c_name", txtCustomerName.Text),
                        new SqlParameter("@c_contact", txtContact.Text),
                        new SqlParameter("@c_email", txtEmail.Text),
                        new SqlParameter("@c_address", txtAddress.Text)
                    );

                    MessageBox.Show(
                        "✓ CUSTOMER ADDED\n\n" +
                        $"Customer '{txtCustomerName.Text}' added successfully!",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                await LoadAllCustomersAsync();
                ClearForm_Click(null, null);
            }
            catch (FormatException)
            {
                MessageBox.Show(
                    "❌ INPUT ERROR\n\n" +
                    "Customer ID must be a valid number.",
                    "Invalid Input",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Message.Contains("invalid contact"))
                {
                    MessageBox.Show(
                        "❌ CONTACT VALIDATION FAILED\n\n" +
                        "Contact number must be exactly 11 digits.\n\n" +
                        "Example: 03001234567\n\n" +
                        "This is enforced by the database stored procedure.",
                        "Invalid Contact Number",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                else if (sqlEx.Message.Contains("invalid email"))
                {
                    MessageBox.Show(
                        "❌ EMAIL VALIDATION FAILED\n\n" +
                        "Email must end with @gmail.com\n\n" +
                        "Example: customer@gmail.com\n\n" +
                        "This is enforced by the database stored procedure.",
                        "Invalid Email Address",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                else if (sqlEx.Message.Contains("customer not found"))
                {
                    MessageBox.Show(
                        "❌ CUSTOMER NOT FOUND\n\n" +
                        $"No customer exists with ID: {txtCustomerId.Text}\n\n" +
                        "Please verify the Customer ID and try again.",
                        "Customer Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                else
                {
                    MessageBox.Show(
                        "❌ DATABASE ERROR\n\n" +
                        sqlEx.Message,
                        "Database Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "❌ ERROR\n\n" +
                    ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        // ================= CLEAR FORM =================
        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            txtCustomerId.Text = isUpdateMode ? "" : "Auto-generated";
            txtCustomerName.Text = "";
            txtContact.Text = "";
            txtEmail.Text = "";
            txtAddress.Text = "";
        }

        // ================= REMOVE CUSTOMER =================
        private async void RemoveCustomer_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Remove Customer",
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
                Text = "Enter the Customer ID to remove:",
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
                Content = "Remove",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Red,
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = System.Windows.Media.Brushes.LightGray,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            okButton.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(inputBox.Text))
                {
                    MessageBox.Show("Please enter a Customer ID.", "Input Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int customerId = Convert.ToInt32(inputBox.Text);
                    inputDialog.Close();

                    DataTable customerData = await GetCustomerByIdAsync(customerId);

                    if (customerData.Rows.Count == 0)
                    {
                        MessageBox.Show($"Customer with ID {customerId} not found.",
                                      "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string customerName = customerData.Rows[0]["c_name"].ToString();

                    MessageBoxResult confirm = MessageBox.Show(
                        $"Are you sure you want to remove:\n\n" +
                        $"Customer ID: {customerId}\n" +
                        $"Customer Name: {customerName}\n\n" +
                        "This action cannot be undone!",
                        "Confirm Deletion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (confirm == MessageBoxResult.Yes)
                    {
                        await ExecuteNonQueryAsync("delete_customer",
                            new SqlParameter("@c_id", customerId));

                        MessageBox.Show($"✓ Customer '{customerName}' removed successfully!",
                                      "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadAllCustomersAsync();
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Customer ID.",
                                  "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message,
                                  "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // ================= VIEW ALL CUSTOMERS =================
        private async void AllCustomers_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "All Customers";
            txtPageSubtitle.Text = "Complete list of registered customers";
            await LoadAllCustomersAsync();
        }

        // ================= VIEW TOP CUSTOMERS =================
        private async void TopCustomers_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Top Customers";
            txtPageSubtitle.Text = "Customers ranked by total spending and order count";

            try
            {
                DataTable dt = await ExecuteProcedureAsync("view_top_customers");

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show(
                        "ℹ️ NO DATA AVAILABLE\n\n" +
                        "No customer orders found in the system.\n\n" +
                        "Top customers are ranked by their total spending.",
                        "No Orders",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    MessageBox.Show(
                        $"📊 TOP CUSTOMERS REPORT\n\n" +
                        $"Showing {dt.Rows.Count} customers ranked by total spending.\n\n" +
                        "Columns shown:\n" +
                        "• Customer ID\n" +
                        "• Customer Name\n" +
                        "• Total Orders\n" +
                        "• Total Spent",
                        "Report Information",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                dgCustomers.ItemsSource = dt.DefaultView;
                dgCustomers.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading top customers: " + ex.Message,
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ================= VIEW CUSTOMER SUMMARY =================
        private void CustomerSummary_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Customer Summary",
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
                Text = "Enter Customer ID:",
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
                Content = "View Summary",
                Width = 120,
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
                    MessageBox.Show("Please enter a Customer ID.", "Input Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int customerId = Convert.ToInt32(inputBox.Text);
                    inputDialog.Close();

                    HideAll();
                    txtPageTitle.Text = $"Customer Summary - ID: {customerId}";
                    txtPageSubtitle.Text = "Complete customer profile with order statistics";

                    DataTable dt = await ExecuteProcedureAsync("view_customer_summary",
                        new SqlParameter("@c_id", customerId));

                    if (dt.Rows.Count > 0)
                    {
                        string custName = dt.Rows[0]["c_name"].ToString();
                        string totalOrders = dt.Rows[0]["total_orders"].ToString();
                        string totalSpent = dt.Rows[0]["total_spent"].ToString();

                        MessageBox.Show(
                            $"📊 CUSTOMER SUMMARY\n\n" +
                            $"Name: {custName}\n" +
                            $"Total Orders: {totalOrders}\n" +
                            $"Total Spent: PKR {totalSpent}",
                            "Customer Profile",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }

                    dgCustomers.ItemsSource = dt.DefaultView;
                    dgCustomers.Visibility = Visibility.Visible;
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Customer ID.",
                                  "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Message.Contains("Customer not found"))
                    {
                        MessageBox.Show($"Customer with ID {inputBox.Text} not found.",
                                      "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Database Error: " + sqlEx.Message,
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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

        // ================= VIEW ORDER HISTORY =================
        private async void OrderHistory_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Customer Order History",
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
                Text = "Enter Customer ID:",
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
                Content = "View History",
                Width = 120,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = System.Windows.Media.Brushes.Green,
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
                    MessageBox.Show("Please enter a Customer ID.", "Input Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int customerId = Convert.ToInt32(inputBox.Text);
                    inputDialog.Close();

                    HideAll();
                    txtPageTitle.Text = $"Order History - Customer ID: {customerId}";
                    txtPageSubtitle.Text = "All orders placed by this customer";

                    DataTable dt = await ExecuteProcedureAsync("view_customer_order_history",
                        new SqlParameter("@c_id", customerId));

                    if (dt.Rows.Count == 0)
                    {
                        MessageBox.Show(
                            $"ℹ️ NO ORDERS FOUND\n\n" +
                            $"Customer ID {customerId} has not placed any orders yet.",
                            "No Order History",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            $"📜 ORDER HISTORY\n\n" +
                            $"Found {dt.Rows.Count} orders for Customer ID {customerId}\n\n" +
                            "Showing:\n" +
                            "• Order ID\n" +
                            "• Order Date\n" +
                            "• Total Amount\n" +
                            "• Order Status",
                            "History Loaded",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }

                    dgCustomers.ItemsSource = dt.DefaultView;
                    dgCustomers.Visibility = Visibility.Visible;
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Customer ID.",
                                  "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (SqlException sqlEx)
                {
                    if (sqlEx.Message.Contains("Customer not found"))
                    {
                        MessageBox.Show($"Customer with ID {inputBox.Text} not found.",
                                      "Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show("Database Error: " + sqlEx.Message,
                                      "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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

        // ================= EXIT TO DASHBOARD =================
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            adminDashboard dashboard = new adminDashboard();
            dashboard.Show();
            this.Close();
        }

        // ================= DATABASE HELPERS =================
        private async Task ExecuteNonQueryAsync(string procedure, params SqlParameter[] parameters)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(procedure, con))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddRange(parameters);

                await con.OpenAsync();
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<DataTable> ExecuteProcedureAsync(string procedure, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(procedure, con))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddRange(parameters);

                await con.OpenAsync();
                da.Fill(dt);
            }

            return dt;
        }

        private async Task LoadAllCustomersAsync()
        {
            DataTable dt = await ExecuteProcedureAsync("view_all_customers");
            dgCustomers.ItemsSource = dt.DefaultView;
            dgCustomers.Visibility = Visibility.Visible;
        }

        private async Task<DataTable> GetCustomerByIdAsync(int customerId)
        {
            DataTable dt = new DataTable();

            using (SqlConnection con = new SqlConnection(connectionString))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT * FROM customer WHERE c_id = @c_id", con))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.Add("@c_id", SqlDbType.Int).Value = customerId;
                await con.OpenAsync();
                da.Fill(dt);
            }

            return dt;
        }
    }
}