using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MommyAndSunny.API.Middleware;
using MommyAndSunny.API.Services;
using MommyAndSunny.Application.Auth.DTOs;
using MommyAndSunny.Application.Common.Interfaces;
using MommyAndSunny.Infrastructure.Persistence;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Railway (và nhiều nền tảng hosting khác) tự cấp port qua biến môi trường PORT —
// ứng dụng phải lắng nghe đúng port đó, không dùng cổng cố định như lúc chạy local
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ===== Controllers + Swagger =====
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Cho phép gửi/nhận enum dưới dạng chuỗi ("COD", "Pending"...) thay vì số (0, 1, 2...)
        // — khớp với định dạng đã thống nhất trong API contract
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

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

// ===== JWT Authentication (phần bị thiếu gây lỗi trước đó) =====
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Secret trong appsettings");

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
});

builder.Services.AddAuthorization();

// ===== CORS — cho phép Frontend Next.js (chạy port 3000) gọi API từ trình duyệt =====
// ===== CORS — đọc danh sách origin cho phép từ config, hỗ trợ cả local dev lẫn production =====
// Ví dụ giá trị "Cors:AllowedOrigins" trên Railway (biến môi trường Cors__AllowedOrigins):
// "http://localhost:3000,https://ten-du-an.vercel.app"
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
    ?? new[] { "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

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
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();