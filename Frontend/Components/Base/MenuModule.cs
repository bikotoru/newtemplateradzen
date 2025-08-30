namespace Frontend.Components.Base;

public class MenuModule
{
    public string Text { get; set; } = "";
    public string Icon { get; set; } = "";
    public List<MenuItem> MenuItems { get; set; } = new();
}