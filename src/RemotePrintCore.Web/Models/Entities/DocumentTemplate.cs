namespace RemotePrintCore.Web.Models.Entities;

using System.ComponentModel.DataAnnotations;

public class DocumentTemplate : BaseEntity
{
    [Required, MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Values { get; set; } = string.Empty;
}
