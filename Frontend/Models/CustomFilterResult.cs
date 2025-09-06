namespace Frontend.Models;

public class CustomFilterResult
{
    public string FieldName { get; set; } = "";
    public string? Operator { get; set; }
    public object? Value { get; set; }
    public Type DataType { get; set; } = typeof(string);
    public bool IsClear { get; set; } = false;
}