using ARCRetailPOE.Models;
using ARCRetailPOE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace ARCRetailPOE.Controllers
{
    public class OrderController : Controller
    {
        private readonly QueueStorageService _queueService;
        private readonly OrderService _orderService;

        public OrderController(QueueStorageService queueService, OrderService orderService)
        {
            _queueService = queueService;
            _orderService = orderService;
        }

        [Authorize(Roles = "Customer")]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "Customer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please fill in all required fields.";
                return View(order);
            }

            try
            {
                var email = User.FindFirstValue(ClaimTypes.Name);
                order.CustomerEmail = email;
                order.OrderDate = DateTime.UtcNow;
                order.Total = order.Price * order.Quantity;
                order.Status = "Pending";

                await _orderService.AddOrderAsync(order);
                await _queueService.SendMessageAsync($"Order placed by {email} for product {order.ProductName}");

                ViewBag.Message = "Order placed successfully!";
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                return View(order);
            }
        }

        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyOrders()
        {
            var email = User.FindFirstValue(ClaimTypes.Name);
            var orders = await _orderService.GetOrdersByCustomerAsync(email);
            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return View(orders);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            if (string.IsNullOrEmpty(newStatus))
            {
                TempData["ErrorMessage"] = "Please select a valid status.";
                return RedirectToAction("Manage");
            }

            await _orderService.UpdateOrderStatusAsync(orderId, newStatus);
            await _queueService.SendMessageAsync($"Order #{orderId} updated to {newStatus}");

            TempData["Message"] = $"Order #{orderId} status updated successfully.";
            return RedirectToAction("Manage");
        }
    }
}

