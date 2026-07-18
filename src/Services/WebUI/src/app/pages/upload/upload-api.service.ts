import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DocumentUploadedResponse {
  documentId: string;
  fileName: string;
  contentType: string;
  authorizedDepartments: number;
  uploadedAtUtc: string;
}

@Injectable({ providedIn: 'root' })
export class UploadApiService {
  private readonly http = inject(HttpClient);

  upload(file: File, departments: string[]): Observable<DocumentUploadedResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('departments', departments.join(','));

    return this.http.post<DocumentUploadedResponse>(
      `${environment.uploadServiceUrl}/api/FileHandler/upload`,
      formData
    );
  }
}
