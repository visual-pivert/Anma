using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Anma.Applications.Helpers;


namespace Anma.Applications.Database;

public class DatabaseService
{
    private readonly AppDbContext _context;

    public DatabaseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DatabaseEntity?> CreateAsync(int workspaceId, CreateDatabaseDto dto, ClaimsPrincipal user)
    {
        // Vérifie si le workspace appartient à l'utilisateur
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId && w.OwnerId == userId);

        if (workspace == null) return null;

        var db = new DatabaseEntity
        {
            Name = dto.Name,
            Slug = SlugHelper.GenerateSlug(dto.Name),
            WorkspaceId = workspaceId
        };

        _context.Databases.Add(db);
        await _context.SaveChangesAsync();

        return db;
    }

    public async Task<List<DatabaseEntity>> GetAllForWorkspace(int workspaceId, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var ws = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId && w.OwnerId == userId);
        if (ws == null) return new();

        return await _context.Databases
            .Where(d => d.WorkspaceId == workspaceId)
            .ToListAsync();
    }

    public async Task<DatabaseEntity?> GetBySlugAsync(int workspaceId, string slug, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId && w.OwnerId == userId);

        if (workspace == null) return null;

        return await _context.Databases
            .FirstOrDefaultAsync(d => d.WorkspaceId == workspaceId && d.Slug == slug);
    }

}

