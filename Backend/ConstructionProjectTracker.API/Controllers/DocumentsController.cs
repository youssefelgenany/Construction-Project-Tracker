using System.Security.Claims;
using ConstructionProjectTracker.API.DTOs.Documents;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>
    /// Uploads a document to a project.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(DocumentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DocumentResponseDto>> Upload([FromForm] UploadDocumentDto dto)
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var document = await _documentService.UploadAsync(dto, userId);
            return CreatedAtAction(nameof(Download), new { id = document.Id }, document);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns all documents for a project.
    /// </summary>
    [HttpGet("project/{projectId:int}")]
    [ProducesResponseType(typeof(IEnumerable<DocumentResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DocumentResponseDto>>> GetProjectDocuments(int projectId)
    {
        var documents = await _documentService.GetProjectDocumentsAsync(projectId);
        return Ok(documents);
    }

    /// <summary>
    /// Downloads a document by id.
    /// </summary>
    [HttpGet("download/{id:int}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(int id)
    {
        try
        {
            var result = await _documentService.DownloadAsync(id);
            if (result is null)
                return NotFound(new { message = $"Document with id {id} was not found." });

            return result;
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a document and its physical file. Admin only.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _documentService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Document with id {id} was not found." });

        return NoContent();
    }
}
