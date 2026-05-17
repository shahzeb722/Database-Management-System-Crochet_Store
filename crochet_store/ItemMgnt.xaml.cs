using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;

namespace crochet_store
{
    public partial class ItemMgnt : Window
    {

        private readonly string connectionString = DBConnection.ConnectionString;


        private bool isUpdateMode = false;

        public ItemMgnt()
        {
            InitializeComponent();
            AllItems_Click(null, null); // Load all items on startup
        }

        // ================= VIEW SWITCHING =================
        private async void HideAll()
        {
            ItemForm.Visibility = Visibility.Collapsed;
            dgItems.Visibility = Visibility.Collapsed;
        }

        private async void ShowAddItem(object sender, RoutedEventArgs e)
        {
            HideAll();
            ItemForm.Visibility = Visibility.Visible;
            isUpdateMode = false;

            txtPageTitle.Text = "Add New Item";
            txtPageSubtitle.Text = "Add a new raw material or supply to inventory";

            btnSave.Content = "Add Item";
            infoPanel.Visibility = Visibility.Collapsed;

            // Clear form
            ClearForm_Click(null, null);

            // Configure fields for Add mode
            txtItemId.IsReadOnly = true;
            txtItemId.Background = System.Windows.Media.Brushes.LightGray;
            txtItemName.IsReadOnly = false;
            txtItemName.Background = System.Windows.Media.Brushes.White;
            txtDescription.IsReadOnly = false;
            txtDescription.Background = System.Windows.Media.Brushes.White;
            txtPrice.IsReadOnly = false;
            txtPrice.Background = System.Windows.Media.Brushes.White;
            txtStockQty.IsReadOnly = true;
            txtStockQty.Background = System.Windows.Media.Brushes.LightGray;
            txtStockQty.Text = "0 (Auto-set)";
        }

        private void ShowUpdateItem(object sender, RoutedEventArgs e)
        {
            HideAll();
            ItemForm.Visibility = Visibility.Visible;
            isUpdateMode = true;

            txtPageTitle.Text = "Update Item";
            txtPageSubtitle.Text = "Modify item description and standard price";

            btnSave.Content = "Update Item";
            infoPanel.Visibility = Visibility.Visible;

            // Clear form
            ClearForm_Click(null, null);

            // Configure fields for Update mode
            txtItemId.IsReadOnly = false;
            txtItemId.Background = System.Windows.Media.Brushes.White;
            txtItemName.IsReadOnly = true;
            txtItemName.Background = System.Windows.Media.Brushes.LightGray;
            txtDescription.IsReadOnly = false;
            txtDescription.Background = System.Windows.Media.Brushes.White;
            txtPrice.IsReadOnly = false;
            txtPrice.Background = System.Windows.Media.Brushes.White;
            txtStockQty.IsReadOnly = true;
            txtStockQty.Background = System.Windows.Media.Brushes.LightGray;

            // Show important notice
            MessageBox.Show(
                "UPDATE ITEM RESTRICTIONS:\n\n" +
                "✓ You can update: Description and Standard Price\n\n" +
                "✗ You CANNOT update: Item Name and Stock Quantity\n\n" +
                "Stock quantity is automatically updated when shipments arrive.\n\n" +
                "Enter the Item ID to load and update item details.",
                "Update Item Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // ================= SAVE ITEM =================
        private async void SaveItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isUpdateMode)
                {
                    // ========== UPDATE MODE ==========
                    if (string.IsNullOrWhiteSpace(txtItemId.Text))
                    {
                        MessageBox.Show(
                            "Please enter the Item ID to update.",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    int itemId = Convert.ToInt32(txtItemId.Text);

                    // Check if at least one field is filled
                    if (string.IsNullOrWhiteSpace(txtDescription.Text) &&
                        string.IsNullOrWhiteSpace(txtPrice.Text))
                    {
                        MessageBox.Show(
                            "Please enter at least one field to update:\n\n" +
                            "• Description\n" +
                            "• Standard Price",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    // Validate price if provided
                    decimal? price = null;
                    if (!string.IsNullOrWhiteSpace(txtPrice.Text))
                    {
                        price = decimal.Parse(txtPrice.Text);

                        if (price < 0)
                        {
                            MessageBox.Show(
                                "❌ VALIDATION ERROR\n\n" +
                                "Standard unit price cannot be negative.\n\n" +
                                "Please enter a valid positive price.",
                                "Invalid Price",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );
                            return;
                        }
                    }

                    // Execute update
                    await ExecuteNonQueryAsync("sp_Update_Item",
                        new SqlParameter("@item_id", itemId),
                        new SqlParameter("@item_description",
                            string.IsNullOrWhiteSpace(txtDescription.Text) ? (object)DBNull.Value : txtDescription.Text),
                        new SqlParameter("@standard_price",
                            price.HasValue ? (object)price.Value : DBNull.Value)
                    );

                    MessageBox.Show(
                        "✓ SUCCESS\n\n" +
                        "Item updated successfully!\n\n" +
                        "Changes have been saved to the database.",
                        "Update Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                else
                {
                    // ========== ADD MODE ==========
                    if (string.IsNullOrWhiteSpace(txtItemName.Text) ||
                        string.IsNullOrWhiteSpace(txtDescription.Text) ||
                        string.IsNullOrWhiteSpace(txtPrice.Text))
                    {
                        MessageBox.Show(
                            "Please fill in all required fields:\n\n" +
                            "• Item Name\n" +
                            "• Description\n" +
                            "• Standard Unit Price",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    decimal price = decimal.Parse(txtPrice.Text);

                    // Validate price
                    if (price < 0)
                    {
                        MessageBox.Show(
                            "❌ VALIDATION ERROR\n\n" +
                            "Standard unit price cannot be negative.\n\n" +
                            "Please enter a valid positive price.",
                            "Invalid Price",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }

                    // Execute add
                    await ExecuteNonQueryAsync("sp_Add_Item",
                        new SqlParameter("@item_name", txtItemName.Text),
                        new SqlParameter("@item_description", txtDescription.Text),
                        new SqlParameter("@standard_price", price)
                    );

                    MessageBox.Show(
                        "✓ SUCCESS\n\n" +
                        "Item added successfully!\n\n" +
                        "Initial stock quantity is set to 0.\n" +
                        "Stock will be updated when shipments arrive.",
                        "Add Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                // Refresh the grid and clear form
                await LoadAllItemsAsync();
                ClearForm_Click(null, null);
            }
            catch (FormatException)
            {
                MessageBox.Show(
                    "❌ INPUT ERROR\n\n" +
                    "Please enter valid values:\n\n" +
                    "• Item ID must be a number\n" +
                    "• Price must be a valid decimal number",
                    "Invalid Input",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            catch (SqlException sqlEx)
            {
                // Handle specific SQL errors
                if (sqlEx.Message.Contains("standard_unit_price") && sqlEx.Message.Contains("CHECK"))
                {
                    MessageBox.Show(
                        "❌ DATABASE CONSTRAINT VIOLATION\n\n" +
                        "Price must be greater than or equal to 0.\n\n" +
                        "Database check constraint prevents negative prices.",
                        "Price Validation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                else if (sqlEx.Message.Contains("Item not found"))
                {
                    MessageBox.Show(
                        "❌ ITEM NOT FOUND\n\n" +
                        $"No item exists with ID: {txtItemId.Text}\n\n" +
                        "Please verify the Item ID and try again.",
                        "Item Not Found",
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
                    "❌ UNEXPECTED ERROR\n\n" +
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
            txtItemId.Text = "";
            txtItemName.Text = "";
            txtDescription.Text = "";
            txtPrice.Text = "";
            txtStockQty.Text = isUpdateMode ? "Cannot be updated manually" : "0 (Auto-set)";
        }

        // ================= VIEW ALL ITEMS =================
        private async void AllItems_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "All Items";
            txtPageSubtitle.Text = "Complete inventory of raw materials and supplies";
            await LoadAllItemsAsync();
        }

        // ================= LOW STOCK ALERT =================
        private async void LowStockAlert_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Low Stock Alert";
            txtPageSubtitle.Text = "Items with stock quantity below 10 units";
            await LoadLowStockAsync();
        }

        // ================= ITEMS BY PRICE =================
        private async void ItemsByPrice_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Items Sorted by Price";
            txtPageSubtitle.Text = "All items ordered by standard unit price";
            await LoadItemsByPriceAsync();
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

        private async Task LoadAllItemsAsync()
        {
            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT item_id, item_name, item_description, standard_unit_price, stock_quantity FROM Item ORDER BY item_name",
                        con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            });

            dgItems.ItemsSource = dt.DefaultView;
            dgItems.Visibility = Visibility.Visible;
        }

        private async Task LoadLowStockAsync()
        {
            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM vw_LowStockAlert", con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            });

            dgItems.ItemsSource = dt.DefaultView;
            dgItems.Visibility = Visibility.Visible;

            if (dt.Rows.Count == 0)
            {
                MessageBox.Show(
                    "✓ ALL CLEAR\n\n" +
                    "No items are currently low on stock.\n\n" +
                    "All items have sufficient inventory (10+ units).",
                    "Stock Status",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            else
            {
                MessageBox.Show(
                    $"⚠️ LOW STOCK WARNING\n\n" +
                    $"{dt.Rows.Count} item(s) are running low on stock.\n\n" +
                    "Please consider ordering new shipments for these items.",
                    "Stock Alert",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private async Task LoadItemsByPriceAsync()
        {
            DataTable dt = new DataTable();

            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(
                        "SELECT item_id, item_name, item_description, standard_unit_price, stock_quantity " +
                        "FROM Item ORDER BY standard_unit_price DESC",
                        con))
                    {
                        SqlDataAdapter da = new SqlDataAdapter(cmd);
                        da.Fill(dt);
                    }
                }
            });

            dgItems.ItemsSource = dt.DefaultView;
            dgItems.Visibility = Visibility.Visible;
        }
    }
}