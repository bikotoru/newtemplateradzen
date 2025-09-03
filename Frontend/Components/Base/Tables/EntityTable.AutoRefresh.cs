using Radzen.Blazor;

namespace Frontend.Components.Base.Tables;

public partial class EntityTable<T>
{
    #region Auto Refresh Methods

    private async Task OnSplitButtonClick(RadzenSplitButtonItem item, string buttonName = "AutoRefresh")
    {
        if (item != null && item.Value != null)
        {
            if (int.TryParse(item.Value.ToString(), out var interval))
            {
                await SetAutoRefresh(interval);
            }
        }
        else
        {
            await ManualRefresh();
        }
    }

    private async Task ManualRefresh()
    {
        if (currentAutoRefreshInterval > 0)
        {
            autoRefreshCountdown = currentAutoRefreshInterval;
        }
        
        if (OnRefresh.HasDelegate)
        {
            await OnRefresh.InvokeAsync();
        }
        else
        {
            await Reload();
        }
    }

    private async Task SetAutoRefresh(int intervalSeconds)
    {
        StopAutoRefresh();

        if (intervalSeconds > 0)
        {
            currentAutoRefreshInterval = intervalSeconds;
            autoRefreshCountdown = intervalSeconds;

            countdownTimer = new Timer(async _ =>
            {
                autoRefreshCountdown--;
                if (autoRefreshCountdown <= 0)
                {
                    autoRefreshCountdown = currentAutoRefreshInterval;
                    await InvokeAsync(async () =>
                    {
                        await ManualRefresh();
                        StateHasChanged();
                    });
                }
                await InvokeAsync(StateHasChanged);
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }
    }

    private void StopAutoRefresh()
    {
        currentAutoRefreshInterval = 0;
        autoRefreshCountdown = 0;
        autoRefreshTimer?.Dispose();
        countdownTimer?.Dispose();
        autoRefreshTimer = null;
        countdownTimer = null;
    }

    private string GetRefreshText()
    {
        if (currentAutoRefreshInterval > 0)
        {
            return $"Actualizar ({autoRefreshCountdown}s)";
        }
        return RefreshButtonText;
    }

    private string GetAutoRefreshLabel(int seconds)
    {
        if (seconds <= 0) return "Nunca";
        
        var hours = seconds / 3600;
        var minutes = (seconds % 3600) / 60;
        var remainingSeconds = seconds % 60;
        
        var parts = new List<string>();
        
        if (hours > 0)
        {
            parts.Add(hours == 1 ? "1 hora" : $"{hours} horas");
        }
        
        if (minutes > 0)
        {
            parts.Add(minutes == 1 ? "1 minuto" : $"{minutes} minutos");
        }
        
        if (remainingSeconds > 0 && hours == 0)
        {
            parts.Add(remainingSeconds == 1 ? "1 segundo" : $"{remainingSeconds} segundos");
        }
        
        if (!parts.Any()) return "Nunca";
        
        var result = parts.Count switch
        {
            1 => parts[0],
            2 => $"{parts[0]} y {parts[1]}",
            _ => $"{string.Join(", ", parts.Take(parts.Count - 1))} y {parts.Last()}"
        };
        
        return $"Cada {result}";
    }

    #endregion
}