namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Select Optimization Methods

    private bool ShouldUseSelectOptimization()
    {
        var shouldUse = EnableSelectOptimization && SelectOnlyVisibleColumns && HasVisibleColumnsConfigured();
        Console.WriteLine($"[DEBUG] ShouldUseSelectOptimization: {shouldUse} (EnableSelectOptimization: {EnableSelectOptimization}, SelectOnlyVisibleColumns: {SelectOnlyVisibleColumns}, HasVisibleColumnsConfigured: {HasVisibleColumnsConfigured()})");
        return shouldUse;
    }

    private bool HasVisibleColumnsConfigured()
    {
        if (ColumnConfigs != null && ColumnConfigs.Any())
        {
            var visibleConfigs = ColumnConfigs.Where(c => c.Visible).ToList();
            
            Console.WriteLine($"[DEBUG] ColumnConfigs detectadas - Total: {ColumnConfigs.Count}, Visible: {visibleConfigs.Count}");
            Console.WriteLine($"[DEBUG] Campos visibles: {string.Join(", ", visibleConfigs.Select(c => c.Property))}");
            
            var shouldOptimize = visibleConfigs.Any();
            Console.WriteLine($"[DEBUG] ColumnConfigs optimization decision: {shouldOptimize}");
            return shouldOptimize;
        }
        
        if (grid?.ColumnsCollection != null)
        {
            var allColumns = grid.ColumnsCollection.Where(c => !string.IsNullOrEmpty(c.Property) && c.Property != "Actions").ToList();
            var visibleColumns = allColumns.Where(c => c.Visible).ToList();
            
            var hasCustomizedView = allColumns.Count > visibleColumns.Count && visibleColumns.Count > 0;
            
            Console.WriteLine($"[DEBUG] Grid columns - Total: {allColumns.Count}, Visible: {visibleColumns.Count}, Customized: {hasCustomizedView}");
            Console.WriteLine($"[DEBUG] Columnas disponibles: {string.Join(", ", allColumns.Select(c => $"{c.Property}({c.Visible})"))}");
            
            return hasCustomizedView;
        }

        Console.WriteLine("[DEBUG] No hay configuración específica de columnas");
        return false;
    }

    private string BuildSelectString()
    {
        var fields = new HashSet<string>();

        if (AlwaysSelectFields != null)
        {
            foreach (var field in AlwaysSelectFields)
            {
                fields.Add(field);
            }
        }

        var visibleFields = GetVisibleFieldNames();
        foreach (var field in visibleFields)
        {
            fields.Add(field);
        }

        if (visibleFields.Count == 0)
        {
            Console.WriteLine("[DEBUG] No hay columnas visibles específicas, usando select básico");
            return "new { Id }";
        }

        var fieldsStr = string.Join(", ", fields.OrderBy(f => f));
        var selectString = $"new {{ {fieldsStr} }}";
        
        Console.WriteLine($"[DEBUG] Select string generado: {selectString}");
        return selectString;
    }

    private List<string> GetVisibleFieldNames()
    {
        var fields = new List<string>();

        if (ColumnConfigs != null && ColumnConfigs.Any())
        {
            foreach (var config in ColumnConfigs.Where(c => c.Visible))
            {
                if (IsSimpleProperty(config.Property))
                {
                    fields.Add(config.Property);
                }
            }
            
            Console.WriteLine($"[DEBUG] Campos desde ColumnConfigs: {string.Join(", ", fields)}");
        }
        else if (grid?.ColumnsCollection != null)
        {
            var userVisibleColumns = grid.ColumnsCollection.Where(c => 
                c.Visible && 
                !string.IsNullOrEmpty(c.Property) && 
                c.Property != "Actions"
            ).ToList();

            foreach (var column in userVisibleColumns)
            {
                if (IsSimpleProperty(column.Property!))
                {
                    fields.Add(column.Property!);
                }
            }

            Console.WriteLine($"[DEBUG] Campos desde Grid: {string.Join(", ", fields)}");
        }

        return fields.Distinct().ToList();
    }

    private bool IsSimpleProperty(string propertyName)
    {
        var complexTypes = new[] { ".", "Usuario", "Modificado", "Creado" };
        return !complexTypes.Any(complex => propertyName.Contains(complex, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}