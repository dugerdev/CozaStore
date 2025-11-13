using CozaStore.Business.Contracts;
using CozaStore.Core.DTOs;
using CozaStore.Entities.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CozaStoreWebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartItemsController : ControllerBase
{
    private readonly ICartItemService _cartItemService;
    private readonly IProductService _productService;

    public CartItemsController(ICartItemService cartItemService, IProductService productService)
    {
        _cartItemService = cartItemService;
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _cartItemService.GetByUserAsync(userId);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        var cartItems = new List<CartItemDto>();
        foreach (var item in result.Data)
        {
            var productResult = await _productService.GetByIdAsync(item.ProductId);
            if (productResult.Success && productResult.Data != null)
            {
                var product = productResult.Data;
                cartItems.Add(new CartItemDto
                {
                    Id = item.Id,
                    UserId = item.UserId,
                    ProductId = item.ProductId,
                    ProductName = product.Name,
                    ProductPrice = product.Price,
                    ProductImageUrl = product.ImageUrl,
                    Quantity = item.Quantity,
                    SubTotal = product.Price * item.Quantity
                });
            }
        }

        return Ok(cartItems);
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        // Ürün kontrolü
        var productResult = await _productService.GetByIdAsync(request.ProductId);
        if (!productResult.Success || productResult.Data == null)
        {
            return BadRequest(new { message = "Ürün bulunamadı." });
        }

        var cartItem = new CartItem
        {
            UserId = userId,
            ProductId = request.ProductId,
            Quantity = request.Quantity
        };

        var result = await _cartItemService.AddAsync(cartItem);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = "Ürün sepete eklendi." });
    }

    [HttpPut("{id}/quantity")]
    public async Task<IActionResult> UpdateQuantity(Guid id, [FromBody] UpdateCartQuantityRequestDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _cartItemService.UpdateQuantityAsync(id, request.Quantity);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = "Miktar güncellendi." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Remove(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _cartItemService.RemoveAsync(id);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = "Ürün sepetten çıkarıldı." });
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _cartItemService.ClearAsync(userId);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new { message = "Sepet temizlendi." });
    }
}


