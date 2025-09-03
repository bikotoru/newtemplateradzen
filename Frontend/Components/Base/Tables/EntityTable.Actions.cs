using Radzen;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Action Methods

    private async Task HandleEdit(T item)
    {
        if (OnEdit.HasDelegate)
        {
            await OnEdit.InvokeAsync(item);
        }
    }

    private async Task HandleDelete(T item)
    {
        if (OnDelete.HasDelegate)
        {
            var confirm = await DialogService.Confirm(
                "¿Está seguro que desea eliminar este registro?", 
                "Confirmar eliminación", 
                new ConfirmOptions { OkButtonText = "Sí", CancelButtonText = "No" }
            );

            if (confirm == true)
            {
                await OnDelete.InvokeAsync(item);
                await Reload();
            }
        }
    }

    #endregion
}