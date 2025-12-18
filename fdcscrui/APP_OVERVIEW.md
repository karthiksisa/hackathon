# Application Overview

## Introduction
This application is a comprehensive **Customer Relationship Management (CRM)** system designed to streamline sales processes, manage customer interactions, and track business opportunities. It is built with a modern architecture to ensure scalability, performance, and a seamless user experience.

## Architecture

### Frontend
- **Framework**: Angular (Latest Version)
- **Design System**: Tailwind CSS for responsive and modern styling.
- **Architecture**: Component-based architecture using standalone components for modularity and performance.
- **State Management**: Service-based state management with RxJS for reactive data handling.

### Backend & API
- **Protocol**: RESTful API adhering to OpenAPI 3.0 standards.
- **Security**: Token-based authentication (JWT) for secure access.
- **Deployment**: containerized using Docker for consistent deployment across environments.

## Core Modules & Features

### 1. Authentication & Security
- **Secure Login**: User authentication via email and password.
- **Session Management**: Automatic token validation and refresh mechanisms.
- **Password Management**: Capabilities for users to update their credentials securely.

### 2. Dashboard
- **Overview**: Provides a high-level view of key metrics.
- **Statistics**: Visual representation of sales performance, lead conversion rates, and active opportunities.

### 3. Lead Management
- **Lead Tracking**: Capture and track potential customers from initial contact.
- **Assignment**: Functionality to assign leads to specific sales representatives or regions.
- **Conversion**: Seamless workflow to convert qualified leads into Accounts, Contacts, and Opportunities.

### 4. Account Management
- **Centralized Data**: maintain detailed records of client organizations.
- **Status Workflow**: Approval and rejection workflows for new accounts.
- **360-View**: centralized view of all related contacts, opportunities, tasks, and documents for each account.

### 5. Contact Management
- **Address Book**: Manage individual contacts associated with accounts.
- **Details**: Specific information tracking for stakeholders and decision-makers.

### 6. Opportunity Management (Sales Pipeline)
- **Pipeline View**: Visual board to manage deals through various stages (e.g., Prospecting, Qualification, Proposal, Closed).
- **Deal Tracking**: Monitor values, probabilities, and expected close dates.

### 7. Task Management
- **Activity Tracking**: Create and assign tasks related to specific accounts or opportunities.
- **Productivity**: detailed views to keep the sales team organized and focused on next steps.

### 8. Document Management
- **Storage**: formatting and storage for proposals, contracts, and other essential sales documents.
- **Association**: Link documents directly to accounts and opportunities for easy access.

### 9. Audit Logs & Compliance
- **Activity History**: Comprehensive logging of user actions within the system for accountability and transparency.
- **Filtering**: Search logs by user, action type, or date range.

### 10. Regional Management
- **Territory Management**: Organization of data and user access based on geographic regions.

To run app, use below mentioned code:
npm run dev