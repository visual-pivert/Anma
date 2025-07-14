using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Anma.Applications.Database;
using Anma.Applications.Helpers;

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Anma.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId}/databases")]
[Authorize]
public class DatabaseController : ControllerBase
{
    private readonly DatabaseService _service;
    private readonly ILogger<DatabaseController> _logger;
    private readonly AppDbContext _context;

    public DatabaseController(AppDbContext context, DatabaseService service, ILogger<DatabaseController> logger)
    {
        _service = service;
        _logger = logger;
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<DatabaseEntity>> CreateDatabase(int workspaceId, [FromBody] CreateDatabaseDto dto)
    {
        var db = await _service.CreateAsync(workspaceId, dto, User);

        if (db == null) return Forbid("Vous n'avez pas accès à ce workspace");

        _logger.LogInformation("Nouvelle base de données '{Name}' créée dans le workspace {WorkspaceId}", db.Name, workspaceId);
        return Ok(db);
    }

    [HttpGet]
    public async Task<ActionResult<List<DatabaseEntity>>> ListDatabases(int workspaceId)
    {
        var list = await _service.GetAllForWorkspace(workspaceId, User);
        return Ok(list);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<DatabaseEntity>> GetDatabaseBySlug(int workspaceId, string slug)
    {
        var db = await _service.GetBySlugAsync(workspaceId, slug, User);

        if (db == null)
            return NotFound($"Aucune base de données trouvée avec le slug '{slug}' dans ce workspace.");

        return Ok(db);
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> UpdateDatabase(int workspaceId, string slug, [FromBody] CreateDatabaseDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var db = await _context.Databases
            .Include(d => d.Workspace)
            .FirstOrDefaultAsync(d => d.Slug == slug && d.WorkspaceId == workspaceId && d.Workspace.OwnerId == userId);

        if (db == null) return NotFound();

        db.Name = dto.Name;
        db.Slug = SlugHelper.GenerateSlug(dto.Name);

        await _context.SaveChangesAsync();
        return Ok(db);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> DeleteDatabase(int workspaceId, string slug)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var db = await _context.Databases
            .Include(d => d.Workspace)
            .FirstOrDefaultAsync(d => d.Slug == slug && d.WorkspaceId == workspaceId && d.Workspace.OwnerId == userId);

        if (db == null) return NotFound();

        _context.Databases.Remove(db);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

