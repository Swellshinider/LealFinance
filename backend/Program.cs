using System.Text;
using LealFinance.Api.Configuration;
using LealFinance.Api.Data;
using LealFinance.Api.Endpoints;
using LealFinance.Api.Security;
using LealFinance.Api.Services;
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
builder.Services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
builder.Services.AddHostedService<RecurringTransactionSchedulerService>();

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
    EnsureTransactionsTable(databasePath);
    EnsureRecurringTransactionsTable(databasePath);
    EnsureRecurringColumnsOnTransactions(databasePath);
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

static void EnsureTransactionsTable(string sqliteDatabasePath)
{
    using var connection = new SqliteConnection($"Data Source={sqliteDatabasePath}");
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS Transactions (
            Id INTEGER NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Type TEXT NOT NULL,
            Amount NUMERIC NOT NULL,
            IncomeOrExpenseCategory TEXT NOT NULL,
            Category TEXT NOT NULL,
            Date TEXT NOT NULL,
            Notes TEXT NULL,
            CreatedAtUtc TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_Transactions_UserId_Date
            ON Transactions(UserId, Date DESC);
        """;

    command.ExecuteNonQuery();
}

static void EnsureRecurringTransactionsTable(string sqliteDatabasePath)
{
    using var connection = new SqliteConnection($"Data Source={sqliteDatabasePath}");
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = """
        CREATE TABLE IF NOT EXISTS RecurringTransactions (
            Id INTEGER NOT NULL CONSTRAINT PK_RecurringTransactions PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Name TEXT NOT NULL,
            Type TEXT NOT NULL,
            Amount NUMERIC NOT NULL,
            Category TEXT NOT NULL,
            Notes TEXT NULL,
            StartDateUtc TEXT NOT NULL,
            FrequencyUnit TEXT NOT NULL,
            FrequencyInterval INTEGER NOT NULL,
            IsInfinite INTEGER NOT NULL,
            MaxOccurrences INTEGER NULL,
            StartPaymentNumber INTEGER NOT NULL,
            GeneratedOccurrences INTEGER NOT NULL,
            NextOccurrenceDateUtc TEXT NULL,
            IsActive INTEGER NOT NULL,
            CreatedAtUtc TEXT NOT NULL,
            UpdatedAtUtc TEXT NOT NULL,
            FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE
        );

        CREATE INDEX IF NOT EXISTS IX_RecurringTransactions_UserId_IsActive_NextOccurrenceDateUtc
            ON RecurringTransactions(UserId, IsActive, NextOccurrenceDateUtc);
        """;

    command.ExecuteNonQuery();
}

static void EnsureRecurringColumnsOnTransactions(string sqliteDatabasePath)
{
    using var connection = new SqliteConnection($"Data Source={sqliteDatabasePath}");
    connection.Open();

    using var tableInfoCommand = connection.CreateCommand();
    tableInfoCommand.CommandText = "PRAGMA table_info(Transactions);";

    var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    using (var reader = tableInfoCommand.ExecuteReader())
    {
        while (reader.Read())
        {
            existingColumns.Add(reader.GetString(1));
        }
    }

    if (!existingColumns.Contains("RecurringTransactionId"))
    {
        using var alterRecurringTransactionId = connection.CreateCommand();
        alterRecurringTransactionId.CommandText = "ALTER TABLE Transactions ADD COLUMN RecurringTransactionId INTEGER NULL;";
        alterRecurringTransactionId.ExecuteNonQuery();
    }

    if (!existingColumns.Contains("RecurringSequenceNumber"))
    {
        using var alterRecurringSequenceNumber = connection.CreateCommand();
        alterRecurringSequenceNumber.CommandText = "ALTER TABLE Transactions ADD COLUMN RecurringSequenceNumber INTEGER NULL;";
        alterRecurringSequenceNumber.ExecuteNonQuery();
    }

    using var recurringIndexCommand = connection.CreateCommand();
    recurringIndexCommand.CommandText = """
        CREATE INDEX IF NOT EXISTS IX_Transactions_RecurringTransactionId
            ON Transactions(RecurringTransactionId);

        CREATE UNIQUE INDEX IF NOT EXISTS IX_Transactions_RecurringTransactionId_RecurringSequenceNumber
            ON Transactions(RecurringTransactionId, RecurringSequenceNumber)
            WHERE RecurringTransactionId IS NOT NULL AND RecurringSequenceNumber IS NOT NULL;
        """;

    recurringIndexCommand.ExecuteNonQuery();
}
