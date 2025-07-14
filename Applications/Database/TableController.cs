using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Anma.Applications.Database;

using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

using Anma.Applications.Helpers;

namespace Anma.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId}/databases/{dbSlug}/tables")]
[Authorize]
public class TableController : ControllerBase
{
    private readonly TableService _service;
    private readonly AppDbContext _context;

    public TableController(AppDbContext context, TableService service)
    {
        _service = service;
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<TableEntity>> CreateTable(int workspaceId, string dbSlug, [FromBody] CreateTableDto dto)
    {
        var table = await _service.CreateAsync(workspaceId, dbSlug, dto, User);
        if (table == null)
            return Forbid("Base de données introuvable ou accès refusé");

        return Ok(table);
    }

    [HttpGet]
    public async Task<ActionResult<List<TableEntity>>> GetTables(int workspaceId, string dbSlug)
    {
        var tables = await _service.GetAllAsync(workspaceId, dbSlug, User);
        return Ok(tables);
    }

    [HttpGet("{tableSlug}")]
    public async Task<ActionResult<TableEntity>> GetTableBySlug(int workspaceId, string dbSlug, string tableSlug)
    {
        var table = await _service.GetBySlugAsync(workspaceId, dbSlug, tableSlug, User);
        if (table == null)
            return NotFound();

        return Ok(table);
    }

    [HttpPut("{tableSlug}")]
    public async Task<IActionResult> UpdateTable(int workspaceId, string dbSlug, string tableSlug, [FromBody] CreateTableDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var table = await _context.Tables
            .Include(t => t.Database)
            .ThenInclude(d => d.Workspace)
            .FirstOrDefaultAsync(t => t.Slug == tableSlug && t.Database.Slug == dbSlug && t.Database.WorkspaceId == workspaceId && t.Database.Workspace.OwnerId == userId);

        if (table == null) return NotFound();

        table.Name = dto.Name;
        table.Slug = SlugHelper.GenerateSlug(dto.Name);

        await _context.SaveChangesAsync();
        return Ok(table);
    }

    [HttpDelete("{tableSlug}")]
    public async Task<IActionResult> DeleteTable(int workspaceId, string dbSlug, string tableSlug)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var table = await _context.Tables
            .Include(t => t.Database)
            .ThenInclude(d => d.Workspace)
            .FirstOrDefaultAsync(t => t.Slug == tableSlug && t.Database.Slug == dbSlug && t.Database.WorkspaceId == workspaceId && t.Database.Workspace.OwnerId == userId);

        if (table == null) return NotFound();

        _context.Tables.Remove(table);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

