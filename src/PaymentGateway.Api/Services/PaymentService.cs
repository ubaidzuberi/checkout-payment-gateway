using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositories;

namespace PaymentGateway.Api.Services;

public class PaymentService
{
    private readonly PaymentsRepository _repository;
    private readonly IBankClient _bankClient;

    public PaymentService(
        PaymentsRepository repository,
        IBankClient bankClient)
    {
        _repository = repository;
        _bankClient = bankClient;
    }

    public PaymentResponse? GetPayment(Guid id)
    {
        return _repository.Get(id);
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(PostPaymentRequest request)
    {
        var bankRequest = new BankRequest
        {
            card_number = request.CardNumber,
            expiry_date = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}",
            currency = request.Currency,
            amount = request.Amount,
            cvv = request.Cvv
        };

        var bankResponse = await _bankClient.ProcessPaymentAsync(bankRequest);

        var status = bankResponse != null && bankResponse.authorized
            ? PaymentStatus.Authorized
            : PaymentStatus.Declined;

        var payment = new PaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = status,
            LastFourDigits = request.CardNumber[^4..],
            ExpiryMonth = request.ExpiryMonth,
            ExpiryYear = request.ExpiryYear,
            Currency = request.Currency,
            Amount = request.Amount
        };

        _repository.Add(payment);

        return payment;
    }
}