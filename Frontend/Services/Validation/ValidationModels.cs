using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Concurrent;

namespace Frontend.Services.Validation;

public class ValidationChanged
{
    public string FieldName { get; }
    public FieldValidationState FieldState { get; }
    
    public ValidationChanged(string fieldName, FieldValidationState fieldState)
    {
        FieldName = fieldName;
        FieldState = fieldState;
    }
}

public class FieldValidationState
{
    public string FieldName { get; }
    public List<string> Errors { get; }
    public bool HasErrors => Errors.Count > 0;
    public bool IsValid => !HasErrors;
    public string? FirstError => Errors.FirstOrDefault();
    
    public FieldValidationState(string fieldName, List<string> errors)
    {
        FieldName = fieldName;
        Errors = errors ?? new List<string>();
    }
    
    public static FieldValidationState Valid(string fieldName) => new(fieldName, new List<string>());
    public static FieldValidationState Empty => new("", new List<string>());
}

public class ValidationResult
{
    public bool IsValid { get; }
    public string ErrorMessage { get; }
    
    public ValidationResult(bool isValid, string errorMessage = "")
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }
    
    public static ValidationResult Success() => new(true);
    public static ValidationResult Error(string message) => new(false, message);
}

public interface IValidationRule
{
    Task<ValidationResult> ValidateAsync(object? value);
}

public class RequiredRule : IValidationRule
{
    private readonly string _errorMessage;
    
    public RequiredRule(string errorMessage = "Este campo es requerido")
    {
        _errorMessage = errorMessage;
    }
    
    public Task<ValidationResult> ValidateAsync(object? value)
    {
        var isValid = value switch
        {
            null => false,
            string str => !string.IsNullOrWhiteSpace(str),
            _ => true
        };
        
        return Task.FromResult(isValid ? ValidationResult.Success() : ValidationResult.Error(_errorMessage));
    }
}

public class LengthRule : IValidationRule
{
    private readonly int? _minLength;
    private readonly int? _maxLength;
    private readonly string _errorMessage;
    
    public LengthRule(int? minLength = null, int? maxLength = null, string? errorMessage = null)
    {
        _minLength = minLength;
        _maxLength = maxLength;
        _errorMessage = errorMessage ?? GenerateDefaultMessage();
    }
    
    private string GenerateDefaultMessage()
    {
        if (_minLength.HasValue && _maxLength.HasValue)
            return $"Debe tener entre {_minLength} y {_maxLength} caracteres";
        if (_minLength.HasValue)
            return $"Debe tener mínimo {_minLength} caracteres";
        if (_maxLength.HasValue)
            return $"Debe tener máximo {_maxLength} caracteres";
        return "Longitud inválida";
    }
    
    public Task<ValidationResult> ValidateAsync(object? value)
    {
        if (value is not string str)
            return Task.FromResult(ValidationResult.Success());
        
        var length = str.Length;
        
        if (_minLength.HasValue && length < _minLength)
            return Task.FromResult(ValidationResult.Error(_errorMessage));
        
        if (_maxLength.HasValue && length > _maxLength)
            return Task.FromResult(ValidationResult.Error(_errorMessage));
        
        return Task.FromResult(ValidationResult.Success());
    }
}

public class RangeRule : IValidationRule
{
    private readonly double? _min;
    private readonly double? _max;
    private readonly string _errorMessage;
    
    public RangeRule(double? min = null, double? max = null, string? errorMessage = null)
    {
        _min = min;
        _max = max;
        _errorMessage = errorMessage ?? GenerateDefaultMessage();
    }
    
    private string GenerateDefaultMessage()
    {
        if (_min.HasValue && _max.HasValue)
            return $"Debe estar entre {_min} y {_max}";
        if (_min.HasValue)
            return $"Debe ser mayor o igual a {_min}";
        if (_max.HasValue)
            return $"Debe ser menor o igual a {_max}";
        return "Valor fuera del rango permitido";
    }
    
    public Task<ValidationResult> ValidateAsync(object? value)
    {
        if (value == null)
            return Task.FromResult(ValidationResult.Success());
        
        double numericValue;
        
        // Intentar convertir el valor a double
        if (!double.TryParse(value.ToString(), out numericValue))
            return Task.FromResult(ValidationResult.Error("Debe ser un número válido"));
        
        if (_min.HasValue && numericValue < _min)
            return Task.FromResult(ValidationResult.Error(_errorMessage));
        
        if (_max.HasValue && numericValue > _max)
            return Task.FromResult(ValidationResult.Error(_errorMessage));
        
        return Task.FromResult(ValidationResult.Success());
    }
}

public class FieldValidationBuilder
{
    private readonly string _fieldName;
    private readonly List<IValidationRule> _rules = new();
    
    public FieldValidationBuilder(string fieldName)
    {
        _fieldName = fieldName;
    }
    
    public FieldValidationBuilder Required(string errorMessage = "Este campo es requerido")
    {
        _rules.Add(new RequiredRule(errorMessage));
        return this;
    }
    
    public FieldValidationBuilder Length(int? minLength = null, int? maxLength = null, string? errorMessage = null)
    {
        _rules.Add(new LengthRule(minLength, maxLength, errorMessage));
        return this;
    }
    
    public FieldValidationBuilder MinLength(int minLength, string? errorMessage = null)
    {
        return Length(minLength, null, errorMessage);
    }
    
    public FieldValidationBuilder MaxLength(int maxLength, string? errorMessage = null)
    {
        return Length(null, maxLength, errorMessage);
    }
    
    public FieldValidationBuilder Range(double? min = null, double? max = null, string? errorMessage = null)
    {
        _rules.Add(new RangeRule(min, max, errorMessage));
        return this;
    }
    
    internal List<IValidationRule> Build() => _rules;
}

public class FormValidationRules
{
    private readonly Dictionary<string, List<IValidationRule>> _fieldRules = new();
    
    public static FormValidationRules Create() => new();
    
    public FieldValidationBuilder For(string fieldName)
    {
        return new FieldValidationBuilder(fieldName);
    }
    
    public FormValidationRules AddField(string fieldName, FieldValidationBuilder builder)
    {
        _fieldRules[fieldName] = builder.Build();
        return this;
    }
    
    public List<IValidationRule> GetRulesForField(string fieldName)
    {
        return _fieldRules.TryGetValue(fieldName, out var rules) ? rules : new List<IValidationRule>();
    }
}

public class FormValidationRulesBuilder
{
    private readonly FormValidationRules _rules = new();
    
    public static FormValidationRulesBuilder Create() => new();
    
    public FormValidationRulesBuilder Field(string fieldName, Action<FieldValidationBuilder> configure)
    {
        var fieldBuilder = new FieldValidationBuilder(fieldName);
        configure(fieldBuilder);
        _rules.AddField(fieldName, fieldBuilder);
        return this;
    }
    
    public FormValidationRules Build() => _rules;
}