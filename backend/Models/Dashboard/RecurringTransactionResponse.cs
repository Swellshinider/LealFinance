namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents a recurring transaction item returned to clients.
/// </summary>
public sealed class RecurringTransactionResponse
{
    /// <summary>
    /// Gets or sets the schedule identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the schedule display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated transaction type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the generated transaction category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional notes template.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the schedule start date in UTC.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the interval unit.
    /// </summary>
    public string FrequencyUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the interval multiplier.
    /// </summary>
    public int FrequencyInterval { get; set; }

    /// <summary>
    /// Gets or sets whether the schedule has no ending payment.
    /// </summary>
    public bool IsInfinite { get; set; }

    /// <summary>
    /// Gets or sets the max payment number when limited.
    /// </summary>
    public int? MaxOccurrences { get; set; }

    /// <summary>
    /// Gets or sets the first payment number configured by the user.
    /// </summary>
    public int StartPaymentNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of generated payments.
    /// </summary>
    public int GeneratedOccurrences { get; set; }

    /// <summary>
    /// Gets or sets the next generation date in UTC.
    /// </summary>
    public DateTime? NextOccurrenceDate { get; set; }

    /// <summary>
    /// Gets or sets whether the schedule is active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the remaining payment count for limited schedules.
    /// </summary>
    public int? RemainingPayments { get; set; }
}
