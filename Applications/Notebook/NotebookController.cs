using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Anma.Applications.Notebook;
using Anma.Applications.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Anma.Api.Controllers;

[ApiController]
[Route("workspaces/{workspaceId}/notebooks")]
[Authorize]
public class NotebookController : ControllerBase
{
    private readonly NotebookService _service;
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotebookController> _logger;
    private readonly NotebookExecutionService _notebookService;

    public NotebookController(AppDbContext context, IHttpClientFactory httpClientFactory, NotebookService service, NotebookExecutionService notebookService, ILogger<NotebookController> logger)
    {
        _service = service;
        _context = context;
        _httpClientFactory = httpClientFactory;
        _notebookService = notebookService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<NotebookEntity>> CreateNotebook(int workspaceId, [FromBody] CreateNotebookDto dto)
    {
        var note = await _service.CreateAsync(workspaceId, dto, User);
        if (note == null) return Forbid("Accès refusé ou workspace invalide");
        return Ok(note);
    }

    [HttpGet]
    public async Task<ActionResult<List<NotebookEntity>>> GetAllNotebooks(int workspaceId)
    {
        var list = await _service.GetAllForWorkspace(workspaceId, User);
        return Ok(list);
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> UpdateNotebook(int workspaceId, string slug, [FromBody] CreateNotebookDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var notebook = await _context.Notebooks
            .Include(n => n.Workspace)
            .FirstOrDefaultAsync(n => n.Slug == slug && n.WorkspaceId == workspaceId && n.Workspace.OwnerId == userId);

        if (notebook == null) return NotFound();

        notebook.Name = dto.Name;
        notebook.Content = JsonSerializer.Serialize(dto.Content);
        notebook.Slug = SlugHelper.GenerateSlug(dto.Name);

        await _context.SaveChangesAsync();
        return Ok(notebook);
    }

    [HttpDelete("{slug}")]
    public async Task<IActionResult> DeleteNotebook(int workspaceId, string slug)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var notebook = await _context.Notebooks
            .Include(n => n.Workspace)
            .FirstOrDefaultAsync(n => n.Slug == slug && n.WorkspaceId == workspaceId && n.Workspace.OwnerId == userId);

        if (notebook == null) return NotFound();

        _context.Notebooks.Remove(notebook);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("execute-cell")]
    public async Task<IActionResult> ExecuteCell([FromBody] ExecuteCodeDto dto)
    {
        try
        {
            await _notebookService.InitializeKernelAsync();

            var result = await _notebookService.ExecuteCellAsync(dto);

            await _notebookService.ShutdownKernelAsync();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur d'exécution cellule notebook.");
            return StatusCode(500, "Erreur lors de l'exécution.");
        }
    }

    [HttpPost("execute-notebook")]
    public async Task<IActionResult> ExecuteNotebook([FromBody] List<ExecuteCodeDto> cells)
    {
        await _notebookService.InitializeKernelAsync();

        var allResults = new List<ExecuteCodeDto>();

        foreach (var cell in cells)
        {
            var result = await _notebookService.ExecuteCellAsync(cell);
            allResults.Add(result);
        }

        await _notebookService.ShutdownKernelAsync();

        return Ok(allResults);
    }
}

