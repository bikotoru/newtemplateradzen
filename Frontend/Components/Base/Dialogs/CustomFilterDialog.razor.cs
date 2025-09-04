using Microsoft.AspNetCore.Components;
using Radzen;
using System.Reflection;
using Frontend.Models;

namespace Frontend.Components.Base.Dialogs;

public partial class CustomFilterDialog : ComponentBase
{
    [Inject] private DialogService dialogService { get; set; } = null!;

    [Parameter] public string FieldName { get; set; } = "";
    [Parameter] public Type DataType { get; set; } = typeof(string);
    [Parameter] public object? CurrentFilterValue { get; set; }
    [Parameter] public string? CurrentFilterOperator { get; set; }

    private string selectedOperator = "";
    private string filterValue = "";
    private decimal? numericValue;
    private bool? boolValue;
    private DateTime? dateValue;

    protected override void OnInitialized()
    {
        // Inicializar con valores actuales si existen
        if (!string.IsNullOrEmpty(CurrentFilterOperator))
        {
            selectedOperator = CurrentFilterOperator;
        }
        else
        {
            // Por defecto usar "Contains" para strings, "Equals" para otros tipos
            selectedOperator = DataType == typeof(string) ? "Contains" : "Equals";
        }

        if (CurrentFilterValue != null)
        {
            if (DataType == typeof(string))
            {
                filterValue = CurrentFilterValue.ToString() ?? "";
            }
            else if (IsNumericType(DataType))
            {
                if (decimal.TryParse(CurrentFilterValue.ToString(), out var num))
                {
                    numericValue = num;
                }
            }
            else if (DataType == typeof(bool) || DataType == typeof(bool?))
            {
                if (bool.TryParse(CurrentFilterValue.ToString(), out var boolVal))
                {
                    boolValue = boolVal;
                }
            }
            else if (DataType == typeof(DateTime) || DataType == typeof(DateTime?))
            {
                if (DateTime.TryParse(CurrentFilterValue.ToString(), out var dateVal))
                {
                    dateValue = dateVal;
                }
            }
        }
    }

    private string DataTypeName
    {
        get
        {
            if (DataType == typeof(string)) return "Texto";
            if (IsNumericType(DataType)) return "Número";
            if (DataType == typeof(bool) || DataType == typeof(bool?)) return "Booleano (Sí/No)";
            if (DataType == typeof(DateTime) || DataType == typeof(DateTime?)) return "Fecha";
            if (DataType == typeof(Guid) || DataType == typeof(Guid?)) return "Identificador";
            return DataType.Name;
        }
    }

    private bool IsNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte);
    }

    private List<dynamic> GetStringOperators()
    {
        return new List<dynamic>
        {
            new { Text = "Contiene", Value = "Contains" },
            new { Text = "No contiene", Value = "DoesNotContain" },
            new { Text = "Igual a", Value = "Equals" },
            new { Text = "No es igual a", Value = "NotEquals" },
            new { Text = "Inicia con", Value = "StartsWith" },
            new { Text = "Termina con", Value = "EndsWith" },
            new { Text = "Es nulo", Value = "IsNull" },
            new { Text = "No es nulo", Value = "IsNotNull" },
            new { Text = "Está vacío", Value = "IsEmpty" },
            new { Text = "No está vacío", Value = "IsNotEmpty" }
        };
    }

    private List<dynamic> GetNumericOperators()
    {
        return new List<dynamic>
        {
            new { Text = "Igual a", Value = "Equals" },
            new { Text = "No es igual a", Value = "NotEquals" },
            new { Text = "Mayor que", Value = "GreaterThan" },
            new { Text = "Mayor que o igual", Value = "GreaterThanOrEquals" },
            new { Text = "Menor que", Value = "LessThan" },
            new { Text = "Menor que o igual", Value = "LessThanOrEquals" },
            new { Text = "Es nulo", Value = "IsNull" },
            new { Text = "No es nulo", Value = "IsNotNull" }
        };
    }

    private List<dynamic> GetBooleanOperators()
    {
        return new List<dynamic>
        {
            new { Text = "Igual a", Value = "Equals" },
            new { Text = "No es igual a", Value = "NotEquals" },
            new { Text = "Es nulo", Value = "IsNull" },
            new { Text = "No es nulo", Value = "IsNotNull" }
        };
    }

    private List<dynamic> GetBooleanValues()
    {
        return new List<dynamic>
        {
            new { Text = "Verdadero", Value = true },
            new { Text = "Falso", Value = false }
        };
    }

    private List<dynamic> GetDateOperators()
    {
        return new List<dynamic>
        {
            new { Text = "Igual a", Value = "Equals" },
            new { Text = "No es igual a", Value = "NotEquals" },
            new { Text = "Mayor que", Value = "GreaterThan" },
            new { Text = "Mayor que o igual", Value = "GreaterThanOrEquals" },
            new { Text = "Menor que", Value = "LessThan" },
            new { Text = "Menor que o igual", Value = "LessThanOrEquals" },
            new { Text = "Es nulo", Value = "IsNull" },
            new { Text = "No es nulo", Value = "IsNotNull" }
        };
    }

    private async Task ApplyFilter()
    {
        if (string.IsNullOrEmpty(selectedOperator))
        {
            await dialogService.Alert("Seleccione un operador", "Error");
            return;
        }

        object? finalValue = null;

        if (selectedOperator != "IsNull" && selectedOperator != "IsNotNull" && 
            selectedOperator != "IsEmpty" && selectedOperator != "IsNotEmpty")
        {
            if (DataType == typeof(string))
            {
                if (string.IsNullOrEmpty(filterValue))
                {
                    await dialogService.Alert("Ingrese un valor para filtrar", "Error");
                    return;
                }
                finalValue = filterValue;
            }
            else if (IsNumericType(DataType))
            {
                if (numericValue == null)
                {
                    await dialogService.Alert("Ingrese un valor numérico", "Error");
                    return;
                }
                finalValue = numericValue;
            }
            else if (DataType == typeof(bool) || DataType == typeof(bool?))
            {
                if (boolValue == null)
                {
                    await dialogService.Alert("Seleccione un valor booleano", "Error");
                    return;
                }
                finalValue = boolValue;
            }
            else if (DataType == typeof(DateTime) || DataType == typeof(DateTime?))
            {
                if (dateValue == null)
                {
                    await dialogService.Alert("Seleccione una fecha", "Error");
                    return;
                }
                finalValue = dateValue;
            }
        }

        var result = new CustomFilterResult
        {
            FieldName = FieldName,
            Operator = selectedOperator,
            Value = finalValue,
            DataType = DataType
        };

        dialogService.Close(result);
    }

    private async Task ClearFilter()
    {
        var result = new CustomFilterResult
        {
            FieldName = FieldName,
            Operator = null,
            Value = null,
            DataType = DataType,
            IsClear = true
        };

        dialogService.Close(result);
    }
}