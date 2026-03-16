using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Models.Dashboard;

/// <summary>
/// Represents the payload to create or update a recurring transaction schedule.
/// </summary>
public sealed class RecurringTransactionUpsertRequest : IValidatableObject
{
    /// <summary>
    /// Gets or sets the display name used in generated payment labels.
    /// </summary>
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated transaction type (<c>Income</c> or <c>Expense</c>).
    /// </summary>
    [Required]
    [RegularExpression("^(Income|Expense)$", ErrorMessage = "Type must be either Income or Expense.")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated transaction amount.
    /// </summary>
    [Range(0.01, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the generated transaction category.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional notes appended to generated transaction labels.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the start date for the recurring schedule.
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the interval unit (<c>Day</c>, <c>Week</c>, <c>Month</c>, or <c>Year</c>).
    /// </summary>
    [Required]
    [RegularExpression("^(Day|Week|Month|Year)$", ErrorMessage = "Frequency unit must be Day, Week, Month, or Year.")]
    public string FrequencyUnit { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the frequency interval multiplier.
    /// </summary>
    [Range(1, 3650)]
    public int FrequencyInterval { get; set; }

    /// <summary>
    /// Gets or sets whether this schedule has no ending payment.
    /// </summary>
    public bool IsInfinite { get; set; }

    /// <summary>
    /// Gets or sets the last payment number when the schedule is limited.
    /// </summary>
    [Range(1, 100000)]
    public int? MaxOccurrences { get; set; }

    /// <summary>
    /// Gets or sets the first payment number to generate.
    /// </summary>
    [Range(1, 100000)]
    public int StartPaymentNumber { get; set; } = 1;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsInfinite && !MaxOccurrences.HasValue)
        {
            yield return new ValidationResult(
                "Max occurrences is required when the schedule is limited.",
                [nameof(MaxOccurrences)]);
        }

        if (!IsInfinite && MaxOccurrences.HasValue && StartPaymentNumber > MaxOccurrences.Value)
        {
            yield return new ValidationResult(
                "Start payment number cannot be greater than max occurrences for limited schedules.",
                [nameof(StartPaymentNumber), nameof(MaxOccurrences)]);
        }
    }
}
