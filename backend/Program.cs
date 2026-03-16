using System.Text;
using LealFinance.Api.Configuration;
using LealFinance.Api.Data;
using LealFinance.Api.Endpoints;
using LealFinance.Api.Security;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalAngular", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");

if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException("JWT signing key must be configured and at least 32 characters long.");
}

var databasePath = Path.Combine(AppContext.BaseDirectory, "lealfinance.sqlite3");
builder.Services.AddDbContext<LealFinanceDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));

builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LealFinanceDbContext>();
    dbContext.Database.EnsureCreated();
    EnsureAuthRefreshTokenColumns(databasePath);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("LocalAngular");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapLealFinanceEndpoints();

app.Run();

static void EnsureAuthRefreshTokenColumns(string sqliteDatabasePath)
{
    using var connection = new SqliteConnection($"Data Source={sqliteDatabasePath}");
    connection.Open();

    using var tableInfoCommand = connection.CreateCommand();
    tableInfoCommand.CommandText = "PRAGMA table_info(Users);";

    var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    using (var reader = tableInfoCommand.ExecuteReader())
    {
        while (reader.Read())
        {
            existingColumns.Add(reader.GetString(1));
        }
    }

    if (!existingColumns.Contains("RefreshToken"))
    {
        using var alterRefreshTokenCommand = connection.CreateCommand();
        alterRefreshTokenCommand.CommandText = "ALTER TABLE Users ADD COLUMN RefreshToken TEXT NULL;";
        alterRefreshTokenCommand.ExecuteNonQuery();
    }

    if (!existingColumns.Contains("RefreshTokenExpiryTime"))
    {
        using var alterRefreshTokenExpiryCommand = connection.CreateCommand();
        alterRefreshTokenExpiryCommand.CommandText = "ALTER TABLE Users ADD COLUMN RefreshTokenExpiryTime TEXT NULL;";
        alterRefreshTokenExpiryCommand.ExecuteNonQuery();
    }
}
