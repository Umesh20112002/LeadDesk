# Verification status

## Confirmed through static review
- Folder/project/namespace names use `LeadDesk.Api`.
- Database and connection string consistently use `LeadDeskDb`.
- Angular service targets HTTPS port 7001, matching the Visual Studio launch profile.
- Required Excel fields, duplicate checks, Dapper and SQL scripts are present.
- Previously shared `LeadManagementTask-Reviewed.zip` was zero bytes in the working environment and was unusable; this archive replaces it.

## Not yet verified
- .NET build (SDK unavailable here).
- Angular production build (dependencies not installed here).
- SQL script execution and full import/export behavior (SQL Server unavailable here).

## Before recruiter submission
1. Open `backend/LeadDesk.Api/LeadDesk.Api.csproj` in Visual Studio 2022.
2. Run `database/01_CreateDatabase.sql` in SSMS.
3. Set SQL connection string and build/run the API.
4. Run `npm install` and `npm run build` in `frontend`.
5. Follow `TEST_PLAN.md`, then push to GitHub.

## Swagger fix
- `Program.cs` now explicitly registers `SwaggerDoc("v1", ...)`, matching the default Swagger UI URL `/swagger/v1/swagger.json`.
- The import endpoint explicitly declares `multipart/form-data`.
- Confirm locally by restarting the API and opening `https://localhost:7001/swagger/v1/swagger.json`; it should return JSON (HTTP 200), then open `/swagger`.
