import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'ask' },
  { path: 'upload', loadComponent: () => import('./pages/upload/upload.component').then(m => m.UploadComponent) },
  { path: 'ask', loadComponent: () => import('./pages/ask/ask.component').then(m => m.AskComponent) },
  { path: 'metrics', loadComponent: () => import('./pages/metrics/metrics.component').then(m => m.MetricsComponent) }
];
