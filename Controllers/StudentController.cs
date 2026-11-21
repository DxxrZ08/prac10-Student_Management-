using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using StudentManagementAPI.Models;

namespace StudentManagementAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public StudentsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private SqlConnection GetConnection()
        {
            return new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection"));
        }

        private bool EmailExists(SqlConnection con, string email, int excludeId = 0)
        {
            string query = @"SELECT COUNT(*) FROM Students
WHERE Email = @Email AND Id <> @ExcludeId";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@ExcludeId", excludeId);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        [HttpGet]
        public IActionResult GetStudents(string? name = null, string? course = null)
        {
            List<Student> students = new List<Student>();

            using SqlConnection con = GetConnection();

            string query = @"SELECT * FROM Students
WHERE (@Name IS NULL OR Name LIKE '%' + @Name + '%')
AND (@Course IS NULL OR Course = @Course)";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Name",
                string.IsNullOrWhiteSpace(name) ? DBNull.Value : name.Trim());
            cmd.Parameters.AddWithValue("@Course",
                string.IsNullOrWhiteSpace(course) ? DBNull.Value : course.Trim());

            con.Open();

            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                students.Add(new Student
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString()!,
                    Email = reader["Email"].ToString()!,
                    Course = reader["Course"].ToString()!
                });
            }

            return Ok(students);
        }

        [HttpGet("{id}")]
        public IActionResult GetStudent(int id)
        {
            using SqlConnection con = GetConnection();

            string query = "SELECT * FROM Students WHERE Id = @Id";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Id", id);

            con.Open();

            using SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                Student student = new Student
                {
                    Id = Convert.ToInt32(reader["Id"]),
                    Name = reader["Name"].ToString()!,
                    Email = reader["Email"].ToString()!,
                    Course = reader["Course"].ToString()!
                };

                return Ok(student);
            }

            return NotFound();
        }

        [HttpPost]
        public IActionResult AddStudent(Student student)
        {
            using SqlConnection con = GetConnection();

            con.Open();

            if (EmailExists(con, student.Email))
                return Conflict("A student with this email already exists.");

            string query = @"INSERT INTO Students (Name, Email, Course)
VALUES (@Name, @Email, @Course);
SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Name", student.Name);
            cmd.Parameters.AddWithValue("@Email", student.Email);
            cmd.Parameters.AddWithValue("@Course", student.Course);

            student.Id = Convert.ToInt32(cmd.ExecuteScalar());

            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
        }

        [HttpPut("{id}")]
        public IActionResult UpdateStudent(int id, Student student)
        {
            using SqlConnection con = GetConnection();

            con.Open();

            if (EmailExists(con, student.Email, id))
                return Conflict("Another student with this email already exists.");

            string query = @"UPDATE Students
SET Name = @Name,
Email = @Email,
Course = @Course
WHERE Id = @Id";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@Name", student.Name);
            cmd.Parameters.AddWithValue("@Email", student.Email);
            cmd.Parameters.AddWithValue("@Course", student.Course);

            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
                return NotFound();

            return Ok("Student updated successfully.");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteStudent(int id)
        {
            using SqlConnection con = GetConnection();

            string query = "DELETE FROM Students WHERE Id = @Id";

            using SqlCommand cmd = new SqlCommand(query, con);

            cmd.Parameters.AddWithValue("@Id", id);

            con.Open();

            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
                return NotFound();

            return Ok("Student deleted successfully.");
        }
    }
}
