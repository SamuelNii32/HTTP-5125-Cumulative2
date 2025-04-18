using Microsoft.AspNetCore.Mvc;
using HTTP_5125_Cumulative2.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using System.Data;

namespace HTTP_5125_Cumulative2.Controllers
{
    [Route("api/Teacher")]
    [ApiController]
    public class TeacherAPIController : ControllerBase
    {
        private readonly SchoolDbContext _context;
        // Dependency injection of the database context
        public TeacherAPIController(SchoolDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Adds a teacher to the database
        /// </summary>
        /// <param name="TeacherData">Teacher Object</param>
        /// <example>
        /// POST: api/Teacher/AddTeacher
        /// Headers: Content-Type: application/json
        /// Request Body (validation errors):
        /// {
        ///     "TeacherFName": "Jane",  // Missing last name
        ///     "EmployeeNumber": "T98765", // Already exists
        ///     "Salary": -1000.00 // Salary cannot be negative
        /// } -> 400 (BadRequest if any validation error occurs)
        /// 
        /// POST: api/Teacher/AddTeacher
        /// Request Body (future date error):
        /// {
        ///     "TeacherFName": "Jane",
        ///     "TeacherLName": "Smith",
        ///     "EmployeeNumber": "T98766",
        ///     "HireDate": "2025-06-01T00:00:00", // HireDate cannot be in the future
        ///     "Salary": 65000.00,
        ///     "TeacherWorkPhone": "416-555-1234"
        /// } -> 400 (BadRequest if HireDate is in the future)
        /// 
        /// POST: api/Teacher/AddTeacher
        /// Request Body (successful request):
        /// {
        ///     "TeacherFName": "Jane",
        ///     "TeacherLName": "Smith",
        ///     "EmployeeNumber": "T98767",
        ///     "HireDate": "2023-08-15T00:00:00",
        ///     "Salary": 65000.00,
        ///     "TeacherWorkPhone": "416-555-1234"
        /// } -> 200 (OK with the inserted Teacher Id)
        /// Response: 
        /// {
        ///     "TeacherId": 12345
        /// }
        /// </example>
        /// <returns>
        /// HTTP Status code 200 OK with the inserted Teacher Id if successful.
        /// HTTP Status code 400 (BadRequest) if any validation error occurs (e.g., missing fields, negative salary, future hire date, etc.).
        /// HTTP Status code 409 (Conflict) if the teacher already exists (based on EmployeeNumber).
        /// HTTP Status code 500 (InternalServerError) in case of a database error.
        /// </returns>


        [HttpPost("AddTeacher")]
        public ActionResult<int> AddTeacher([FromBody] Teacher TeacherData)
        {
            if (TeacherData == null)
            {
                return BadRequest("Teacher data is required.");
            }

            // Error Handling: Teacher Name Validation
            if (string.IsNullOrWhiteSpace(TeacherData.TeacherFName) || string.IsNullOrWhiteSpace(TeacherData.TeacherLName))
            {
                return BadRequest("Teacher's first and last names are required.");
            }

            // Error Handling: Salary Validation (Salary cannot be negative)
            if (TeacherData.Salary < 0)
            {
                return BadRequest("Salary cannot be negative.");
            }

            // Error Handling: HireDate Validation (Hire Date cannot be in the future)
            if (TeacherData.HireDate > DateTime.Now)
            {
                return BadRequest("HireDate cannot be in the future.");
            }

            // Error Handling: Employee Number Validation (must be "T" followed by digits)
            if (!Regex.IsMatch(TeacherData.EmployeeNumber, @"^T\d+$"))
            {
                return BadRequest("Employee Number must start with 'T' followed by digits.");
            }

            // Error Handling: Teacher Work Phone Validation (must be in a valid phone format)
            if (!string.IsNullOrWhiteSpace(TeacherData.TeacherWorkPhone) && !Regex.IsMatch(TeacherData.TeacherWorkPhone, @"^\d{3}-\d{3}-\d{4}$"))
            {
                return BadRequest("Teacher Work Phone must be in the format xxx-xxx-xxxx.");
            }

            try
            {
                // 'using' will close the connection after the code executes
                using (MySqlConnection Connection = _context.AccessDatabase())
                {
                    Connection.Open();
                    // Establish a new command (query) for our database
                    MySqlCommand Command = Connection.CreateCommand();

                    // Check if the employee number already exists
                    Command.CommandText = "SELECT COUNT(*) FROM teachers WHERE employeenumber = @employeenumber";
                    Command.Parameters.AddWithValue("@employeenumber", TeacherData.EmployeeNumber);
                    int count = Convert.ToInt32(Command.ExecuteScalar());

                    // Error Handling: If the teacher already exists (employee number conflict)
                    if (count > 0)
                    {
                        return Conflict("Teacher with the same EmployeeNumber already exists.");
                    }

                    // If teacher does not exist, proceed with insertion
                    Command.CommandText = "INSERT INTO teachers (teacherfname, teacherlname, employeenumber, hiredate, salary, teacherworkphone) VALUES (@teacherfname, @teacherlname, @employeenumber, @hiredate, @salary, @teacherworkphone)";
                    Command.Parameters.AddWithValue("@teacherfname", TeacherData.TeacherFName);
                    Command.Parameters.AddWithValue("@teacherlname", TeacherData.TeacherLName);
                    Command.Parameters.AddWithValue("@hiredate", TeacherData.HireDate);
                    Command.Parameters.AddWithValue("@salary", TeacherData.Salary);
                    Command.Parameters.AddWithValue("@teacherworkphone", TeacherData.TeacherWorkPhone);

                    // Execute the insert command
                    Command.ExecuteNonQuery();

                    // Return the ID of the newly inserted teacher
                    return Ok(Convert.ToInt32(Command.LastInsertedId));  // 200 OK with Teacher ID
                }
            }
            catch (MySqlException sqlEx)
            {
                // Handle any SQL exceptions (database connection, query issues)
                return StatusCode(500, $"Database error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                // Handle any other unexpected exceptions
                return StatusCode(500, $"An unexpected error occurred: {ex.Message}");
            }
        }




        /// <summary>
        /// Retrieves a list of all teachers from the database with optional search functionality.
        /// </summary>
        /// <param name="SearchKey">Optional search term to filter teachers by first name, last name, or employee number.</param>
        /// <returns>
        /// A list of teachers matching the search criteria, or all teachers if no search key is provided.
        /// </returns>
        [HttpGet]
        [Route("ListTeachers")]
        public List<Teacher> ListTeachers(string SearchKey = null)
        {
            List<Teacher> Teachers = new List<Teacher>();

            using (MySqlConnection Connection = _context.AccessDatabase())
            {
                Connection.Open();
                MySqlCommand Command = Connection.CreateCommand();

                string query = "SELECT * FROM teachers";

                if (SearchKey != null)
                {
                    query += " WHERE lower(teacherfname) LIKE @key OR lower(teacherlname) LIKE @key OR lower(employeenumber) LIKE @key";
                    Command.Parameters.AddWithValue("@key", $"%{SearchKey.ToLower()}%");
                }

                Command.CommandText = query;
                Command.Prepare();

                using (MySqlDataReader ResultSet = Command.ExecuteReader())
                {
                    while (ResultSet.Read())
                    {
                        int TeacherId = Convert.ToInt32(ResultSet["teacherid"]);
                        string TeacherFName = ResultSet["teacherfname"].ToString();
                        string TeacherLName = ResultSet["teacherlname"].ToString();
                        string EmployeeNumber = ResultSet["employeenumber"].ToString();
                        DateTime HireDate = Convert.ToDateTime(ResultSet["hiredate"]);
                        decimal Salary = Convert.ToDecimal(ResultSet["salary"]);
                        string TeacherWorkPhone = ResultSet["teacherworkphone"] == DBNull.Value ? null : ResultSet["teacherworkphone"].ToString();

                        Teacher CurrentTeacher = new Teacher()
                        {
                            TeacherId = TeacherId,
                            TeacherFName = TeacherFName,
                            TeacherLName = TeacherLName,
                            EmployeeNumber = EmployeeNumber,
                            HireDate = HireDate,
                            Salary = Salary,
                            TeacherWorkPhone = TeacherWorkPhone
                        };

                        Teachers.Add(CurrentTeacher);
                    }
                }
            }

            return Teachers;
        }


        /// <summary>
        /// Returns a teacher in the database by their ID
        /// </summary>
        /// <example>
        /// GET: api/Teacher/FindTeacher/3 -> {"TeacherId":3,"TeacherFName":"Sam","TeacherLName":"Owusu", ...}
        /// </example>
        /// <param name="id">The teacher's ID</param>
        /// <returns>A matching teacher object. Empty object if teacher not found.</returns>
        [HttpGet]
        [Route("FindTeacher/{id}")]
        public Teacher FindTeacher(int id)
        {
            // Empty Teacher object
            Teacher SelectedTeacher = new Teacher();

            using (MySqlConnection Connection = _context.AccessDatabase())
            {
                Connection.Open();
                MySqlCommand Command = Connection.CreateCommand();

                Command.CommandText = "SELECT * FROM teachers WHERE teacherid = @id";
                Command.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader ResultSet = Command.ExecuteReader())
                {
                    while (ResultSet.Read())
                    {
                        int TeacherId = Convert.ToInt32(ResultSet["teacherid"]);
                        string TeacherFName = ResultSet["teacherfname"].ToString();
                        string TeacherLName = ResultSet["teacherlname"].ToString();
                        string EmployeeNumber = ResultSet["employeenumber"].ToString();
                        DateTime HireDate = Convert.ToDateTime(ResultSet["hiredate"]);
                        decimal Salary = Convert.ToDecimal(ResultSet["salary"]);
                        string TeacherWorkPhone = ResultSet["teacherworkphone"] == DBNull.Value ? null : ResultSet["teacherworkphone"].ToString();

                        SelectedTeacher.TeacherId = TeacherId;
                        SelectedTeacher.TeacherFName = TeacherFName;
                        SelectedTeacher.TeacherLName = TeacherLName;
                        SelectedTeacher.EmployeeNumber = EmployeeNumber;
                        SelectedTeacher.HireDate = HireDate;
                        SelectedTeacher.Salary = Salary;
                        SelectedTeacher.TeacherWorkPhone = TeacherWorkPhone;
                    }
                }
            }

            return SelectedTeacher;
        }



        /// <summary>
        /// Deletes a Teacher from the database
        /// </summary>
        /// <param name="TeacherId">Primary key of the teacher to delete</param>
        /// <example>
        /// DELETE: api/Teacher/DeleteTeacher/{TeacherId} -> 1
        /// </example>
        /// <returns>
        /// Number of rows affected by delete operation.
        /// </returns>
        [HttpDelete("DeleteTeacher/{TeacherId}")]
        public ActionResult DeleteTeacher(int TeacherId)
        {
            // 'using' will close the connection after the code executes
            using (MySqlConnection Connection = _context.AccessDatabase())
            {
                Connection.Open();
                // Establish a new command (query) for our database
                MySqlCommand Command = Connection.CreateCommand();

                // Check if the teacher exists before attempting to delete
                Command.CommandText = "SELECT COUNT(*) FROM teachers WHERE teacherid = @teacherid";
                Command.Parameters.AddWithValue("@teacherid", TeacherId);
                int count = Convert.ToInt32(Command.ExecuteScalar());

                if (count == 0)
                {
                    // Return 404 Not Found if the teacher doesn't exist
                    return NotFound($"Teacher with ID {TeacherId} does not exist.");
                }

                // Proceed with delete operation if the teacher exists
                Command.CommandText = "DELETE FROM teachers WHERE teacherid = @teacherid";
               

                // Execute the delete command
                Command.ExecuteNonQuery();

                // Return 204 No Content to indicate successful deletion (no content to return)
                return NoContent();
            }
        }


        /// <summary>
        /// Updates a Teacher in the database. Data is Teacher object, request query contains ID
        /// </summary>
        /// <param name="TeacherData">Teacher Object</param>
        /// <param name="TeacherId">The Teacher ID primary key</param>
        /// <example>
        /// PUT: api/Teacher/UpdateTeacher/4
        /// Headers: Content-Type: application/json
        /// Request Body:
        /// {
        ///     "TeacherFName": "Lauren",
        ///     "TeacherLName": "Smith",
        ///     "EmployeeNumber": "T385",
        ///     "HireDate": "2014-06-22T00:00:00",
        ///     "Salary": 74.2,
        ///     "TeacherWorkPhone": "Null"
        /// } -> 
        /// {
        ///     "TeacherId": 4,
        ///     "TeacherFName": "Lauren",
        ///     "TeacherLName": "Owusu",
        ///     "EmployeeNumber": "T385",
        ///     "HireDate": "2014-06-22T00:00:00",
        ///     "Salary": 74.2,
        ///     "TeacherWorkPhone": "123-456-7890"
        /// }
        /// </example>
        /// <returns>
        /// The updated Teacher object
        /// </returns>
        [HttpPut("UpdateTeacher/{TeacherId}")]
        public IActionResult UpdateTeacher(int TeacherId, [FromBody] Teacher TeacherData)
        {
            // Server-side validation
            if (string.IsNullOrWhiteSpace(TeacherData.TeacherFName))
            {
                return BadRequest("Teacher first name cannot be empty.");
            }
            if (TeacherData.HireDate > DateTime.Now)
            {
                return BadRequest("Hire date cannot be in the future.");
            }
            if (TeacherData.Salary < 0)
            {
                return BadRequest("Salary cannot be less than 0.");
            }

            // Connect to database
            using (MySqlConnection Connection = _context.AccessDatabase())
            {
                Connection.Open();
                MySqlCommand Command = Connection.CreateCommand();

                // Check if the teacher exists
                Command.CommandText = "SELECT COUNT(*) FROM teachers WHERE TeacherId = @TeacherId";
                Command.Parameters.AddWithValue("@TeacherId", TeacherId);

                var count = Convert.ToInt32(Command.ExecuteScalar());
                if (count == 0)
                {
                    return NotFound("Teacher not found.");
                }

                // Update teacher details
                Command.CommandText = @"
            UPDATE teachers 
            SET TeacherFName = @TeacherFName, 
                TeacherLName = @TeacherLName, 
                EmployeeNumber = @EmployeeNumber, 
                HireDate = @HireDate, 
                Salary = @Salary, 
                TeacherWorkPhone = @TeacherWorkPhone 
            WHERE TeacherId = @TeacherId";

                Command.Parameters.AddWithValue("@TeacherFName", TeacherData.TeacherFName);
                Command.Parameters.AddWithValue("@TeacherLName", TeacherData.TeacherLName);
                Command.Parameters.AddWithValue("@EmployeeNumber", TeacherData.EmployeeNumber);
                Command.Parameters.AddWithValue("@HireDate", TeacherData.HireDate);
                Command.Parameters.AddWithValue("@Salary", TeacherData.Salary);
                Command.Parameters.AddWithValue("@TeacherWorkPhone", TeacherData.TeacherWorkPhone);

                Command.ExecuteNonQuery();
            }

            // Return the updated teacher object
            return Ok(TeacherData);
        }



    }
}


