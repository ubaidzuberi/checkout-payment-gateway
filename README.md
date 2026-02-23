# Assumptions and Design Considerations


# Assumptions

- The 3 major currencies were chosen: USD, EUR, GBP.
- Cards are valid until the end of the expiry month.
- Only Authorized and Declined payments are stored as they are the only payments that are processed by the bank simulator.
- Data is lost on application restart.
- Bank authorization code is not stored, as it was not required in the retrieval response.

# Design Considerations

## Architecture

This solution follows a standard 3 layer structure: PaymentController handles HTTP concerns, the PaymentService contains business logic, and the infrastructure layer (PaymentsRepository and BankClient) handles storage and communication with the bank simulator.

## Data Model

The template included 2 response DTOs: PostPaymentResponse and GetPaymentResponse. This was redundant so I consolidated them into a single DTO called PaymentResponse. This DTO serves as both the API response model and the repository storage model.

## Request Validation

To validate request fields, I used ASP.NET Core’s built-in model validation rather than adding validation logic to the PaymentService. Data annotations were sufficient for most properties, and I implemented IValidatableObject for expiry date validation, as it requires cross-field validation across month and year.

I also ensured validation failures mapped to a status of “Rejected” as per the requirements in the spec, this was implemented by configuring a custom InvalidModelStateResponseFactory.

## Error Handling

The specification states that responses successfully sent to the acquiring bank must include either Authorized or Declined. A 503 response from the bank indicates that the request was not successfully processed upstream, so returning Declined would be misleading.

To handle this, the BankClient throws a custom exception when a 503 is received. This is translated at the controller level into a 502 Bad Gateway response, clearly indicating an upstream dependency failure rather than a declined payment.

## Security Considerations

Since only the last 4 digits are returned in a response, there was no need to store the entire card number as this would unnecessarily increase security risk. CVV is also never stored as it is not needed after the call to the bank simulator and storing it would only increase the security risk if the system was ever compromised.

## BankClient Implementation

I introduced IBankClient to make testing easier, as it allowed me to substitute a mock implementation when testing PaymentService. PaymentService and PaymentsRepository were not abstracted behind interfaces because they were not being replaced in any of the tests and doing so would add unnecessary complexity.

BankRequest, BankResponse, BankServiceUnavailableException, and IBankClient are all defined within BankClient.cs rather than separate files. These are implementation details of the bank integration and are not referenced outside of this context, so colocating them keeps all bank related code self contained in one place.