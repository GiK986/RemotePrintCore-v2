namespace RemotePrintCore.Web.Services.Banners;

using RemotePrintCore.Web.Models.Entities;

public interface IBannerService
{
    Task<string?> GetRandomActiveBannerFileNameAsync();
    Task<List<Banner>> GetAllAsync();
    Task<Banner> CreateAsync(string name, DateTime from, DateTime to, string fileName);
    Task UpdateAsync(int id, string name, DateTime from, DateTime to);
    Task DeleteAsync(int id);
}
