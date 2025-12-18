namespace FDCS.CRM.Backend.DTOs
{
    public class DashboardResponse
    {
        public ScopeDTO Scope { get; set; } = new();
        public KpisDTO Kpis { get; set; } = new();
        public List<FunnelItemDTO> Funnel { get; set; } = new();
        public List<ForecastItemDTO> ForecastByRep { get; set; } = new();
        
        // Using dynamic object or specific structure for Pivot? 
        // Request says "forecastPivot Object... For Admin(RegionStage)... For Lead/Rep(StageSummary)"
        // To keep it strongly typed but flexible:
        public ForecastPivotDTO ForecastPivot { get; set; } = new();

        public List<StalledDealDetailDTO> StalledDeals { get; set; } = new();
    }

    public class ScopeDTO
    {
        public string Role { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? RegionId { get; set; }
        public string? RegionName { get; set; }
    }

    public class KpisDTO
    {
        public decimal RevenueWon { get; set; }
        public decimal PipelineOpen { get; set; }
        public int OpenDealsCount { get; set; }
        public double WinRate { get; set; }
        public double AvgSalesCycleDays { get; set; }
        public int StalledDealsCount { get; set; }
    }

    public class FunnelItemDTO
    {
        public string StageKey { get; set; }
        public string StageLabel { get; set; }
        public int Count { get; set; }
        public decimal Amount { get; set; }
    }

    public class ForecastItemDTO
    {
        public string Label { get; set; } // Rep Name or "My Pipeline"
        public decimal InProgressValue { get; set; }
        public decimal StalledValue { get; set; }
    }

    // Supports both Pivot (Region/Stage) and Summary (Stage only)
    public class ForecastPivotDTO
    {
        public string Mode { get; set; } // "RegionStage" or "StageSummary"
        public List<PivotRowDTO> Rows { get; set; } = new();
    }

    public class PivotRowDTO
    {
        public string RowLabel { get; set; } // RegionName or StageName
        // Key-Value pairs for columns. For StageSummary: "TotalAmount", "Count". For RegionStage: "Prospecting": 1000...
        public Dictionary<string, decimal> Columns { get; set; } = new();
    }

    public class StalledDealDetailDTO
    {
        public int Id { get; set; }
        public string OpportunityName { get; set; }
        public string AccountName { get; set; }
        public string StageLabel { get; set; }
        public decimal Value { get; set; }
        public int DaysInStage { get; set; }
        public string OwnerName { get; set; }
    }
}
