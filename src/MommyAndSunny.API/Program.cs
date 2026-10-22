using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MommyAndSunny.API.Filters;
using MommyAndSunny.API.Middleware;
using MommyAndSunny.API.Services;
using MommyAndSunny.API.Hubs;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Application.Common.Options;
using MommyAndSunny.Infrastructure.BackgroundJobs;
using MommyAndSunny.Infrastructure.ExternalServices;
using MommyAndSunny.Infrastructure.ExternalServices.Email;
using MommyAndSunny.Infrastructure.Persistence;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    options.MultipartBodyLengthLimit = 6 * 1024 * 1024);

// Railway (và nhiều nền tảng hosting khác) tự cấp port qua biến môi trường PORT —
// ứng dụng phải lắng nghe đúng port đó, không dùng cổng cố định như lúc chạy local
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ===== Controllers + Swagger =====
builder.Services.AddControllers(options =>
    {
        // Tự động validate MỌI action có tham số khớp với 1 IValidator<T> đã đăng ký —
        // xem ghi chú chi tiết trong ValidationActionFilter.cs
        options.Filters.Add<ValidationActionFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Cho phép gửi/nhận enum dưới dạng chuỗi ("COD", "Pending"...) thay vì số (0, 1, 2...)
        // — khớp với định dạng đã thống nhất trong API contract
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 16 * 1024;
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => string.IsNullOrWhiteSpace(entry.Key) ? "request" : entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Giá trị không đúng định dạng"
                        : error.ErrorMessage)
                    .Distinct()
                    .ToArray());

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
        {
            success = false,
            data = (object?)null,
            message = "Dữ liệu không hợp lệ",
            errors
        });
    };
});

// Cấu hình Swagger UI hiểu và gửi kèm JWT Bearer token trong header —
// nếu không có đoạn này, nút "Authorize" không thực sự đính kèm token vào request
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Chỉ cần dán access token (KHÔNG cần gõ chữ 'Bearer' phía trước)"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

// ===== Database =====
// KHÔNG dùng EnableRetryOnFailure ở đây — nó tương tác xấu với xmin concurrency token:
// nếu 1 lệnh UPDATE thành công ở DB nhưng client không kịp nhận ACK (trục trặc mạng cực nhỏ),
// EF Core tự gửi lại y hệt lệnh đó, nhưng xmin đã đổi do lần đầu thực ra đã thành công
// -> báo nhầm thành DbUpdateConcurrencyException dù không hề có xung đột thật.
// Docker Postgres giờ đã chạy ổn định, không còn cần retry cho lỗi "transient failure" ban đầu nữa.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// IApplicationDbContext forward về instance ApplicationDbContext — giữ nguyên đăng ký thủ công
// vì đây là factory lambda đặc biệt, Scrutor không tự suy luận được kiểu đăng ký này
builder.Services.AddScoped<IApplicationDbContext>(provider =>
    provider.GetRequiredService<ApplicationDbContext>());

// ===== AUTO-REGISTER Services/Hasher/Generator bằng Scrutor =====
// Quét 3 assembly (Application, Infrastructure, API hiện tại), tự tìm class có tên
// kết thúc bằng "Service"/"Hasher"/"Generator" và match với interface cùng tên có tiền tố "I"
// (VD: class AuthService -> tự động đăng ký cho interface IAuthService).
// Thêm Service mới sau này KHÔNG cần sửa Program.cs nữa, chỉ cần đặt đúng tên class + interface.
builder.Services.Scan(scan => scan
    .FromAssemblies(
        typeof(IApplicationDbContext).Assembly,      // Application
        typeof(ApplicationDbContext).Assembly,        // Infrastructure
        typeof(CurrentUserService).Assembly)          // API
    .AddClasses(classes => classes.Where(type =>
        type.Name.EndsWith("Service") ||
        type.Name.EndsWith("Hasher") ||
        type.Name.EndsWith("Generator")))
    .AsMatchingInterface()
    .WithScopedLifetime());

// ===== FluentValidation — tự động đăng ký toàn bộ Validator trong Application =====
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequest>();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("uploads", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));
    options.AddPolicy("payment-callbacks", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));
    options.AddPolicy("sensitive-authentication", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0
        }));
    options.AddPolicy("chat", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ??
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});

// ===== Email Service — fallback thông minh, không phụ thuộc cứng vào môi trường =====
// Nếu CHƯA cấu hình Smtp:Host (dù ở Development hay Production) -> chỉ ghi log,
// tránh việc đăng ký tài khoản bị lỗi 500 vì thiếu cấu hình SMTP thật.
// Khi nào có SMTP thật, chỉ cần thêm biến môi trường Smtp:Host trên Railway là
// tự động chuyển sang gửi email thật, không cần sửa code hay redeploy thủ công logic này.
var smtpHost = builder.Configuration["Smtp:Host"];
if (string.IsNullOrWhiteSpace(smtpHost))
{
    builder.Services.AddScoped<IEmailService, LogEmailSender>();
}
else
{
    builder.Services.AddScoped<IEmailService, SmtpEmailSender>();
}

// SePay bank transfer QR + authenticated webhook.
var sePayOptions = builder.Services.AddOptions<SePayOptions>()
    .Bind(builder.Configuration.GetSection("SePay"));
if (!builder.Environment.IsDevelopment())
{
    sePayOptions.Validate(o => !o.UseMock, "SePay mock mode is only allowed in Development")
        .Validate(o => !string.IsNullOrWhiteSpace(o.Bank) &&
                              !string.IsNullOrWhiteSpace(o.AccountNumber) &&
                              !string.IsNullOrWhiteSpace(o.WebhookApiKey),
        "Configure SePay:Bank, SePay:AccountNumber and SePay:WebhookApiKey")
        .ValidateOnStart();
}
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SePay:UseMock"))
    builder.Services.AddSingleton<ISePayPaymentService, MockSePayPaymentGateway>();
else
    builder.Services.AddSingleton<ISePayPaymentService, SePayPaymentGateway>();

builder.Services.AddHostedService<StaleOrderCleanupJob>();

// ===== JWT Authentication (phần bị thiếu gây lỗi trước đó) =====
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Secret trong appsettings");

if (!builder.Environment.IsDevelopment() &&
    (jwtSecret.Length < 32 || jwtSecret.Contains("doi-truoc-khi-len-production", StringComparison.OrdinalIgnoreCase)))
{
    throw new InvalidOperationException("Jwt:Secret production phải là secret riêng, dài tối thiểu 32 ký tự");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ===== CORS — cho phép Frontend Next.js (chạy port 3000) gọi API từ trình duyệt =====
// ===== CORS — đọc danh sách origin cho phép từ config, hỗ trợ cả local dev lẫn production =====
// Ví dụ giá trị "Cors:AllowedOrigins" trên Railway (biến môi trường Cors__AllowedOrigins):
// "http://localhost:3000,https://ten-du-an.vercel.app"
var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:3000")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();
if (!builder.Environment.IsDevelopment() &&
    (allowedOrigins.Length == 0 || allowedOrigins.Any(origin =>
        !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
        uri.Scheme != Uri.UriSchemeHttps ||
        !string.IsNullOrEmpty(uri.PathAndQuery.Trim('/')))))
{
    throw new InvalidOperationException("Cors:AllowedOrigins production phải chứa HTTPS origin hợp lệ");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

// ===== Seed dữ liệu mẫu (chỉ ở Development) =====
// ===== Migration + Seed data =====
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Tự động áp dụng migration khi khởi động — quan trọng cho production, vì bạn không có
    // sẵn terminal để chạy `dotnet ef database update` thủ công trên server như ở local
    await dbContext.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedAsync(dbContext);
    }
}

// ===== Middleware pipeline =====
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Chỉ ép HTTPS redirect ở production — ở Development, cho phép gọi HTTP trực tiếp
// để tránh rắc rối chứng chỉ tự ký (self-signed certificate) khi FE (Next.js Server Component)
// gọi API từ phía Node.js server, vốn không tin tưởng chứng chỉ dev của .NET
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Thứ tự bắt buộc: Authentication TRƯỚC Authorization
// Thứ tự bắt buộc: CORS phải đứng TRƯỚC Authentication/Authorization
// Phục vụ file tĩnh từ wwwroot (VD: /uploads/xxx.jpg) — dùng cho ảnh khách tự tải lên
// qua UploadsController, lưu trực tiếp trên đĩa vì dự án chưa cấu hình storage ngoài (S3/Cloudinary)
app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
