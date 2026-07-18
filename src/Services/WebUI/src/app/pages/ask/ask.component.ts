import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DEPARTMENTS, DEPARTMENT_LABELS } from '../../shared/departments';
import { AnswerSource, SearchApiService } from './search-api.service';

interface ChatTurn {
  question: string;
  answer?: string;
  sources?: AnswerSource[];
  isLoading: boolean;
}

@Component({
  selector: 'app-ask',
  imports: [
    FormsModule,
    DecimalPipe,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './ask.component.html',
  styleUrl: './ask.component.scss'
})
export class AskComponent {
  private readonly searchApi = inject(SearchApiService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly scrollAnchor = viewChild<ElementRef<HTMLDivElement>>('scrollAnchor');

  readonly departments = DEPARTMENTS;
  readonly departmentLabels = DEPARTMENT_LABELS;

  selectedDepartment = signal<string>('');
  currentQuestion = signal('');
  turns = signal<ChatTurn[]>([]);

  canAsk(): boolean {
    return this.selectedDepartment().length > 0 && this.currentQuestion().trim().length > 0;
  }

  ask(): void {
    if (!this.canAsk()) {
      return;
    }

    const question = this.currentQuestion().trim();
    const department = this.selectedDepartment();
    const turn: ChatTurn = { question, isLoading: true };
    this.turns.update(turns => [...turns, turn]);
    this.currentQuestion.set('');
    this.scrollToBottom();

    this.searchApi.askQuestion(question, department).subscribe({
      next: response => {
        this.updateLastTurn({ answer: response.answer, sources: response.sources, isLoading: false });
        this.scrollToBottom();
      },
      error: () => {
        this.updateLastTurn({
          answer: 'Something went wrong reaching the search service. Please try again.',
          sources: [],
          isLoading: false
        });
        this.snackBar.open('Failed to get an answer.', 'Dismiss', { duration: 5000 });
        this.scrollToBottom();
      }
    });
  }

  private updateLastTurn(patch: Partial<ChatTurn>): void {
    this.turns.update(turns => {
      const copy = [...turns];
      const last = copy[copy.length - 1];
      copy[copy.length - 1] = { ...last, ...patch };
      return copy;
    });
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      this.scrollAnchor()?.nativeElement.scrollIntoView({ behavior: 'smooth' });
    });
  }
}
