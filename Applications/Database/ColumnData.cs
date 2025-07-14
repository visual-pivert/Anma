public class ColumnData
{
    public string Type { get; set; } = "string"; // string, number, reference, etc.
    public List<object> Value { get; set; } = new();

    // Facultatif : utilisé si Type == "choice"
    public List<string>? Options { get; set; }
}
