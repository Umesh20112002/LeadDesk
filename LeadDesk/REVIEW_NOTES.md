# Review notes

## Changes made during source review

- Fixed an in-workbook duplicate-detection bug: mobile and email duplicate sets are now checked independently so short-circuit evaluation cannot skip tracking an email.
- Added mobile-number normalization for common display separators and reject unsupported characters rather than silently stripping letters.
- Added duplicate/ambiguous header checks and clearer rejection of corrupt `.xlsx` files.
- Limited search text to 200 characters for listing and export.
- Changed paging offset calculation to use 64-bit arithmetic.
- Kept exported contact fields as text and made the UI display timestamps explicitly in UTC.
- Added client-side 10 MB file-size validation and delayed object-URL cleanup after export.
- Expanded the manual regression checklist.

## Verification status

The source was reviewed and the listed corrections were applied. A 100% error-free guarantee is not technically possible without building and running against the target environment. The current execution environment does not have the .NET SDK available, and `npm install` did not complete before the environment timeout, so neither backend compilation nor Angular compilation has been verified here. Run `dotnet restore`, `dotnet build`, `npm ci` (if a lockfile is added) or `npm install`, and `npm run build` on a machine with the required SDKs before submitting. Then execute `TEST_PLAN.md` against a real SQL Server instance.

## Follow-up review: Swagger HTTP 500

- Root cause found in `Program.cs`: `AddSwaggerGen()` did not register a `v1` Swagger document, while Swagger UI requests `/swagger/v1/swagger.json`. This can produce HTTP 500 with an "Unknown Swagger document" exception.
- Registered the `v1` document with title `LeadDesk API` and version `v1`.
- Marked the Excel import action as consuming `multipart/form-data`; kept `IFormFile` as a direct action parameter (do not add `[FromForm]` to the `IFormFile` parameter for Swashbuckle).
- This is a source-level fix. The .NET SDK and SQL Server are not available in this environment, so the fix still needs to be built and verified locally.
