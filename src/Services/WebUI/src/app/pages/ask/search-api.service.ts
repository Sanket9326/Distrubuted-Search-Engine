import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AnswerSource {
  chunkId: string;
  documentId: string;
  fileName: string;
  chunkIndex: number;
  score: number;
}

export interface AnswerResponse {
  answer: string;
  sources: AnswerSource[];
}

@Injectable({ providedIn: 'root' })
export class SearchApiService {
  private readonly http = inject(HttpClient);

  askQuestion(query: string, department: string, topK = 5): Observable<AnswerResponse> {
    return this.http.post<AnswerResponse>(`${environment.searchServiceUrl}/api/search/answer`, {
      query,
      departments: [department],
      topK
    });
  }
}
