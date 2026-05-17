using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;

namespace crochet_store
{
    // --- DATA MODELS ---

    public class Supplier
    {
        public int sup_id { get; set; }
        public string sup_name { get; set; }
    }

    public class Item
    {
        public int item_id { get; set; }
        public string item_name { get; set; }
    }

    public class ShipmentItemDetail
    {
        public int shipment_id { get; set; }
        public int item_id { get; set; }
        public string item_name { get; set; }
        public int quantity_received { get; set; }
        public decimal cost_per_unit { get; set; }
        public decimal LineTotal => quantity_received * cost_per_unit;
    }

    // --- DATABASE MANAGER (DAL) ---

    public class ShipmentDBManager
    {
        // FIX: The variable name is now correctly declared as '_connectionString'
        // to match its usage in all methods below (GetSuppliers, GetItems, etc.).
        private readonly string connectionString = DBConnection.ConnectionString;

        public DataTable GetSuppliers()
        {
            // Fetch suppliers
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter("SELECT sup_id, sup_name FROM Supplier", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetItems()
        {
            // Fetch items
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlDataAdapter da = new SqlDataAdapter("SELECT item_id, item_name FROM Item", conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        // Maps to sp_Create_Shipment_Header
        public int CreateShipmentHeader(int supId, DateTime shipDate, DateTime arrivalDate)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("sp_Create_Shipment_Header", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@sup_id", supId);
                    cmd.Parameters.AddWithValue("@shipment_date", shipDate);
                    cmd.Parameters.AddWithValue("@arrival_date", arrivalDate);

                    SqlParameter outputParam = new SqlParameter("@new_shipment_id", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    cmd.ExecuteNonQuery();

                    return (int)outputParam.Value;
                }
            }
        }

        // Maps to sp_Add_Shipment_Item
        public void AddShipmentItem(int shipmentId, int itemId, int quantity, decimal costPerUnit)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("sp_Add_Shipment_Item", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@shipment_id", shipmentId);
                    cmd.Parameters.AddWithValue("@item_id", itemId);
                    cmd.Parameters.AddWithValue("@quantity_received", quantity);
                    cmd.Parameters.AddWithValue("@cost_per_unit", costPerUnit);

                    cmd.ExecuteNonQuery();
                }
            }
        }
    }


    // --- CODE-BEHIND: CHANGED TO WINDOW ---
    public partial class ShipMgnt : Window
    {
        private readonly ShipmentDBManager _dbManager = new ShipmentDBManager();
        private int _currentShipmentId = 0;
        private ObservableCollection<ShipmentItemDetail> _shipmentItems = new ObservableCollection<ShipmentItemDetail>();

        public ShipMgnt()
        {
            InitializeComponent();
            LoadInitialData();
            DgShipmentItems.ItemsSource = _shipmentItems;

            // Fix: Correctly referencing the named border element and disabling it initially.
            ItemDetailsCard.IsEnabled = false;
            UpdateTotalBill();
        }

        private void LoadInitialData()
        {
            try
            {
                DataTable suppliersDt = _dbManager.GetSuppliers();
                CmbSupplier.ItemsSource = suppliersDt.AsEnumerable().Select(r => new Supplier
                {
                    sup_id = r.Field<int>("sup_id"),
                    sup_name = r.Field<string>("sup_name")
                }).ToList();

                DataTable itemsDt = _dbManager.GetItems();
                CmbItem.ItemsSource = itemsDt.AsEnumerable().Select(r => new Item
                {
                    item_id = r.Field<int>("item_id"),
                    item_name = r.Field<string>("item_name")
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load initial data (Suppliers/Items). Check your database connection: {ex.Message}",
                                "Database Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Step 1: Create Shipment Header ---
        private void BtnCreateHeader_Click(object sender, RoutedEventArgs e)
        {
            if (CmbSupplier.SelectedValue == null)
            {
                MessageBox.Show("Please select a supplier.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (DpShipmentDate.SelectedDate == null || DpArrivalDate.SelectedDate == null)
            {
                MessageBox.Show("Please select both Shipment and Arrival Dates.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int supId = (int)CmbSupplier.SelectedValue;
            DateTime shipDate = DpShipmentDate.SelectedDate.Value;
            DateTime arrivalDate = DpArrivalDate.SelectedDate.Value;

            try
            {
                // Calls sp_Create_Shipment_Header
                _currentShipmentId = _dbManager.CreateShipmentHeader(supId, shipDate, arrivalDate);

                // Update UI state upon successful header creation
                TxtShipmentIdDisplay.Text = $"Current Shipment ID: {_currentShipmentId}";

                // Fix: Enable the Item Details section after creation
                ItemDetailsCard.IsEnabled = true;

                BtnCreateHeader.IsEnabled = false;
                CmbSupplier.IsEnabled = false;
                DpShipmentDate.IsEnabled = false;
                DpArrivalDate.IsEnabled = false;

                MessageBox.Show($"Shipment Header successfully created! You can now add items to Shipment ID {_currentShipmentId}.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (SqlException sqlEx)
            {
                // Catches RAISERROR (e.g., date validation: arrival date < shipment date)
                MessageBox.Show($"Database Error: {sqlEx.Message.Split('\n')[0]}", "Error Creating Shipment", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // --- Step 2: Add Item Details ---
        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (_currentShipmentId == 0) return;

            if (CmbItem.SelectedValue == null ||
                !int.TryParse(TxtQuantity.Text, out int quantity) ||
                !decimal.TryParse(TxtCostPerUnit.Text, out decimal costPerUnit))
            {
                MessageBox.Show("Please select an item and enter valid numbers for Quantity and Cost.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (quantity <= 0)
            {
                MessageBox.Show("Quantity received must be greater than 0.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int itemId = (int)CmbItem.SelectedValue;

            // Prevent adding the same item twice in the current session
            if (_shipmentItems.Any(i => i.item_id == itemId))
            {
                MessageBox.Show("This item is already added to this shipment. Please choose a different item.",
                                "Duplicate Item", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string itemName = ((Item)CmbItem.SelectedItem).item_name;

            try
            {
                // Calls sp_Add_Shipment_Item (updates stock and price via trigger)
                _dbManager.AddShipmentItem(_currentShipmentId, itemId, quantity, costPerUnit);

                // Update local list for UI
                var detail = new ShipmentItemDetail
                {
                    shipment_id = _currentShipmentId,
                    item_id = itemId,
                    item_name = itemName,
                    quantity_received = quantity,
                    cost_per_unit = costPerUnit
                };

                _shipmentItems.Add(detail);

                // Clear inputs for next item
                CmbItem.SelectedIndex = -1;
                TxtQuantity.Text = "1";
                TxtCostPerUnit.Text = "0.00";

                UpdateTotalBill();

                // Optional: Show the updated stock level (requires fetching stock after trigger runs, omitted for brevity)
                MessageBox.Show($"Item '{itemName}' added. Inventory stock and standard price updated.", "Item Added", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            catch (SqlException sqlEx)
            {
                MessageBox.Show($"Database Error: {sqlEx.Message.Split('\n')[0]}", "Error Adding Item", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateTotalBill()
        {
            decimal total = _shipmentItems.Sum(i => i.LineTotal);
            TxtTotalBill.Text = $"PKR {total:N2}";
        }
    }
}