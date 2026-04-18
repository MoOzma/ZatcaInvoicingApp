using System.ComponentModel.DataAnnotations;

namespace ZatcaInvoicingApp.Models
{
    public class Product
    {
        [Key]
        public int ProductID { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        public decimal UnitPrice { get; set; }

        // نسبة الضريبة (افتراضياً 15% للسعودية)
        public decimal TaxRate { get; set; } = 15.00m;
    }
}