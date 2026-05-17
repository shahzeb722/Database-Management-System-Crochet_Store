using System;
using System.Windows;

namespace crochet_store
{
    public partial class adminDashboard : Window
    {
        public adminDashboard()
        {
            InitializeComponent();
        }

        private void btnManageEmployees_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EmployeeMgnt win = new EmployeeMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Employee Management Error");
            }
        }

        private void btnManageCustomers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CustMgnt win = new CustMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Customer Management Error");
            }
        }

        private void btnManageProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductMgnt win = new ProductMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Product Management Error");
            }
        }

        private void btnManageOrders_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OrderMgnt win = new OrderMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Order Management Error");
            }
        }

        private void btnManageSuppliers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SupMgnt win = new SupMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Supplier Management Error");
            }
        }

        private void btnManageShipments_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShipMgnt win = new ShipMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Shipment Management Error");
            }
        }

        private void btnManageInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ItemMgnt win = new ItemMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Inventory Management Error");
            }
        }

        private void btnManageDelivery_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DeliveryMgnt win = new DeliveryMgnt();
                win.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delivery Management Error");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
