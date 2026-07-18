import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormsModule } from '@angular/forms';
import { DEPARTMENTS, DEPARTMENT_LABELS } from '../../shared/departments';
import { DocumentUploadedResponse, UploadApiService } from './upload-api.service';

@Component({
  selector: 'app-upload',
  imports: [
    FormsModule,
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './upload.component.html',
  styleUrl: './upload.component.scss'
})
export class UploadComponent {
  private readonly uploadApi = inject(UploadApiService);
  private readonly snackBar = inject(MatSnackBar);

  readonly departments = DEPARTMENTS;
  readonly departmentLabels = DEPARTMENT_LABELS;

  selectedFile = signal<File | null>(null);
  selectedDepartments = signal<string[]>([]);
  isUploading = signal(false);
  recentUploads = signal<DocumentUploadedResponse[]>([]);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile.set(input.files?.[0] ?? null);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.selectedFile.set(file);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  canUpload(): boolean {
    return this.selectedFile() !== null && this.selectedDepartments().length > 0 && !this.isUploading();
  }

  upload(): void {
    const file = this.selectedFile();
    if (!file) {
      return;
    }

    this.isUploading.set(true);
    this.uploadApi.upload(file, this.selectedDepartments()).subscribe({
      next: response => {
        this.isUploading.set(false);
        this.recentUploads.update(uploads => [response, ...uploads]);
        this.snackBar.open(`Uploaded "${response.fileName}"`, 'Dismiss', { duration: 4000 });
        this.selectedFile.set(null);
        this.selectedDepartments.set([]);
      },
      error: err => {
        this.isUploading.set(false);
        const message = typeof err.error === 'string' ? err.error : 'Upload failed.';
        this.snackBar.open(message, 'Dismiss', { duration: 6000, panelClass: 'error-snackbar' });
      }
    });
  }
}
