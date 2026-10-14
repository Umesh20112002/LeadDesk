import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ImportResult, LeadPage } from '../models/lead.model';

@Injectable({ providedIn: 'root' })
export class LeadService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'https://localhost:7001/api/leads';

  getLeads(search: string, pageNumber: number, pageSize: number): Observable<LeadPage> {
    const params = new HttpParams()
      .set('pageNumber', pageNumber)
      .set('pageSize', pageSize)
      .set('search', search);
    return this.http.get<LeadPage>(this.apiUrl, { params });
  }

  importExcel(file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ImportResult>(`${this.apiUrl}/import`, formData);
  }

  exportLeads(search: string): Observable<Blob> {
    const params = new HttpParams().set('search', search);
    return this.http.get(`${this.apiUrl}/export`, { params, responseType: 'blob' });
  }
}
