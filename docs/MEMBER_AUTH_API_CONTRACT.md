# MommyAndSunny — hợp đồng Member Authentication

Tài liệu cho backend và frontend sau thay đổi này. Chỉ backend đã được sửa. Không có BFF, Redis, Firebase, Supabase Auth hay ASP.NET Core Identity.

## 1. Trạng thái và phạm vi

- **Đã triển khai trong mã:** JWT HS256 + session PostgreSQL; refresh cookie; CSRF; logout; thu hồi JWT theo session/security version; profile; Google/Facebook handlers; handoff một lần; email onboarding; liên kết chủ động; migration và test.
- **Chưa xác minh trên môi trường thực:** Google/Meta credentials, provider callback, SMTP delivery, reverse proxy, trình duyệt và PostgreSQL concurrency. Đọc mục kiểm thử trước khi phát hành.
- **Frontend đã tích hợp ở mức mã:** Next.js/React/Zustand/Axios; xem MEMBER_AUTH_FRONTEND_INTEGRATION.md. Browser/proxy/provider thật vẫn cần kiểm thử staging.
- Giữ nguyên `Users.Id` và FK Orders/Carts/Addresses/Payments/Wishlist. `AuthProvider` là nguồn đăng ký ban đầu; `ExternalLogins` mới là nguồn liên kết danh tính.

## 2. Origin, cookie và phiên

Một origin HTTPS công khai, cấu hình bởi `Frontend__BaseUrl` (chỉ origin, không path/query/fragment).

Reverse proxy chuyển `/api/*` và `/hubs/chat` (bao gồm negotiate và WebSocket upgrade) tới C#; các đường dẫn khác tới Next.js. Giữ nguyên public `Host`, ghi đè `X-Forwarded-For` và `X-Forwarded-Proto`, không nối/giữ header không tin cậy từ client. Chỉ proxy được kết nối Kestrel; không expose port backend ra Internet.

| Cookie | Thuộc tính |
|---|---|
| `__Host-ms-refresh` | Secure, HttpOnly, SameSite=Lax, Path=/, không Domain; hết hạn theo session 7 ngày |
| `__Host-ms-csrf` | Secure, HttpOnly, SameSite=Lax, Path=/; framework antiforgery cookie |
| `__Host-ms-oauth` | Secure, HttpOnly, SameSite=Lax, Path=/; browser binding, 10 phút |
| `__Host-ms-external` | Secure, HttpOnly, SameSite=Lax, Path=/; external principal tạm 5 phút |

Không có HTTP cookie fallback, kể cả local development. Dùng HTTPS local. Cookie chỉ do backend tạo/xóa. Access JWT chỉ lưu trong RAM frontend, không persist localStorage/sessionStorage. Refresh token không bao giờ serialize ra JSON; DB tiếp tục chỉ lưu SHA-256 hash.

Access JWT: cấu hình 1–15 phút, mặc định 15; giá trị cấu hình cũ lớn hơn 15 được giới hạn về 15. `accessTokenExpiresAt` trả UTC ISO-8601. Refresh session có hạn tuyệt đối 7 ngày, rotation không kéo dài vô hạn.

JWT có `sub=Users.Id`, `sid`, `auth_ver`, role và `jti`. Mỗi request Bearer kiểm tra chữ ký/issuer/audience/HS256/thời hạn và đọc session/user hiện tại từ DB. Session revoked, security version hoặc role không khớp làm JWT bị từ chối. JWT legacy thiếu `sid` không được chấp nhận.

## 3. CSRF protocol

`GET /api/auth/csrf` trả:

```json
{"success":true,"data":{"csrfToken":"opaque-request-token"},"message":null}
```

Framework đồng thời tạo cookie antiforgery HttpOnly. JavaScript giữ request token trong RAM.

Các route **bắt buộc** có cookie antiforgery + header `X-CSRF-TOKEN` + `Origin` đúng origin cấu hình:

- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`
- `POST /api/auth/external/exchange`
- `POST /api/auth/external/email`

Browser tự gửi Origin; frontend không tự đặt Origin. `Origin` thiếu, `null`, nhiều giá trị, khác origin hoặc request HTTP đều bị từ chối `403 FORBIDDEN`. Không dùng Referer làm fallback. Công cụ non-browser phải lấy CSRF cookie/token và cung cấp Origin rõ ràng; không có ngoại lệ cho request thiếu Origin.

Antiforgery token của ASP.NET gắn với trạng thái principal. Login/refresh/logout lấy CSRF **không gửi Authorization**, và gọi chúng không gửi Authorization. Sau đăng nhập, muốn exchange liên kết kèm Bearer thì lấy CSRF mới với **cùng Bearer còn hợp lệ**. Không tái sử dụng token CSRF anonymous cho request có principal khác. Nếu principal thay đổi/hết hạn, lấy lại token trong ngữ cảnh phù hợp.

API nghiệp vụ/profile tiếp tục dùng Bearer trong Authorization; refresh cookie không xác thực những API đó. Callback GET của OAuth dùng state/correlation framework, không dùng header CSRF. OAuth start chỉ chấp nhận điều hướng HTTPS có `Sec-Fetch-Site: same-origin`; mở qua nút/link cùng origin, không prefetch.

## 4. API chính xác

Các body JSON dùng camelCase. Envelope giữ `success`, `data`, `message`; lỗi thêm `code`, validation có thể có `errors`. Không có lớp ApiResponse dùng chung trong source cũ; giữ cấu trúc anonymous envelope hiện tại.

| Method và route | Body / xác thực | Thành công |
|---|---|---|
| POST `/api/auth/register` | `{email,password,fullName,phoneNumber?}` | 201, data `{userId,email}`; gửi xác thực, không tự login |
| POST `/api/auth/login` | `{email,password}` + CSRF | 200, AuthResult + Set-Cookie |
| POST `/api/auth/refresh-token` | Không body; cookie + CSRF | 200, AuthResult + cookie rotation |
| POST `/api/auth/logout` | Không body; cookie + CSRF | 200, data null; thu hồi family, xóa cookie |
| GET `/api/auth/me` | Bearer | 200, data User |
| POST `/api/auth/verify-email` | `{token}` | 200, data null |
| POST `/api/auth/forgot-password` | `{email}` | 200, thông báo chung |
| POST `/api/auth/reset-password` | `{token,newPassword}` | 200, data null; thu hồi phiên và security version |
| POST `/api/auth/resend-verification` | `{email}` | 202, thông báo chung |
| GET `/api/account/me` | Bearer | 200, data User |
| PATCH `/api/account/me` | Bearer; `{fullName,phoneNumber}` | 200, data User |

AuthResult:

```json
{
  "success": true,
  "data": {
    "accessToken": "JWT",
    "accessTokenExpiresAt": "2026-10-26T10:15:00Z",
    "user": {
      "id": "UUID",
      "email": "member@example.com",
      "fullName": "Tên thành viên",
      "phoneNumber": null,
      "role": "Customer",
      "emailVerified": true
    }
  },
  "message": null
}
```

User là object bên trong `data.user` trên. `/me` trả trực tiếp User trong `data`.

Hồ sơ chỉ cho phép fullName (không trống, tối đa 150 ký tự) và phoneNumber (null hoặc số bắt đầu 0, tổng 10–11 chữ số). Field không khai báo trên PATCH bị từ chối, kể cả role/userId/emailVerified/tier/points. Email không sửa qua endpoint này.

Mật khẩu mới ít nhất 8 ký tự, tối đa **72 byte UTF-8** để phù hợp BCrypt. Tài khoản social có PasswordHash null; forgot-password không tự tạo mật khẩu cho tài khoản social-only. Xác thực email 24 giờ, reset 1 giờ; token hash, single-use có optimistic concurrency.

### Thay đổi tương thích và cutover

Route/envelope login/register/reset được giữ. **Thay đổi bảo mật có chủ đích:** bỏ `data.refreshToken`; `/refresh-token` không đọc body token nữa. Không có legacy mode trả refresh token trong JSON vì browser có thể lợi dụng mode đó. Chưa xác minh có client nào ngoài frontend được mô tả; phải kiểm kê trước rollout.

Phát hành frontend/backend đồng bộ, xóa token persisted cũ, buộc đăng nhập lại. Refresh token cũ không có session không được dùng để nâng cấp phiên. JWT cũ thiếu thông tin session bị từ chối. Không triển khai backend đơn lẻ rồi kỳ vọng frontend localStorage cũ tiếp tục hoạt động.

## 5. Refresh, race và logout

- Một SaveChanges relational transaction cập nhật consumed/revoked token, session version và thêm đúng một successor.
- Concurrency token trên refresh token và session ngăn hai rotation cùng commit.
- Token consumed gửi lại hoặc rotation thua race: thu hồi cả session, `409 REFRESH_CONFLICT`. Không có grace/retry trả lại secret cũ.
- Token thuộc family đã thu hồi: `401 SESSION_REVOKED`; token thiếu/legacy/hết hạn: `401 SESSION_EXPIRED`.
- Với AuthException refresh (401/409), backend xóa cookie. HTTP 429 xảy ra trước action không đồng nghĩa phiên revoked và không xóa cookie. Response thành công khác có thể về sau response lỗi trong một race; cookie đó vẫn trỏ phiên revoked và không dùng được.
- Frontend phải dùng single-flight trong tab và Web Locks hoặc cơ chế phối hợp tương đương giữa các tab. Khi tab khác refresh xong, tab chờ đọc lại phiên bằng một refresh được tuần tự hóa; không gửi lại token request cũ.
- Nếu response refresh bị mất sau khi server commit, hệ thống không khôi phục successor từ plaintext; xử lý an toàn là đăng nhập lại. Không tự retry refresh bằng request đồng thời.
- Logout idempotent nếu cookie không tồn tại/không biết; nếu có token cũ vẫn thu hồi family của token đó. Xóa state/cache user trên frontend sau logout. Nếu mạng lỗi, không được thông báo đã thu hồi phiên phía server.
- Không tự retry checkout/payment POST khi lỗi mạng vì có thể đã xử lý. Xử lý idempotency nghiệp vụ riêng nếu sau này cần.

## 6. OAuth sequence và linking

| Route | Hành vi |
|---|---|
| GET `/api/auth/external/google/start` | Framework Google challenge |
| GET `/api/auth/external/facebook/start` | Framework Facebook challenge |
| GET `/api/auth/external/google/callback` | Callback do Google handler xử lý |
| GET `/api/auth/external/facebook/callback` | Callback do Facebook handler xử lý |
| GET `/api/auth/external/complete` | Đọc temporary external principal, tạo handoff |
| POST `/api/auth/external/exchange` | `{code,emailVerificationToken?,linkExistingAccount:false}` + CSRF |
| POST `/api/auth/external/email` | `{code,email,fullName}` + CSRF; 202 gửi xác minh onboarding |

1. Browser điều hướng từ cùng origin tới start; backend tạo browser transaction cookie ngẫu nhiên, state/correlation được handler bảo vệ.
2. Provider callback theo authorization-code flow. Google bật PKCE; Facebook dùng flow handler hỗ trợ. Đây là OAuth handlers, không tự tiêu thụ ID token nên không bổ sung nonce OIDC giả.
3. External principal chỉ có hiệu lực tạm; không phải member authentication scheme mặc định. Không lưu provider access/refresh tokens.
4. Complete tạo code 256-bit, lưu hash, ràng buộc hash cookie browser; redirect cố định `/auth/callback?code=...`. Không có returnUrl tùy ý, không access/refresh token trong URL.
5. Trang Next.js đọc code, loại khỏi address bar/history ngay khi có thể, không analytics, đặt no-referrer/no-store và gọi exchange bằng POST sau lấy CSRF.
6. Exchange kiểm tra code, expiry và browser binding; consumption + identity/session được commit atomic. Thành công trả AuthResult và refresh cookie, xóa transaction cookie.

Code ban đầu hết hạn 5 phút; khi gửi email onboarding thời hạn code được làm mới 5 phút, vẫn cần browser cookie 10 phút từ lúc start. Email verification phải mở trên cùng browser. Bắt đầu lại nếu hết hạn. Một browser chỉ có một OAuth transaction đang hoạt động; start mới thay thế binding cũ.

### Quy tắc danh tính

- Lookup theo provider chuẩn hóa và stable provider subject, không theo email client.
- External login tồn tại → login đúng Users.Id đã liên kết.
- Email trùng user hiện hữu → `409 ACCOUNT_LINK_REQUIRED`; không merge hoặc tạo user trùng.
- User mới (kể cả Google) → yêu cầu xác minh email qua ứng dụng. Chính sách bảo thủ này không tin email provider là đã xác thực chỉ vì có claim email.
- Thiếu email Facebook → gọi `/external/email`, mở link gửi qua SMTP rồi exchange với `emailVerificationToken`.
- Onboarding email do client nhập chỉ được dùng sau token chứng minh quyền sở hữu; không thay thế provider subject.
- Tạo user mới, ExternalLogin, handoff consumed và session trong cùng transaction. PasswordHash null, role Customer.
- Link chủ động: đăng nhập tài khoản đích lại trong vòng 5 phút, bắt đầu OAuth (nếu cần), exchange với `linkExistingAccount:true`, Bearer của user đích và CSRF phù hợp principal. Có thể giữ handoff để đăng nhập tài khoản cũ rồi exchange trong thời hạn.
- Identity đã thuộc user khác → `409 IDENTITY_CONFLICT`. DB có unique `(Provider,ProviderSubject)` và `(UserId,Provider)`.
- Unlink, đổi email chính và thêm password cho social-only **chưa triển khai**.

Email onboarding chứa code + emailToken xác minh ngắn hạn, không chứa access/refresh token. Không ghi query này ở proxy, ứng dụng hoặc analytics.

## 7. Mã lỗi và trạng thái HTTP

| Code | HTTP | Ý nghĩa |
|---|---|---|
| INVALID_CREDENTIALS | 401 | Sai email/password hoặc social-only dùng password |
| SESSION_EXPIRED | 401 | JWT/refresh thiếu, hết hạn hoặc legacy |
| SESSION_REVOKED | 401 | Session/security version/role không hợp lệ |
| EMAIL_VERIFICATION_REQUIRED | 403 | Checkout/onboarding cần xác minh email |
| FORBIDDEN | 403 | CSRF/Origin không hợp lệ hoặc thiếu quyền |
| REFRESH_CONFLICT | 409 | Refresh replay/race; handoff commit concurrency cũng dùng code này |
| RATE_LIMITED | 429 | Không phải revocation. Retry-After lấy thời gian còn lại từ limiter lease khi có metadata, fallback 60 giây |
| ACCOUNT_LINK_REQUIRED | 409 | Email đã có tài khoản, cần link chủ động |
| IDENTITY_CONFLICT | 409 | Danh tính/unique constraint xung đột |
| REAUTHENTICATION_REQUIRED | 401 | Link cần phiên mới dưới 5 phút |
| INVALID_HANDOFF | 400 | Code/cookie thiếu, sai, hết hạn hoặc đã dùng |
| INVALID_OAUTH_TRANSACTION | 400 | External principal/browser transaction không hợp lệ |
| PROVIDER_NOT_CONFIGURED | 503 | Chưa có đủ credentials provider |
| INVALID_TOKEN | 400/409 | Email/reset token không hợp lệ hoặc mất race |
| VALIDATION_FAILED | 400 | Chính sách password |
| HTTPS_REQUIRED / INVALID_HOST | 400 | Sai public origin/HTTPS |
| REQUEST_FAILED | theo lỗi | Các lỗi nghiệp vụ cũ; validation model cũ có thể chưa có code |

Provider cancel/failure redirect cố định `/auth/callback?error=OAUTH_FAILED`, không phản chiếu chi tiết lỗi provider.

## 8. Session recovery trong Next.js

1. Load/reload: chưa có access token RAM → GET csrf anonymous → POST refresh cookie → lưu JWT trong module RAM, chỉ user/status trong Zustand. AuthResult đã có user hiện tại, không GET /auth/me lặp ngay sau refresh.
2. Nếu refresh 401/409: xóa state user, yêu cầu đăng nhập; không vòng lặp retry.
3. Gửi Bearer access token cho `/api/account/me`, order/cart/address và API Admin.
4. Refresh chủ động trước expiry; serialize giữa các tab. Chỉ retry tối đa một request nhận 401 do token hết hạn; không retry FORBIDDEN và không retry POST sau lỗi mạng không rõ kết quả.
5. Sau login/refresh, có thể GET `/api/auth/me` để lấy hồ sơ mới; quyền client chỉ điều khiển UI, quyền server mới quyết định.

### Rate limit tách theo mục đích

| Mục đích | Quota | Partition |
|---|---|---|
| Login / verify-email | 20 / 5 phút | IP |
| Register / forgot-password / reset-password / resend-verification | 5 / 15 phút | IP, chung sensitive bucket như trước |
| GET csrf | 20 / 5 phút | IP, bucket riêng |
| POST refresh | 40 / 5 phút | IP, bucket riêng |
| GET auth/me | 20 / 5 phút | UserId đã xác thực, fallback IP cho anonymous |
| POST logout | 20 / 5 phút | IP, bucket riêng |
| OAuth start | 5 / 15 phút | IP, riêng với credential/recovery |
| OAuth complete / exchange | 20 / 5 phút | IP, bucket riêng |
| OAuth onboarding email | 5 / 15 phút | IP, bucket riêng |

Refresh quota 40 bao phủ 3 tab ở JWT TTL tối thiểu 60 giây và refresh sớm 30 giây:
3 bootstrap + tối đa 3 × 10 rotations/5 phút = 33. Không partition theo raw/hash
refresh cookie vì giá trị thay đổi sau rotation sẽ cho phép bypass quota.
Login/recovery giữ nguyên quota chống brute force. IP quota vẫn cần load test cho NAT lớn.

Frontend cache CSRF request token anonymous trong RAM, reuse khi principal vẫn
anonymous; link Bearer luôn lấy token đúng principal. Nếu nhận 403, bỏ cache trước
lần thử chủ động tiếp theo; không tự replay POST. Bootstrap và read revalidation
single-flight, read debounce 60 giây. Không chia sẻ JWT giữa các tab.

HTTP 429, lỗi mạng hoặc 5xx không xóa user/JWT còn hạn và không broadcast logout.
Tôn trọng Retry-After (seconds hoặc HTTP-date), không retry vòng lặp. Bootstrap
chưa xác nhận được danh tính giữ checking cùng thông báo lỗi. Nếu refresh POST có
thể đã commit nhưng mất response/5xx, giữ JWT còn hạn, chặn refresh replay và yêu
cầu đăng nhập lại; JWT hết hạn không được dùng cho mutation. Chỉ 401 SESSION_EXPIRED,
SESSION_REVOKED và 409 REFRESH_CONFLICT là lỗi phiên dứt khoát trong session manager.

### SePay mock (Development)

Production tiếp tục trả HTTPS QR image URL trong paymentUrl; không thay DTO/tổng
tiền/chữ ký/webhook/trạng thái đơn. Mock trả URL tuyệt đối cùng HTTPS public origin:
`/api/payments/mock-qr?orderCode=...&amount=...`, không trả data URL. GET endpoint
chỉ hoạt động khi IsDevelopment và SePay:UseMock=true; môi trường khác trả 404.
Ảnh SVG chỉ là placeholder không thể chuyển tiền, có no-store, nosniff và sandbox
CSP; orderCode được HTML encode. Không mở rộng frontend để chấp nhận data: URLs.

Axios cần `withCredentials` cho auth cookie flow, không lấy refresh token từ JSON. Interceptor loại Authorization cho anonymous csrf/login/refresh/logout. Zustand persist phải bỏ token; dọn các key legacy trong cutover. Không sửa frontend trong thay đổi này.

## 9. Admin, SignalR và nghiệp vụ

- Role lấy từ Users; đổi quyền tăng SecurityVersion và revoke các session hiện hữu trong cùng SaveChanges. Frontend không cung cấp userId/role cho login.
- `/hubs/chat` giữ nguyên đường dẫn và query `access_token` cho tương thích SignalR browser; đó chỉ là access JWT ngắn hạn. Proxy phải che query log. Không gửi refresh token tới hub.
- Hub đóng khi JWT hết hạn; client reconnect với access token mới.
- Mỗi hub invocation kiểm tra session; worker kiểm tra cả idle connection mỗi chu kỳ 2 giây để ngắt connection revoked/role đổi. Thời gian phát hiện gồm chu kỳ và thời gian DB/xử lý, không bảo đảm tức thời tuyệt đối. Worker fail-closed nếu DB validation lỗi.
- Giữ order/cart/address ownership checks. Checkout chưa verify email trả code rõ ràng. Không thay logic tồn kho/coupon/webhook/payment.

## 10. Cấu hình bắt buộc — chỉ tên biến

Hiện hữu:

- `ASPNETCORE_ENVIRONMENT`
- `ConnectionStrings__DefaultConnection`
- `Frontend__BaseUrl`
- `Jwt__Secret`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__AccessTokenExpirationMinutes`
- `Smtp__Host`
- `Smtp__Port`
- `Smtp__Username`
- `Smtp__Password`
- `Smtp__FromEmail`
- `Smtp__FromName`
- Các cấu hình SePay hiện hữu không thay đổi.

Mới:

- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`
- `Authentication__Facebook__AppId`
- `Authentication__Facebook__AppSecret`
- `Authentication__DataProtectionKeysPath`
- `ReverseProxy__KnownProxies`
- `Database__ApplyMigrationsOnStartup`
- `MOMMY_AUTH_TEST_POSTGRES` — chỉ test

CORS chỉ cho public origin; `Cors__AllowedOrigins` legacy không còn điều khiển browser auth. Proxy trust giữ loopback mặc định, chỉ thêm IP cấu hình rõ ràng, không clear toàn bộ trust list. Data Protection keys cần volume bền vững, quyền đọc/ghi giới hạn tài khoản ứng dụng, bảo vệ backup; các instance phải dùng cùng key ring. Production thiếu SMTP host/from address hoặc persistent key path sẽ fail startup. Development sink không gửi email và không log link/token.

## 11. Google / Meta setup thủ công

Google: tạo OAuth web client, cấu hình consent screen/audience/test users và production publishing phù hợp; đăng ký đúng HTTPS public origin và redirect URI `/api/auth/external/google/callback`; cấp ClientId/ClientSecret qua secret manager.

Meta: tạo ứng dụng và Facebook Login phù hợp; đăng ký domain/website URL, Valid OAuth Redirect URI `/api/auth/external/facebook/callback`, privacy policy/data deletion và chế độ public/review theo yêu cầu Meta. Cấp AppId/AppSecret; yêu cầu email nhưng vẫn xử lý trường hợp không được trả. Kiểm tra phiên bản Graph API handler đang dùng và yêu cầu tài khoản developer tại thời điểm cấu hình.

Start, callback, complete đều dưới `/api/*` để reverse proxy chuyển đúng backend. `/auth/callback` là trang frontend phải xây. Không có credentials trong repo mới; provider thiếu cấu hình trả 503 có kiểm soát. Không tạo/publish ứng dụng provider tự động.

## 12. Migration, audit dữ liệu và phát hành

Migration: `20261026040155_HardenMemberAuthentication` (chưa áp dụng). Thêm AuthSessions, ExternalLogins, ExternalHandoffs; metadata refresh; NormalizedEmail/SecurityVersion/ConcurrencyStamp. Không đổi Users.Id hoặc business FKs.

Trước áp dụng, chạy audit **chỉ đọc**, giới hạn người được xem dữ liệu:

```sql
SELECT count(*) AS conflicting_email_groups
FROM (
  SELECT lower(btrim("Email"))
  FROM "Users"
  GROUP BY lower(btrim("Email")) HAVING count(*) > 1
) AS conflicts;
SELECT count(*) AS empty_email_rows FROM "Users" WHERE btrim("Email") = '';
```

Migration tự chặn nếu trùng/rỗng; không in email vào exception, không merge. Operator điều tra từng nhóm bằng quyền được cấp, xử lý riêng và review trước unique index. Migration backfill normalized email và security/concurrency UUID cho user cũ. Không xóa dữ liệu user/token cũ; legacy refresh không có session bị từ chối.

Audit thêm tài khoản legacy có PasswordHash null nhưng chưa có ExternalLogin. Chưa xác minh có bản ghi như vậy trong dữ liệu thật. Không suy đoán provider subject từ email hoặc tự tạo mật khẩu để cứu tài khoản; nếu có, cần quy trình phục hồi danh tính được review trước cutover. Việc chuẩn hóa email quốc tế/Unicode trong dữ liệu cũ cũng cần đối chiếu collation PostgreSQL với ToLowerInvariant của ứng dụng trước khi áp dụng.

Production **không** chạy migration/seed lúc startup. Development cũng mặc định không chạy, chỉ chạy khi bật opt-in trên database development đã xác minh. Design-time factory dùng địa chỉ không kết nối được để scaffold mà không build web host/đọc deployment secrets.

Trước release: backup/restore rehearsal, SQL review, kiểm tra duplicate email, PostgreSQL race tests, browser/proxy/SMTP/provider E2E, coordinated frontend cutover. Không rollback schema bằng cách drop các bảng mới khi đã có dữ liệu auth; cần kế hoạch riêng.

Cleanup bảng handoff/session/token hết hạn **chưa có scheduled job**. Cần retention phù hợp; giữ token consumed trong thời hạn session để phát hiện replay. Chưa bổ sung hàng đợi retry email: lỗi SMTP sau khi lưu user có thể cần resend; đây là hạn chế được giữ rõ ràng.

## 13. Kiểm thử và giới hạn

Đã chạy build và test không khởi động web host. Test in-memory bao gồm normalized login, JSON không refresh token, JWT signature/audience, CSRF framework thực, reset single-use, role revoke, handoff browser binding/replay, link chủ động, onboarding email, profile mass-assignment và order/cart/address isolation. Test hiện hữu chat/validation/payment gateway/webhook tiếp tục chạy.

PostgreSQL tests chỉ bật khi biến test được cấp rõ ràng. Guard yêu cầu loopback và database tên bắt đầu `mommy_auth_test_`; mỗi test tạo schema `auth_test_<random>`, tạo cấu trúc từ model, rồi xóa đúng schema test đó. Không lấy connection string ứng dụng, không chạy migrations, không dùng database production.

Các test PostgreSQL gồm race rotation, replay/logout, concurrent handoff consumption, unique identity/user concurrency. Nếu không có database local, chúng SKIP; in-memory pass không chứng minh concurrency PostgreSQL đúng. Migration upgrade trên bản sao dữ liệu cũng chưa được kiểm thử.

Google/Facebook thực, gửi thư thật, nginx/TLS, WebSocket và luồng checkout trình duyệt chưa được kiểm thử. Không coi thay đổi này là sẵn sàng deploy trước khi hoàn tất các bước đó.

K?t qu? phi?n tri?n khai: `dotnet build MommyAndSunny.slnx --no-restore` th?nh c?ng, 0 warning/error; `dotnet test MommyAndSunny.slnx --no-restore`: 43 pass, 4 PostgreSQL tests skip, 0 fail. `dotnet ef migrations has-pending-model-changes` x?c nh?n model kh?p migration. Ch?a ch?y ?ng d?ng, migrations ho?c k?t n?i database th?t.

## 14. Danh s?ch file thay ??i ch?nh x?c

- `docs/MEMBER_AUTH_API_CONTRACT.md`
- `src/MommyAndSunny.API/Controllers/AccountController.cs`
- `src/MommyAndSunny.API/Controllers/AuthController.cs`
- `src/MommyAndSunny.API/Controllers/ExternalAuthController.cs`
- `src/MommyAndSunny.API/Hubs/SessionHubFilter.cs`
- `src/MommyAndSunny.API/Middleware/ExceptionHandlingMiddleware.cs`
- `src/MommyAndSunny.API/MommyAndSunny.API.csproj`
- `src/MommyAndSunny.API/Program.cs`
- `src/MommyAndSunny.API/Services/BrowserSession.cs`
- `src/MommyAndSunny.API/Services/SessionValidationService.cs`
- `src/MommyAndSunny.Application/Admin/Users/Services/AdminUserService.cs`
- `src/MommyAndSunny.Application/Auth/DTOs/AuthResponse.cs`
- `src/MommyAndSunny.Application/Auth/DTOs/UserDto.cs`
- `src/MommyAndSunny.Application/Auth/Services/AuthSecurity.cs`
- `src/MommyAndSunny.Application/Auth/Services/AuthService.cs`
- `src/MommyAndSunny.Application/Auth/Services/IAuthService.cs`
- `src/MommyAndSunny.Application/Common/Interfaces/IApplicationDbContext.cs`
- `src/MommyAndSunny.Application/Common/Interfaces/IJwtTokenGenerator.cs`
- `src/MommyAndSunny.Application/Orders/Services/OrderService.cs`
- `src/MommyAndSunny.Domain/Entities/AuthSession.cs`
- `src/MommyAndSunny.Domain/Entities/ExternalHandoff.cs`
- `src/MommyAndSunny.Domain/Entities/ExternalLogin.cs`
- `src/MommyAndSunny.Domain/Entities/RefreshToken.cs`
- `src/MommyAndSunny.Domain/Entities/User.cs`
- `src/MommyAndSunny.Domain/Enums/AuthProvider.cs`
- `src/MommyAndSunny.Domain/Exceptions/AuthException.cs`
- `src/MommyAndSunny.Infrastructure/ExternalServices/Email/LogEmailSender.cs`
- `src/MommyAndSunny.Infrastructure/ExternalServices/Email/SmtpEmailSender.cs`
- `src/MommyAndSunny.Infrastructure/Identity/JwtTokenGenerator.cs`
- `src/MommyAndSunny.Infrastructure/Migrations/20261026040155_HardenMemberAuthentication.Designer.cs`
- `src/MommyAndSunny.Infrastructure/Migrations/20261026040155_HardenMemberAuthentication.cs`
- `src/MommyAndSunny.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs`
- `src/MommyAndSunny.Infrastructure/Persistence/ApplicationDbContext.cs`
- `src/MommyAndSunny.Infrastructure/Persistence/Configurations/MemberAuthConfiguration.cs`
- `src/MommyAndSunny.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`
- `src/MommyAndSunny.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `src/MommyAndSunny.Infrastructure/Persistence/DesignTimeDbContextFactory.cs`
- `tests/MommyAndSunny.IntegrationTests/AuthMigrationSafetyTests.cs`
- `tests/MommyAndSunny.IntegrationTests/MemberAuthTests.cs`
- `tests/MommyAndSunny.IntegrationTests/PostgresMemberAuthTests.cs`
