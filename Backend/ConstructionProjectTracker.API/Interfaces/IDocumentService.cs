using ConstructionProjectTracker.API.DTOs.Documents;
using Microsoft.AspNetCore.Mvc;

namespace ConstructionProjectTracker.API.Interfaces;

public interface IDocumentService
{
    Task<DocumentResponseDto> UploadAsync(UploadDocumentDto dto, int uploadedByUserId);

    Task<FileStreamResult?> DownloadAsync(int id);

    Task<IEnumerable<DocumentResponseDto>> GetProjectDocumentsAsync(int projectId);

    Task<bool> DeleteAsync(int id);
}
