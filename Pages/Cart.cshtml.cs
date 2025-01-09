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
    private readonly ISessionCartService _sessionCartService; // ��������� ���� ��� ���������� �������

    public CartModel(ICartService cartService, ISessionCartService sessionCartService) // ��������� ����������� � �����������
    {
        _cartService = cartService;
        _sessionCartService = sessionCartService; // �������������� ����
    }

    [BindProperty]
    public List<CartItem> CartItems { get; set; } = new();

    [BindProperty]
    public decimal CartTotal => CartItems?.Sum(item => item.TotalPrice) ?? 0;

    public async Task<IActionResult> OnGetAsync()
    {
        // �������� ������������� ������������ �� ��������� ��������������
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ��������� ������ ��� ������� ������������
        CartItems = userId != null ? await _cartService.GetCartItemsAsync(userId) : _sessionCartService.GetCartItems();
        return Page();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // ���� ������������ �� �����������, ��������� ����� � ������
            var newItem = new CartItem { ProductId = productId, Quantity = quantity };
            _sessionCartService.AddCartItem(newItem);
            TempData["Notification"] = "����� �������� � �������!";
        }
        else
        {
            var newItem = new CartItem { ProductId = productId, Quantity = quantity };
            await _cartService.AddToCartAsync(userId, newItem);
            TempData["Notification"] = "����� �������� � �������!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int productId, int newQuantity)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // ���� ������������ �� �����������, ��������� ������� � ������
            _sessionCartService.UpdateCartItem(productId, newQuantity);
            TempData["Notification"] = "���������� ������ ���������!";
        }
        else
        {
            await _cartService.UpdateCartItemQuantityAsync(userId, productId, newQuantity);
            TempData["Notification"] = "���������� ������ ���������!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveItemAsync(int productId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // ���� ������������ �� �����������, ������� ������� �� ������
            _sessionCartService.RemoveCartItem(productId);
            TempData["Notification"] = "����� ������ �� �������!";
        }
        else
        {
            await _cartService.RemoveFromCartAsync(userId, productId);
            TempData["Notification"] = "����� ������ �� �������!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearCartAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            // ���� ������������ �� �����������, ������� ������� � ������
            _sessionCartService.ClearCart();
            TempData["Notification"] = "������� �������!";
        }
        else
        {
            await _cartService.ClearCartAsync(userId);
            TempData["Notification"] = "������� �������!";
        }

        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return RedirectToPage("/Orders/Order"); 
    }
}
