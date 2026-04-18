using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZatcaInvoicingApp.Models; // تأكد من مطابقة اسم الـ Namespace

[Route("api/[controller]")]
[ApiController]
public class InvoiceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public InvoiceController(ApplicationDbContext context)
    {
        _context = context;
    }

    // POST: api/invoice
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] Invoice invoice)
    {
        try
        {
            // 1. حساب القيم الضريبية (منطق بسيط مؤقتاً)
            invoice.TotalVAT = invoice.TotalExcludingVAT * 0.15m;
            invoice.TotalIncludingVAT = invoice.TotalExcludingVAT + invoice.TotalVAT;

            // 2. توليد البيانات المطلوبة للمرحلة الثانية
            invoice.UUID = Guid.NewGuid();
            invoice.IssueDate = DateTime.UtcNow;

            // 3. حفظ في قاعدة البيانات
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حفظ الفاتورة بنجاح", invoiceId = invoice.InvoiceID });
        }
        catch (Exception ex)
        {
            return BadRequest($"خطأ أثناء الحفظ: {ex.Message}");
        }
    }
}