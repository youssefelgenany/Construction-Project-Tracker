using AutoMapper;
using ConstructionProjectTracker.API.Data;
using ConstructionProjectTracker.API.DTOs.Documents;
using ConstructionProjectTracker.API.Entities;
using ConstructionProjectTracker.API.Helpers;
using ConstructionProjectTracker.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConstructionProjectTracker.API.Services;

public class DocumentService : IDocumentService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        ApplicationDbContext context,
        IMapper mapper,
        IWebHostEnvironment environment,
        ILogger<DocumentService> logger)
    {
        _context = context;
        _mapper = mapper;
        _environment = environment;
        _logger = logger;
    }

    public async Task<DocumentResponseDto> UploadAsync(UploadDocumentDto dto, int uploadedByUserId)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId))
            throw new InvalidOperationException("Project does not exist.");

        var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
        if (!DocumentFileRules.IsAllowedExtension(extension))
            throw new InvalidOperationException("File type is not allowed.");

        if (dto.File.Length > DocumentFileRules.MaxFileSizeBytes)
            throw new InvalidOperationException("File size must not exceed 20 MB.");

        var storedFileName = DocumentFileRules.BuildStoredFileName(extension);
        var relativeDirectory = DocumentFileRules.GetRelativeDirectory(dto.ProjectId);
        var relativeFilePath = $"{relativeDirectory}/{storedFileName}".Replace('\\', '/');

        var physicalDirectory = Path.Combine(_environment.WebRootPath, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalDirectory);

        var physicalFilePath = Path.Combine(physicalDirectory, storedFileName);

        await using (var stream = new FileStream(physicalFilePath, FileMode.CreateNew))
        {
            await dto.File.CopyToAsync(stream);
        }

        var document = new Document
        {
            ProjectId = dto.ProjectId,
            UploadedByUserId = uploadedByUserId,
            OriginalFileName = dto.File.FileName,
            StoredFileName = storedFileName,
            Extension = extension,
            ContentType = string.IsNullOrWhiteSpace(dto.File.ContentType)
                ? "application/octet-stream"
                : dto.File.ContentType,
            FileSize = dto.File.Length,
            Category = dto.Category,
            RelativeFilePath = relativeFilePath,
            UploadDate = DateTime.UtcNow
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Document uploaded: DocumentId={DocumentId}, ProjectId={ProjectId}, UserId={UserId}",
            document.Id, document.ProjectId, uploadedByUserId);

        return await MapToResponseDtoAsync(document.Id);
    }

    public async Task<FileStreamResult?> DownloadAsync(int id)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null)
            return null;

        var physicalFilePath = GetPhysicalPath(document.RelativeFilePath);
        if (!File.Exists(physicalFilePath))
            throw new FileNotFoundException("The physical file could not be found.");

        var stream = new FileStream(physicalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        _logger.LogInformation(
            "Document downloaded: DocumentId={DocumentId}, ProjectId={ProjectId}, UserId={UserId}",
            document.Id, document.ProjectId, document.UploadedByUserId);

        return new FileStreamResult(stream, document.ContentType)
        {
            FileDownloadName = document.OriginalFileName
        };
    }

    public async Task<IEnumerable<DocumentResponseDto>> GetProjectDocumentsAsync(int projectId)
    {
        if (!await _context.Projects.AnyAsync(p => p.Id == projectId))
            return Array.Empty<DocumentResponseDto>();

        var documents = await _context.Documents
            .AsNoTracking()
            .Include(d => d.UploadedByUser)
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.UploadDate)
            .ToListAsync();

        return documents.Select(d => MapToResponseDto(d));
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var document = await _context.Documents.FindAsync(id);
        if (document is null)
            return false;

        var physicalFilePath = GetPhysicalPath(document.RelativeFilePath);
        if (File.Exists(physicalFilePath))
            File.Delete(physicalFilePath);

        var documentId = document.Id;
        var projectId = document.ProjectId;
        var userId = document.UploadedByUserId;

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Document deleted: DocumentId={DocumentId}, ProjectId={ProjectId}, UserId={UserId}",
            documentId, projectId, userId);

        return true;
    }

    private async Task<DocumentResponseDto> MapToResponseDtoAsync(int documentId)
    {
        var document = await _context.Documents
            .AsNoTracking()
            .Include(d => d.UploadedByUser)
            .FirstAsync(d => d.Id == documentId);

        return MapToResponseDto(document);
    }

    private DocumentResponseDto MapToResponseDto(Document document)
    {
        var dto = _mapper.Map<DocumentResponseDto>(document);
        dto.DownloadUrl = $"/api/documents/download/{document.Id}";
        return dto;
    }

    private string GetPhysicalPath(string relativeFilePath) =>
        Path.Combine(_environment.WebRootPath, relativeFilePath.Replace('/', Path.DirectorySeparatorChar));
}
