namespace Anma.Applications.Workspace;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Anma;

[ApiController]
[Route("workspaces/{workspaceId}/content")]
[Authorize]
public class WorkspaceContentController : ControllerBase
{
    private readonly AppDbContext _context;

    public WorkspaceContentController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetWorkspaceContent(int workspaceId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var ws = await _context.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId && w.OwnerId == userId);
        if (ws == null) return NotFound("Workspace non trouvé ou accès interdit");

        var databases = await _context.Databases
            .Where(d => d.WorkspaceId == workspaceId)
            .ToListAsync();

        var notebooks = await _context.Notebooks
            .Where(n => n.WorkspaceId == workspaceId)
            .ToListAsync();

        return Ok(new
        {
            Workspace = ws,
            Databases = databases,
            Notebooks = notebooks
        });
    }
}

