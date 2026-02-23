using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Tests.TestDoubles;

namespace PaymentGateway.Api.Tests.Integration;

public class PaymentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public PaymentsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PostPayment_ValidRequest_ReturnsAuthorized()
    {
        var bankResponse = new BankResponse { authorized = true, authorization_code = "AUTH123" };
        var client = CreateClientWithFakeBank(bankResponse);
        
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123455",
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };

        var response = await client.PostAsJsonAsync("/api/payments", request);
        var result = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Authorized, result!.Status);
        Assert.Equal("3455", result.LastFourDigits);
        Assert.Equal(1000, result.Amount);
        Assert.Equal("USD", result.Currency);
        Assert.Equal(12, result.ExpiryMonth);
        Assert.Equal(2026, result.ExpiryYear);
    }

    [Fact]
    public async Task PostPayment_ValidRequest_ReturnsDeclined()
    {
        var bankResponse = new BankResponse { authorized = false, authorization_code = null };
        var client = CreateClientWithFakeBank(bankResponse);
        
        var request = new PostPaymentRequest
        {
            CardNumber = "9876543210987654",
            ExpiryMonth = 6,
            ExpiryYear = 2027,
            Currency = "GBP",
            Amount = 500,
            Cvv = "456"
        };

        var response = await client.PostAsJsonAsync("/api/payments", request);
        var result = await response.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Declined, result!.Status);
        Assert.Equal("7654", result.LastFourDigits);
    }

    [Fact]
    public async Task PostPayment_BankUnavailable_Returns502()
    {
        var client = CreateClientWithFakeBank(null, throwServiceUnavailable: true);
        
        var request = new PostPaymentRequest
        {
            CardNumber = "1111222233334440",
            ExpiryMonth = 3,
            ExpiryYear = 2028,
            Currency = "EUR",
            Amount = 250,
            Cvv = "789"
        };

        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task PostPayment_InvalidRequest_Returns400AndBankNotCalled()
    {
        var fakeBank = new FakeBankClient(new BankResponse { authorized = true });
        var client = CreateClientWithFakeBank(fakeBank);
        
        var request = new PostPaymentRequest
        {
            CardNumber = "12345",
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 100,
            Cvv = "123"
        };

        var response = await client.PostAsJsonAsync("/api/payments", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, fakeBank.CallCount);
    }

    [Fact]
    public async Task GetPayment_ExistingPayment_Returns200WithCorrectDetails()
    {
        var bankResponse = new BankResponse { authorized = true, authorization_code = "AUTH999" };
        var client = CreateClientWithFakeBank(bankResponse);
        
        var postRequest = new PostPaymentRequest
        {
            CardNumber = "5555444433332221",
            ExpiryMonth = 9,
            ExpiryYear = 2027,
            Currency = "EUR",
            Amount = 750,
            Cvv = "321"
        };

        var postResponse = await client.PostAsJsonAsync("/api/payments", postRequest);
        var postResult = await postResponse.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);
        var paymentId = postResult!.Id;

        var getResponse = await client.GetAsync($"/api/payments/{paymentId}");
        var getResult = await getResponse.Content.ReadFromJsonAsync<PaymentResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(getResult);
        Assert.Equal(paymentId, getResult!.Id);
        Assert.Equal(PaymentStatus.Authorized, getResult.Status);
        Assert.Equal("2221", getResult.LastFourDigits);
        Assert.Equal("EUR", getResult.Currency);
        Assert.Equal(750, getResult.Amount);
    }

    [Fact]
    public async Task GetPayment_NonExistentPayment_Returns404()
    {
        var client = CreateClientWithFakeBank(new BankResponse { authorized = false });
        var nonExistentId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/payments/{nonExistentId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private HttpClient CreateClientWithFakeBank(BankResponse? bankResponse = null, bool throwServiceUnavailable = false)
    {
        return CreateClientWithFakeBank(new FakeBankClient(bankResponse, throwServiceUnavailable));
    }

    private HttpClient CreateClientWithFakeBank(FakeBankClient fakeBank)
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var bankDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBankClient));
                if (bankDescriptor != null)
                    services.Remove(bankDescriptor);

                services.AddTransient<IBankClient>(_ => fakeBank);
                
                var repoDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(PaymentsRepository));
                if (repoDescriptor != null)
                {
                    services.Remove(repoDescriptor);
                    services.AddSingleton<PaymentsRepository>();
                }
            });
        });

        return factory.CreateClient();
    }
}
