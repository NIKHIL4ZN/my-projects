using ARCRetailPOE.Models;
using ARCRetailPOE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ARCRetailPOE.Helpers;


namespace ARCRetailPOE.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly ProductTableService _productService;

        public CartController(ProductTableService productService)
        {
            _productService = productService;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> AddToCart(string id)
        {
            var product = await _productService.GetProductAsync(id);
            if (product == null) return NotFound();

            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var existing = cart.FirstOrDefault(c => c.ProductId == id);

            if (existing != null)
            {
                existing.Quantity++;
                TempData["Message"] = $"✅ Increased quantity of '{product.Name}' in your cart.";
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = id,
                    ProductName = product.Name,
                    Price = (decimal)product.Price,
                    Quantity = 1
                });

                TempData["Message"] = $"✅ '{product.Name}' added to your cart!";
            }

            HttpContext.Session.SetObjectAsJson("Cart", cart);

            return RedirectToAction("Index", "Product");
        }

        public IActionResult Remove(string id)
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var item = cart.FirstOrDefault(c => c.ProductId == id);
            if (item != null)
                cart.Remove(item);

            HttpContext.Session.SetObjectAsJson("Cart", cart);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(
    [FromServices] OrderService orderService,
    [FromServices] QueueStorageService queueService) 
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("Cart");
            if (cart == null || !cart.Any())
                return RedirectToAction(nameof(Index));

            string email = User.Identity?.Name ?? "guest@example.com";

            foreach (var item in cart)
            {
                var order = new Order
                {
                    CustomerEmail = email,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Total = item.Total,
                    Status = "Pending",
                    OrderDate = DateTime.UtcNow
                };

                await orderService.AddOrderAsync(order);
            }

            string message = $"🛒 New order placed by {email} with {cart.Count} items at {DateTime.UtcNow}.";
            try
            {
                await queueService.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Queue error: {ex.Message}");
            }

            HttpContext.Session.Remove("Cart");

            TempData["Message"] = "✅ Your order has been placed successfully and is being processed.";
            return RedirectToAction("MyOrders", "Order");
        }
    }
}

