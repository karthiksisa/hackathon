using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FDCS.CRM.Backend.Data;
using FDCS.CRM.Backend.DTOs;
using FDCS.CRM.Backend.Models;
using System.Security.Claims;

namespace FDCS.CRM.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContactsController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<ContactsController> _logger;

        public ContactsController(CrmDbContext context, ILogger<ContactsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all contacts with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ContactDTO>>> GetContacts([FromQuery] int? accountId = null)
        {
            try
            {
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);
                var query = _context.Contacts.Include(c => c.Account).AsQueryable();

                // RBAC Filtering (Account Inheritance)
                if (User.IsInRole("Sales Rep"))
                {
                    query = query.Where(c => c.Account.SalesRepId == currentUserId);
                }
                else if (User.IsInRole("Regional Lead"))
                {
                    var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                    if (currentUser != null)
                    {
                        var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                        query = query.Where(c => regionIds.Contains(c.Account.RegionId));
                    }
                    else
                    {
                         return Ok(new List<ContactDTO>());
                    }
                }

                if (accountId.HasValue)
                    query = query.Where(c => c.AccountId == accountId);

                var contacts = await query.ToListAsync();
                var dtos = contacts.Select(MapToDTO).ToList();

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting contacts: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Get contact by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ContactDTO>> GetContact(int id)
        {
            try
            {
                var contact = await _context.Contacts.Include(c => c.Account).FirstOrDefaultAsync(c => c.Id == id);
                if (contact == null)
                    return NotFound();

                if (!await HasAccountAccess(contact.AccountId)) return Forbid();

                return Ok(MapToDTO(contact));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting contact: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Create new contact
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ContactDTO>> CreateContact([FromBody] CreateContactRequest request)
        {
            try
            {
                // Check Access to Account
                if (!await HasAccountAccess(request.AccountId))
                    return BadRequest(new { message = "You do not have permission to add contacts to this Account." }); // Or 403

                var contact = new Contact
                {
                    AccountId = request.AccountId,
                    Name = request.Name,
                    Email = request.Email,
                    Phone = request.Phone,
                    Title = request.Title
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetContact), new { id = contact.Id }, MapToDTO(contact));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating contact: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Update contact
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> UpdateContact(int id, [FromBody] UpdateContactRequest request)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null)
                    return NotFound();

                if (!await HasAccountAccess(contact.AccountId)) return Forbid();

                if (!string.IsNullOrEmpty(request.Name))
                    contact.Name = request.Name;
                if (!string.IsNullOrEmpty(request.Email))
                    contact.Email = request.Email;
                if (!string.IsNullOrEmpty(request.Phone))
                    contact.Phone = request.Phone;
                if (!string.IsNullOrEmpty(request.Title))
                    contact.Title = request.Title;

                contact.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Contact updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating contact: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        /// <summary>
        /// Delete contact
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> DeleteContact(int id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null)
                    return NotFound();

                if (!await HasAccountAccess(contact.AccountId)) return Forbid();

                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Contact deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting contact: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        private ContactDTO MapToDTO(Contact contact)
        {
            return new ContactDTO
            {
                Id = contact.Id,
                AccountId = contact.AccountId,
                Name = contact.Name,
                Email = contact.Email,
                Phone = contact.Phone,
                Title = contact.Title
            };
        }

        // Helper Method (Copied from AccountsController/OppController logic)
        private async Task<bool> HasAccountAccess(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);
            if (account == null) return false;

            int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);

            if (User.IsInRole("Sales Rep") && account.SalesRepId != currentUserId) return false;
            
            if (User.IsInRole("Regional Lead"))
            {
                var currentUser = await _context.Users.Include(u => u.UserRegions).FirstOrDefaultAsync(u => u.Id == currentUserId);
                if (currentUser != null)
                {
                    var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
                    if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

                    if (!regionIds.Contains(account.RegionId)) return false;
                }
            }
            return true;
        }
    }
}
