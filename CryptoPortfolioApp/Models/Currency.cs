using System.ComponentModel.DataAnnotations;

namespace CryptoPortfolioApp.Models
{
    public class Currency
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        [Display(Name = "Owner Name")]
        public string OwnerName { get; set; } = string.Empty;
        public decimal Value { get; set; } = decimal.Zero;
        public decimal Quantity { get; set; } = decimal.Zero;
        [Display(Name = "Portfolio Value")]
        public decimal PortfolioValue { get; set; } = decimal.Zero;
        [Display(Name = "Daily Change")]
        public decimal DailyChange { get; set; } = decimal.Zero;
    }
}
