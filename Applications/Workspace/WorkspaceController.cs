using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Anma.Applications.Workspace;
using Anma.Applications.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Anma.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize] // Nécessite un JWT valide
public class WorkspaceController : ControllerBase
{
    private readonly ILogger<WorkspaceController> _logger;
    private readonly AppDbContext _context;

    public WorkspaceController(ILogger<WorkspaceController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<WorkspaceEntity>> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Invalid token");

        int ownerId = int.Parse(userIdClaim.Value);

        var slug = SlugHelper.GenerateSlug(dto.Name);

        var workspace = new WorkspaceEntity
        {
            Name = dto.Name,
            Slug = slug,
            Color = dto.Color ?? "#FFFFFF",
            Logo = dto.Logo ?? "",
            OwnerId = ownerId
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Workspace '{Name}' (ID {Id}) créé par l'utilisateur {OwnerId}", workspace.Name, workspace.Id, ownerId);

        return Ok(workspace);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<WorkspaceEntity>> GetWorkspace(string slug)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == slug);

        if (workspace == null) return NotFound();
        return Ok(workspace);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> DeleteWorkspace(string slug)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Invalid JWT");

        int ownerId = int.Parse(userIdClaim.Value);

        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Slug == slug);

        if (workspace == null)
            return NotFound($"Aucun workspace avec le slug '{slug}'.");

        if (workspace.OwnerId != ownerId)
            return Forbid("Vous n'êtes pas autorisé à supprimer ce workspace.");

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Workspace '{Slug}' supprimé par l'utilisateur {OwnerId}", slug, ownerId);

        return NoContent();
    }

    [HttpPut("{slug}")]
    public async Task<ActionResult<WorkspaceEntity>> UpdateWorkspace(string slug, [FromBody] UpdateWorkspaceDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Invalid JWT");

        int ownerId = int.Parse(userIdClaim.Value);

        var workspace = await _context.Workspaces.FirstOrDefaultAsync(w => w.Slug == slug);

        if (workspace == null)
            return NotFound($"Aucun workspace avec le slug '{slug}'.");

        if (workspace.OwnerId != ownerId)
            return Forbid("Vous n'êtes pas autorisé à modifier ce workspace.");

        // Mise à jour des champs modifiables
        if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != workspace.Name)
        {
            workspace.Name = dto.Name;
            workspace.Slug = SlugHelper.GenerateSlug(dto.Name); // Met à jour aussi le slug si le nom change
        }

        if (!string.IsNullOrWhiteSpace(dto.Color))
            workspace.Color = dto.Color;

        if (dto.Logo != null)
            workspace.Logo = dto.Logo;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Workspace '{Slug}' mis à jour par l'utilisateur {OwnerId}", workspace.Slug, ownerId);

        return Ok(workspace);
    }

    [HttpGet]
    public async Task<ActionResult<List<WorkspaceEntity>>> GetWorkspaces()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized("Invalid token");

        int ownerId = int.Parse(userIdClaim.Value);

        var workspaces = await _context.Workspaces
            .Where(w => w.OwnerId == ownerId)
            .ToListAsync();

        return Ok(workspaces);
    }
}

