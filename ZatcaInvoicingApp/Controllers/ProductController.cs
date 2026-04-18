using Microsoft.AspNetCore.Mvc;
using ZatcaInvoicingApp.Models;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public ProductController(ApplicationDbContext context) => _context = context;

    [HttpPost]
    public async Task<IActionResult> AddProduct([FromBody] Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return Ok(product);
    }
}