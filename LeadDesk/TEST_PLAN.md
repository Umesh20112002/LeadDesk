# Manual verification checklist

Run these checks after SQL Server, the API and Angular UI are running.

| Case | Input/action | Expected result |
|---|---|---|
| Empty database | Open the Leads page | Empty state appears; total count is 0 |
| Valid import | Import two unique rows with Name + Mobile Number | Imported count is 2; leads appear in the table |
| Existing mobile | Import a row whose mobile already exists | Imported count does not increase; existing count increases |
| Existing email | Import a row with a unique mobile but an email already stored | Row is skipped as a duplicate |
| Duplicate in same file | Include the same mobile/email twice in one workbook | First valid row is imported; duplicate row is skipped |
| Missing name | Include a row with mobile but no name | Row is counted invalid and a row-level message is returned |
| Missing mobile | Include a row with name but no mobile | Row is counted invalid and a row-level message is returned |
| Invalid email | Supply a malformed non-empty email | Row is counted invalid |
| Optional email | Leave email blank for a valid name/mobile pair | Row imports successfully |
| Wrong file extension | Upload `.csv` or `.xls` | API returns HTTP 400 with a useful message |
| Missing required headers | Upload a workbook without Name or Mobile Number | API returns HTTP 400 |
| Search | Search a partial name, mobile or email | Only matching rows are returned; total count reflects the filter |
| Pagination | Change page and page size | Results change and page boundaries remain valid |
| Export | Export with and without a search term | A valid `leads.xlsx` downloads with matching records |
| Import summary | Complete an import with valid/duplicate/invalid rows | `ImportSummary` stores totals and counts for that attempt |
| Error logging | Trigger an unexpected API exception in a development test | Error is written to console and rolling log file |


## Additional regression cases

- Within one workbook, row A has an existing mobile and a new email; row B has a different mobile and the same email as row A. Row B must be skipped as a duplicate email.
- A mobile value containing formatting characters such as `+91 (98765) 43210` should normalize consistently to `+919876543210`.
- A mobile value containing letters should be reported as invalid, not silently converted to digits.
- Duplicate or ambiguous headers (for example, both `Mobile` and `Mobile Number`) should return a clear validation error rather than a server error.
- A renamed non-Excel file and a corrupt `.xlsx` file should be rejected without inserting rows.
- Exported `Created At (UTC)` values should display in UTC, and mobile numbers should remain text in Excel.
- Very large page numbers should not overflow offset calculation.
