using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Entities;

/// <summary>
/// Represents a recurring transaction template used to generate standard transactions automatically.
/// </summary>
public sealed class RecurringTransaction
{
    /// <summary>
    /// Gets or sets the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the owner user identifier.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the display name used for generated payment labels.
    /// </summary>
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transaction type (<c>Income</c> or <c>Expense</c>).
    /// </summary>
    [Required]
    [MaxLength(24)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the generated transaction category.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional default notes appended to generated transactions.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets encrypted recurring payload at rest.
    /// </summary>
    [MaxLength(10000)]
    public string? EncryptedPayload { get; set; }

    /// <summary>
    /// Gets or sets the UTC start date for schedule generation.
    /// </summary>
    public DateTime StartDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the interval unit (<c>Day</c>, <c>Week</c>, <c>Month</c>, or <c>Year</c>).
    /// </summary>
    [Required]
    [MaxLength(16)]
    public string FrequencyUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schedule interval multiplier.
    /// </summary>
    public int FrequencyInterval { get; set; }

    /// <summary>
    /// Gets or sets whether the schedule has no ending occurrence.
    /// </summary>
    public bool IsInfinite { get; set; }

    /// <summary>
    /// Gets or sets the final payment sequence number for limited schedules.
    /// </summary>
    public int? MaxOccurrences { get; set; }

    /// <summary>
    /// Gets or sets the first payment sequence number to generate.
    /// </summary>
    public int StartPaymentNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of generated payments already persisted.
    /// </summary>
    public int GeneratedOccurrences { get; set; }

    /// <summary>
    /// Gets or sets the next UTC date to generate. Null means completed or canceled.
    /// </summary>
    public DateTime? NextOccurrenceDateUtc { get; set; }

    /// <summary>
    /// Gets or sets whether this recurring schedule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the UTC update timestamp.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
