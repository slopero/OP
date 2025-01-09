using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace WebApplication7.Pages
{
    public class Auth : PageModel
    {
        [BindProperty]
        public string Email { get; set; }
        public string UserID { get; set; }
        [BindProperty]
        public string Password { get; set; }
        public string Message { get; set; }

        public void OnGet()
        {
            // ѕроверка сессии дл€ предотвращени€ повторной авторизации
            if (HttpContext.Session.GetString("UserEmail") != null)
            {
                TempData["Notification"] = "¬ы уже авторизированы";
                Response.Redirect("/index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                Message = "Email и пароль не могут быть пустыми.";
                return Page();
            }

            string connectionString = "Data Source=E:/TUSUR/2 курс/OP/ опии вариантов/ опи€/WebApplication7/WebApplication7/database/table4.db";
            string sqlExpression = "SELECT id, email, password FROM users WHERE email = @Email AND password = @Password";

            using (var connection = new SqliteConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqliteCommand(sqlExpression, connection))
                {
                    command.Parameters.AddWithValue("@Email", Email);
                    command.Parameters.AddWithValue("@Password", Password);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (!reader.HasRows)
                        {
                            Message = "Ќеверный логин или пароль.";
                            return Page();
                        }

                        await reader.ReadAsync();
                        UserID = reader.GetInt32(0).ToString();
                        // —оздаем JWT токен и сохран€ем email пользовател€ в сессии
                        var claims = new List<Claim> { new Claim(ClaimTypes.Name, Email) };
                        var jwt = new JwtSecurityToken(
                            issuer: AuthOptions.ISSUER,
                            audience: AuthOptions.AUDIENCE,
                            claims: claims,
                            expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(2)),
                            signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

                        var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);
  
                        HttpContext.Session.SetString("UserEmail", Email);
                        HttpContext.Session.SetString("Id", UserID);

                        // ”ведомл€ем пользовател€ об успешной авторизации и перенаправл€ем на главную страницу
                        Message = "”спешна€ авторизаци€.";
                        return RedirectToPage("/Index"); // √лавна€ страница
                    }
                }
            }
        }
    }
}
