using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using Anma.Applications.Helpers;

namespace Anma.Applications.Database;

public class TableService
{
    private readonly AppDbContext _context;

    public TableService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TableEntity?> CreateAsync(int workspaceId, string dbSlug, CreateTableDto dto, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var db = await _context.Databases
            .Include(d => d.Workspace)
            .FirstOrDefaultAsync(d => d.Slug == dbSlug && d.WorkspaceId == workspaceId && d.Workspace.OwnerId == userId);

        if (db == null) return null;

        var table = new TableEntity
        {
            Name = dto.Name,
            Slug = SlugHelper.GenerateSlug(dto.Name),
            DatabaseId = db.Id,
            ColumnsJson = "{}"
        };

        _context.Tables.Add(table);
        await _context.SaveChangesAsync();

        return table;
    }

    public async Task<List<TableEntity>> GetAllAsync(int workspaceId, string dbSlug, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var db = await _context.Databases
            .Include(d => d.Workspace)
            .FirstOrDefaultAsync(d => d.Slug == dbSlug && d.WorkspaceId == workspaceId && d.Workspace.OwnerId == userId);

        if (db == null) return new();

        return await _context.Tables
            .Where(t => t.DatabaseId == db.Id)
            .ToListAsync();
    }

    public async Task<TableEntity?> GetBySlugAsync(int workspaceId, string dbSlug, string tableSlug, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var db = await _context.Databases
            .Include(d => d.Workspace)
            .FirstOrDefaultAsync(d => d.Slug == dbSlug && d.WorkspaceId == workspaceId && d.Workspace.OwnerId == userId);

        if (db == null) return null;

        return await _context.Tables
            .FirstOrDefaultAsync(t => t.DatabaseId == db.Id && t.Slug == tableSlug);
    }

    public async Task<TableEntity?> UpdateAsync(int workspaceId, string dbSlug, string tableSlug, CreateTableDto dto, string? newColumnsJson, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var db = await _context.Databases
            .Include(d => d.Workspace)
            .FirstOrDefaultAsync(d => d.Slug == dbSlug && d.WorkspaceId == workspaceId && d.Workspace.OwnerId == userId);

        if (db == null) return null;

        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.DatabaseId == db.Id && t.Slug == tableSlug);

        if (table == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            table.Name = dto.Name;
            table.Slug = SlugHelper.GenerateSlug(dto.Name);
        }

        if (!string.IsNullOrWhiteSpace(newColumnsJson))
        {
            table.ColumnsJson = newColumnsJson;
        }

        await _context.SaveChangesAsync();
        return table;
    }

}

