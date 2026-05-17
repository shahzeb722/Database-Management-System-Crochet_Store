using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using Microsoft.VisualBasic;

namespace crochet_store
{
    public partial class EmployeeMgnt : Window
    {

        private readonly string connectionString = DBConnection.ConnectionString;

        public EmployeeMgnt()
        {
            InitializeComponent();

            txtPageTitle.Text = "Employee Management Dashboard";
            txtPageSubtitle.Text = "Select an action from the sidebar.";
            txtOutput.Text = "System ready. Choose an action.";
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(connectionString);
        }



        // ================= NAVIGATION =================
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            adminDashboard dashboard = new adminDashboard();
            dashboard.Show();
            this.Close();
        }

        // ================= ADD EMPLOYEE =================
        private void btnAddEmployee_Click(object sender, RoutedEventArgs e)
        {
            txtPageTitle.Text = "Add New Employee";
            txtPageSubtitle.Text = "Enter employee details.";

            string name = Interaction.InputBox("Enter Employee Name:");
            string contact = Interaction.InputBox("Enter Contact (11 digits):");
            string email = Interaction.InputBox("Enter Email:");
            string role = Interaction.InputBox("Enter Role:");
            string hireDateStr = Interaction.InputBox("Enter Hire Date (yyyy-mm-dd):");

            if (!DateTime.TryParse(hireDateStr, out DateTime hireDate))
            {
                MessageBox.Show("Invalid date format.");
                return;
            }

            try
            {
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand("dbo.add_employee", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@emp_name", name);
                    cmd.Parameters.AddWithValue("@emp_contact", contact);
                    cmd.Parameters.AddWithValue("@emp_email", email);
                    cmd.Parameters.AddWithValue("@emp_role", role);
                    cmd.Parameters.AddWithValue("@emp_hiredate", hireDate);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                txtOutput.Text = $"✔ Employee '{name}' added successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ================= UPDATE EMPLOYEE =================
        private void btnUpdateEmployee_Click(object sender, RoutedEventArgs e)
        {
            string idStr = Interaction.InputBox("Enter Employee ID:");
            if (!int.TryParse(idStr, out int empId)) return;

            string newContact = Interaction.InputBox("New Contact (leave blank to skip):");
            string newRole = Interaction.InputBox("New Role (leave blank to skip):");

            try
            {
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand("dbo.update_employee", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@emp_id", empId);
                    cmd.Parameters.AddWithValue("@emp_contact",
                        string.IsNullOrEmpty(newContact) ? DBNull.Value : (object)newContact);
                    cmd.Parameters.AddWithValue("@emp_role",
                        string.IsNullOrEmpty(newRole) ? DBNull.Value : (object)newRole);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                txtOutput.Text = $"✔ Employee ID {empId} updated successfully.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // ================= DELETE EMPLOYEE =================
        private void btnDeleteEmployee_Click(object sender, RoutedEventArgs e)
        {
            string idStr = Interaction.InputBox("Enter Employee ID to delete:");
            if (!int.TryParse(idStr, out int empId)) return;

            if (MessageBox.Show("Confirm delete?", "Warning",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand("dbo.delete_employee", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@emp_id", empId);

                    con.Open();
                    cmd.ExecuteNonQuery();
                }

                txtOutput.Text = $"✔ Employee ID {empId} deleted.";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }






        // ================= VIEW DELIVERIES =================
        private void btnViewDeliveries_Click(object sender, RoutedEventArgs e)
        {
            string idStr = Interaction.InputBox("Enter Employee ID:");
            if (!int.TryParse(idStr, out int empId)) return;

            try
            {
                using (SqlConnection con = GetConnection())
                using (SqlCommand cmd = new SqlCommand("dbo.view_deliveries_by_employee", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@emp_id", empId);

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    txtOutput.Clear();

                    if (dt.Rows.Count == 0)
                    {
                        txtOutput.Text = $"No deliveries found for Employee ID {empId}.";
                        return;
                    }

                    txtOutput.AppendText($"--- Deliveries for Employee ID {empId} ---\n\n");

                    foreach (DataRow row in dt.Rows)
                    {
                        string date =
                            row["del_date"] == DBNull.Value
                            ? "N/A"
                            : Convert.ToDateTime(row["del_date"]).ToShortDateString();

                        txtOutput.AppendText(
                            $"Delivery ID: {row["del_id"]}, " +
                            $"Order ID: {row["o_id"]}, " +
                            $"Date: {date}, " +
                            $"Status: {row["del_status"]}\n"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
