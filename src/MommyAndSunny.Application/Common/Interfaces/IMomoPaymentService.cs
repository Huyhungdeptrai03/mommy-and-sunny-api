using MommyAndSunny.Application.Common.Payments.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Common.Interfaces
{
    public interface IMomoPaymentService
    {
        Task<MomoCreatePaymentResult> CreatePaymentRequestAsync(
            string orderCode, decimal amount, string orderInfo, CancellationToken cancellationToken = default);

        bool VerifyIpnSignature(MomoIpnRequest request);
    }

}
