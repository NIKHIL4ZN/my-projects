using ARCRetailPOE.Models;
using ARCRetailPOE.Services;
using ARCRetailPOE.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ARCRetailPOE.Controllers
{
    [Authorize(Roles = "Customer,Admin")]
    public class ProductController : Controller
    {
        private readonly ProductTableService _productService;
        private readonly BlobStorageService _blobService;
        private readonly QueueStorageService _queueService;

        public ProductController(ProductTableService productService, BlobStorageService blobService, QueueStorageService queueService)
        {
            _productService = productService;
            _blobService = blobService;
            _queueService = queueService;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }


        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _productService.GetProductAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Price,Category")] Product product, IFormFile? imageFile)
        {
            if (!IsAdmin())
                return RedirectToAction("AccessDenied", "Login");

            ModelState.Remove("PartitionKey");
            ModelState.Remove("RowKey");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("ImageUrl");

            if (Request.Form.TryGetValue("Price", out var rawPrice))
            {
                if (decimal.TryParse(rawPrice.ToString().Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedPrice))
                {
                    product.Price = parsedPrice;
                }
                else
                {
                    ModelState.AddModelError("Price", "Invalid price format. Use numbers like 49.99");
                }
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ViewBag.ErrorMessage = "Validation failed: " + string.Join(", ", errors);
                return View(product);
            }

            try
            {
                product.RowKey = Guid.NewGuid().ToString();
                product.CreatedAt = DateTime.UtcNow;

                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ViewBag.ImageError = "Only JPG, JPEG, PNG, or GIF files are allowed.";
                        return View(product);
                    }

                    using var stream = imageFile.OpenReadStream();
                    product.ImageUrl = await _blobService.UploadFileAsync(stream, imageFile.FileName);
                    await _queueService.SendMessageAsync($"Uploading image '{imageFile.FileName}'");
                }

                await _productService.AddProductAsync(product);
                await _queueService.SendMessageAsync($"Processing product creation: {product.Name}");
                ViewBag.Message = "Product created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                return View(product);
            }
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _productService.GetProductAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("PartitionKey,RowKey,Name,Description,Price,ImageUrl,Category,CreatedAt")] Product product, IFormFile? imageFile)
        {
            if (id != product.RowKey)
                return NotFound();

            ModelState.Remove("PartitionKey");
            ModelState.Remove("RowKey");
            ModelState.Remove("ETag");
            ModelState.Remove("CreatedAt");
            ModelState.Remove("ImageUrl");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                ViewBag.ErrorMessage = "Validation failed: " + string.Join(", ", errors);
                return View(product);
            }

            try
            {
                var existing = await _productService.GetProductAsync(id);
                if (existing == null)
                    return NotFound();

                if (imageFile != null && imageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ViewBag.ImageError = "Only JPG, JPEG, PNG, or GIF files are allowed.";
                        return View(product);
                    }

                    using var stream = imageFile.OpenReadStream();
                    product.ImageUrl = await _blobService.UploadFileAsync(stream, imageFile.FileName);
                    await _queueService.SendMessageAsync($"Uploading new image '{imageFile.FileName}'");
                }
                else
                {
                    product.ImageUrl = existing.ImageUrl;
                }

                product.ETag = existing.ETag;
                product.CreatedAt = existing.CreatedAt;

                await _productService.UpdateProductAsync(product);
                await _queueService.SendMessageAsync($"Processing product update: {product.Name}");

                TempData["Message"] = "✅ Product updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"An error occurred: {ex.Message}";
                return View(product);
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var product = await _productService.GetProductAsync(id);
            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _productService.DeleteProductAsync(id);
            await _queueService.SendMessageAsync($"Processing product deletion: ID {id}");
            TempData["Message"] = "🗑️ Product deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}

