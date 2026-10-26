# MommyAndSunny — hồ sơ chuẩn bị nâng cấp auth

Ngày kiểm tra: 2026-10-26. Trạng thái: BLOCKED trước approval gate; chưa kết nối VPS, chưa deploy.

## 1. Revision và bằng chứng

- Backend local: `dff9584cafc27db90172033b8b6acbdd71946313` (`DeployBE26/10`). Working tree sạch trước khi thêm hồ sơ này.
- VPS `/opt/mommy-son/api`: CHƯA XÁC ĐỊNH revision, dirty files, image đang chạy, database migration history hoặc service manager. Chưa được cung cấp SSH host/user.
- Frontend: CHƯA CÓ source/revision/artifact; không xác nhận tương thích chỉ từ ghi chú trong backend contract.
- `f7fa136f2ef89e821edad8aec0dafa51eea299df` là commit trước local, KHÔNG phải revision VPS đã xác nhận.
- Chạy lại `dotnet test MommyAndSunny.slnx --no-restore`: 22 unit pass; 40 integration pass; 4 PostgreSQL skip; 0 fail. Tổng 62 pass / 4 skip / 66 tests. Chưa tái lập bằng chứng 66/66 pass.
- Đã đọc `docs/MEMBER_AUTH_API_CONTRACT.md`; kết quả test cuối tài liệu đã cũ. Tài liệu cũng nói chưa rehearsal migration và chưa xác minh browser/proxy/provider/SMTP thực.

## 2. Đối chiếu cần hoàn tất trên VPS

Các lệnh sau chỉ đọc, chạy trong phiên SSH đã xác thực. Không xuất `.env`, process environment, private key, connection string hoặc toàn bộ docker inspect. Kiểm tra output cục bộ và che bí mật trước khi chia sẻ.

```bash
cd /opt/mommy-son/api
git rev-parse HEAD
git status --short
git diff --name-status
git ls-files --others --exclude-standard
git log -5 --format='%H %s'
git ls-files '*Program.cs' '*AuthController.cs' '*AuthService.cs' '*AuthResponse.cs' '*RefreshTokenRequest.cs' '*JwtTokenGenerator.cs' '*User.cs' '*RefreshToken.cs' '*Migrations*' '*Dockerfile*' '*compose*' '*.service'
docker ps --format '{{.Names}}\t{{.Image}}\t{{.Ports}}'
systemctl list-units --type=service --all --no-pager
```

Đọc từng file đã liệt kê và deployment config trong phiên bảo mật; ghi hash file. Không dùng `git pull`, `checkout`, `reset` hoặc ghi đè working tree VPS. Đọc unit/compose/nginx đã xác định và chỉ ghi service name, image ID/digest, mounts, listen/upstream, env key names. Revision source chưa chứng minh revision binary đang chạy: cần image provenance/build manifest; nếu thiếu, ghi UNKNOWN.

Truy vấn chỉ đọc bằng kết nối hiện có, không đặt credential trong command line:

```sql
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT count(*) AS conflicting_email_groups FROM (
 SELECT lower(btrim("Email")) FROM "Users"
 GROUP BY lower(btrim("Email")) HAVING count(*) > 1
) AS conflicts;
SELECT count(*) AS empty_email_rows FROM "Users" WHERE btrim("Email") = '';
SELECT count(*) AS legacy_social_rows FROM "Users" WHERE "PasswordHash" IS NULL;
```

Nếu đã có ExternalLogins, audit social-only bằng anti-join thay vì coi mọi password null là lỗi. Không xuất email khách hàng. Đếm Users/Orders/Carts/Addresses; lưu snapshot kiểm tra ID/FK trong vùng bảo mật để so sánh trên clone và sau migration.

| Thành phần | Đã xác nhận ở local | VPS |
|---|---|---|
| Program.cs | antiforgery, session validation, trusted proxy, HTTPS/Host guard, OAuth, persistent Data Protection, production không auto-migrate/seed | Chờ đọc |
| AuthController | refresh đọc cookie, CSRF/login/refresh/logout, csrf/me/resend | Theo mô tả người dùng còn đọc body; chờ xác minh |
| AuthService/JWT | GenerateAccessToken(user, session.Id), sid/auth_ver, DB session, rotation concurrency, revocation | Theo mô tả còn GenerateAccessToken(user); chờ xác minh |
| AuthResponse | RefreshToken và RefreshTokenExpiresAt có JsonIgnore | Theo mô tả còn trả secret trong JSON; chờ xác minh |
| RefreshTokenRequest | Vẫn có DTO RefreshToken dùng nội bộ; controller không bind body token | Chờ đọc controller, không kết luận từ DTO |
| User/RefreshToken | NormalizedEmail, SecurityVersion, ConcurrencyStamp; SessionId, ConsumedAt | Chờ đọc |
| Account/ExternalAuth | profile, provider start/callback/complete, one-time exchange/email | Chờ đọc |
| Migration | 20261026040155_HardenMemberAuthentication | Chờ source và DB history |

## 3. Frontend phải chứng minh bằng source và browser

- Access JWT chỉ RAM, xóa token persisted cũ; refresh secret không đọc bằng JavaScript.
- GET `/api/auth/csrf`; gửi cookie + `X-CSRF-TOKEN` cho POST `/api/auth/login`, `/api/auth/refresh-token`, `/api/auth/logout`, `/api/auth/external/exchange`, `/api/auth/external/email`.
- Login/refresh/logout và CSRF tương ứng không gửi Authorization; Origin do browser gửi. Linking lấy CSRF cùng Bearer principal.
- Refresh không body; single-flight trong tab và serialize giữa tab; 401/409 kết thúc phiên; không retry mù checkout/payment POST.
- GET `/api/auth/external/google/start` hoặc `/api/auth/external/facebook/start` bằng navigation cùng origin; backend callback tương ứng `/api/auth/external/{provider}/callback`; frontend `/auth/callback` trao code qua POST exchange rồi xóa code khỏi URL. Không analytics/log query nhạy cảm.
- GET/PATCH `/api/account/me` dùng Bearer; PATCH chỉ fullName/phoneNumber. `/api/auth/me` vẫn tồn tại.
- Frontend không cần đọc sid để xác thực; backend kiểm tra session. Release đồng bộ buộc đăng nhập lại vì JWT/refresh legacy bị từ chối.
- Contract nhắc `MEMBER_AUTH_FRONTEND_INTEGRATION.md` nhưng file này không có trong workspace hiện tại.

## 4. Artifact và rehearsal trước approval

Chưa có artifact production được phê duyệt. Chỉ có candidate source SHA ở mục 1. Không chuyển cả workspace: repository có `mommy-son-backup.dump`; tuyệt đối loại dump, secrets, appsettings development, uploads và Git metadata khỏi artifact.

Build trong checkout sạch của đúng SHA, giữ nguyên production source hiện tại. Lưu SDK/tool versions, test report, contract hash, source SHA, base image digest, dependency resolution, backend image digest, frontend SHA/image digest, migration SQL SHA-256 trong release manifest. Dockerfile hiện dùng tag .NET 10.0 chưa pin digest; `.dockerignore` loại dump/secrets/appsettings. Kiểm tra thực tế nội dung image. Không lấy compose local làm production: compose này chỉ có Postgres và cấu hình development.

Chuyển OCI image đã review qua registry riêng theo digest, hoặc `docker save` ra tar và xác minh SHA-256 độc lập trước `docker load`. Chuyển riêng manifest/contract/SQL/test report qua kênh xác thực. Không build từ file chắp vá trên VPS. Nếu không tìm thấy tested SHA/CI report, dừng và yêu cầu bundle Git của đúng reviewed commit hoặc artifact CI có provenance, hash và test report; không dựng lại auth từ snippet.

Sinh SQL trên máy build, không kết nối production. Chỉ dùng baseline sau khi đã đọc migration history VPS. Nếu baseline đúng `20261022131113_AddChatProductTags`, lệnh dự kiến:

```bash
dotnet ef migrations script 20261022131113_AddChatProductTags 20261026040155_HardenMemberAuthentication --project src/MommyAndSunny.Infrastructure --startup-project src/MommyAndSunny.API --output artifacts/member-auth-up.sql
```

Nếu baseline khác, review TOÀN BỘ migration còn thiếu trước khi thay lệnh. Review SQL và rehearsal trên bản restore biệt lập, cùng PostgreSQL major/extensions/collation. Không cấp clone credentials SMTP/OAuth/payment production, không bật worker gửi ra dịch vụ thật. Đo thời gian lock, backfill, unique index và downtime. Chạy PostgreSQL concurrency tests riêng trên loopback database `mommy_auth_test_*` qua `MOMMY_AUTH_TEST_POSTGRES`; các test xóa schema test, không trỏ vào clone nghiệp vụ hay production.

Migration auth thêm Users.NormalizedEmail/SecurityVersion/ConcurrencyStamp, RefreshTokens.ConsumedAt/SessionId; tạo AuthSessions/ExternalLogins/ExternalHandoffs, indexes/FKs. Backfill Users với normalized email và UUID. Không đổi Users.Id, không sửa FK Orders/Carts/Addresses. Có table update/index locks và WAL; thời gian chưa đo. Duplicate/empty email chặn migration; Unicode normalization/collation và legacy social account cần review riêng. Không chạy Down: có drop bảng/cột auth.

## 5. Backup, cấu hình và proxy

Chưa biết DB container/service/user/database/version; lệnh bên dưới là TEMPLATE chưa được phép chạy production. Dùng libpq service/pgpass được bảo vệ sẵn, không đưa password vào argv/log.

```bash
pg_dump --dbname=service=PROD_READ_BACKUP --format=custom --file=BACKUP_PATH/pre-auth.dump
pg_restore --list BACKUP_PATH/pre-auth.dump
sha256sum BACKUP_PATH/pre-auth.dump
pg_restore --exit-on-error --no-owner --no-privileges --dbname=service=ISOLATED_RESTORE BACKUP_PATH/pre-auth.dump
```

Backup roles/grants bằng quy trình DBA; bảo vệ dump và chỉ giữ bản mã hóa ngoài VPS. `pg_restore --list` chưa chứng minh restore thành công; phải restore vào DB mới, kiểm tra counts/IDs/FKs và ứng dụng. Backup uploads, cấu hình, Data Protection keys, image cũ và frontend cũ riêng. Không dùng dump đang nằm trong repo làm backup production.

Cấu hình cần đối chiếu tên key, không xuất giá trị:

- ASPNETCORE_ENVIRONMENT=Production; ConnectionStrings__DefaultConnection; Jwt__Secret (production riêng tối thiểu 32 ký tự), Jwt__Issuer, Jwt__Audience, Jwt__AccessTokenExpirationMinutes (giới hạn 1–15, mặc định 15).
- Frontend__BaseUrl = duy nhất public HTTPS origin; Authentication__DataProtectionKeysPath trên volume bền vững, quyền ghi cho runtime user; ReverseProxy__KnownProxies danh sách IP proxy tin cậy, phân cách dấu phẩy.
- Smtp__Host, Smtp__Port, Smtp__Username, Smtp__Password, Smtp__FromEmail, Smtp__FromName. Implementation dùng EnableSsl=true, port mặc định 587; xác minh SMTP delivery/resend với mailbox test, không log recovery token.
- Authentication__Google__ClientId/ClientSecret; Authentication__Facebook__AppId/AppSecret. Provider dashboard cho phép đúng HTTPS callback ở mục 3; kiểm tra trạng thái app/test users/quyền email theo tài khoản thực. Thiếu credentials trả PROVIDER_NOT_CONFIGURED.
- Giữ và kiểm tra cấu hình nghiệp vụ hiện có: SePay__UseMock=false, SePay__Bank, SePay__AccountNumber, SePay__WebhookApiKey và mọi options đang sử dụng. Không test thanh toán thật.

Proxy giữ public Host, ghi đè X-Forwarded-For/Proto, không nối header client; Kestrel không public. Route `/api/*`, `/hubs/chat` negotiate/WebSocket tới backend; frontend paths tới frontend. Kiểm tra route uploads đang dùng thực tế trước cutover. Không cache auth/Set-Cookie; không log code/emailToken query. HTTPS chứng chỉ hợp lệ và cùng origin, cookie Secure/HttpOnly/SameSite=Lax/Path=/, không Domain. Kiểm tra health probe hiện tại vì Host/HTTPS guard có thể chặn probe nội bộ.

## 6. Approval gate và rollback

Chưa yêu cầu phê duyệt deploy khi chưa có host/service/artifact digest và rehearsal. Trước khi xin phép phải thay mọi placeholder bằng lệnh thực tế, đính kèm:

1. VPS SHA + dirty-file hashes + running image digest; backend/frontend target SHA/digest và manifest checksum.
2. Baseline DB, SQL checksum và review, audit counts, rehearsal/restore results.
3. Maintenance window đo được, tác động forced login, rollback RTO/RPO.
4. Toàn bộ lệnh backup, khóa ghi, migration, chuyển image frontend/backend, service restart/proxy reload và runtime validation theo đúng Docker HOẶC systemd đã phát hiện.
5. Rollback image/config cả hai phía đã thử trên clone với schema mới.
6. Blockers còn lại và câu hỏi phê duyệt rõ ràng. Không chạy migration/restart/config change trước câu trả lời chấp thuận.

Trình tự sau khi được duyệt: maintenance cho frontend/API có ghi, xử lý webhook/worker/in-flight checkout theo topology đã review (không dừng dịch vụ khác); final consistent backup; áp dụng đúng SQL đã hash với stop-on-error và transaction đã review; kiểm tra migration history/IDs/FKs; chuyển cặp frontend/backend; smoke test trước mở ghi. Không dùng `docker compose down -v`, database reset, schema Down, hay copy đè source.

Rollback ưu tiên cặp image/config cũ trên schema mở rộng, CHỈ khi rehearsal chứng minh tương thích và đã chấp thuận việc trở lại auth cũ. Giữ schema/data mới, không drop auth tables. Có rủi ro phiên bị thu hồi được code cũ chấp nhận lại: cần quyết định security/forced re-login và secret strategy trước approval. Nếu không an toàn, giữ maintenance và forward-fix. Restore backup chỉ vào DB mới sau phê duyệt riêng; chuyển kết nối có kiểm soát, giữ DB hiện tại để đối soát và bảo toàn writes/payments/webhook sau backup. Không restore đè DB live và không chấp nhận mất đơn hàng ngầm.

## 7. Runtime validation sau release được duyệt

- Email registration/verify/resend và login với tài khoản test; không in password/token/cookie.
- GET csrf, thiếu/sai Origin/header bị từ chối; cookie flags đúng; AuthResponse không có refreshToken.
- Refresh rotation, phiên cũ/replay bị từ chối; server logout làm access JWT cũ không dùng được; reload và nhiều tab ổn định.
- Google/Facebook callback/handoff/email/link flow với test users, kiểm tra cấu hình provider thực; không coi chỉ có env key là OAuth thành công.
- GET/PATCH Member Account; member bị chặn admin; admin dùng được đúng quyền, không mass assignment.
- User hiện hữu giữ ID và order history, isolation tài khoản; cart/address/checkout validation đúng. Không thực hiện payment thật; việc tạo đơn test cần dữ liệu và cách đối soát đã duyệt.
- WebSocket/chat, uploads và các dịch vụ hiện hữu hoạt động; không log secrets. Ghi runtime evidence theo timestamp/status đã che PII. Chỉ tuyên bố release thành công sau các kiểm tra này.
