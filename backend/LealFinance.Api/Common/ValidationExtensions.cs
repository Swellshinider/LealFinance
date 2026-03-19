using System.ComponentModel.DataAnnotations;

namespace LealFinance.Api.Common;

/// <summary>
/// Provides DataAnnotations validation helpers for endpoint models.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Validates a model and returns a dictionary consumable by <c>Results.ValidationProblem</c>.
    /// </summary>
    /// <typeparam name="T">Model type.</typeparam>
    /// <param name="model">Model instance.</param>
    /// <param name="errors">Validation errors by field.</param>
    /// <returns><c>true</c> when valid; otherwise <c>false</c>.</returns>
    public static bool TryValidate<T>(this T model, out Dictionary<string, string[]> errors)
        where T : class
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(model);
        var isValid = Validator.TryValidateObject(model, context, validationResults, true);

        errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty), (result, memberName) => new { memberName, result.ErrorMessage })
            .GroupBy(item => string.IsNullOrWhiteSpace(item.memberName) ? "request" : item.memberName)
            .ToDictionary(group => group.Key, group => group.Select(item => item.ErrorMessage ?? "Invalid value.").ToArray());

        return isValid;
    }
}
