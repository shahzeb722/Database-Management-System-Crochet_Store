using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace crochet_store
{
    public partial class SupMgnt : Window
    {

        private readonly string connectionString =DBConnection.ConnectionString;


        private bool isUpdateMode = false;

        public SupMgnt()
        {
            InitializeComponent();
            AllSuppliers_Click(null, null); // Load all suppliers on startup
        }

        // ================= VIEW SWITCHING =================
        private void HideAll()
        {
            SupplierForm.Visibility = Visibility.Collapsed;
            dgSuppliers.Visibility = Visibility.Collapsed;
        }

        private void ShowAddSupplier(object sender, RoutedEventArgs e)
        {
            HideAll();
            SupplierForm.Visibility = Visibility.Visible;
            isUpdateMode = false;

            txtPageTitle.Text = "Add New Supplier";
            txtPageSubtitle.Text = "Enter supplier details to add to the system";

            btnSave.Content = "Add Supplier";

            // Clear form
            ClearForm_Click(null, null);

            txtSupplierId.IsReadOnly = true;
            txtSupplierId.Background = System.Windows.Media.Brushes.LightGray;
            txtSupplierName.IsReadOnly = false;
        }

        private void ShowUpdateSupplier(object sender, RoutedEventArgs e)
        {
            HideAll();
            SupplierForm.Visibility = Visibility.Visible;
            isUpdateMode = true;

            txtPageTitle.Text = "Update Supplier";
            txtPageSubtitle.Text = "Enter Supplier ID and update the details";

            btnSave.Content = "Update Supplier";

            // Clear form
            ClearForm_Click(null, null);

            txtSupplierId.IsReadOnly = false;
            txtSupplierId.Background = System.Windows.Media.Brushes.White;
            txtSupplierName.IsReadOnly = true;
            txtSupplierName.Background = System.Windows.Media.Brushes.LightGray;

            MessageBox.Show("Enter the Supplier ID to update, then modify the contact, email, or address fields.\n\nNote: Supplier name cannot be updated.",
                          "Update Supplier",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        // ================= SAVE SUPPLIER =================
        private async void SaveSupplier_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isUpdateMode)
                {
                    // UPDATE MODE
                    if (string.IsNullOrWhiteSpace(txtSupplierId.Text))
                    {
                        MessageBox.Show("Please enter the Supplier ID to update.",
                                      "Validation Error",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        return;
                    }

                    int supplierId = Convert.ToInt32(txtSupplierId.Text);

                    // At least one field should be filled
                    if (string.IsNullOrWhiteSpace(txtContact.Text) &&
                        string.IsNullOrWhiteSpace(txtEmail.Text) &&
                        string.IsNullOrWhiteSpace(txtAddress.Text))
                    {
                        MessageBox.Show("Please enter at least one field to update (Contact, Email, or Address).",
                                      "Validation Error",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        return;
                    }

                    await ExecuteNonQueryAsync("sp_Update_Supplier",
                        new SqlParameter("@sup_id", supplierId),
                        new SqlParameter("@sup_contact", string.IsNullOrWhiteSpace(txtContact.Text) ? (object)DBNull.Value : txtContact.Text),
                        new SqlParameter("@sup_email", string.IsNullOrWhiteSpace(txtEmail.Text) ? (object)DBNull.Value : txtEmail.Text),
                        new SqlParameter("@sup_address", string.IsNullOrWhiteSpace(txtAddress.Text) ? (object)DBNull.Value : txtAddress.Text)
                    );

                    MessageBox.Show("Supplier updated successfully! ✓",
                                  "Success",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                else
                {
                    // ADD MODE
                    if (string.IsNullOrWhiteSpace(txtSupplierName.Text) ||
                        string.IsNullOrWhiteSpace(txtContact.Text) ||
                        string.IsNullOrWhiteSpace(txtEmail.Text) ||
                        string.IsNullOrWhiteSpace(txtAddress.Text) ||
                        cmbRegion.SelectedItem == null)
                    {
                        MessageBox.Show("Please fill in all required fields (Name, Contact, Email, Address, Region).",
                                      "Validation Error",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        return;
                    }

                    // Email validation
                    if (!txtEmail.Text.Contains("@") || !txtEmail.Text.Contains("."))
                    {
                        MessageBox.Show("Please enter a valid email address.",
                                      "Validation Error",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        return;
                    }

                    string region = (cmbRegion.SelectedItem as ComboBoxItem).Content.ToString();

                    await ExecuteNonQueryAsync("sp_Add_Supplier",
                        new SqlParameter("@sup_name", txtSupplierName.Text),
                        new SqlParameter("@sup_contact", txtContact.Text),
                        new SqlParameter("@sup_email", txtEmail.Text),
                        new SqlParameter("@sup_address", txtAddress.Text),
                        new SqlParameter("@sup_region", region)
                    );

                    MessageBox.Show("Supplier added successfully! ✓",
                                  "Success",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }

                // Refresh the grid
                await LoadAllSuppliersAsync();
                ClearForm_Click(null, null);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter a valid numeric Supplier ID.",
                              "Input Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            catch (SqlException sqlEx)
            {
                if (sqlEx.Message.Contains("sup_email"))
                {
                    MessageBox.Show("Invalid email format. Please use a valid email address (e.g., example@domain.com).",
                                  "Email Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show("Database Error: " + sqlEx.Message,
                                  "Database Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        // ================= CLEAR FORM =================
        private async void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            txtSupplierId.Text = "";
            txtSupplierName.Text = "";
            txtContact.Text = "";
            txtEmail.Text = "";
            txtAddress.Text = "";
            cmbRegion.SelectedIndex = -1;
        }

        // ================= DELETE SUPPLIER =================
        private void DeleteSupplier_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Delete Supplier",
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
                Text = "Enter the Supplier ID to delete:",
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
                Content = "Delete",
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
                    MessageBox.Show("Please enter a Supplier ID.",
                                  "Input Required",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int supplierId = Convert.ToInt32(inputBox.Text);
                    inputDialog.Close();

                    // Get supplier details first
                    DataTable supplierData = await GetSupplierByIdAsync(supplierId);

                    if (supplierData.Rows.Count == 0)
                    {
                        MessageBox.Show($"Supplier with ID {supplierId} not found.",
                                      "Not Found",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                        return;
                    }

                    string supplierName = supplierData.Rows[0]["sup_name"].ToString();

                    MessageBoxResult confirm = MessageBox.Show(
                        $"Are you sure you want to delete:\n\n" +
                        $"Supplier ID: {supplierId}\n" +
                        $"Supplier Name: {supplierName}\n\n" +
                        $"This action cannot be undone!",
                        "Confirm Deletion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (confirm == MessageBoxResult.Yes)
                    {
                        await ExecuteNonQueryAsync("sp_Delete_Supplier",
                            new SqlParameter("@sup_id", supplierId));

                        MessageBox.Show($"Supplier '{supplierName}' deleted successfully! ✓",
                                      "Success",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);

                        await LoadAllSuppliersAsync();
                    }
                    else
                    {
                        MessageBox.Show("Deletion cancelled.",
                                      "Cancelled",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Supplier ID.",
                                  "Input Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message,
                                  "Database Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
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

        // ================= VIEW ALL SUPPLIERS =================
        private async void AllSuppliers_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "All Suppliers";
            txtPageSubtitle.Text = "Complete list of suppliers in the system";
            await LoadAllSuppliersAsync();
        }

        // ================= VIEW SUPPLIERS BY REGION =================
        private async void SuppliersByRegion_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Filter by Region",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = System.Windows.Media.Brushes.White
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(20) };

            TextBlock prompt = new TextBlock
            {
                Text = "Select region:",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 10)
            };

            ComboBox regionCombo = new ComboBox
            {
                FontSize = 14,
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 20)
            };
            regionCombo.Items.Add("Punjab");
            regionCombo.Items.Add("Sindh");
            regionCombo.Items.Add("Khyber Pakhtunkhwa");
            regionCombo.Items.Add("Balochistan");
            regionCombo.Items.Add("Islamabad");
            regionCombo.Items.Add("Gilgit-Baltistan");
            regionCombo.Items.Add("Azad Kashmir");

            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Button okButton = new Button
            {
                Content = "Search",
                Width = 100,
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
                if (regionCombo.SelectedItem == null)
                {
                    MessageBox.Show("Please select a region.",
                                  "Input Required",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                inputDialog.Close();

                string region = regionCombo.SelectedItem.ToString();

                HideAll();
                txtPageTitle.Text = $"Suppliers in {region}";
                txtPageSubtitle.Text = "Filtered supplier list";

                await LoadSuppliersByRegionAsync(region);
            };

            cancelButton.Click += (s, ev) => inputDialog.Close();

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(prompt);
            panel.Children.Add(regionCombo);
            panel.Children.Add(buttonPanel);
            inputDialog.Content = panel;
            inputDialog.ShowDialog();
        }

        // ================= VIEW SHIPMENT HISTORY =================
        private async void ShipmentHistory_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Supplier Shipment History";
            txtPageSubtitle.Text = "Complete shipment history from all suppliers";
            await LoadShipmentHistoryAsync();
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

        private async Task LoadAllSuppliersAsync()
        {
            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Supplier ORDER BY sup_name", con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            });

            dgSuppliers.ItemsSource = dt.DefaultView;
            dgSuppliers.Visibility = Visibility.Visible;
        }

        private async Task LoadSuppliersByRegionAsync(string region)
        {
            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT * FROM Supplier WHERE sup_region = @region ORDER BY sup_name", con))
                    {
                        cmd.Parameters.AddWithValue("@region", region);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            });

            dgSuppliers.ItemsSource = dt.DefaultView;
            dgSuppliers.Visibility = Visibility.Visible;
        }

        private async Task LoadShipmentHistoryAsync()
        {
            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM vw_SupplierShipmentHistory", con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            });

            dgSuppliers.ItemsSource = dt.DefaultView;
            dgSuppliers.Visibility = Visibility.Visible;
        }

        private async Task<DataTable> GetSupplierByIdAsync(int supplierId)
        {
            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT * FROM Supplier WHERE sup_id = @sup_id", con))
                    {
                        cmd.Parameters.AddWithValue("@sup_id", supplierId);
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            });

            return dt;
        }
    }
}