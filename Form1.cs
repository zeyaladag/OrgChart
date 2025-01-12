using Microsoft.VisualBasic;
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
        private Panel chartPanel;

        public MainForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized; // open window in full screen

            employees = GetEmployees("Server=localhost;Database=employee_management;User Id=root;Password=password;");
            var organizationalChart = BuildOrganizationalChart(employees);

            int verticalSpacing = 100;  // space between levels
            int horizontalSpacing = 150;  // space between siblings
            int totalWidth = this.ClientSize.Width;  // use the form's width to center employee with id 1
            this.Paint += (sender, e) => DrawOrganizationalChart(e.Graphics, organizationalChart, 0, 50, 150, 50, verticalSpacing, horizontalSpacing, totalWidth);

            // for employee data input
            nameTextBox = new TextBox { Location = new Point(10, 400), Width = 200 };
            jobTitleTextBox = new TextBox { Location = new Point(10, 430), Width = 200 };
            managerIdTextBox = new TextBox { Location = new Point(10, 460), Width = 200 };
            nameTextBox.Text = "Employee Name";
            jobTitleTextBox.Text = "Job Title";
            managerIdTextBox.Text = "Manager ID";
            Controls.Add(nameTextBox);
            Controls.Add(jobTitleTextBox);
            Controls.Add(managerIdTextBox);

            Button addButton = new Button
            {
                Text = "Add Employee",
                Location = new Point(10, 490),
                Width = 200,
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

                // clean window
                Controls.Remove(chartPanel);
                employees = GetEmployees("Server=localhost;Database=employee_management;User Id=root;Password=password;");
                var organizationalChart = BuildOrganizationalChart(employees);

                // draw updated chart
                chartPanel = new Panel();
                chartPanel.Dock = DockStyle.Fill;
                Controls.Add(chartPanel);
                chartPanel.Paint += (sender, e) =>
                {
                    DrawOrganizationalChart(e.Graphics, organizationalChart, 0, 50, 150, 50, 100, 150, chartPanel.ClientSize.Width);
                };
                chartPanel.Invalidate();
            }
            else
            {
                MessageBox.Show("Please enter valid credentials");
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            // correct entered id
            string managerIdText = managerIdTextBox.Text.Trim();
            if (int.TryParse(managerIdText, out int managerId))
            {
                // delete entered employee
                var employeesToDelete = employees.Where(e => e.ManagerId == managerId).ToList();
                if (employeesToDelete.Any())
                {
                    foreach (var employee in employeesToDelete)
                    {
                        DeleteEmployee(employee.EmpId);
                    }
                    MessageBox.Show($"{employeesToDelete.Count} employees under Manager ID {managerId} have been deleted.");
                    
                    // update chart
                    Controls.Remove(chartPanel);
                    employees = GetEmployees("Server=localhost;Database=employee_management;User Id=root;Password=password;");
                    var organizationalChart = BuildOrganizationalChart(employees);
                    chartPanel = new Panel();
                    chartPanel.Dock = DockStyle.Fill;
                    chartPanel.AutoScroll = false;
                    Controls.Add(chartPanel);
                    chartPanel.Paint += (sender, e) =>
                    {
                        DrawOrganizationalChart(e.Graphics, organizationalChart, 0, 50, 150, 50, 100, 150, chartPanel.ClientSize.Width);
                    };
                    chartPanel.Invalidate();
                    else
                    {
                        MessageBox.Show($"No employees found under Manager ID {managerId}.");
                    }
                }
                else
                {
                    MessageBox.Show("Please enter a valid Manager ID.");
                }
            }

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
                    manager.Children.Add(employee); // add employee as a child to their manager
                }
                else
                {
                    topLevelEmployees.Add(employee); // employees without manager
                }
            }
            return topLevelEmployees;
        }

        private void DrawOrganizationalChart(Graphics g, List<Employee> employees, int x, int y, int boxWidth, int boxHeight, int verticalSpacing, int horizontalSpacing, int totalWidth)
        {
            foreach (var employee in employees)
            {
                // draw first employye in center of the screen
                int currentX = (employee.EmpId == 1) ? totalWidth / 2 - boxWidth / 2 : x;

                DrawEmployee(g, employee, currentX, y, boxWidth, boxHeight);

                if (employee.Children.Count > 0)
                {
                    // calculate the starting position for child nodes
                    int childX = currentX - (employee.Children.Count - 1) * horizontalSpacing / 2;
                    int childY = y + verticalSpacing;  // Y position for children

                    foreach (var child in employee.Children)
                    {
                        g.DrawLine(Pens.Black, currentX + boxWidth / 2, y + boxHeight, childX + boxWidth / 2, childY);
                        DrawOrganizationalChart(g, new List<Employee> { child }, childX, childY, boxWidth, boxHeight, verticalSpacing, horizontalSpacing, totalWidth);
                        childX += horizontalSpacing;
                    }
                }
            }
        }

        private void DrawEmployee(Graphics g, Employee employee, int x, int y, int boxWidth, int boxHeight)
        {
            g.FillRectangle(Brushes.LightBlue, x, y, boxWidth, boxHeight);
            g.DrawRectangle(Pens.Black, x, y, boxWidth, boxHeight);
            g.DrawString(employee.Name, new Font("Arial", 9), Brushes.Black, x + 5, y + 5);
            g.DrawString(employee.JobTitle, new Font("Arial", 8), Brushes.Black, x + 5, y + 20);
            g.DrawString(employee.EmpId.ToString(), new Font("Arial", 7), Brushes.Black, x + 5, y + 35);
        }
    }
}
