using System;
using System.Collections.Generic;
using System.Text;

namespace MommyAndSunny.Application.Common.Options
{
    public class MomoOptions
    {
        // 5 giá trị mặc định bên dưới là tài khoản SANDBOX công khai do MoMo công bố
        // cho developer test miễn phí (không phải secret thật, không cần tài khoản Business
        // để dùng thử) — an toàn khi để trong code, KHÔNG dùng được cho giao dịch tiền thật.
        // Khi có tài khoản MoMo Business thật, ghi đè 5 giá trị này qua biến môi trường
        // Momo__PartnerCode, Momo__AccessKey, Momo__SecretKey, Momo__Endpoint (link production
        // của MoMo, khác link test), Momo__RedirectUrl, Momo__IpnUrl (2 link cuối trỏ đúng domain thật).
        public string PartnerCode { get; set; } = "MOMO";
        public string AccessKey { get; set; } = "F8BBA842ECF85";
        public string SecretKey { get; set; } = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        public string Endpoint { get; set; } = "https://test-payment.momo.vn/v2/gateway/api/create";
        public string RedirectUrl { get; set; } = "http://localhost:3000/orders";
        public string IpnUrl { get; set; } = "http://localhost:5215/api/payments/momo/ipn";
    }


}
