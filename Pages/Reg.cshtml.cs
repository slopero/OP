using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;


namespace WebApplication7.Pages
{
    public class Reg : PageModel
    {
        [BindProperty]
    public string Email { get; set; }
    [BindProperty]
    public string Password { get; set; }
    public string Message { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            Message = "Email и пароль не могут быть пустыми.";
            return Page();
        }

        string connectionString = "Data Source=E:/TUSUR/2 курс/OP/Копии вариантов/Копия/WebApplication7/WebApplication7/database/table4.db";
        string checkEmailQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";

        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqliteCommand(checkEmailQuery, connection))
            {
                command.Parameters.AddWithValue("@Email", Email);
                var emailCount = (long)await command.ExecuteScalarAsync();

                if (emailCount > 0)
                {
                    Message = "Пользователь с таким email уже существует.";
                    return Page();
                }
            }

            // Добавление нового пользователя
            string insertUserQuery = "INSERT INTO users (email, password) VALUES (@Email, @Password)";
            using (var command = new SqliteCommand(insertUserQuery, connection))
            {
                command.Parameters.AddWithValue("@Email", Email);
                command.Parameters.AddWithValue("@Password", Password);
                await command.ExecuteNonQueryAsync();
            }
        }

        Message = "Пользователь успешно зарегистрирован.";
        return Page();
    }
    }
}
