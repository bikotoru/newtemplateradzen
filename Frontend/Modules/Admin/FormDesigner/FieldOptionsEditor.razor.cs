using Microsoft.AspNetCore.Components;
using Radzen;
using Forms.Models.Configurations;

namespace Frontend.Modules.Admin.FormDesigner;

public partial class FieldOptionsEditor : ComponentBase
{
    [Inject] public DialogService DialogService { get; set; } = default!;

    [Parameter] public string FieldName { get; set; } = "";
    [Parameter] public List<SelectOption> Options { get; set; } = new();

    private List<SelectOption> WorkingOptions = new();

    protected override void OnInitialized()
    {
        // Crear una copia de trabajo de las opciones
        WorkingOptions = Options.Select(o => new SelectOption
        {
            Value = o.Value,
            Label = o.Label,
            Disabled = o.Disabled
        }).ToList();

        // Si no hay opciones, agregar una por defecto
        if (!WorkingOptions.Any())
        {
            AddNewOption();
        }
    }

    private void AddNewOption()
    {
        var newOption = new SelectOption
        {
            Value = $"opcion_{WorkingOptions.Count + 1}",
            Label = $"Opción {WorkingOptions.Count + 1}",
            Disabled = false
        };

        WorkingOptions.Add(newOption);
        StateHasChanged();
    }

    private void RemoveOption(int index)
    {
        if (index >= 0 && index < WorkingOptions.Count)
        {
            WorkingOptions.RemoveAt(index);
            StateHasChanged();
        }
    }

    private void MoveOptionUp(int index)
    {
        if (index > 0 && index < WorkingOptions.Count)
        {
            var option = WorkingOptions[index];
            WorkingOptions.RemoveAt(index);
            WorkingOptions.Insert(index - 1, option);
            StateHasChanged();
        }
    }

    private void MoveOptionDown(int index)
    {
        if (index >= 0 && index < WorkingOptions.Count - 1)
        {
            var option = WorkingOptions[index];
            WorkingOptions.RemoveAt(index);
            WorkingOptions.Insert(index + 1, option);
            StateHasChanged();
        }
    }

    private void SaveOptions()
    {
        // Validar que todas las opciones tengan al menos label y value
        var validOptions = WorkingOptions.Where(o =>
            !string.IsNullOrWhiteSpace(o.Label) &&
            !string.IsNullOrWhiteSpace(o.Value)).ToList();

        DialogService.Close(validOptions);
    }

    private void Cancel()
    {
        DialogService.Close(null);
    }
}