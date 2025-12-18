# FDCS CRM Application Summary

## Overview
FDCS CRM is a comprehensive Customer Relationship Management system designed to assist sales teams in managing their workflows, from lead generation to deal closure. The application provides a structured environment for tracking interactions, managing tasks, and analyzing performance across different regions.

## Architecture
The application is built on a modern, scalable technology stack:

*   **Backend Framework**: ASP.NET Core Web API (.NET 10 Preview)
*   **Database**: MySQL
*   **Data Access Layer**: Entity Framework Core 8.0 (using Code-First approach)
*   **Authentication**: Secure JWT (JSON Web Token) based authentication
*   **API Documentation**: Integrated Swagger/OpenAPI UI for easy API exploration

## Core Modules & Features

### 1. Authentication & User Management (`AuthController`, `UsersController`)
*   **Secure Access**: Token-based login ensures secure access to API endpoints.
*   **Role-Based Access Control (RBAC)**: Supports multiple roles such as 'Admin', 'Sales Rep', and 'Regional Lead' to categorize user permissions.
*   **Regional Assignment**: Users can be assigned to specific geographic regions, allowing for localized data management.

### 2. Lead Management (`LeadsController`)
*   **Lifecycle Tracking**: Manage sales leads from initial creation to conversion.
*   **Ownership**: Leads are assigned to specific owners and regions.
*   **Status Workflow**: Track lead progress through various statuses (e.g., New, Contacted, Qualified).

### 3. Account Management (`AccountsController`)
*   **Company Profiles**: Store detailed information about client companies or organizations.
*   **Segmentation**: Accounts are categorized by industry and assigned to specific sales representatives and regions.

### 4. Contact Management (`ContactsController`)
*   **People Directory**: Manage individual contacts associated with Accounts.
*   **Detailed Profiles**: Store contact details, titles, and direct associations with their parent companies.

### 5. Sales Opportunities (`OpportunitiesController`)
*   **Pipeline Management**: Track potential deals through defined stages (e.g., Prospecting, Negotiation, Closed Won).
*   **Revenue Tracking**: Monitor deal amounts and expected close dates.
*   **Outcome Analysis**: Record reasons for lost opportunities to improve future sales strategies.

### 6. Task Management (`TasksController`)
*   **Activity Tracking**: Create and assign tasks (e.g., Calls, Meetings, Emails).
*   **Integration**: Tasks can be directly linked to other entities like Leads, Accounts, or Opportunities.
*   **Scheduling**: Set due dates and track completion status.

### 7. Document Management (`DocumentsController`)
*   **File Storage**: Upload and manage file attachments (e.g., Proposals, Contracts).
*   **Association**: Documents can be linked to specific CRM entities for easy retrieval.

### 8. Regional Administration (`RegionsController`)
*   **Geographic Organization**: Define and manage operational regions.
*   **Data Segmentation**: Serves as a foundational layer for organizing users, leads, and accounts.

### 9. Analytics & Dashboard (`DashboardController`)
*   **Performance Insights**: (Architecture implies presence) Endpoints to retrieve statistical data and key performance indicators (KPIs) for the dashboard view.

### 10. Audit Logging (`AuditLogsController`)
*   **Activity History**: Automatically records system actions (creates, updates, deletes) for security and accountability.
*   **Traceability**: Logs include details such as user ID, action type, entity affected, and timestamp.



To run app, you can use play button in visual studio or run `dotnet run` in the terminal.
