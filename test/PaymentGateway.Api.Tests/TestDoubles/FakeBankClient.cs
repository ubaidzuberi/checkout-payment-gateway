using PaymentGateway.Api.Clients;

namespace PaymentGateway.Api.Tests.TestDoubles;

public class FakeBankClient : IBankClient
{
    private readonly BankResponse? _responseToReturn;
    private readonly bool _throwServiceUnavailable;

    public int CallCount { get; private set; }

    public FakeBankClient(BankResponse? responseToReturn = null, bool throwServiceUnavailable = false)
    {
        _responseToReturn = responseToReturn;
        _throwServiceUnavailable = throwServiceUnavailable;
    }

    public Task<BankResponse?> ProcessPaymentAsync(BankRequest request)
    {
        CallCount++;

        if (_throwServiceUnavailable)
        {
            throw new BankServiceUnavailableException();
        }

        return Task.FromResult(_responseToReturn);
    }
}
