using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Security.Claims;
using Anma.Applications.Helpers;
using System.Text;

namespace Anma.Applications.Notebook;

public class NotebookService
{
    private readonly AppDbContext _context;

    public NotebookService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotebookEntity?> CreateAsync(int workspaceId, CreateNotebookDto dto, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var ws = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId && w.OwnerId == userId);
        if (ws == null) return null;

        var notebook = new NotebookEntity
        {
            Name = dto.Name,
            Slug = SlugHelper.GenerateSlug(dto.Name),
            WorkspaceId = workspaceId,
            Content = JsonSerializer.Serialize(dto.Content)
        };

        _context.Notebooks.Add(notebook);
        await _context.SaveChangesAsync();
        return notebook;
    }

    public async Task<List<NotebookEntity>> GetAllForWorkspace(int workspaceId, ClaimsPrincipal user)
    {
        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var ws = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId && w.OwnerId == userId);
        if (ws == null) return new();

        return await _context.Notebooks
            .Where(n => n.WorkspaceId == workspaceId)
            .ToListAsync();
    }

}

