namespace RemotePrintCore.Web.Models.Entities;

using System.ComponentModel.DataAnnotations;

public class Printer : BaseEntity
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    public int Port { get; set; } = 9100;

    public bool IsActive { get; set; } = true;
}
