namespace HTTP_5125_Cumulative2.Models
{
    public class Teacher
    {
        public int TeacherId { get; set; } // Primary Key, Auto Increment

        public string? TeacherFName { get; set; } // First Name of the Teacher

        public string? TeacherLName { get; set; } // Last Name of the Teacher

        public string? EmployeeNumber { get; set; } // Employee Number (should start with "T" followed by digits)

        public DateTime HireDate { get; set; } // Date the Teacher was hired

        public decimal Salary { get; set; } // Salary of the Teacher

        public string? TeacherWorkPhone { get; set; }
    }
}
