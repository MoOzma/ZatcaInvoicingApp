using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZatcaInvoicingApp.Models
{
    public class InvoiceDetail
    {
        [Key]
        public int DetailID { get; set; }

        public int InvoiceID { get; set; }

        public int ProductID { get; set; }

        public int Quantity { get; set; }

        // إجمالي السطر (الكمية × سعر الوحدة)
        public decimal LineTotal { get; set; }

        // العلاقات (Navigation Properties)
        [ForeignKey("InvoiceID")]
        public Invoice? Invoice { get; set; }

        [ForeignKey("ProductID")]
        public Product? Product { get; set; }
    }
}