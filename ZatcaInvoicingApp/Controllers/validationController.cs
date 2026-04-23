using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace ZatcaInvoicingApp.Controllers
{
    public class validationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public validationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private string GenerateInvoiceHash(Invoice invoice)
        {
            // 1. توحيد تنسيق التاريخ (بدون أجزاء من الثانية)
            string formattedDate = invoice.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss");

            // 2. توحيد الأرقام العشرية (رقمين بعد العلامة دائماً)
            string totalStr = invoice.TotalIncludingVAT.ToString("F2");
            string vatStr = invoice.TotalVAT.ToString("F2");

            // 3. تجميع السلسلة النصية بنفس الترتيب
            string rawData = $"{invoice.InvoiceNumber}|{formattedDate}|{totalStr}|{vatStr}|{invoice.PreviousInvoiceHash}";

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                return Convert.ToBase64String(bytes);
            }
        }

        [HttpGet("validate-chain")]
        public async Task<IActionResult> ValidateInvoicesChain()
        {
            var invoices = await _context.Invoices.OrderBy(i => i.InvoiceID).ToListAsync();
            var report = new List<string>();

            // البداية الافتراضية
            string lastSeenHash = "0";

            foreach (var inv in invoices)
            {
                // تنظيف القيم من أي فراغات جانبية عشان المقارنة تنجح 100%
                string dbPreviousHash = inv.PreviousInvoiceHash?.Trim() ?? "0";

                // 1. التحقق من الربط
                if (dbPreviousHash != lastSeenHash)
                {
                    return BadRequest($"الفاتورة رقم {inv.InvoiceNumber} لا ترتبط بالتي قبلها. المتوقع: {lastSeenHash} لكن الموجود: {dbPreviousHash}");
                }

                // 2. التحقق من سلامة البيانات الداخلية (التلاعب)
                string recalculatedHash = GenerateInvoiceHash(inv);
                if (inv.CurrentHash != recalculatedHash)
                {
                    return BadRequest($"تنبيه! بيانات الفاتورة {inv.InvoiceNumber} تم تعديلها يدوياً.");
                }

                lastSeenHash = inv.CurrentHash;
                report.Add($"الفاتورة {inv.InvoiceNumber}: سليمة وموثقة ✅");
            }

            return Ok(new { Status = "Chain is secure", Details = report });
        }
    }
}
