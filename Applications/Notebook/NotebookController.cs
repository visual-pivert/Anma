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

    [HttpGet("{slug}")]
    public async Task<ActionResult<NotebookEntity>> GetNotebook(int workspaceId, string slug)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        var notebook = await _context.Notebooks
            .Include(n => n.Workspace)
            .FirstOrDefaultAsync(n => n.Slug == slug && n.WorkspaceId == workspaceId && n.Workspace.OwnerId == userId);

        if (notebook == null)
        {
            _logger.LogWarning("GetNotebook: Notebook not found for slug {Slug}", slug);
            return NotFound();
        }

        _logger.LogInformation("GetNotebook: Returning notebook {Name} for slug {Slug}", notebook.Name, slug);
        return Ok(notebook);
    }

    [HttpPut("{slug}")]
    public async Task<IActionResult> UpdateNotebook(int workspaceId, string slug, [FromBody] CreateNotebookDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        _logger.LogInformation("UpdateNotebook called with Name: {Name}", dto.Name);

        var notebook = await _context.Notebooks
            .Include(n => n.Workspace)
            .FirstOrDefaultAsync(n => n.Slug == slug && n.WorkspaceId == workspaceId && n.Workspace.OwnerId == userId);

        if (notebook == null)
        {
            _logger.LogWarning("UpdateNotebook: Notebook not found for slug {Slug}", slug);
            return NotFound();
        }

        notebook.Name = dto.Name;
        notebook.Content = JsonSerializer.Serialize(dto.Content);

        if (notebook.Name != dto.Name)
        {
            notebook.Slug = SlugHelper.GenerateSlug(dto.Name);
            _logger.LogInformation("Slug changed to {NewSlug}", notebook.Slug);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Notebook {Name} saved successfully", notebook.Name);

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
    public async Task<IActionResult> ExecuteCell([FromBody] ExecuteCodeDto dto, int workspaceId)
    {
        try
        {
            var token = GetTokenFromRequest();
            _notebookService.SetTokenAndWorkspace(token, workspaceId);

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
    public async Task<IActionResult> ExecuteNotebook([FromBody] List<ExecuteCodeDto> cells, int workspaceId)
    {
        try
        {
            var token = GetTokenFromRequest();
            _notebookService.SetTokenAndWorkspace(token, workspaceId);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur d'exécution notebook.");
            return StatusCode(500, "Erreur lors de l'exécution.");
        }
    }

    private string GetTokenFromRequest()
    {
        var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader != null && authHeader.StartsWith("Bearer "))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }
        return string.Empty;
    }
}

