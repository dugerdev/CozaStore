using CozaStore.Business.Contracts;
using CozaStore.Core.DTOs;
using CozaStore.Core.Utilities.Results;
using CozaStore.Entities.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CozaStoreWebAPI.Controllers;

/// <summary>
/// Ürün işlemlerini yöneten API controller'ı.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// GET /api/products
    /// Tüm aktif ürünleri listeler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _productService.GetAllAsync();
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        var products = result.Data.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.StockQuantity,
            ImageUrl = p.ImageUrl,
            CategoryId = p.CategoryId,
            CreatedDate = p.CreatedDate,
            IsActive = p.IsActive
        }).ToList();

        return Ok(products);
    }

    /// <summary>
    /// GET /api/products/{id}
    /// Belirli bir ürünü ID'ye göre getirir.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _productService.GetByIdAsync(id);
        
        if (!result.Success || result.Data == null)
        {
            return NotFound(new { message = result.Message });
        }

        var product = result.Data;
        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            CategoryId = product.CategoryId,
            CreatedDate = product.CreatedDate,
            IsActive = product.IsActive
        };

        return Ok(productDto);
    }

    /// <summary>
    /// GET /api/products/category/{categoryId}
    /// Belirli bir kategoriye ait ürünleri listeler.
    /// </summary>
    [HttpGet("category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(Guid categoryId)
    {
        var result = await _productService.GetByCategoryAsync(categoryId);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        var products = result.Data.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Stock = p.StockQuantity,
            ImageUrl = p.ImageUrl,
            CategoryId = p.CategoryId,
            CreatedDate = p.CreatedDate,
            IsActive = p.IsActive
        }).ToList();

        return Ok(products);
    }

    /// <summary>
    /// POST /api/products
    /// Yeni ürün oluşturur. (Admin yetkisi gerekli)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        var result = await _productService.AddAsync(product);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// PUT /api/products/{id}
    /// Mevcut ürünü günceller. (Admin yetkisi gerekli)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Product product)
    {
        if (id != product.Id)
        {
            return BadRequest(new { message = "ID uyuşmazlığı." });
        }

        var result = await _productService.UpdateAsync(product);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return NoContent();
    }

    /// <summary>
    /// DELETE /api/products/{id}
    /// Ürünü soft delete yapar. (Admin yetkisi gerekli)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _productService.DeleteAsync(id);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return NoContent();
    }
}


