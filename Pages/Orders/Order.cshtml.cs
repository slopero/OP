using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

public class OrderModel : PageModel
{

    public class OrderViewModel
    {
        public List<CartItem> CartItems { get; set; }
        public string PaymentType { get; set; } // ����� ���� ������
        public string DeliveryType { get; set; } // ����� ���� ��������
        public string Comment { get; set; } // ����������� ������������
        public decimal TotalAmount { get; set; } // ����� ����� ������
    }

    private readonly ICartService _cartService;
    private readonly ISessionCartService _sessionCartService;
    private readonly ApplicationDbContext _context; // ��� ������ � ����� ������

    public OrderModel(ICartService cartService, ISessionCartService sessionCartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _sessionCartService = sessionCartService;
        _context = context; // �������������� �������� ���� ������
    }

    [BindProperty]
    public List<CartItem> CartItems { get; set; }

    [BindProperty]
    public string PaymentType { get; set; }

    [BindProperty]
    public string DeliveryType { get; set; }

    [BindProperty]
    public string Comment { get; set; }

    [BindProperty]
    public decimal TotalAmount => CartItems?.Sum(item => item.TotalPrice) ?? 0;

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        CartItems = userId != null ? await _cartService.GetCartItemsAsync(userId) : _sessionCartService.GetCartItems();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            // ��������� ��� ���������������� �������������, ���� �����
            return RedirectToPage("/Index");
        }

        // �������� ������ ������
        var order = new Order
        {
            UserId = userId,
            PaymentType = PaymentType,
            DeliveryType = DeliveryType,
            Comment = Comment,
            TotalAmount = TotalAmount,
            OrderItems = CartItems.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };

        // ���������� ������ � ���� ������
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // ������� ������� ����� ���������� ������
        await _cartService.ClearCartAsync(userId);
        return RedirectToPage("OrderConfirmation"); // ��������������� �� �������� �������������
    }
}
