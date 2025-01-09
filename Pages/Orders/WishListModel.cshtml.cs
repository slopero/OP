using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApplication7.Pages
{
    public class WishlistModel : PageModel
    {
        private readonly string _connectionString = "Data Source=E:/TUSUR/2 курс/OP/Work_variant/WebApplication7/WebApplication7/database/table4.db";

        [BindProperty]
        public List<WishlistItem> WishlistItems { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(string userId)
        {
            WishlistItems = await GetWishlistItemsAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostAddToWishlistAsync(string userId, int productId, string productName, string imageUrl, decimal unitPrice)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // ѕровер€ем, есть ли уже этот товар в списке желаемого
                string checkQuery = "SELECT COUNT(*) FROM WishlistItems WHERE ProductId = @ProductId AND UserId = @UserId";
                using (var command = new SqliteCommand(checkQuery, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    var existingCount = (long)await command.ExecuteScalarAsync();

                    if (existingCount > 0)
                    {
                        TempData["Notification"] = "Ётот товар уже добавлен в список желаемого.";
                        return RedirectToPage();
                    }
                }

                // ƒобавл€ем товар в таблицу WishlistItems
                string insertQuery = @"
                    INSERT INTO WishlistItems (ProductId, UserId, ProductName, ImageUrl, UnitPrice)
                    VALUES (@ProductId, @UserId, @ProductName, @ImageUrl, @UnitPrice)";
                using (var command = new SqliteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@ProductName", productName);
                    command.Parameters.AddWithValue("@ImageUrl", imageUrl);
                    command.Parameters.AddWithValue("@UnitPrice", unitPrice);
                    await command.ExecuteNonQueryAsync();
                }
            }

            TempData["Notification"] = "“овар добавлен в список желаемого!";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveFromWishlistAsync(string userId, int productId)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // ”дал€ем товар из таблицы WishlistItems
                string deleteQuery = "DELETE FROM WishlistItems WHERE ProductId = @ProductId AND UserId = @UserId";
                using (var command = new SqliteCommand(deleteQuery, connection))
                {
                    command.Parameters.AddWithValue("@ProductId", productId);
                    command.Parameters.AddWithValue("@UserId", userId);
                    await command.ExecuteNonQueryAsync();
                }
            }

            TempData["Notification"] = "“овар удален из списка желаемого!";
            return RedirectToPage();
        }

        private async Task<List<WishlistItem>> GetWishlistItemsAsync(string userId)
        {
            var items = new List<WishlistItem>();

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // ѕолучаем список желаемого пользовател€
                string selectQuery = @"
                    SELECT ProductId, ProductName, ImageUrl, UnitPrice
                    FROM WishlistItems
                    WHERE UserId = @UserId";
                using (var command = new SqliteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            items.Add(new WishlistItem
                            {
                                ProductId = reader.GetInt32(0),
                                ProductName = reader.GetString(1),
                                ImageUrl = reader.GetString(2),
                                UnitPrice = reader.GetDecimal(3)
                            });
                        }
                    }
                }
            }

            return items;
        }
    }

    public class WishlistItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
