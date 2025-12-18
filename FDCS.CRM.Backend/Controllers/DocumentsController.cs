using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FDCS.CRM.Backend.Data;
using FDCS.CRM.Backend.DTOs;
using FDCS.CRM.Backend.Models;
using System.Linq;

namespace FDCS.CRM.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(CrmDbContext context, ILogger<DocumentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all documents with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DocumentDTO>>> GetDocuments(
            [FromQuery] string? relatedEntityType = null,
            [FromQuery] int? relatedEntityId = null,
            [FromQuery] string? type = null)
        {
            try
            {
                var query = _context.Documents.AsQueryable();

                if (!string.IsNullOrEmpty(relatedEntityType))
                    query = query.Where(d => d.RelatedEntityType == relatedEntityType);

                if (relatedEntityId.HasValue)
                    query = query.Where(d => d.RelatedEntityId == relatedEntityId);

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(d => d.Type == type);

                var documents = await query.ToListAsync();
                var dtos = await MapToDTOListWithDetails(documents);

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting documents: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get document by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DocumentDTO>> GetDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                    return NotFound();

                return Ok(await MapToDTOWithDetails(document));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting document: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Upload document
        /// </summary>
        [HttpPost("upload")]
        public async Task<ActionResult<DocumentDTO>> UploadDocument([FromForm] DocumentUploadRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                    return Unauthorized();

                // In a real implementation, save the file to disk/cloud storage
                var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
                
                // Requirement: Store in project directory "Uploads"
                var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                if (!Directory.Exists(uploadDir))
                {
                    Directory.CreateDirectory(uploadDir);
                }

                var filePath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                var document = new Document
                {
                    Name = request.File.FileName,
                    Type = request.Type,
                    Status = "Draft",
                    UploadedById = userId,
                    UploadedDate = DateTime.Now,
                    RelatedEntityType = request.RelatedEntityType,
                    RelatedEntityId = request.RelatedEntityId,
                    FileSize = (int)request.File.Length,
                    FilePath = filePath
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetDocument), new { id = document.Id }, MapToDTO(document));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading document: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update document
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateDocument(int id, [FromBody] UpdateDocumentRequest request)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                    return NotFound();

                if (!string.IsNullOrEmpty(request.Status))
                    document.Status = request.Status;
                if (!string.IsNullOrEmpty(request.Name))
                    document.Name = request.Name;

                document.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Document updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating document: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Download document
        /// </summary>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                    return NotFound();

                if (!System.IO.File.Exists(document.FilePath))
                    return NotFound(new { message = "File not found on server" });

                var memory = new MemoryStream();
                using (var stream = new FileStream(document.FilePath, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, GetContentType(document.FilePath), document.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading document: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.ContainsKey(ext) ? types[ext] : "application/octet-stream";
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }

        /// <summary>
        /// Delete document
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteDocument(int id)
        {
            try
            {
                var document = await _context.Documents.FindAsync(id);
                if (document == null)
                    return NotFound();

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting document: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private async Task<DocumentDTO> MapToDTOWithDetails(Document document)
        {
            var dto = MapToDTO(document);

            var user = await _context.Users.FindAsync(document.UploadedById);
            if (user != null) dto.uploadedByName = user.Name;

            if (document.RelatedEntityType == "Account")
            {
                var account = await _context.Accounts
                    .Include(a => a.Region)
                    .Include(a => a.SalesRep)
                    .FirstOrDefaultAsync(a => a.Id == document.RelatedEntityId);

                if (account != null)
                {
                    dto.RegionId = account.RegionId;
                    dto.OwnerId = account.SalesRepId;
                    dto.regionName = account.Region?.Name;
                    dto.ownerName = account.SalesRep?.Name;
                }
            }
            else if (document.RelatedEntityType == "Opportunity")
            {
                var opportunity = await _context.Opportunities
                    .Include(o => o.Account).ThenInclude(a => a.Region)
                    .Include(o => o.Account).ThenInclude(a => a.SalesRep)
                    .FirstOrDefaultAsync(o => o.Id == document.RelatedEntityId);

                if (opportunity != null && opportunity.Account != null)
                {
                    dto.RegionId = opportunity.Account.RegionId;
                    dto.OwnerId = opportunity.Account.SalesRepId;
                    dto.regionName = opportunity.Account.Region?.Name;
                    dto.ownerName = opportunity.Account.SalesRep?.Name;
                }
            }
            return dto;
        }

        private async Task<List<DocumentDTO>> MapToDTOListWithDetails(List<Document> documents)
        {
            var accountIds = documents.Where(d => d.RelatedEntityType == "Account").Select(d => d.RelatedEntityId).Distinct().ToList();
            var oppIds = documents.Where(d => d.RelatedEntityType == "Opportunity").Select(d => d.RelatedEntityId).Distinct().ToList();
            var userIds = documents.Select(d => d.UploadedById).Distinct().ToList();

            var accounts = await _context.Accounts
                .Include(a => a.Region)
                .Include(a => a.SalesRep)
                .Where(a => accountIds.Contains(a.Id))
                .ToDictionaryAsync(a => a.Id);

            var opportunities = await _context.Opportunities
                .Include(o => o.Account).ThenInclude(a => a.Region)
                .Include(o => o.Account).ThenInclude(a => a.SalesRep)
                .Where(o => oppIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id);

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var dtos = new List<DocumentDTO>();
            foreach (var document in documents)
            {
                var dto = MapToDTO(document);

                if (users.TryGetValue(document.UploadedById, out var u))
                {
                    dto.uploadedByName = u.Name;
                }

                if (document.RelatedEntityType == "Account" && accounts.TryGetValue(document.RelatedEntityId, out var account))
                {
                    dto.RegionId = account.RegionId;
                    dto.OwnerId = account.SalesRepId;
                    dto.regionName = account.Region?.Name;
                    dto.ownerName = account.SalesRep?.Name;
                }
                else if (document.RelatedEntityType == "Opportunity" && opportunities.TryGetValue(document.RelatedEntityId, out var opportunity))
                {
                    if (opportunity.Account != null)
                    {
                        dto.RegionId = opportunity.Account.RegionId;
                        dto.OwnerId = opportunity.Account.SalesRepId;
                        dto.regionName = opportunity.Account.Region?.Name;
                        dto.ownerName = opportunity.Account.SalesRep?.Name;
                    }
                }
                dtos.Add(dto);
            }
            return dtos;
        }

        private DocumentDTO MapToDTO(Document document)
        {
            return new DocumentDTO
            {
                Id = document.Id,
                Name = document.Name,
                Type = document.Type,
                Status = document.Status,
                UploadedById = document.UploadedById,
                UploadedDate = document.UploadedDate,
                RelatedEntityType = document.RelatedEntityType,
                RelatedEntityId = document.RelatedEntityId,
                FileSize = document.FileSize
            };
        }
    }
}
