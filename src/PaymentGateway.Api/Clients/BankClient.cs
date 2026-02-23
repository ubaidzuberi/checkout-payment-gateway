using System.Net;
using System.Net.Http.Json;

namespace PaymentGateway.Api.Clients;

public interface IBankClient
{
    Task<BankResponse?> ProcessPaymentAsync(BankRequest request);
}

public class BankClient : IBankClient
{
    private readonly HttpClient _httpClient;

    public BankClient(HttpClient httpClient)    
    {
        _httpClient = httpClient;
    }

    public async Task<BankResponse?> ProcessPaymentAsync(BankRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/payments", request);

        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            throw new BankServiceUnavailableException();
        }

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<BankResponse>();
    }
}

public class BankRequest
{
    public string card_number { get; set; } = string.Empty;
    public string expiry_date { get; set; } = string.Empty;
    public string currency { get; set; } = string.Empty;
    public int amount { get; set; }
    public string cvv { get; set; } = string.Empty;
}

public class BankResponse
{
    public bool authorized { get; set; }
    public string? authorization_code { get; set; }
}

public class BankServiceUnavailableException : Exception {}