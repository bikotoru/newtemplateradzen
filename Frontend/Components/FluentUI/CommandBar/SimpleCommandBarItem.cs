using System.Windows.Input;

namespace Frontend.Components.FluentUI;

public class SimpleCommandBarItem
{
    public string Key { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public bool IconOnly { get; set; } = false;
    public bool Disabled { get; set; } = false;
    public bool Checked { get; set; } = false;
    public bool CanCheck { get; set; } = false;
    public bool Toggle { get; set; } = false;
    public bool IsRadioButton { get; set; } = false;
    public string GroupName { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string Target { get; set; } = "_self";
    
    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }
    
    public Action<SimpleItemClickedArgs>? OnClick { get; set; }
    
    public SimpleCommandBarItemType ItemType { get; set; } = SimpleCommandBarItemType.Normal;
}

public class SimpleItemClickedArgs
{
    public string Key { get; set; } = string.Empty;
}

public enum SimpleCommandBarItemType
{
    Normal,
    Divider,
    Header
}