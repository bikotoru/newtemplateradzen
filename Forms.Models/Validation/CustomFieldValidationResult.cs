using System.ComponentModel.DataAnnotations;

namespace Forms.Models.Validation;

public class CustomFieldValidationResult
{
    public bool IsValid { get; set; }
    public List<CustomFieldValidationError> Errors { get; set; } = new();
    public Dictionary<string, object?> ProcessedValues { get; set; } = new();

    public static CustomFieldValidationResult Success(Dictionary<string, object?> processedValues = null!)
    {
        return new CustomFieldValidationResult
        {
            IsValid = true,
            ProcessedValues = processedValues ?? new Dictionary<string, object?>()
        };
    }

    public static CustomFieldValidationResult Failure(params CustomFieldValidationError[] errors)
    {
        return new CustomFieldValidationResult
        {
            IsValid = false,
            Errors = errors.ToList()
        };
    }

    public static CustomFieldValidationResult Failure(string fieldName, string errorMessage)
    {
        return new CustomFieldValidationResult
        {
            IsValid = false,
            Errors = new List<CustomFieldValidationError>
            {
                new(fieldName, errorMessage)
            }
        };
    }

    public void AddError(string fieldName, string errorMessage)
    {
        Errors.Add(new CustomFieldValidationError(fieldName, errorMessage));
        IsValid = false;
    }

    public void Merge(CustomFieldValidationResult other)
    {
        if (!other.IsValid)
        {
            IsValid = false;
            Errors.AddRange(other.Errors);
        }

        // Merge processed values
        foreach (var kvp in other.ProcessedValues)
        {
            ProcessedValues[kvp.Key] = kvp.Value;
        }
    }
}

public class CustomFieldValidationError
{
    public string FieldName { get; set; }
    public string ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public object? AttemptedValue { get; set; }

    public CustomFieldValidationError(string fieldName, string errorMessage, string? errorCode = null)
    {
        FieldName = fieldName;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}

public static class ValidationErrorCodes
{
    public const string Required = "REQUIRED";
    public const string InvalidFormat = "INVALID_FORMAT";
    public const string OutOfRange = "OUT_OF_RANGE";
    public const string TooShort = "TOO_SHORT";
    public const string TooLong = "TOO_LONG";
    public const string InvalidOption = "INVALID_OPTION";
    public const string InvalidReference = "INVALID_REFERENCE";
    public const string DuplicateValue = "DUPLICATE_VALUE";
}