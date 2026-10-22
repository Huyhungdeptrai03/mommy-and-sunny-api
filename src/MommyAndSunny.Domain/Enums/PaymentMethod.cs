namespace MommyAndSunny.Domain.Enums;

public enum PaymentMethod
{
    COD = 0,
    // Historical orders/payments only; never accept for new checkout.
    Momo = 1,
    VNPay = 2,
    SePay = 3
}
