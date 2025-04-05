# Teacher Management System – HTTP 5125 Cumulative Project

This is a web-based Teacher Management System developed as a cumulative project for the HTTP 5125 course. The system uses **ASP.NET Core MVC** for the frontend and **ASP.NET Core Web API** for backend logic. It allows users to add, view, search, and delete teacher records.

## Technologies Used

- ASP.NET Core MVC
- ASP.NET Core Web API
- Entity Framework Core
- MySQL
- Bootstrap 4
- Visual Studio 2022

## Features

- View a list of teachers with optional search
- View teacher details
- Add new teacher information
- Confirm and delete teacher records
- API integrated backend for all data operations

## Pages

- `List.cshtml` – Displays a list of teachers (with search functionality)
- `Show.cshtml` – Displays detailed information for a selected teacher
- `Add.cshtml` – Form to add a new teacher
- `DeleteConfirm.cshtml` – Confirms before deleting a teacher

## How It Works

The MVC `TeacherPageController` interacts directly with the `TeacherAPIController` to perform all data operations. When a teacher is added, the API returns the new teacher's ID, and the app redirects to the detailed view (`Show.cshtml`). When a teacher is deleted, the app redirects to the teacher list view.

## Model Structure

```csharp
public class Teacher
{
    public int TeacherId { get; set; }
    public string? TeacherFName { get; set; }
    public string? TeacherLName { get; set; }
    public string? EmployeeNumber { get; set; }
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
    public string? TeacherWorkPhone { get; set; }
}
