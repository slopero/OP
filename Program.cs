using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;


var builder = WebApplication.CreateBuilder(args);

// Configure database context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=E:/TUSUR/2 курс/OP/Копии вариантов/Копия/WebApplication7/WebApplication7/database/table4.db"));

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = AuthOptions.ISSUER,
            ValidateAudience = true,
            ValidAudience = AuthOptions.AUDIENCE,
            ValidateLifetime = true,
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            ValidateIssuerSigningKey = true,
        };
    });

// Configure Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register services
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionCartService, SessionCartService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ISessionWishlistService, SessionWishlistService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapRazorPages();
app.Run();


public class ApplicationDbContext : IdentityDbContext<IdentityUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<WishlistItem> WishlistItems { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int Count { get; set; }
    public decimal Price { get; set; } // Измените на decimal для цен
    public string ImageUrl { get; set; }
    public string Link { get; set; }
}

public class CartItem
{
    public int Id { get; set; }
    public string UserId { get; set; } // Изменено на string для соответствия типу IdentityUser
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string ImageUrl { get; set; }
    public decimal TotalPrice => UnitPrice * Quantity;
}

public interface ICartService
{
    Task<List<CartItem>> GetCartItemsAsync(string userId);
    Task AddToCartAsync(string userId, CartItem item);
    Task UpdateCartItemQuantityAsync(string userId, int productId, int newQuantity);
    Task RemoveFromCartAsync(string userId, int productId);
    Task ClearCartAsync(string userId);
}

public interface IWishlistService
{
    Task<List<WishlistItem>> GetWishlistItemsAsync(string userId = null);
    Task AddToWishlistAsync(string userId, int productID);
    Task RemoveFromWishlistAsync(string userId, int productId);
    Task ClearWishlistAsync(string userId);
}

public interface ISessionWishlistService
{
    void AddWishlistItem(WishlistItem item);
    void RemoveWishlistItem(int productId);
    List<WishlistItem> GetWishlistItems();
    void ClearWishlist();
}

public class SessionWishlistService : ISessionWishlistService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string SessionKey = "Wishlist";

    public SessionWishlistService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void AddWishlistItem(WishlistItem item)
    {
        var wishlist = GetWishlistItems();
        if (!wishlist.Any(w => w.ProductId == item.ProductId))
        {
            wishlist.Add(item);
            SaveWishlist(wishlist);
        }
    }

    public void RemoveWishlistItem(int productId)
    {
        var wishlist = GetWishlistItems();
        var itemToRemove = wishlist.FirstOrDefault(w => w.ProductId == productId);
        if (itemToRemove != null)
        {
            wishlist.Remove(itemToRemove);
            SaveWishlist(wishlist);
        }
    }

    public List<WishlistItem> GetWishlistItems()
    {
        var wishlist = _httpContextAccessor.HttpContext.Session.GetString(SessionKey);
        return string.IsNullOrEmpty(wishlist) ? new List<WishlistItem>() : JsonConvert.DeserializeObject<List<WishlistItem>>(wishlist);
    }

    public void ClearWishlist()
    {
        _httpContextAccessor.HttpContext.Session.Remove(SessionKey);
    }

    private void SaveWishlist(List<WishlistItem> wishlist)
    {
        _httpContextAccessor.HttpContext.Session.SetString(SessionKey, JsonConvert.SerializeObject(wishlist));
    }
}

public interface ISessionCartService
{
    List<CartItem> GetCartItems();
    void AddCartItem(CartItem item);
    void UpdateCartItem(int productId, int quantity);
    void RemoveCartItem(int productId);
    void ClearCart();
}

public class WishlistService : IWishlistService
{
    private readonly ApplicationDbContext _context;
    private readonly ISessionWishlistService _sessionWishlistService;

    public WishlistService(ApplicationDbContext context, ISessionWishlistService sessionWishlistService)
    {
        _context = context;
        _sessionWishlistService = sessionWishlistService;
    }

    public async Task<List<WishlistItem>> GetWishlistItemsAsync(string userId)
    {
        return await _context.WishlistItems
            .Where(w => w.UserId == userId)
            .ToListAsync();
    }

    public async Task AddToWishlistAsync(string userId, int productId)
    {
        // Проверка на наличие товара в списке желаемого
        var existingItem = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (existingItem == null)
        {
            var item = new WishlistItem
            {
                UserId = userId,
                ProductId = productId
            };
            _context.WishlistItems.Add(item);
            await _context.SaveChangesAsync();
        }
    }


    public async Task RemoveFromWishlistAsync(string userId, int productId)
    {
        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (item != null)
        {
            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearWishlistAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _sessionWishlistService.ClearWishlist();
            return;
        }

        var items = await _context.WishlistItems
            .Where(wishlistItem => wishlistItem.UserId == userId)
            .ToListAsync();

        _context.WishlistItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}

public class SessionCartService : ISessionCartService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CartSessionKey = "CartSession";

    public SessionCartService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public List<CartItem> GetCartItems()
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var cartItemsJson = session.GetString(CartSessionKey);
        return string.IsNullOrEmpty(cartItemsJson) ? new List<CartItem>() : System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(cartItemsJson);
    }

    public void AddCartItem(CartItem item)
    {
        var cartItems = GetCartItems();
        var existingItem = cartItems.FirstOrDefault(c => c.ProductId == item.ProductId);
        if (existingItem != null)
        {
            existingItem.Quantity += item.Quantity;
        }
        else
        {
            cartItems.Add(item);
        }
        SaveCartItems(cartItems);
    }

    public void UpdateCartItem(int productId, int quantity)
    {
        var cartItems = GetCartItems();
        var existingItem = cartItems.FirstOrDefault(c => c.ProductId == productId);
        if (existingItem != null)
        {
            existingItem.Quantity = quantity;
        }
        SaveCartItems(cartItems);
    }

    public void RemoveCartItem(int productId)
    {
        var cartItems = GetCartItems();
        cartItems.RemoveAll(c => c.ProductId == productId);
        SaveCartItems(cartItems);
    }

    public void ClearCart()
    {
        SaveCartItems(new List<CartItem>());
    }

    private void SaveCartItems(List<CartItem> cartItems)
    {
        var session = _httpContextAccessor.HttpContext.Session;
        var cartItemsJson = System.Text.Json.JsonSerializer.Serialize(cartItems);
        session.SetString(CartSessionKey, cartItemsJson);
    }
}

public class CartService : ICartService
{
    private readonly ApplicationDbContext _context;
    private readonly ISessionCartService _sessionCartService;

    public CartService(ApplicationDbContext context, ISessionCartService sessionCartService)
    {
        _context = context;
        _sessionCartService = sessionCartService; // Внедряем сервис для работы с сессией
    }

    public async Task<List<CartItem>> GetCartItemsAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Если пользователь не авторизован, возвращаем элементы из сессии
            return _sessionCartService.GetCartItems();
        }

        // Если пользователь авторизован, получаем товары из базы данных
        return await _context.CartItems
            .Where(item => item.UserId == userId)
            .ToListAsync();
    }

    public async Task AddToCartAsync(string userId, CartItem item)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Если пользователь не авторизован, добавляем товар в сессию
            _sessionCartService.AddCartItem(item);
            return;
        }

        // Попытка найти существующий элемент в корзине
        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(cartItem => cartItem.UserId == userId && cartItem.ProductId == item.ProductId);

        if (existingItem != null)
        {
            // Если товар уже в корзине, увеличиваем его количество
            existingItem.Quantity += item.Quantity;
        }
        else
        {
            // Если товара в корзине нет, добавляем его
            item.UserId = userId;
            await _context.CartItems.AddAsync(item);
        }

        // Сохраняем изменения в базе данных
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Логирование ошибки или обработка исключения
            throw new InvalidOperationException("Ошибка при сохранении данных в базу", ex);
        }
    }

    public async Task UpdateCartItemQuantityAsync(string userId, int productId, int newQuantity)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Если пользователь не авторизован, обновляем элемент в сессии
            _sessionCartService.UpdateCartItem(productId, newQuantity);
            return;
        }

        var item = await _context.CartItems
            .FirstOrDefaultAsync(cartItem => cartItem.UserId == userId && cartItem.ProductId == productId);

        if (item != null)
        {
            item.Quantity = newQuantity;
            if (item.Quantity <= 0)
            {
                _context.CartItems.Remove(item);
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveFromCartAsync(string userId, int productId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Если пользователь не авторизован, удаляем элемент из сессии
            _sessionCartService.RemoveCartItem(productId);
            return;
        }

        var item = await _context.CartItems
            .FirstOrDefaultAsync(cartItem => cartItem.UserId == userId && cartItem.ProductId == productId);

        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    public async Task ClearCartAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            // Если пользователь не авторизован, очищаем корзину в сессии
            _sessionCartService.ClearCart();
            return;
        }

        var items = await _context.CartItems
            .Where(cartItem => cartItem.UserId == userId)
            .ToListAsync();

        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync();
    }
}

public class AuthOptions
{
    public const string ISSUER = "MyAuthServer"; // Издатель токена
    public const string AUDIENCE = "MyAuthClient"; // Потребитель токена
    const string KEY = "mysupersecret_secretsecretkey123"; // Ключ для шифрования

    public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
}

