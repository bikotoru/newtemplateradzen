using Microsoft.AspNetCore.Components;
using Radzen;

namespace Frontend.Modules.Admin.DatabaseMigration;

public partial class DatabaseMigrationPage : ComponentBase
{
    private bool _migrationExecuted = false;
    private bool _checking = false;
    private bool _executing = false;
    private bool _testing = false;

    private string _statusMessage = "";
    private AlertStyle _alertStyle = AlertStyle.Info;
    private string _constraintDefinition = "";

    // Test fields
    private string _testEntityName = "TestEntity";
    private string _testFieldName = "TestField";
    private string _testFieldType = "entity_reference";

    private readonly List<FieldTypeOption> _fieldTypes = new()
    {
        new FieldTypeOption("entity_reference", "Entity Reference"),
        new FieldTypeOption("user_reference", "User Reference"),
        new FieldTypeOption("file_reference", "File Reference")
    };

    protected override async Task OnInitializedAsync()
    {
        await CheckMigrationStatus();
    }

    private async Task CheckMigrationStatus()
    {
        try
        {
            _checking = true;
            StateHasChanged();

            var response = await Api.GetAsync<MigrationStatusResponse>("api/DatabaseMigration/check-migration-status");

            if (response.Success && response.Data != null)
            {
                _migrationExecuted = response.Data.MigrationExecuted;
                _constraintDefinition = response.Data.ConstraintDefinition;
                _statusMessage = response.Data.Message;
                _alertStyle = _migrationExecuted ? AlertStyle.Success : AlertStyle.Warning;
            }
            else
            {
                _statusMessage = response.Message ?? "Error verificando estado de migración";
                _alertStyle = AlertStyle.Danger;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error verificando estado de migración");
            _statusMessage = $"Error: {ex.Message}";
            _alertStyle = AlertStyle.Danger;
        }
        finally
        {
            _checking = false;
            StateHasChanged();
        }
    }

    private async Task ExecuteMigration()
    {
        try
        {
            _executing = true;
            StateHasChanged();

            var response = await Api.PostAsync<MigrationResponse>("api/DatabaseMigration/execute-reference-fields-migration", new { });

            if (response.Success && response.Data != null)
            {
                _migrationExecuted = true;
                _statusMessage = response.Data.Message;
                _alertStyle = AlertStyle.Success;

                // Verificar estado después de ejecutar
                await CheckMigrationStatus();
            }
            else
            {
                _statusMessage = response.Message ?? "Error ejecutando migración";
                _alertStyle = AlertStyle.Danger;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error ejecutando migración");
            _statusMessage = $"Error: {ex.Message}";
            _alertStyle = AlertStyle.Danger;
        }
        finally
        {
            _executing = false;
            StateHasChanged();
        }
    }

    private async Task TestFieldCreation()
    {
        try
        {
            _testing = true;
            StateHasChanged();

            var testField = new
            {
                EntityName = _testEntityName,
                FieldName = _testFieldName + "_" + Guid.NewGuid().ToString("N")[..8],
                DisplayName = $"Test {_testFieldType} Field",
                FieldType = _testFieldType,
                IsRequired = false,
                Active = true
            };

            var response = await Api.PostAsync<object>("api/test-customfields", testField);

            if (response.Success)
            {
                _statusMessage = $"✅ Campo de prueba creado exitosamente: {testField.FieldName}";
                _alertStyle = AlertStyle.Success;
            }
            else
            {
                _statusMessage = $"❌ Error creando campo de prueba: {response.Message}";
                _alertStyle = AlertStyle.Danger;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error probando creación de campo");
            _statusMessage = $"❌ Error: {ex.Message}";
            _alertStyle = AlertStyle.Danger;
        }
        finally
        {
            _testing = false;
            StateHasChanged();
        }
    }
}

public class FieldTypeOption
{
    public string Value { get; set; }
    public string Text { get; set; }

    public FieldTypeOption(string value, string text)
    {
        Value = value;
        Text = text;
    }
}

public class MigrationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string? Details { get; set; }
}

public class MigrationStatusResponse
{
    public bool Success { get; set; }
    public bool MigrationExecuted { get; set; }
    public string ConstraintDefinition { get; set; } = "";
    public string Message { get; set; } = "";
}