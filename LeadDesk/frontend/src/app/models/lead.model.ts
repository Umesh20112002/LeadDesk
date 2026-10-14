export interface Lead {
  id: number;
  name: string;
  mobileNumber: string;
  email: string | null;
  createdAtUtc: string;
}

export interface LeadPage {
  items: Lead[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
}

export interface ImportResult {
  importSummaryId: number;
  totalRows: number;
  importedCount: number;
  existingCount: number;
  invalidCount: number;
  errors: string[];
}
