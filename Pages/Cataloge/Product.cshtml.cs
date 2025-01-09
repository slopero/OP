using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.Data.Sqlite;

public class ProductsModel : PageModel
{
    private readonly string _connectionString = "Data Source=E:/TUSUR/2 курс/OP/Work_variant/WebApplication7/WebApplication7/database/table4.db";
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ISessionCartService _sessionCartService;
    private readonly ICartService _cartService;
    private readonly IWishlistService _wishlistService;

    // Обновленный конструктор, чтобы включить IWishlistService
    public ProductsModel(
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        ISessionCartService sessionCartService,
        ICartService cartService,
        IWishlistService wishlistService) // Добавлен IWishlistService
    {
        _context = context;
        _userManager = userManager;
        _sessionCartService = sessionCartService;
        _cartService = cartService;
        _wishlistService = wishlistService; // Инициализация IWishlistService
    }

    public List<Product> Products { get; set; } = new List<Product>();

    public async Task OnGetAsync()
    {
        // Получаем все продукты из базы данных
        Products = await _context.Products.ToListAsync();
    }

    

    public async Task<IActionResult> OnPostAddToCartAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            return NotFound();
        }

        string userId = HttpContext.Session.GetString("Id");

    var cartItem = new CartItem
        {
            ProductId = productId,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = 1,
            ImageUrl = product.ImageUrl,
            UserId = userId // Добавляем идентификатор пользователя
        };

        if (userId == "0")
        {
            _sessionCartService.AddCartItem(cartItem);
        }
        else
        {
            // Сохраняем в базе данных
            await _cartService.AddToCartAsync(userId, cartItem);

            // Убедимся, что изменения сохраняются
            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage();
    }


    public async Task<IActionResult> OnPostAddToWishlistAsync(string userId, int productId, string productName, string imageUrl, decimal unitPrice)
    {
        using (var connection = new SqliteConnection(_connectionString))
        {
            await connection.OpenAsync();

            // Проверяем, есть ли уже этот товар в списке желаемого
            string checkQuery = "SELECT COUNT(*) FROM WishlistItems WHERE ProductId = @ProductId AND UserId = @UserId";
            using (var command = new SqliteCommand(checkQuery, connection))
            {
                command.Parameters.AddWithValue("@ProductId", productId);
                command.Parameters.AddWithValue("@UserId", userId);
             
            }

            // Добавляем товар в таблицу WishlistItems
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
                //await command.ExecuteNonQueryAsync();
            }
        }

        TempData["Notification"] = "Товар добавлен в список желаемого!";
        return RedirectToPage();
    }

}
