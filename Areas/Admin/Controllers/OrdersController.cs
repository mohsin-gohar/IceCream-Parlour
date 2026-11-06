using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Helpers;
using Ice_Cream_Parlour_Eproject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ice_Cream_Parlour_Eproject.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = AppRoles.AllStaff)]
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, OrderStatus? status, int page = 1)
    {
        ViewData["Title"] = "Orders";
        return View(await _orderService.GetPagedAsync(search, status, page, 10));
    }

    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "New Order";
        return View(await _orderService.GetCreateModelAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        model.Lines ??= new List<OrderLineDto>();

        if (!ModelState.IsValid)
        {
            var fresh = await _orderService.GetCreateModelAsync();
            model.Customers = fresh.Customers;
            model.Products = fresh.Products;
            ViewData["Title"] = "New Order";
            return View(model);
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var orderId = await _orderService.CreateAsync(model, userId);

        if (orderId == 0)
        {
            ModelState.AddModelError(string.Empty,
                "The order could not be created. Please check the customer, products and available stock.");
            var fresh = await _orderService.GetCreateModelAsync();
            model.Customers = fresh.Customers;
            model.Products = fresh.Products;
            ViewData["Title"] = "New Order";
            return View(model);
        }

        TempData["Success"] = "Order created successfully.";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _orderService.GetByIdAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Order {model.OrderNumber}";
        return View(model);
    }

    public async Task<IActionResult> Invoice(int id)
    {
        var model = await _orderService.GetByIdAsync(id);
        if (model == null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"Invoice {model.OrderNumber}";
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus orderStatus, PaymentStatus paymentStatus, string? notes)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await _orderService.UpdateStatusAsync(id, orderStatus, paymentStatus, userId, notes);

        _logger.LogInformation("Order {OrderId} updated by {UserId}.", id, userId);
        TempData["Success"] = "Order status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
