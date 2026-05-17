using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace crochet_store
{
    public partial class ProductMgnt : Window
    {

        private readonly string connectionString = DBConnection.ConnectionString;


        public ProductMgnt()
        {
            InitializeComponent();
            AllProducts_Click(null, null); // load all products on startup
        }

        // ================= VIEW SWITCHING =================
        private void HideAll()
        {
            ProductForm.Visibility = Visibility.Collapsed;
            dgProducts.Visibility = Visibility.Collapsed;
        }

        private void ShowAddProduct(object sender, RoutedEventArgs e)
        {
            HideAll();
            ProductForm.Visibility = Visibility.Visible;
            btnSave.Content = "Add Product";

            txtPageTitle.Text = "Add New Product";
            txtPageSubtitle.Text = "Fill in the details below to add a new product to inventory";

            txtProductId.Text = "";
            txtProductId.IsReadOnly = true;
            txtProductId.Background = System.Windows.Media.Brushes.LightGray;

            txtName.Text = "";
            txtDesc.Text = "";
            txtCategory.Text = "";
            txtPrice.Text = "";
            txtStock.Text = "";
        }

        private void ShowUpdateProduct(object sender, RoutedEventArgs e)
        {
            HideAll();
            ProductForm.Visibility = Visibility.Visible;
            btnSave.Content = "Update Product";

            txtPageTitle.Text = "Update Product";
            txtPageSubtitle.Text = "Enter Product ID and modify the details you want to update";

            txtProductId.Text = "";
            txtProductId.IsReadOnly = false;
            txtProductId.Background = System.Windows.Media.Brushes.White;

            txtName.Text = "";
            txtDesc.Text = "";
            txtCategory.Text = "";
            txtPrice.Text = "";
            txtStock.Text = "";

            MessageBox.Show("Please enter the Product ID to update, then fill in the new values.",
                            "Update Product", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ================= BUTTON ACTIONS =================
        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (btnSave.Content.ToString() == "Add Product")
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text) ||
                        string.IsNullOrWhiteSpace(txtCategory.Text) ||
                        string.IsNullOrWhiteSpace(txtPrice.Text) ||
                        string.IsNullOrWhiteSpace(txtStock.Text))
                    {
                        MessageBox.Show("Please fill in all required fields.", "Validation Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await ExecuteNonQueryAsync("add_product",
                        new SqlParameter("@p_name", txtName.Text),
                        new SqlParameter("@p_description", txtDesc.Text),
                        new SqlParameter("@p_category", txtCategory.Text),
                        new SqlParameter("@p_price", decimal.Parse(txtPrice.Text)),
                        new SqlParameter("@p_stockqty", int.Parse(txtStock.Text))
                    );

                    MessageBox.Show("Product added successfully! ✓", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    AllProducts_Click(null, null);
                }
                else if (btnSave.Content.ToString() == "Update Product")
                {
                    if (string.IsNullOrWhiteSpace(txtProductId.Text))
                    {
                        MessageBox.Show("Please enter the Product ID to update.", "Validation Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(txtPrice.Text) ||
                        string.IsNullOrWhiteSpace(txtStock.Text))
                    {
                        MessageBox.Show("Please enter both Price and Stock Quantity.", "Validation Error",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int productId = Convert.ToInt32(txtProductId.Text);

                    await ExecuteNonQueryAsync("update_product",
                        new SqlParameter("@p_id", productId),
                        new SqlParameter("@p_price", decimal.Parse(txtPrice.Text)),
                        new SqlParameter("@p_stockqty", int.Parse(txtStock.Text))
                    );

                    MessageBox.Show("Product updated successfully! ✓", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);

                    AllProducts_Click(null, null);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter valid numeric values for Price, Stock, and Product ID.",
                              "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Database Error",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AllProducts_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "All Products";
            txtPageSubtitle.Text = "Complete list of products in inventory";
            await LoadGridAsync("view_all_products");
        }

        private async void LowStock_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Low Stock Alert";
            txtPageSubtitle.Text = "Products with stock quantity below 10 units";
            await LoadGridAsync("view_low_stock_products");
        }

        private async void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Remove Product",
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
                Text = "Enter the Product ID to remove:",
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
            inputBox.PreviewKeyDown += TextBox_PreviewKeyDown;

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
                Cursor = Cursors.Hand
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Background = System.Windows.Media.Brushes.LightGray,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            okButton.Click += async (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(inputBox.Text))
                {
                    MessageBox.Show("Please enter a Product ID.", "Input Required",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int productId = Convert.ToInt32(inputBox.Text);
                    inputDialog.DialogResult = true;
                    inputDialog.Close();

                    DataTable productData = await LoadProductByIdAsync(productId);
                    if (productData.Rows.Count == 0)
                    {
                        MessageBox.Show($"Product with ID {productId} not found.", "Not Found",
                                      MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    string productName = productData.Rows[0]["p_name"].ToString();

                    MessageBoxResult confirm = MessageBox.Show(
                        $"Are you sure you want to remove:\n\n" +
                        $"Product ID: {productId}\n" +
                        $"Product Name: {productName}\n\n" +
                        $"This action cannot be undone!",
                        "Confirm Deletion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (confirm == MessageBoxResult.Yes)
                    {
                        await ExecuteNonQueryAsync("remove_product",
                            new SqlParameter("@p_id", productId));

                        MessageBox.Show($"Product '{productName}' removed successfully! ✓", "Success",
                                      MessageBoxButton.OK, MessageBoxImage.Information);

                        await LoadGridAsync("view_all_products");
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Product ID.", "Input Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message, "Database Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (s, ev) =>
            {
                inputDialog.DialogResult = false;
                inputDialog.Close();
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            panel.Children.Add(prompt);
            panel.Children.Add(inputBox);
            panel.Children.Add(buttonPanel);
            inputDialog.Content = panel;
            inputDialog.ShowDialog();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            adminDashboard dashboard = new adminDashboard();
            dashboard.Show();
            this.Close();
        }

        // ================= DB HELPERS =================
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

        private async Task LoadGridAsync(string procedure, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlDataAdapter da = new SqlDataAdapter(procedure, con))
                    {
                        da.SelectCommand.CommandType = CommandType.StoredProcedure;
                        da.SelectCommand.Parameters.AddRange(parameters);
                        da.Fill(dt);
                    }
                }
            });

            dgProducts.ItemsSource = dt.DefaultView;
            dgProducts.Visibility = Visibility.Visible;
        }

        private async Task<DataTable> LoadProductByIdAsync(int productId)
        {
            DataTable dt = new DataTable();
            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM products WHERE p_id = @p_id", con))
                    {
                        cmd.Parameters.Add(new SqlParameter("@p_id", productId));
                        using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                        {
                            da.Fill(dt);
                        }
                    }
                }
            });
            return dt;
        }

        // ================= PREVENT ENTER KEY DEFAULT =================
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                e.Handled = true;
        }
    }
}
