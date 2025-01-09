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
        public string PaymentType { get; set; } // Выбор типа оплаты
        public string DeliveryType { get; set; } // Выбор типа доставки
        public string Comment { get; set; } // Комментарий пользователя
        public decimal TotalAmount { get; set; } // Общая сумма заказа
    }

    private readonly ICartService _cartService;
    private readonly ISessionCartService _sessionCartService;
    private readonly ApplicationDbContext _context; // Для работы с базой данных

    public OrderModel(ICartService cartService, ISessionCartService sessionCartService, ApplicationDbContext context)
    {
        _cartService = cartService;
        _sessionCartService = sessionCartService;
        _context = context; // Инициализируем контекст базы данных
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
            // Обработка для неавторизованных пользователей, если нужно
            return RedirectToPage("/Index");
        }

        // Создание нового заказа
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

        // Сохранение заказа в базе данных
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Очистка корзины после оформления заказа
        await _cartService.ClearCartAsync(userId);
        return RedirectToPage("OrderConfirmation"); // Перенаправление на страницу подтверждения
    }
}
