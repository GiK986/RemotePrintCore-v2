using Microsoft.EntityFrameworkCore;
using RemotePrintCore.Web.Data;
using RemotePrintCore.Web.Models.Entities;

namespace RemotePrintCore.Web.Services.Banners;

public class BannerService : IBannerService
{
    private readonly AppDbContext _db;

    public BannerService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<string?> GetRandomActiveBannerFileNameAsync()
    {
        var now = DateTime.UtcNow;

        var fileNames = await _db.Banners
            .Where(b => b.FromDate <= now && b.ToDate >= now)
            .Select(b => b.FileName)
            .ToListAsync();

        if (fileNames.Count == 0)
            return null;

        return fileNames[Random.Shared.Next(fileNames.Count)];
    }

    public async Task<List<Banner>> GetAllAsync()
    {
        return await _db.Banners
            .OrderByDescending(b => b.CreatedOn)
            .ToListAsync();
    }

    public async Task<Banner> CreateAsync(string name, DateTime from, DateTime to, string fileName)
    {
        var banner = new Banner
        {
            Name = name,
            FromDate = from,
            ToDate = to,
            FileName = fileName,
        };

        _db.Banners.Add(banner);
        await _db.SaveChangesAsync();

        return banner;
    }

    public async Task UpdateAsync(int id, string name, DateTime from, DateTime to)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner is null) return;

        banner.Name = name;
        banner.FromDate = from;
        banner.ToDate = to;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner is null) return;

        _db.Banners.Remove(banner);
        await _db.SaveChangesAsync();
    }
}
