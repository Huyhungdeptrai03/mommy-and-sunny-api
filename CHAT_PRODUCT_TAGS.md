# Tag sản phẩm trong chat

Gửi tin nhắn qua API chat hoặc SignalR như hiện tại, ví dụ:

```json
{ "content": "Mình muốn mua #Sữa cho bé, shop tư vấn giúp nhé." }
```

Tên sau `#` phải khớp tên đầy đủ của sản phẩm Published, không phân biệt hoa thường, giữ dấu tiếng Việt và khoảng trắng. Có thể tag nhiều sản phẩm trong một tin nhắn. Tag không khớp được giữ nguyên dưới dạng văn bản. Nếu nhiều tên có chung tiền tố thì ưu tiên tên dài nhất; nếu trùng tên hoàn toàn thì chọn ID nhỏ nhất để kết quả ổn định.

Tin nhắn trả về từ API, lịch sử và sự kiện SignalR `ReceiveMessage` có thêm `productTags`:

```json
{
  "productTags": [
    {
      "productId": "<id sản phẩm>",
      "productName": "Sữa cho bé",
      "slug": "sua-cho-be",
      "start": 14,
      "length": 11
    }
  ]
}
```

`start` và `length` là vị trí UTF-16 trong `content` đã trim, tính cả dấu `#`, phù hợp với JavaScript `slice(start, start + length)`. Giao diện có thể dùng các vị trí này để tô nổi tag và dùng `slug` để mở trang sản phẩm. Render tên bằng text để tránh chèn HTML. Repo này chỉ có backend, chưa có phần giao diện chọn gợi ý hoặc hiển thị tag.

Tag lưu thông tin tại thời điểm gửi, nên đổi tên hay ngừng bán sản phẩm không làm mất tag trong lịch sử. Tin nhắn cũ trả về danh sách tag rỗng.

Áp dụng migration trước khi chạy phiên bản mới:

```powershell
dotnet ef database update --project src/MommyAndSunny.Infrastructure --startup-project src/MommyAndSunny.API
```
