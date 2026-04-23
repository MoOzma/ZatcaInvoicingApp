using System.ComponentModel.DataAnnotations;
using ZatcaInvoicingApp.Models;

public class Invoice
{
    public int InvoiceID { get; set; }

    [Required]
    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    // حقول ZATCA للمرحلة الثانية
    public Guid UUID { get; set; } = Guid.NewGuid();
    public string? PreviousInvoiceHash { get; set; }
    public string? CurrentHash { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerTaxNumber { get; set; }

    public decimal TotalExcludingVAT { get; set; }
    public decimal TotalVAT { get; set; }
    public decimal TotalIncludingVAT { get; set; }
    public string? QRCode { get; set; }

    // علاقة مع تفاصيل الفاتورة
    public List<InvoiceDetail> Details { get; set; } = new();
    public bool IsDeleted { get; set; } = false;
}