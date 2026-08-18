using Dapper;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using DotNetEnv;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SupermarketSystem.Api.Data;
using SupermarketSystem.Api.Interface;
using SupermarketSystem.Api.Middleware;
using SupermarketSystem.Api.Services.Jwt;
using SupermarketSystem.Api.Services.Permissions;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

// 1️⃣ تسجيل الـ Connection Factory
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();

// 2️⃣ تسجيل خدمات الـ JWT والصلاحيات
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();

// 3️⃣ إضافة خدمات الـ Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });

// 3.1️⃣ إضافة إعدادات الـ CORS للسماح لـ React بالاتصال
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                    "http://localhost:5173", 
                    "http://localhost:5174", 
                    "http://127.0.0.1:5173", 
                    "http://127.0.0.1:5174"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4️⃣ إضافة MediatR و FluentValidation
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// 4.3️⃣ إعداد الـ JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is missing from configuration.");

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

// 5️⃣ إضافة Swagger مع إعدادات زر الـ Authorize (Bearer Token)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Supermarket System API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "ادخل الـ JWT Token بالشكل التالي: Bearer {your_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 5.1️⃣ Middleware الأخطاء
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ تفعيل الـ CORS قبل Authentication و Authorization
app.UseCors("AllowFrontend");

// 5.2️⃣ Authentication + Authorization (قبل MapControllers)
app.UseAuthentication();
app.UseAuthorization();

// 6️⃣ ربط الـ Controllers
app.MapControllers();

// 7️⃣ تهيئة وتأكيد وجود جدول السلات المعلقة HeldInvoices عند بدء التشغيل
try
{
    using var scope = app.Services.CreateScope();
    var connectionFactory = scope.ServiceProvider.GetRequiredService<IDbConnectionFactory>();
    using var connection = connectionFactory.CreateConnection();
    connection.Open();
    const string createTableSql = """
        CREATE TABLE IF NOT EXISTS HeldInvoices (
            Id BIGINT NOT NULL AUTO_INCREMENT,
            EmployeeId BIGINT NOT NULL,
            ReferenceTag VARCHAR(100) NOT NULL,
            CustomerName VARCHAR(150) NULL,
            DiscountPercentage VARCHAR(10) NULL,
            CartState TEXT NOT NULL,
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,

            CONSTRAINT PK_HeldInvoices PRIMARY KEY (Id),
            CONSTRAINT FK_HeldInvoices_Employees
                FOREIGN KEY (EmployeeId)
                REFERENCES Employees(Id)
                ON DELETE CASCADE,

            INDEX IX_HeldInvoices_EmployeeId (EmployeeId)
        ) ENGINE = InnoDB
          DEFAULT CHARACTER SET utf8mb4
          COLLATE utf8mb4_unicode_ci;
        """;
    connection.Execute(createTableSql);
}
catch (Exception ex)
{
    Console.WriteLine($"Error running startup migration for HeldInvoices: {ex.Message}");
}

app.Run();