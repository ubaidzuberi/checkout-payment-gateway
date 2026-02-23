using System.ComponentModel.DataAnnotations;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Tests.Unit;

public class PaymentServiceTests
{
    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var validationResults = ValidateModel(request);
        Assert.Empty(validationResults);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345678901234567890")]
    [InlineData("abcd5678901234")]
    public void InvalidCardNumber_FailsValidation(string cardNumber)
    {
        var request = new PostPaymentRequest
        {
            CardNumber = cardNumber,
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var validationResults = ValidateModel(request);
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("CardNumber"));
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12345")]
    [InlineData("abc")]
    public void InvalidCvv_FailsValidation(string cvv)
    {
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 100,
            Cvv = cvv
        };

        var validationResults = ValidateModel(request);
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Cvv"));
    }

    [Theory]
    [InlineData("gbp")]
    [InlineData("PKR")]
    public void InvalidCurrency_FailsValidation(string currency)
    {
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = currency,
            Amount = 100,
            Cvv = "123"
        };

        var validationResults = ValidateModel(request);
        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => v.MemberNames.Contains("Currency"));
    }

    [Fact]
    public void InvalidExpiry_FailsValidation()
    {
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 1,
            ExpiryYear = 2020,
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var validationResults = ValidateModel(request);

        Assert.NotEmpty(validationResults);
        Assert.Contains(validationResults, v => 
        v.MemberNames.Contains("ExpiryMonth") || v.MemberNames.Contains("ExpiryYear"));
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        
        if (model is IValidatableObject validatableModel)
        {
            validationResults.AddRange(validatableModel.Validate(validationContext));
        }
        
        return validationResults;
    }
}
