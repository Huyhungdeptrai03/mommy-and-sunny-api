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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using MommyAndSunny.API.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

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
    options.AddFilter<SessionHubFilter>();
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
        (type.Name.EndsWith("Service") && type != typeof(SessionValidationService)) ||
        type.Name.EndsWith("Hasher") ||
        type.Name.EndsWith("Generator")))
    .AsMatchingInterface()
    .WithScopedLifetime());

// ===== FluentValidation — tự động đăng ký toàn bộ Validator trong Application =====
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequest>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BrowserSession>();
builder.Services.AddScoped<SessionValidationService>();
builder.Services.AddSingleton<HubConnections>();
builder.Services.AddHostedService<HubSessionRevocationWorker>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-ms-csrf";
    options.Cookie.Path = "/";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
var publicUrl = builder.Configuration["Frontend:BaseUrl"];
if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicUri) ||
    publicUri.Scheme != "https" || !string.IsNullOrEmpty(publicUri.UserInfo) ||
    publicUri.AbsolutePath != "/" || !string.IsNullOrEmpty(publicUri.Query) || !string.IsNullOrEmpty(publicUri.Fragment))
    throw new InvalidOperationException("Frontend:BaseUrl must be the single public HTTPS origin (including local HTTPS development).");
var keys = builder.Services.AddDataProtection().SetApplicationName("MommyAndSunny.MemberAuth");
var keysPath = builder.Configuration["Authentication:DataProtectionKeysPath"];
if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(keysPath))
    throw new InvalidOperationException("Configure Authentication:DataProtectionKeysPath on a protected persistent volume.");
if (!string.IsNullOrWhiteSpace(keysPath)) keys.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Keep framework loopback defaults. Only add explicitly configured reverse proxy IPs.
    foreach (var ip in (builder.Configuration["ReverseProxy:KnownProxies"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        options.KnownProxies.Add(System.Net.IPAddress.Parse(ip));
    options.ForwardLimit = 1;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = MemberAuthRateLimits.RejectAsync;
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
    MemberAuthRateLimits.AddPolicies(options);
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

// Production requires SMTP. Development sink never logs recovery secrets.
var smtpHost = builder.Configuration["Smtp:Host"];
if (string.IsNullOrWhiteSpace(smtpHost))
{
    if (!builder.Environment.IsDevelopment())
        throw new InvalidOperationException("Production requires real SMTP; logging recovery links is prohibited.");
    builder.Services.AddScoped<IEmailService, LogEmailSender>();
}
else
{
    if (!builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Smtp:FromEmail"]))
        throw new InvalidOperationException("Configure Smtp:FromEmail.");
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
    builder.Services.AddSingleton<ISePayPaymentService>(_ => new MockSePayPaymentGateway(publicUrl!));
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

var authentication = builder.Services.AddAuthentication(options =>
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
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        ClockSkew = TimeSpan.FromSeconds(15),
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            if (!await context.HttpContext.RequestServices.GetRequiredService<SessionValidationService>()
                .IsValidAsync(context.Principal, context.HttpContext.RequestAborted))
            {
                context.HttpContext.Items["auth_error"] = "SESSION_REVOKED";
                context.Fail("Session no longer valid.");
            }
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { success = false, data = (object?)null,
                code = context.HttpContext.Items["auth_error"] as string ?? "SESSION_EXPIRED", message = "Yêu cầu đăng nhập lại." });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsJsonAsync(new { success = false, data = (object?)null, code = "FORBIDDEN", message = "Không đủ quyền." });
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

authentication.AddCookie(ExternalAuthController.ExternalScheme, options =>
{
    options.Cookie.Name = "__Host-ms-external";
    options.Cookie.Path = "/";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    options.SlidingExpiration = false;
});
void ConfigureExternal(Microsoft.AspNetCore.Authentication.OAuth.OAuthOptions options, string provider)
{
    options.SignInScheme = ExternalAuthController.ExternalScheme;
    options.CallbackPath = $"/api/auth/external/{provider}/callback";
    options.SaveTokens = false;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
    options.CorrelationCookie.HttpOnly = true;
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.RemoteAuthenticationTimeout = TimeSpan.FromMinutes(5);
    options.Events.OnRemoteFailure = context =>
    {
        context.HandleResponse();
        context.Response.Redirect("/auth/callback?error=OAUTH_FAILED");
        return Task.CompletedTask;
    };
}
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
    authentication.AddGoogle(options =>
    {
        ConfigureExternal(options, "google");
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.UsePkce = true;
    });
if (!string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Facebook:AppId"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Facebook:AppSecret"]))
    authentication.AddFacebook(options =>
    {
        ConfigureExternal(options, "facebook");
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
        options.Fields.Add("email");
        options.Scope.Add("email");
    });

builder.Services.AddAuthorization();

// ===== CORS — cho phép Frontend Next.js (chạy port 3000) gọi API từ trình duyệt =====
// ===== CORS — đọc danh sách origin cho phép từ config, hỗ trợ cả local dev lẫn production =====
// Ví dụ giá trị "Cors:AllowedOrigins" trên Railway (biến môi trường Cors__AllowedOrigins):
// "http://localhost:3000,https://ten-du-an.vercel.app"
var allowedOrigins = publicUri.GetLeftPart(UriPartial.Authority)
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
              .WithHeaders("Authorization", "Content-Type", "X-CSRF-TOKEN")
              .WithMethods("GET", "POST", "PATCH", "PUT", "DELETE", "OPTIONS")
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

// ===== Seed dữ liệu mẫu (chỉ ở Development) =====
// ===== Migration + Seed data =====
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Explicit development opt-in only; production migrations are reviewed and applied separately.
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

// Require HTTPS after trusted forwarded headers, including secure local development.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.Use(async (context, next) =>
{
    if (!string.Equals(context.Request.Host.Value, publicUri.Authority, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { success = false, data = (object?)null, code = "INVALID_HOST", message = "Invalid public host." });
        return;
    }
    if (!context.Request.IsHttps)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { success = false, data = (object?)null, code = "HTTPS_REQUIRED", message = "HTTPS required." });
        return;
    }
    if (context.Request.Path.StartsWithSegments("/api/auth"))
    {
        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
    }
    await next(context);
});

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
app.MapHub<ChatHub>("/hubs/chat", options => options.CloseOnAuthenticationExpiration = true);

app.Run();
