namespace Anma.Applications.Notebook;


public class ExecuteCodeDto
{
    public string Input { get; set; } = "";
    public List<NotebookOutputItem> Outputs { get; set; } = new();
}

public class NotebookOutputItem
{
    public string Type { get; set; } = "text"; // Ex: "text", "image", "html", "error", etc.
    public string Value { get; set; }  = "";       // Base64 ou texte brut
}

