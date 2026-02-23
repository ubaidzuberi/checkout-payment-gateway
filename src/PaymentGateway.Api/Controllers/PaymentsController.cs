using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<PaymentResponse>> PostPayment(PostPaymentRequest request)
    {
        try
        {
            var response = await _paymentService.ProcessPaymentAsync(request);
            return Ok(response);
        }
        catch (Clients.BankServiceUnavailableException)
        {
            return StatusCode(502, new { error = "Bank service is currently unavailable" });
        }
    }


    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public ActionResult<PaymentResponse> GetPayment(Guid id)
    {
        var response = _paymentService.GetPayment(id);

        if (response == null)
            return NotFound();

        return Ok(response);
    }
}