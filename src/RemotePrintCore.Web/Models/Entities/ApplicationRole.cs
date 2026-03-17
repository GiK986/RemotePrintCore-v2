namespace RemotePrintCore.Web.Models.Entities;

using Microsoft.AspNetCore.Identity;

public class ApplicationRole : IdentityRole
{
    public DateTime CreatedOn { get; set; }

    public DateTime? ModifiedOn { get; set; }
}
