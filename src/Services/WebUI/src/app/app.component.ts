import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatIconModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  readonly navLinks = [
    { path: '/upload', label: 'Upload', icon: 'upload_file' },
    { path: '/ask', label: 'Ask', icon: 'forum' },
    { path: '/metrics', label: 'Metrics', icon: 'insights' }
  ];
}
