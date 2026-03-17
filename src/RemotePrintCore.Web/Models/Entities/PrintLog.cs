namespace RemotePrintCore.Web.Models.Entities;

public class PrintLog
{
    public int Id { get; set; }

    public string DocumentNumber { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    public string PrinterName { get; set; } = string.Empty;

    public string TemplateName { get; set; } = string.Empty;

    public int NumberOfCopies { get; set; }

    public PrintStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedOn { get; set; }
}

public enum PrintStatus
{
    Success,
    Error
}
