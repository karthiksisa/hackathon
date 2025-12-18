# Security Fix: Unauthorized Lead Reassignment

## Issue Summary
**Severity:** CRITICAL  
**Date:** December 17, 2025  
**Component:** LeadsController.cs

### Problem Description
A Sales Rep reported that their assigned lead was reassigned to another team member without their knowledge or authorization. Investigation revealed multiple critical security vulnerabilities in the lead management system.

## Root Causes Identified

### 1. Insufficient Authorization on Lead Updates (CRITICAL)
**Location:** [LeadsController.cs](Controllers/LeadsController.cs#L218) - `UpdateLead` method

**Vulnerability:**
- Sales Reps could modify the `OwnerId` field to reassign leads to other users
- Regional Leads had no validation checks on their region access
- No verification that the new owner was authorized or even existed
- Line 243 allowed unrestricted reassignment: `if (request.OwnerId.HasValue) lead.OwnerId = request.OwnerId;`

**Example Attack Scenario:**
```json
PUT /api/Leads/123
{
  "OwnerId": 999,  // Sales Rep could change this to any user ID
  "Status": "Qualified"
}
```

### 2. No Audit Logging (HIGH)
**Impact:** 
- Zero audit trail for ownership changes
- Cannot investigate who made changes, when, or why
- No accountability or compliance tracking
- Difficult to detect and prevent future incidents

### 3. Weak Regional Lead Authorization (MEDIUM)
**Location:** [LeadsController.cs](Controllers/LeadsController.cs#L247)

**Issue:**
- Commented code: `// Validation could go here...`
- Regional Leads could access and modify leads outside their assigned regions

## Implemented Fixes

### 1. Role-Based Authorization Controls

#### Sales Rep Restrictions
```csharp
// Sales Reps can ONLY update their own leads
if (lead.OwnerId != currentUserId)
{
    await LogAuditAsync("Update", "Lead", id, lead.Name, 
        $"Unauthorized update attempt - Lead owned by different user");
    return Forbid();
}

// Sales Reps CANNOT reassign leads
if (request.OwnerId.HasValue && request.OwnerId != currentUserId)
{
    await LogAuditAsync("Update", "Lead", id, lead.Name, 
        $"Unauthorized ownership change attempt from {currentUserId} to {request.OwnerId}");
    return StatusCode(403, new { message = "Sales Reps cannot reassign leads to other users. Please contact your Regional Lead or Admin." });
}
```

#### Regional Lead Validation
```csharp
// Regional Lead can only update leads in their assigned regions
var regionIds = currentUser.UserRegions.Select(ur => ur.RegionId).ToList();
if (currentUser.RegionId.HasValue) regionIds.Add(currentUser.RegionId.Value);

if (!regionIds.Contains(lead.RegionId.Value))
{
    await LogAuditAsync("Update", "Lead", id, lead.Name, 
        $"Unauthorized update attempt - Lead in different region");
    return Forbid();
}

// When reassigning, validate new owner has access to the region
if (request.OwnerId.HasValue && request.OwnerId != originalOwnerId)
{
    var newOwner = await _context.Users.FindAsync(request.OwnerId.Value);
    if (newOwner == null)
        return BadRequest(new { message = "The specified owner does not exist." });

    // Verify new owner has region access
    var newOwnerRegions = await _context.UserRegions
        .Where(ur => ur.UserId == request.OwnerId.Value)
        .Select(ur => ur.RegionId)
        .ToListAsync();
    
    if (!newOwnerRegions.Contains(lead.RegionId.Value))
        return BadRequest(new { message = "The specified owner does not have access to this lead's region." });
}
```

#### Super Admin Controls
```csharp
// Super Admin can reassign but must validate user exists
if (request.OwnerId.HasValue && request.OwnerId != originalOwnerId)
{
    var newOwner = await _context.Users.FindAsync(request.OwnerId.Value);
    if (newOwner == null)
        return BadRequest(new { message = "The specified owner does not exist." });
}
```

### 2. Comprehensive Audit Logging

#### New Audit Helper Method
```csharp
private async Task LogAuditAsync(string action, string entityType, int entityId, 
    string? entityName, string? details)
{
    var auditLog = new AuditLog
    {
        UserId = userId,
        UserName = userName,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        EntityName = entityName,
        Details = details,
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
    };
    
    _context.AuditLogs.Add(auditLog);
    await _context.SaveChangesAsync();
}
```

#### Audit Events Now Tracked
1. **Lead Reassignment** - Captures old and new owner details
2. **Unauthorized Access Attempts** - Logs all failed authorization checks
3. **Lead Creation** - Records initial owner assignment
4. **Lead Updates** - General change tracking
5. **Lead Conversion** - Tracks lead-to-account conversion
6. **Lead Deletion** - Records who deleted what lead
7. **Region Changes** - Tracks regional reassignments

#### Example Audit Log Entry
```
Action: Reassign
EntityType: Lead
EntityId: 123
EntityName: "Acme Corp Lead"
Details: "Lead reassigned from John Smith (ID: 15) to Jane Doe (ID: 22)"
UserId: 8
UserName: "admin@company.com"
IpAddress: "192.168.1.100"
Timestamp: 2025-12-17 10:30:00 UTC
```

### 3. Owner Validation
- All ownership changes now validate that the new owner exists in the database
- Regional assignments are validated against user region access
- Prevents assignment to non-existent or unauthorized users

## Testing Recommendations

### 1. Authorization Tests
- [ ] Sales Rep attempts to update another user's lead → Should be **Forbidden**
- [ ] Sales Rep attempts to reassign their own lead → Should be **Forbidden** with clear error message
- [ ] Regional Lead attempts to update lead outside their region → Should be **Forbidden**
- [ ] Regional Lead reassigns lead to user without region access → Should be **BadRequest**
- [ ] Super Admin reassigns lead to non-existent user → Should be **BadRequest**

### 2. Audit Trail Tests
- [ ] Lead reassignment creates audit log with both old and new owner details
- [ ] Failed authorization attempts are logged
- [ ] Audit logs include IP address and user agent
- [ ] All CRUD operations on leads create audit entries

### 3. Regression Tests
- [ ] Legitimate lead updates still work correctly
- [ ] Lead creation still assigns owners properly
- [ ] Lead conversion still functions
- [ ] Role-based filtering still works

## Deployment Steps

1. **Backup Database** - Ensure audit logs table exists and is properly indexed
2. **Deploy Code Changes** - Deploy updated LeadsController.cs
3. **Monitor Logs** - Watch for any authorization errors in first 24 hours
4. **Review Audit Logs** - Check that audit entries are being created correctly
5. **User Communication** - Notify users of new authorization rules

## Monitoring & Alerts

### Key Metrics to Monitor
- **Failed authorization attempts** - Spike could indicate attack or misconfiguration
- **Audit log volume** - Should increase with new logging
- **Error rate on lead updates** - Should remain stable or decrease

### Recommended Alerts
1. **High Failed Authorization Rate** - Alert if >10 failures per hour
2. **Missing Audit Logs** - Alert if audit logging stops working
3. **Invalid User Assignment Attempts** - Alert on repeated attempts

## User Impact

### Sales Reps
- **BREAKING CHANGE:** Can no longer reassign their leads to other users
- Must contact Regional Lead or Admin for reassignments
- Clear error message explains the policy

### Regional Leads
- Can still reassign leads within their regions
- Must ensure new owner has proper region access
- Get validation errors if trying to assign to unauthorized users

### Super Admins
- Full reassignment capabilities maintained
- Must use valid user IDs
- All actions are now logged

## Compliance & Audit

This fix addresses:
- **Data Integrity** - Prevents unauthorized ownership changes
- **Accountability** - Full audit trail of all changes
- **Access Control** - Proper role-based authorization
- **Incident Response** - Can now investigate similar incidents
- **Compliance** - Meets audit logging requirements for CRM systems

## Related Files Modified

1. [Controllers/LeadsController.cs](Controllers/LeadsController.cs)
   - Updated `UpdateLead` method (Lines 218-354)
   - Added `LogAuditAsync` helper method (Lines 401-427)
   - Updated `CreateLead`, `ConvertLead`, `DeleteLead` with audit logging

## Future Improvements

1. **Notification System** - Add email/in-app notifications when leads are reassigned
2. **Approval Workflow** - Implement approval process for sensitive reassignments
3. **Audit Log UI** - Create admin interface to view and search audit logs
4. **Rate Limiting** - Add rate limiting to prevent brute force attacks
5. **Bulk Operations** - Ensure bulk update operations also respect authorization
6. **Database Constraints** - Add foreign key constraints to prevent orphaned records

## References

- OWASP Authorization Cheat Sheet
- CWE-639: Authorization Bypass Through User-Controlled Key
- CWE-778: Insufficient Logging
