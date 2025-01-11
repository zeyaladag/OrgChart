using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OrgChartApp
{
    public class Employee
    {
        public int EmpId { get; set; }
        public string Name { get; set; }
        public string JobTitle { get; set; }
        public int? ManagerId { get; set; }
        public List<Employee> Children { get; set; } = new List<Employee>();

        public Employee(int empId, string name, string jobTitle, int? managerId)
        {
            EmpId = empId;
            Name = name;
            JobTitle = jobTitle;
            ManagerId = managerId;
        }
    }

    public partial class MainForm : Form
    {
        private List<Employee> employees;
        private TextBox nameTextBox;
        private TextBox jobTitleTextBox;
        private TextBox managerIdTextBox;

        public MainForm()
        {
            //TODO
            InitializeComponent();
            employees = GetEmployees("Server=localhost;Database=employee_management;User Id=root;Password=password;");
            var organizationalChart = BuildOrganizationalChart(employees);

            this.Paint += (sender, e) => DrawOrganizationalChart(e.Graphics, organizationalChart, 50, 50, 150, 50);

            // Add Textboxes for employee data input
            nameTextBox = new TextBox { Location = new Point(10, 400), Width = 200 };
            jobTitleTextBox = new TextBox { Location = new Point(10, 430), Width = 200 };
            managerIdTextBox = new TextBox { Location = new Point(10, 460), Width = 200 };
            nameTextBox.Text = "Employee Name";
            jobTitleTextBox.Text = "Job Title";
            managerIdTextBox.Text = "Manager ID";
            Controls.Add(nameTextBox);
            Controls.Add(jobTitleTextBox);
            Controls.Add(managerIdTextBox);

            // Add Buttons for Add and Delete
            Button addButton = new Button
            {
                Text = "Add Employee",
                Location = new Point(10, 490),
                Width = 200
            };
            addButton.Click += AddButton_Click;

            Button deleteButton = new Button
            {
                Text = "Delete Employee",
                Location = new Point(10, 520),
                Width = 200
            };
            deleteButton.Click += DeleteButton_Click;

            Controls.Add(addButton);
            Controls.Add(deleteButton);
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            string name = nameTextBox.Text;
            string jobTitle = jobTitleTextBox.Text;
            int? managerId = string.IsNullOrEmpty(managerIdTextBox.Text) ? (int?)null : int.Parse(managerIdTextBox.Text);

            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(jobTitle))
            {
                int newEmpId = AddEmployee(name, jobTitle, managerId);
                MessageBox.Show($"Employee added with ID {newEmpId}");
                employees = GetEmployees("Server=localhost;Database=employee_management;User Id=root;Password=password;");
                this.Invalidate(); // Refresh the chart
            }
            else
            {
                MessageBox.Show("Please enter valid name and job title.");
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            string empIdText = nameTextBox.Text;

            if (int.TryParse(empIdText, out int empId))
            {
                DeleteEmployee(empId);
                MessageBox.Show($"Employee with ID {empId} deleted");
                employees = GetEmployees("Server=localhost;Database=employee_management;User Id=root;Password=password;");
                this.Invalidate(); // Refresh the chart
            }
            else
            {
                MessageBox.Show("Please enter a valid employee ID to delete.");
            }
        }

        // Add employee to the database
        private int AddEmployee(string name, string jobTitle, int? managerId)
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Database=employee_management;User Id=root;Password=password;"))
            {
                connection.Open();
                string query = "INSERT INTO employees (name, job_name, manager_id) VALUES (@name, @jobTitle, @managerId);";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@jobTitle", jobTitle);
                command.Parameters.AddWithValue("@managerId", managerId.HasValue ? (object)managerId.Value : DBNull.Value);

                command.ExecuteNonQuery();
                command.CommandText = "SELECT LAST_INSERT_ID();";
                int newEmpId = Convert.ToInt32(command.ExecuteScalar());
                return newEmpId;
            }
        }

        // Delete employee from the database
        private void DeleteEmployee(int empId)
        {
            using (MySqlConnection connection = new MySqlConnection("Server=localhost;Database=employee_management;User Id=root;Password=password;"))
            {
                connection.Open();
                string query = "DELETE FROM employees WHERE emp_id = @empId";
                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@empId", empId);
                command.ExecuteNonQuery();
            }
        }

        // Retrieve employees from MySQL database
        private List<Employee> GetEmployees(string connectionString)
        {
            List<Employee> employees = new List<Employee>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT emp_id, name, job_name, manager_id FROM employees";
                MySqlCommand command = new MySqlCommand(query, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    int empId = reader.GetInt32("emp_id");
                    string name = reader.GetString("name");
                    string jobTitle = reader.GetString("job_name");
                    int? managerId = reader.IsDBNull(reader.GetOrdinal("manager_id")) ? (int?)null : reader.GetInt32("manager_id");

                    Employee employee = new Employee(empId, name, jobTitle, managerId);
                    employees.Add(employee);
                }
            }

            return employees;
        }

        // Build the organizational chart from employee data
        private List<Employee> BuildOrganizationalChart(List<Employee> employees)
        {
            Dictionary<int, Employee> employeeLookup = new Dictionary<int, Employee>();

            foreach (var employee in employees)
            {
                employeeLookup[employee.EmpId] = employee;
            }

            List<Employee> topLevelEmployees = new List<Employee>();

            foreach (var employee in employees)
            {
                if (employee.ManagerId.HasValue)
                {
                    Employee manager = employeeLookup[employee.ManagerId.Value];
                    manager.Children.Add(employee);
                }
                else
                {
                    topLevelEmployees.Add(employee);
                }
            }

            return topLevelEmployees;
        }

        // Draw the organizational chart on the form
        private void DrawOrganizationalChart(Graphics g, List<Employee> employees, int x, int y, int boxWidth, int boxHeight)
        {
            const int verticalSpacing = 100;
            const int horizontalSpacing = 150;

            foreach (var employee in employees)
            {
                DrawEmployee(g, employee, x, y, boxWidth, boxHeight);

                if (employee.Children.Count > 0)
                {
                    int childX = x - (employee.Children.Count - 1) * horizontalSpacing / 2;
                    int childY = y + verticalSpacing;
                    foreach (var child in employee.Children)
                    {
                        DrawOrganizationalChart(g, new List<Employee> { child }, childX, childY, boxWidth, boxHeight);
                        childX += horizontalSpacing;
                    }
                }
            }
        }

        // Draw individual employee box and connecting lines
        private void DrawEmployee(Graphics g, Employee employee, int x, int y, int boxWidth, int boxHeight)
        {
            // Draw rectangle for the employee
            g.FillRectangle(Brushes.LightBlue, x, y, boxWidth, boxHeight);
            g.DrawRectangle(Pens.Black, x, y, boxWidth, boxHeight);

            // Draw employee name and job title
            g.DrawString(employee.Name, new Font("Arial", 10), Brushes.Black, x + 5, y + 5);
            g.DrawString(employee.JobTitle, new Font("Arial", 8), Brushes.Black, x + 5, y + 25);

            // Draw lines to children (subordinates)
            if (employee.Children.Count > 0)
            {
                foreach (var child in employee.Children)
                {
                    int childX = x - (employee.Children.Count - 1) * 150 / 2;
                    int childY = y + 100;
                    g.DrawLine(Pens.Black, x + boxWidth / 2, y + boxHeight, childX + boxWidth / 2, childY);
                }
            }
        }
    }
}
