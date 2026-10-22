# SePay bank transfer integration

This backend uses QR bank transfers with SePay Webhooks. It does not use the separate SePay hosted Payment Gateway product.

## Configuration

Set these environment variables (or equivalent user secrets) before starting the API:

- `SePay__Bank`: bank short name, e.g. `Vietcombank`.
- `SePay__AccountNumber`: linked receiving account number, preserving leading zeros.
- `SePay__WebhookApiKey`: secret configured for API Key authentication in the SePay webhook dashboard.

Production startup validates these settings. Development can start without them; SePay checkout returns a configuration error before saving an order or changing the cart/inventory. COD remains available. There are no default bank accounts or API keys.
Create an incoming-transfer webhook pointing to `https://YOUR_API/api/payments/sepay/webhook`, select the same receiving account, and use API Key authentication. SePay sends `Authorization: Apikey YOUR_SECRET`.
Configure payment code detection with prefix `MS` so that the entire generated order code (MS followed by 17 digits) is returned in webhook `code`. A missing or unknown code is acknowledged without changing an order; reconcile these transactions in SePay.

## Checkout contract

- Send `paymentMethod: "SePay"` (or numeric `3`); COD remains `0`.
- `PaymentUrl` in the create-order response is a QR **image URL**, not a hosted checkout redirect. Display it as an image.
- Poll the existing authenticated order detail endpoint to read `PaymentStatus`. QR generation alone never marks an order paid.
- Only incoming transfers to the configured account, with a matching code and exact positive amount, can pay a SePay order.
- Duplicate transaction IDs are idempotent using the existing unique provider/transaction index.
- Cancelled orders, other payment methods, wrong amounts, and additional transactions on already paid orders require manual reconciliation; they never reopen or repay orders automatically.
- Pre-order products retain the existing COD/manual deposit workflow.
- Stale unpaid SePay orders retain existing stock reservation and coupon cleanup behavior. Review `OrderCleanup:StaleThresholdMinutes` before going live (current default is 2 minutes).

## Existing data

The historical `Momo = 1` enum and old migration names remain for reading existing records. New checkouts accept only COD and SePay. Existing MoMo pending orders should be reconciled/cancelled before switching providers; they are not converted into SePay orders. No schema conversion is required because provider/payment method columns already store strings and the unique index applies to every provider. Ensure existing migrations are applied.

## Verification

Run `dotnet test MommyAndSunny.slnx --no-restore`. Tests use an in-memory database; validate webhook delivery and one real small bank transfer after configuring SePay. Frontend code is not present in this repository and must adopt the checkout contract above.

Official references:
- https://docs.sepay.vn/tich-hop-webhooks.html
- https://developer.sepay.vn/en/sepay-webhooks/tao-qr-va-form-thanh-toan

## Local mock mode (enabled in Development)

`appsettings.Development.json` enables `SePay:UseMock`. No account, real API key, external connection, or money transfer is required. Production rejects this flag. Development order expiry is 30 minutes to allow manual testing.

1. Create a regular in-stock order with `paymentMethod: "SePay"` (numeric `3`). The returned PaymentUrl is a local SVG placeholder image, not a bank QR. The order stays Unpaid.
2. POST `/api/payments/sepay/webhook` in Swagger/Postman with header `Authorization: Apikey sepay-local-test`, content type `application/json`, and this body (replace code/amount with the created order):

```json
{
  "id": 900001,
  "gateway": "LocalMock",
  "accountNumber": "SEPAY_TEST_ACCOUNT",
  "code": "YOUR_ORDER_CODE",
  "transferType": "in",
  "transferAmount": 100000
}
```

3. Read `GET /api/orders/YOUR_ORDER_CODE` with your login token: PaymentStatus should be Paid. Repeating the same webhook must not add a second payment. Use a new id for each new payment; wrong amounts and cancelled orders are rejected.

When ready for real SePay, set `SePay:UseMock` to false and supply Bank, AccountNumber, WebhookApiKey. Restart the API. Existing mock transactions are development data and do not represent bank transfers.
