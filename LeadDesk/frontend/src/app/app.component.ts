import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ImportResult, Lead, LeadPage } from './models/lead.model';
import { LeadService } from './services/lead.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html'
})
export class AppComponent implements OnInit {
  private readonly leadService = inject(LeadService);

  leads: Lead[] = [];
  searchText = '';
  appliedSearch = '';
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  loading = false;
  importing = false;
  exporting = false;
  selectedFile: File | null = null;
  errorMessage = '';
  successMessage = '';
  importResult: ImportResult | null = null;

  ngOnInit(): void {
    this.loadLeads();
  }

  get totalPages(): number {
    return Math.max(1, Math.ceil(this.totalCount / this.pageSize));
  }

  get firstRecord(): number {
    return this.totalCount === 0 ? 0 : (this.pageNumber - 1) * this.pageSize + 1;
  }

  get lastRecord(): number {
    return Math.min(this.pageNumber * this.pageSize, this.totalCount);
  }

  loadLeads(): void {
    this.loading = true;
    this.errorMessage = '';
    this.leadService.getLeads(this.appliedSearch, this.pageNumber, this.pageSize)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (page: LeadPage) => {
          this.leads = page.items;
          this.totalCount = page.totalCount;
        },
        error: () => this.errorMessage = 'Could not load leads. Confirm the API is running and try again.'
      });
  }

  search(): void {
    this.appliedSearch = this.searchText.trim();
    this.pageNumber = 1;
    this.loadLeads();
  }

  clearSearch(): void {
    this.searchText = '';
    this.appliedSearch = '';
    this.pageNumber = 1;
    this.loadLeads();
  }

  changePage(nextPage: number): void {
    if (nextPage < 1 || nextPage > this.totalPages || nextPage === this.pageNumber) return;
    this.pageNumber = nextPage;
    this.loadLeads();
  }

  changePageSize(value: string): void {
    this.pageSize = Number(value);
    this.pageNumber = 1;
    this.loadLeads();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.importResult = null;
    this.successMessage = '';
    this.errorMessage = '';
    if (this.selectedFile && !this.selectedFile.name.toLowerCase().endsWith('.xlsx')) {
      this.errorMessage = 'Select an Excel .xlsx file.';
      this.selectedFile = null;
      input.value = '';
      return;
    }
    if (this.selectedFile && this.selectedFile.size > 10 * 1024 * 1024) {
      this.errorMessage = 'The file size cannot exceed 10 MB.';
      this.selectedFile = null;
      input.value = '';
    }
  }

  importFile(): void {
    if (!this.selectedFile || this.importing) return;
    this.importing = true;
    this.errorMessage = '';
    this.successMessage = '';
    this.importResult = null;
    this.leadService.importExcel(this.selectedFile)
      .pipe(finalize(() => this.importing = false))
      .subscribe({
        next: (result: ImportResult) => {
          this.importResult = result;
          this.successMessage = 'Excel file processed successfully.';
          this.selectedFile = null;
          this.pageNumber = 1;
          this.loadLeads();
        },
        error: (error: { error?: { message?: string } }) => {
          this.errorMessage = error.error?.message ?? 'Import failed. Check the file and try again.';
        }
      });
  }

  exportFile(): void {
    if (this.exporting) return;
    this.exporting = true;
    this.errorMessage = '';
    this.leadService.exportLeads(this.appliedSearch)
      .pipe(finalize(() => this.exporting = false))
      .subscribe({
        next: (file: Blob) => {
          const url = URL.createObjectURL(file);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = 'leads.xlsx';
          document.body.appendChild(anchor);
          anchor.click();
          anchor.remove();
          window.setTimeout(() => URL.revokeObjectURL(url), 1000);
        },
        error: () => this.errorMessage = 'Export failed. Please try again.'
      });
  }
}
