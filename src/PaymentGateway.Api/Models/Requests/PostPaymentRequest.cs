using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Responses;

public class PostPaymentRequest : IValidatableObject
{
    [Required]
    [RegularExpression(@"^\d{14,19}$", ErrorMessage = "Card number must be 14-19 digits")]
    public string CardNumber { get; set; } = string.Empty;
    
    [Required]
    [Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12")]
    public int ExpiryMonth { get; set; }
    
    [Required]
    [Range(1999, 2099, ErrorMessage = "Expiry year is not valid")]
    public int ExpiryYear { get; set; }   
    
    [Required]
    [RegularExpression(@"^(USD|EUR|GBP)$", ErrorMessage = "Currency must be USD, EUR, or GBP")]
    public string Currency { get; set; } = string.Empty;
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public int Amount { get; set; }
    
    [Required]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3-4 digits")]
    public string Cvv { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var expiry = new DateTime(ExpiryYear, ExpiryMonth, 1)
            .AddMonths(1)
            .AddDays(-1);

        if (expiry < DateTime.UtcNow)
        {
            yield return new ValidationResult(
                "Card has expired",
                new[] { nameof(ExpiryMonth), nameof(ExpiryYear) });
        }
    }
}