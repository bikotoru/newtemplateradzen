using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Concurrent;

namespace Frontend.Services.Validation;

public class ValidationContext : IDisposable
{
    private readonly Subject<ValidationChanged> _validationStream = new();
    private readonly ConcurrentDictionary<string, FieldValidationState> _fieldStates = new();
    private FormValidationRules _rules = new();
    private object? _entity;
    private IDisposable? _entitySubscription;
    
    public IObservable<ValidationChanged> ValidationChanges => _validationStream.AsObservable();
    
    public void Initialize(object entity, FormValidationRules rules)
    {
        _entity = entity;
        _rules = rules;
        
        // Suscribirse a cambios de la entidad si implementa INotifyPropertyChanged
        if (entity is INotifyPropertyChanged notifyEntity)
        {
            _entitySubscription?.Dispose();
            _entitySubscription = Observable
                .FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                    h => notifyEntity.PropertyChanged += h,
                    h => notifyEntity.PropertyChanged -= h)
                .Where(e => !string.IsNullOrEmpty(e.EventArgs.PropertyName))
                .Subscribe(async e => {
                    var propertyName = e.EventArgs.PropertyName!;
                    var value = GetPropertyValue(_entity, propertyName);
                    await ValidateFieldAsync(propertyName, value);
                });
        }
    }
    
    public async Task ValidateFieldAsync(string fieldName, object? value)
    {
        var rules = _rules.GetRulesForField(fieldName);
        var errors = new List<string>();
        
        // Ejecutar todas las reglas para este campo
        foreach (var rule in rules)
        {
            try
            {
                var result = await rule.ValidateAsync(value);
                if (!result.IsValid && !string.IsNullOrEmpty(result.ErrorMessage))
                {
                    errors.Add(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error en validación: {ex.Message}");
            }
        }
        
        // Actualizar estado del campo
        var fieldState = new FieldValidationState(fieldName, errors);
        _fieldStates[fieldName] = fieldState;
        
        // Notificar cambio
        _validationStream.OnNext(new ValidationChanged(fieldName, fieldState));
    }
    
    public async Task<bool> ValidateAllAsync()
    {
        if (_entity == null) return true;
        
        var entityType = _entity.GetType();
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        var validationTasks = new List<Task>();
        
        foreach (var property in properties)
        {
            var rules = _rules.GetRulesForField(property.Name);
            if (rules.Count > 0)
            {
                var value = property.GetValue(_entity);
                validationTasks.Add(ValidateFieldAsync(property.Name, value));
            }
        }
        
        await Task.WhenAll(validationTasks);
        
        return _fieldStates.Values.All(state => state.IsValid);
    }
    
    public FieldValidationState GetFieldState(string fieldName)
    {
        return _fieldStates.TryGetValue(fieldName, out var state) 
            ? state 
            : FieldValidationState.Valid(fieldName);
    }
    
    public List<string> GetAllErrors()
    {
        return _fieldStates.Values
            .SelectMany(state => state.Errors)
            .ToList();
    }
    
    public bool HasErrors => _fieldStates.Values.Any(state => state.HasErrors);
    
    public object? GetEntity() => _entity;
    
    private object? GetPropertyValue(object? obj, string propertyName)
    {
        if (obj == null) return null;
        
        try
        {
            var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return property?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }
    
    public void Dispose()
    {
        _entitySubscription?.Dispose();
        _validationStream?.Dispose();
    }
}