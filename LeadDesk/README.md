# LeadDesk — Lead Management System

A small full-stack lead import and export application built for the supplied coding task.

## Stack

- ASP.NET Core Web API (.NET 8)
- Angular 18 + Bootstrap 5
- Microsoft SQL Server
- Dapper for data access
- ClosedXML for `.xlsx` import/export
- Serilog console + rolling file logging

## Features

- Import `.xlsx` files with `Name`, `Mobile Number` and optional `Email` columns.
- Validate required fields and email format; skip blank worksheet rows.
- Skip duplicates by mobile number or non-empty email, including duplicates in the same workbook.
- Return imported, existing, invalid and total-row counts, plus row-level messages.
- Search across name, mobile number and email with server-side pagination.
- Export all matching leads to an Excel workbook.
- Persist each import attempt's summary in `ImportSummary`.
- SQL schema/indexes and an optional stored procedure are included.
- Unexpected import/export errors are logged to console and `backend/LeadDesk.Api/logs/`.

## Database setup

1. Open SQL Server Management Studio or Azure Data Studio using a login that can create databases.
2. Run `database/01_CreateDatabase.sql`.
3. The script creates `LeadDeskDb`, `dbo.Leads`, `dbo.ImportSummary`, unique indexes for mobile/email and `dbo.usp_Leads_Search`.
4. Update `ConnectionStrings:DefaultConnection` in `backend/LeadDesk.Api/appsettings.json` for your SQL Server. The default assumes local SQL Server with Windows authentication.

The paginated listing/search uses the included `dbo.usp_Leads_Search` stored procedure through Dapper. Import inserts, duplicate checks, summaries and export use parameterized Dapper commands.

## Run the API

Prerequisites: .NET 8 SDK and SQL Server.

```bash
cd backend/LeadDesk.Api
dotnet restore
dotnet run
```

The launch profile uses `https://localhost:7001` and `http://localhost:5001`; Swagger is available at `https://localhost:7001/swagger` in Development. If the local HTTPS certificate is not trusted, run `dotnet dev-certs https --trust` or use the HTTP endpoint for local testing.

## Run the Angular UI

Prerequisites: Node.js 20.11+ (or a compatible Node 20 release) and npm.

```bash
cd frontend
npm install
npm start
```

Open `http://localhost:4200`. The API URL is currently configured in `frontend/src/app/services/lead.service.ts` as `https://localhost:7001/api/leads`; change it there if the API uses another port. If your browser rejects the local HTTPS certificate, trust the .NET development certificate or configure the client to use `http://localhost:5001/api/leads` and update the API CORS origin as needed.

## Excel template

Use the first worksheet and include a header row similar to:

| Name | Mobile Number | Email |
|---|---|---|
| Asha Patil | 9876543210 | asha@example.com |
| Rahul Shah | 9123456780 | |

Header matching ignores spaces and punctuation for common variations such as `Mobile`, `Mobile No`, `Phone Number`, and `Email Address`. `Name` and mobile number are mandatory; email is optional.

## API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/leads?search=&pageNumber=1&pageSize=10` | Search and paginate leads |
| POST | `/api/leads/import` | Multipart form upload with field name `file` |
| GET | `/api/leads/export?search=` | Download filtered `.xlsx` export |

## Design decisions

- Controllers handle HTTP concerns; the service owns import validation and orchestration; the repository owns SQL/Dapper access.
- SQL parameters are used throughout to avoid concatenating user input into SQL.
- The import is wrapped in a transaction so inserted leads and the import summary commit together.
- Mobile and email are unique at the database level as a final duplicate safeguard.
- Page size is limited to 100 and import uploads are limited to 10 MB.
- Emails are stored lowercased to make duplicate detection consistent. Mobile numbers are stored as text to preserve leading zeros and optional `+` prefixes. The import trims common formatting characters (spaces, hyphens, parentheses and periods) before duplicate checks; format mobile-number cells as Text in Excel to preserve leading zeros. Search input is limited to 200 characters.

## Before submitting

1. Run the SQL script and verify the connection string.
2. Run the API and test all endpoints in Swagger.
3. Run the Angular UI and test empty, valid, invalid, and duplicate-heavy Excel files.
4. Run `dotnet build` and `npm run build`.
5. Add a `.gitignore`, commit the source, and push it to a GitHub repository.
6. In GitHub repository **Settings → Collaborators**, invite `developerpravin18@gmail.com` with the access level requested by the recruiter.

## Notes / possible follow-up improvements

- Add automated unit/integration tests for duplicate handling, malformed workbooks, paging and export. The supplied `TEST_PLAN.md` lists the required manual regression scenarios; automated test execution has not been verified in this environment.
- For very large workbooks, process rows in bounded batches and return a downloadable error report rather than a long inline error list.
- Configure production secrets through environment variables or a secret manager; do not commit production credentials.
