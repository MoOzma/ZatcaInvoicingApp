using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
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
            // 1. جلب آخر فاتورة مسجلة لجلب الـ Hash السابق
            var lastInvoice = await _context.Invoices
                .OrderByDescending(i => i.InvoiceID)
                .FirstOrDefaultAsync();

            invoice.PreviousInvoiceHash = lastInvoice?.CurrentHash ?? "0";

            // 2. توليد البيانات الأساسية
            invoice.UUID = Guid.NewGuid();
            invoice.IssueDate = DateTime.UtcNow;

            // 3. حساب القيم المالية بناءً على الأصناف (Details)
            // ملاحظة: تأكد أنك ترسل قائمة Details في الـ JSON
            decimal subTotal = 0;
            if (invoice.Details != null && invoice.Details.Any())
            {
                foreach (var detail in invoice.Details)
                {
                    var product = await _context.Products.FindAsync(detail.ProductID);
                    if (product != null)
                    {
                        detail.LineTotal = product.UnitPrice * detail.Quantity;
                        subTotal += detail.LineTotal;
                    }
                }
            }
            else
            {
                // في حالة كنت ترسل الإجمالي مباشرة للتجربة فقط
                subTotal = invoice.TotalExcludingVAT;
            }

            invoice.TotalExcludingVAT = subTotal;
            invoice.TotalVAT = subTotal * 0.15m;
            invoice.TotalIncludingVAT = subTotal + invoice.TotalVAT;

            // 4. السطر الناقص: استدعاء ميثود الـ Hashing وحفظ النتيجة
            // لازم نستدعيها بعد حساب كل القيم المالية وقبل الـ Save
            invoice.CurrentHash = GenerateInvoiceHash(invoice);
            invoice.QRCode = GenerateZatcaQrCode(invoice);
            // 5. حفظ في قاعدة البيانات
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "تم حفظ الفاتورة بنجاح",
                invoiceId = invoice.InvoiceID,
                hashGenerated = invoice.CurrentHash // عشان تتأكد في الـ Swagger إن الهاش طلع
            });
        }
        catch (Exception ex)
        {
            // دي هتطلع لك السبب الحقيقي من SQL مباشرة (مثلاً: نص طويل جداً أو عمود ناقص)
            var innerError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            return BadRequest($"خطأ قاعدة البيانات: {innerError}");
        }
    }




    // أضف هذه الميثود في أسفل الكلاس
    private string GenerateInvoiceHash(Invoice invoice)
{
    // بنجمع الداتا اللي عايزين نحميها من التلاعب
    // الترتيب والبيانات دي مهمة جداً لـ ZATCA
    string rawData = $"{invoice.InvoiceNumber}|{invoice.IssueDate:yyyy-MM-ddTHH:mm:ss}|" +
                     $"{invoice.TotalIncludingVAT}|{invoice.TotalVAT}|{invoice.PreviousInvoiceHash}";

    using (SHA256 sha256 = SHA256.Create())
    {
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        return Convert.ToBase64String(bytes);
    }
}

    private string GenerateZatcaQrCode(Invoice invoice)
    {
        // تحويل البيانات لـ TLV Tags
        byte[] sellerNameBuf = GetTlv(1, invoice.CustomerName ?? "Default Seller");
        byte[] vatNumBuf = GetTlv(2, invoice.CustomerTaxNumber ?? "300000000000003");
        byte[] timeBuf = GetTlv(3, invoice.IssueDate.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        byte[] totalBuf = GetTlv(4, invoice.TotalIncludingVAT.ToString("F2"));
        byte[] taxBuf = GetTlv(5, invoice.TotalVAT.ToString("F2"));
        byte[] hashBuf = GetTlv(6, invoice.CurrentHash ?? "");

        // دمج كل الـ Buffers في مصفوفة واحدة
        List<byte> fullQrBuffer = new List<byte>();
        fullQrBuffer.AddRange(sellerNameBuf);
        fullQrBuffer.AddRange(vatNumBuf);
        fullQrBuffer.AddRange(timeBuf);
        fullQrBuffer.AddRange(totalBuf);
        fullQrBuffer.AddRange(taxBuf);
        fullQrBuffer.AddRange(hashBuf);

        return Convert.ToBase64String(fullQrBuffer.ToArray());
    }

    private byte[] GetTlv(int tag, string value)
    {
        byte[] tagBuf = { (byte)tag };
        byte[] valBuf = Encoding.UTF8.GetBytes(value);
        byte[] lenBuf = { (byte)valBuf.Length };

        byte[] result = new byte[tagBuf.Length + lenBuf.Length + valBuf.Length];
        Buffer.BlockCopy(tagBuf, 0, result, 0, tagBuf.Length);
        Buffer.BlockCopy(lenBuf, 0, result, tagBuf.Length, lenBuf.Length);
        Buffer.BlockCopy(valBuf, 0, result, tagBuf.Length + lenBuf.Length, valBuf.Length);
        return result;
    }///jkjkkj


    [HttpGet("validate-chain")]
    public async Task<IActionResult> ValidateInvoicesChain()
    {
        var invoices = await _context.Invoices.OrderBy(i => i.InvoiceID).ToListAsync();
        var report = new List<string>();

        // هذا المتغير سيحمل الهاش الخاص بالفاتورة السابقة في كل لفة
        string lastSeenHash = "0";

        foreach (var inv in invoices)
        {
            // 1. التحقق من الربط بالسلسلة
            // لو دي أول فاتورة (Previous == "0") والـ lastSeenHash لسه "0"، هنعديها
            if (inv.PreviousInvoiceHash != lastSeenHash)
            {
                return BadRequest($"الفاتورة رقم {inv.InvoiceNumber} لا ترتبط بالتي قبلها. المتوقع: {lastSeenHash} لكن الوجد: {inv.PreviousInvoiceHash}");
            }

            // 2. التحقق من سلامة بيانات الفاتورة نفسها (التلاعب الداخلي)
            string recalculatedHash = GenerateInvoiceHash(inv);
            if (inv.CurrentHash != recalculatedHash)
            {
                return BadRequest($"تنبيه! بيانات الفاتورة {inv.InvoiceNumber} تم تعديلها يدوياً والهاش لم يعد متطابقاً.");
            }

            // تحديث الهاش "الأخير" ليكون هو الهاش الحالي للفاتورة دي، عشان الفاتورة اللي بعدها تقارن بيه
            lastSeenHash = inv.CurrentHash;
            report.Add($"الفاتورة {inv.InvoiceNumber}: سليمة وموثقة ✅");
        }

        return Ok(new { Status = "Chain is secure", TotalChecked = invoices.Count, Details = report });
    } 
}