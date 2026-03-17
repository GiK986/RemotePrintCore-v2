namespace RemotePrintCore.Web.Models.Entities;

using System.ComponentModel.DataAnnotations;

public class Banner : BaseEntity
{
    [Required, MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(60)]
    public string FileName { get; set; } = string.Empty;

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }
}
