# SePay trên Production

SePay mặc định tắt, kể cả khi đã có thông tin ngân hàng. Đặt rõ trong môi trường API:

```env
SePay__Enabled=false
SePay__UseMock=false
```

Khi tắt, không cần `SePay__Bank`, `SePay__AccountNumber`, `SePay__WebhookApiKey`.
Backend từ chối tạo đơn SePay trước khi sửa giỏ hàng/tồn kho, không sinh QR,
webhook trả 404 và không ghi giao dịch. Endpoint mock cũng trả 404.
Job dọn đơn bỏ qua nhánh đơn SePay khi tắt; nhánh COD giữ nguyên.
Không xóa hoặc chuyển đổi đơn, lịch sử thanh toán; không cần migration cho thay đổi này.

Checkout lấy `GET /api/payments/methods` (không cache):

```json
{"success":true,"data":{"paymentMethods":["COD"]},"message":null}
```

Frontend chỉ hiển thị phương thức API trả về, mặc định chọn COD và bỏ lựa chọn
SePay đã lưu khi không còn khả dụng. API tạo đơn và giá trị enum giữ nguyên;
client cũ gửi SePay khi tắt sẽ nhận lỗi nghiệp vụ, không tự đổi phương thức.
Repository này không chứa frontend; cần xác minh giao diện thực tế trước phát hành.

Chỉ bật `SePay__Enabled=true` sau khi kích hoạt dịch vụ và cấu hình đủ ba biến
Bank, AccountNumber, WebhookApiKey. Khi bật thật, thiếu bất kỳ giá trị nào làm
startup thất bại. Production luôn từ chối `UseMock=true`, kể cả khi Enabled=false.
Development mock cần cả Enabled=true và UseMock=true; không cần credentials thật.
Webhook thật vẫn yêu cầu Authorization: Apikey <secret>, tài khoản nhận đúng,
số tiền khớp và giữ kiểm tra giao dịch trùng.

## Công việc triển khai còn lại (thực hiện thủ công)

1. Review diff và tạo artifact/commit phát hành; giữ nguyên cấu hình Authentication,
   JWT, SMTP, database, reverse proxy và data protection keys hiện hữu.
2. Đặt hai biến trên trong môi trường container/process API; nếu dùng Compose,
   bảo đảm chúng được truyền vào container rồi tạo lại process/container bằng artifact mới.
3. Phối hợp frontend để dùng endpoint phương thức thanh toán; kiểm tra chỉ thấy COD,
   tạo đơn COD thành công, đơn SePay bị từ chối và webhook trả 404.
4. Xác minh đăng nhập/refresh/CSRF/OAuth/session và truy xuất đơn/lịch sử cũ.
   SePay không cần thay đổi schema. Migration auth đã có là phạm vi phát hành riêng,
   phải review theo tài liệu rollout hiện hữu, không chạy tự động vì thay đổi này.

Không có thao tác VPS hay triển khai nào được thực hiện trong tác vụ này.
