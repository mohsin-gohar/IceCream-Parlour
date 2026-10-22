using Ice_Cream_Parlour_Eproject.Areas.Models;
using Ice_Cream_Parlour_Eproject.Areas.Models.Enums;
using Ice_Cream_Parlour_Eproject.Areas.Models.ViewModels;
using Ice_Cream_Parlour_Eproject.Data;
using Ice_Cream_Parlour_Eproject.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Ice_Cream_Parlour_Eproject.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<OrderListViewModel> GetPagedAsync(string? search, OrderStatus? status, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Orders.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(o =>
                    o.OrderNumber.Contains(term) ||
                    o.CustomerName.Contains(term) ||
                    o.CustomerEmail.Contains(term));
            }

            if (status.HasValue)
            {
                var statusValue = status.Value.ToString();
                query = query.Where(o => o.OrderStatus == statusValue);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    OrderDate = o.OrderDate,
                    CustomerId = o.CustomerId,
                    CustomerName = o.CustomerName,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    Notes = o.Notes
                })
                .ToListAsync();

            return new OrderListViewModel
            {
                Orders = new PaginatedList<OrderViewModel>
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total
                },
                SearchTerm = search,
                StatusFilter = status
            };
        }

        public async Task<OrderViewModel?> GetByIdAsync(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return null;
            }

            var items = order.OrderItems
                .Select(i => new OrderItemDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    LineTotal = i.LineTotal
                })
                .ToList();

            var subTotal = items.Count > 0 ? items.Sum(i => i.LineTotal) : order.TotalAmount;

            return new OrderViewModel
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName,
                SubTotal = subTotal,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                OrderStatus = order.OrderStatus,
                Notes = order.Notes,
                Items = items
            };
        }

        public async Task<OrderCreateViewModel> GetCreateModelAsync()
        {
            return new OrderCreateViewModel
            {
                Customers = await _context.Customers
                    .AsNoTracking()
                    .OrderBy(c => c.FullName)
                    .Select(c => new CustomerOrderSelectDto
                    {
                        Id = c.Id,
                        FullName = c.FullName,
                        Email = c.Email
                    })
                    .ToListAsync(),

                Products = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Status == ProductStatus.Active && p.StockQuantity > 0)
                    .OrderBy(p => p.Name)
                    .Select(p => new ProductOrderSelectDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        FinalPrice = p.Price - (p.Price * p.DiscountPercent / 100m),
                        StockQuantity = p.StockQuantity
                    })
                    .ToListAsync(),

                Lines = new List<OrderLineDto> { new() }
            };
        }

        public async Task<int> CreateAsync(OrderCreateViewModel model, string? userId)
        {
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == model.CustomerId);
            if (customer == null)
            {
                return 0;
            }

            var requestedLines = model.Lines
                .Where(l => l.ProductId > 0 && l.Quantity > 0)
                .GroupBy(l => l.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            if (requestedLines.Count == 0)
            {
                return 0;
            }

            var productIds = requestedLines.Select(l => l.ProductId).ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.Status == ProductStatus.Active)
                .ToListAsync();

            if (products.Count != productIds.Count)
            {
                return 0;
            }

            var orderItems = new List<OrderItem>();
            foreach (var line in requestedLines)
            {
                var product = products.First(p => p.Id == line.ProductId);
                if (product.StockQuantity < line.Quantity)
                {
                    return 0; // not enough stock
                }

                var unitPrice = product.Price - (product.Price * product.DiscountPercent / 100m);
                orderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = line.Quantity,
                    UnitPrice = Math.Round(unitPrice, 2)
                });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    CustomerId = customer.Id,
                    CustomerName = customer.FullName,
                    CustomerEmail = customer.Email,
                    CustomerPhone = customer.Phone,
                    DeliveryAddress = customer.Address,
                    OrderNumber = await OrderNumberHelper.GenerateUniqueAsync(
                        number => _context.Orders.AnyAsync(o => o.OrderNumber == number)),
                    OrderDate = DateTime.UtcNow,
                    TotalAmount = orderItems.Sum(i => i.LineTotal),
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = PaymentStatus.Pending.ToString(),
                    OrderStatus = OrderStatus.Pending.ToString(),
                    Notes = model.Notes?.Trim()
                };

                foreach (var item in orderItems)
                {
                    order.OrderItems.Add(item);
                    var product = products.First(p => p.Id == item.ProductId);
                    product.StockQuantity -= item.Quantity;
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderNumber} created by {UserId}.", order.OrderNumber, userId ?? "system");
                return order.Id;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to create an order for customer {CustomerId}.", model.CustomerId);
                return 0;
            }
        }

        public async Task UpdateStatusAsync(int id, OrderStatus orderStatus, PaymentStatus paymentStatus, string? userId, string? notes)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return;
            }

            order.OrderStatus = orderStatus.ToString();
            order.PaymentStatus = paymentStatus.ToString();

            if (!string.IsNullOrWhiteSpace(notes))
            {
                order.Notes = notes.Trim();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {OrderId} status changed to {Status}/{Payment} by {UserId}.",
                id, order.OrderStatus, order.PaymentStatus, userId ?? "system");
        }
    }
}
