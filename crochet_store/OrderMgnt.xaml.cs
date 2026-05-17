using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace crochet_store
{
    public partial class OrderMgnt : Window
    {

        private readonly string connectionString = DBConnection.ConnectionString;


        // List to store products for the order
        private List<OrderProduct> orderProducts = new List<OrderProduct>();

        public OrderMgnt()
        {
            InitializeComponent();
            dpOrderDate.SelectedDate = DateTime.Today;
            AllOrders_Click(null, null); // Load all orders on startup
        }

        // Helper class to store product information
        private class OrderProduct
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }

            public override string ToString()
            {
                return $"Product ID: {ProductId}, Quantity: {Quantity}";
            }
        }

        // ================= VIEW SWITCHING =================
        private void HideAll()
        {
            OrderForm.Visibility = Visibility.Collapsed;
            dgOrders.Visibility = Visibility.Collapsed;
        }

        private void ShowAddOrder(object sender, RoutedEventArgs e)
        {
            HideAll();
            OrderForm.Visibility = Visibility.Visible;

            txtPageTitle.Text = "Add New Order";
            txtPageSubtitle.Text = "Create a new order by entering customer and product details";

            // Clear form
            txtCustomerId.Text = "";
            dpOrderDate.SelectedDate = DateTime.Today;
            txtProductId.Text = "";
            txtQuantity.Text = "";
            orderProducts.Clear();
            lstProducts.ItemsSource = null;
        }

        // ================= ADD PRODUCT TO ORDER =================
        private void AddProductToList(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtProductId.Text) ||
                    string.IsNullOrWhiteSpace(txtQuantity.Text))
                {
                    MessageBox.Show("Please enter both Product ID and Quantity.",
                                  "Input Required",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                int productId = Convert.ToInt32(txtProductId.Text);
                int quantity = Convert.ToInt32(txtQuantity.Text);

                if (quantity <= 0)
                {
                    MessageBox.Show("Quantity must be greater than 0.",
                                  "Invalid Quantity",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                // Add to list
                orderProducts.Add(new OrderProduct
                {
                    ProductId = productId,
                    Quantity = quantity
                });

                // Update display
                lstProducts.ItemsSource = null;
                lstProducts.ItemsSource = orderProducts;

                // Clear input fields
                txtProductId.Text = "";
                txtQuantity.Text = "";
                txtProductId.Focus();

                MessageBox.Show($"Product added to order! Total products: {orderProducts.Count}",
                              "Success",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter valid numeric values.",
                              "Input Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message,
                              "Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        // ================= PLACE ORDER =================
        private async void PlaceOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validation
                if (string.IsNullOrWhiteSpace(txtCustomerId.Text))
                {
                    MessageBox.Show("Please enter Customer ID.",
                                  "Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (!dpOrderDate.SelectedDate.HasValue)
                {
                    MessageBox.Show("Please select an order date.",
                                  "Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                if (orderProducts.Count == 0)
                {
                    MessageBox.Show("Please add at least one product to the order.",
                                  "Validation Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                int customerId = Convert.ToInt32(txtCustomerId.Text);
                DateTime orderDate = dpOrderDate.SelectedDate.Value;

                // Create the order
                await AddOrderAsync(customerId, orderDate, orderProducts);

                MessageBox.Show("Order placed successfully! ✓",
                              "Success",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);

                // Refresh the order list
                await LoadGridAsync("view_all_orders");

                // Clear form
                ClearForm_Click(null, null);
            }
            catch (FormatException)
            {
                MessageBox.Show("Please enter valid numeric values for Customer ID.",
                              "Input Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error placing order: " + ex.Message,
                              "Database Error",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
        }

        // ================= CLEAR FORM =================
        private void ClearForm_Click(object sender, RoutedEventArgs e)
        {
            txtCustomerId.Text = "";
            dpOrderDate.SelectedDate = DateTime.Today;
            txtProductId.Text = "";
            txtQuantity.Text = "";
            orderProducts.Clear();
            lstProducts.ItemsSource = null;
        }

        // ================= REMOVE ORDER =================
        private void RemoveOrder_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Remove Order",
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
                Text = "Enter the Order ID to remove:",
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
                    MessageBox.Show("Please enter an Order ID.",
                                  "Input Required",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int orderId = Convert.ToInt32(inputBox.Text);
                    inputDialog.Close();

                    MessageBoxResult confirm = MessageBox.Show(
                        $"Are you sure you want to remove Order #{orderId}?\n\n" +
                        $"This will also remove associated deliveries.\n" +
                        $"This action cannot be undone!",
                        "Confirm Deletion",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (confirm == MessageBoxResult.Yes)
                    {
                        await ExecuteNonQueryAsync("remove_order",
                            new SqlParameter("@odr_id", orderId));

                        MessageBox.Show($"Order #{orderId} removed successfully! ✓",
                                      "Success",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);

                        await LoadGridAsync("view_all_orders");
                    }
                    else
                    {
                        MessageBox.Show("Order removal cancelled.",
                                      "Cancelled",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Information);
                    }
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Order ID.",
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

        // ================= VIEW ALL ORDERS =================
        private async void AllOrders_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "All Orders";
            txtPageSubtitle.Text = "Complete list of all orders in the system";
            await LoadGridAsync("view_all_orders");
        }

        // ================= VIEW PENDING ORDERS =================
        private async void PendingOrders_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Pending Orders";
            txtPageSubtitle.Text = "Orders waiting for processing";
            await LoadGridAsync("view_pending_orders");
        }

        // ================= VIEW ORDERS BY STATUS =================
        private async void OrdersByStatus_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Filter by Status",
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
                Text = "Enter order status (pending/shipped/completed):",
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
                if (string.IsNullOrWhiteSpace(inputBox.Text))
                {
                    MessageBox.Show("Please enter a status.",
                                  "Input Required",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                inputDialog.Close();

                HideAll();
                txtPageTitle.Text = $"Orders with Status: '{inputBox.Text}'";
                txtPageSubtitle.Text = "Filtered order list";

                await LoadGridAsync("view_orders_by_status",
                    new SqlParameter("@status", inputBox.Text));
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

        // ================= VIEW ORDER DETAILS =================
        private void OrderDetails_Click(object sender, RoutedEventArgs e)
        {
            Window inputDialog = new Window
            {
                Title = "Order Details",
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
                Text = "Enter Order ID:",
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
                Content = "View Details",
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
                    MessageBox.Show("Please enter an Order ID.",
                                  "Input Required",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    int orderId = Convert.ToInt32(inputBox.Text);
                    inputDialog.Close();

                    HideAll();
                    txtPageTitle.Text = $"Order Details - Order #{orderId}";
                    txtPageSubtitle.Text = "Complete order information";

                    await LoadGridAsync("view_order_details",
                        new SqlParameter("@odr_id", orderId));
                }
                catch (FormatException)
                {
                    MessageBox.Show("Please enter a valid numeric Order ID.",
                                  "Input Error",
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

        // ================= VIEW MONTHLY REVENUE =================
        private async void MonthlyRevenue_Click(object sender, RoutedEventArgs e)
        {
            HideAll();
            txtPageTitle.Text = "Monthly Revenue Report";
            txtPageSubtitle.Text = "Revenue breakdown by month";
            await LoadGridAsync("view_monthly_revenue");
        }

        // ================= EXIT TO DASHBOARD =================
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            adminDashboard dashboard = new adminDashboard();
            dashboard.Show();
            this.Close();
        }

        // ================= DATABASE HELPERS =================
        private async Task AddOrderAsync(int customerId, DateTime orderDate, List<OrderProduct> products)
        {
            await Task.Run(() =>
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();

                    using (SqlCommand cmd = new SqlCommand("add_order", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        // Add parameters
                        cmd.Parameters.AddWithValue("@c_id", customerId);
                        cmd.Parameters.AddWithValue("@o_date", orderDate);

                        // Create table parameter for products
                        DataTable productsTable = new DataTable();
                        productsTable.Columns.Add("p_id", typeof(int));
                        productsTable.Columns.Add("qty", typeof(int));

                        foreach (var product in products)
                        {
                            productsTable.Rows.Add(product.ProductId, product.Quantity);
                        }

                        SqlParameter tvpParam = cmd.Parameters.AddWithValue("@products", productsTable);
                        tvpParam.SqlDbType = SqlDbType.Structured;
                        tvpParam.TypeName = "orderproducts";

                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

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

            dgOrders.ItemsSource = dt.DefaultView;
            dgOrders.Visibility = Visibility.Visible;
        }
    }
}