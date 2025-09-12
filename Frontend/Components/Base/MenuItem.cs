namespace Frontend.Components.Base;

public class MenuItem
{
    public string Text { get; set; } = "";
    public string Icon { get; set; } = "";
    public string Path { get; set; } = "";
    public List<string> Permissions { get; set; } = new();
    public List<MenuItem> SubItems { get; set; } = new();
}