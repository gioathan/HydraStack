using Hydra.Api.Data;
using Hydra.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hydra.Api.Repositories.VenuePhotos;

public class VenuePhotoRepository : IVenuePhotoRepository
{
    private readonly AppDbContext _context;

    public VenuePhotoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<VenuePhoto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.VenuePhotos.FindAsync([id], ct);
    }

    public async Task<VenuePhoto> AddAsync(VenuePhoto photo, CancellationToken ct = default)
    {
        _context.VenuePhotos.Add(photo);
        await _context.SaveChangesAsync(ct);
        return photo;
    }

    public async Task DeleteAsync(VenuePhoto photo, CancellationToken ct = default)
    {
        _context.VenuePhotos.Remove(photo);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<VenuePhoto> photos, CancellationToken ct = default)
    {
        _context.VenuePhotos.UpdateRange(photos);
        await _context.SaveChangesAsync(ct);
    }
}
