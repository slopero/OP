using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class CartModel : PageModel
{
    private readonly ICartService _cartService;
    private readonly ISessionCartService _sessionCartService; // Добавляем поле для сессионной корзины

    public CartModel(ICartService cartService, ISessionCartService sessionCartService) // Добавляем зависимость в конструктор
    {
        _cartService = cartService;
        _sessionCartService = sessionCartService; // Инициализируем поле
    }

    [BindProperty]
    public List<CartItem> CartItems { get; set; } = new();

    [BindProperty]
    public decimal CartTotal => CartItems?.Sum(item => item.TotalPrice) ?? 0;

    public async Task<IActionResult> OnGetAsync()
    {
        // Получаем идентификатор пользователя из контекста аутентификации
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Загружаем товары для данного пользователя
        CartItems = userId != null ? await _cartService.GetCartItemsAsync(userId) : _sessionCartService.GetCartItems();
        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // Если пользователь не авторизован, добавляем товар в сессию
            var newItem = new CartItem { ProductId = productId, Quantity = quantity };
            _sessionCartService.AddCartItem(newItem);
            TempData["Notification"] = "Товар добавлен в корзину!";
        }
        else
        {
            var newItem = new CartItem { ProductId = productId, Quantity = quantity };
            await _cartService.AddToCartAsync(userId, newItem);
            TempData["Notification"] = "Товар добавлен в корзину!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int newQuantity)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // Если пользователь не авторизован, обновляем элемент в сессии
            _sessionCartService.UpdateCartItem(productId, newQuantity);
            TempData["Notification"] = "Количество товара обновлено!";
        }
        else
        {
            await _cartService.UpdateCartItemQuantityAsync(userId, productId, newQuantity);
            TempData["Notification"] = "Количество товара обновлено!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveItemAsync(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // Если пользователь не авторизован, удаляем элемент из сессии
            _sessionCartService.RemoveCartItem(productId);
            TempData["Notification"] = "Товар удален из корзины!";
        }
        else
        {
            await _cartService.RemoveFromCartAsync(userId, productId);
            TempData["Notification"] = "Товар удален из корзины!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearCartAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // Если пользователь не авторизован, очищаем корзину в сессии
            _sessionCartService.ClearCart();
            TempData["Notification"] = "Корзина очищена!";
        }
        else
        {
            await _cartService.ClearCartAsync(userId);
            TempData["Notification"] = "Корзина очищена!";
        }

        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return RedirectToPage("/Orders/Order"); 
    }
}
