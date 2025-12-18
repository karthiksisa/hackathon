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
    public class DashboardController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(CrmDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardResponse>> GetDashboard(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] string? pipelineType = "Open")
        {
            try
            {
                int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int currentUserId);
                string userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Sales Rep";
                string userName = User.Identity?.Name ?? "Unknown";

                var response = new DashboardResponse();
                
                // 1. Scope and User Context
                var userEntity = await _context.Users.Include(u => u.UserRegions).ThenInclude(ur => ur.Region).Include(u => u.Region).FirstOrDefaultAsync(u => u.Id == currentUserId);
                
                response.Scope.Role = userRole;
                response.Scope.UserId = currentUserId;
                response.Scope.UserName = userEntity?.Name ?? userName;
                
                // Logic for Region Name/ID (Primary region)
                if (userEntity?.Region != null)
                {
                    response.Scope.RegionId = userEntity.RegionId;
                    response.Scope.RegionName = userEntity.Region.Name;
                }
                // If Regional Lead has multiple, we might pick first or list them (Requirement says "regionName: Required for Regional Leads").
                // Assuming single context for display or "Multi-Region".
                if (userRole == "Regional Lead" && response.Scope.RegionName == null && userEntity.UserRegions.Any())
                {
                     response.Scope.RegionName = userEntity.UserRegions.First().Region.Name; // Simplified
                }


                // 2. Data Scoping Query
                IQueryable<Opportunity> oppQuery = _context.Opportunities
                    .Include(o => o.Account)
                    .ThenInclude(a => a.Region)
                    .Include(o => o.Owner)
                    .AsQueryable();

                // Apply Date Filters (Used for KPIs like RevenueWon, not necessarily for Open Pipeline snapshot, but let's apply widely if date range is meant for "Reporting Period")
                // Usually "Open Pipeline" is point-in-time (Now), while "Won" is within period.
                // Requirement: "dateFrom... start of reporting period".
                // We'll apply Date filter CAREFULLY.
                // Won Deals: CloseDate within range.
                // Open Deals: Currently Open (Created before DateTo?). 
                // Let's get ALL relevant data first and filter in memory for complex dual-logic or apply sophisticated predicates.
                // Given dataset size likely small, memory is fine. Or use separate queries.
                // Let's use separate lists from base query.

                // Scope Filter
                List<int> regionIds = new List<int>();
                if (userRole == "Sales Rep")
                {
                    oppQuery = oppQuery.Where(o => o.OwnerId == currentUserId);
                }
                else if (userRole == "Regional Lead")
                {
                    if (userEntity != null)
                    {
                        regionIds = userEntity.UserRegions.Select(ur => ur.RegionId).ToList();
                        if (userEntity.RegionId.HasValue) regionIds.Add(userEntity.RegionId.Value);

                        oppQuery = oppQuery.Where(o => regionIds.Contains(o.Account.RegionId));
                    }
                }
                // Admin: All

                var allOpportunities = await oppQuery.ToListAsync();
                
                var from = dateFrom ?? DateTime.Now.AddDays(-30);
                var to = dateTo ?? DateTime.Now;

                // 3. KPI Calculations
                var wonDeals = allOpportunities.Where(o => o.Stage == "Closed Won" && o.CloseDate >= from && o.CloseDate <= to).ToList();
                var lostDeals = allOpportunities.Where(o => o.Stage == "Closed Lost" && o.LostAt >= from && o.LostAt <= to).ToList();
                
                // Open deals are "Active Now", usually ignored Date Filters or filtered by CreatedDate? 
                // "Open Pipeline Value... within the date range" -> Likely means opps active during that time? 
                // Or simplified: Current Snapshot. Typically dashboard shows Current Pipeline.
                var openDeals = allOpportunities.Where(o => o.Stage != "Closed Won" && o.Stage != "Closed Lost").ToList();

                response.Kpis.RevenueWon = wonDeals.Sum(o => o.Amount);
                response.Kpis.PipelineOpen = openDeals.Sum(o => o.Amount);
                response.Kpis.OpenDealsCount = openDeals.Count;
                
                int totalClosed = wonDeals.Count + lostDeals.Count;
                response.Kpis.WinRate = totalClosed > 0 ? (double)wonDeals.Count / totalClosed * 100 : 0;

                if (wonDeals.Any())
                {
                    response.Kpis.AvgSalesCycleDays = wonDeals.Average(o => (o.CloseDate - o.CreatedAt).TotalDays);
                }

                // Stalled Threshold (e.g., 21 days as per example)
                var stalledThreshold = DateTime.UtcNow.AddDays(-21); 
                var stalledDealsList = openDeals.Where(o => o.UpdatedAt < stalledThreshold).ToList();
                response.Kpis.StalledDealsCount = stalledDealsList.Count;


                // 4. Funnel (Open Pipeline by Stage)
                response.Funnel = openDeals
                    .GroupBy(o => o.Stage)
                    .Select(g => new FunnelItemDTO 
                    { 
                        StageKey = g.Key.ToLower().Replace(" ", ""), 
                        StageLabel = g.Key, 
                        Count = g.Count(), 
                        Amount = g.Sum(o => o.Amount) 
                    })
                    .OrderBy(f => f.Amount) // Or custom Stage Order
                    .ToList();


                // 5. Forecast By Rep
                if (userRole == "Sales Rep")
                {
                    response.ForecastByRep.Add(new ForecastItemDTO
                    {
                        Label = "My Pipeline",
                        InProgressValue = openDeals.Except(stalledDealsList).Sum(o => o.Amount),
                        StalledValue = stalledDealsList.Sum(o => o.Amount)
                    });
                }
                else
                {
                    var repGroups = openDeals.GroupBy(o => o.Owner?.Name ?? "Unknown");
                    foreach (var group in repGroups)
                    {
                        var repStalled = group.Where(o => o.UpdatedAt < stalledThreshold);
                        var repProgress = group.Except(repStalled);
                        
                        response.ForecastByRep.Add(new ForecastItemDTO
                        {
                            Label = group.Key,
                            InProgressValue = repProgress.Sum(o => o.Amount),
                            StalledValue = repStalled.Sum(o => o.Amount)
                        });
                    }
                }


                // 6. Forecast Pivot
                if (userRole == "Super Admin")
                {
                    response.ForecastPivot.Mode = "RegionStage";
                    // Rows: Regions, Cols: Stages
                    var regionGroups = openDeals.GroupBy(o => o.Account?.Region?.Name ?? "Unknown");
                    foreach (var group in regionGroups)
                    {
                        var row = new PivotRowDTO { RowLabel = group.Key };
                        
                        // Pivot Columns: Amount per Stage
                        var stageBreakdown = group.GroupBy(o => o.Stage);
                        foreach (var stageBatch in stageBreakdown)
                        {
                            row.Columns[stageBatch.Key] = stageBatch.Sum(o => o.Amount);
                        }
                        response.ForecastPivot.Rows.Add(row);
                    }
                }
                else
                {
                    response.ForecastPivot.Mode = "StageSummary";
                    // Rows: Stages, Cols: Total Amount, Count (Represented as dictionary keys?)
                    // "Columns should simply be Total Amount and Count"
                    var stageGroups = openDeals.GroupBy(o => o.Stage);
                    foreach (var group in stageGroups)
                    {
                         var row = new PivotRowDTO { RowLabel = group.Key };
                         row.Columns["TotalAmount"] = group.Sum(o => o.Amount);
                         row.Columns["Count"] = group.Count();
                         response.ForecastPivot.Rows.Add(row);
                    }
                }


                // 7. Stalled Deals Table
                response.StalledDeals = stalledDealsList
                    .OrderByDescending(o => (DateTime.UtcNow - o.UpdatedAt).TotalDays)
                    .Select(o => new StalledDealDetailDTO
                    {
                        Id = o.Id,
                        OpportunityName = o.Name,
                        AccountName = o.Account?.Name ?? "Unknown",
                        StageLabel = o.Stage,
                        Value = o.Amount,
                        DaysInStage = (int)(DateTime.UtcNow - o.UpdatedAt).TotalDays,
                        OwnerName = o.Owner?.Name ?? "Unknown"
                    })
                    .Take(10)
                    .ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting dashboard stats: {ex.Message}");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }
    }
}
