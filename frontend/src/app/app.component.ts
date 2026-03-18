import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { ThemeService } from './core/services/theme.service';

/**
 * Root application host that renders routed content.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.scss'
})
export class AppComponent {
  private readonly themeService = inject(ThemeService);

  public constructor() {
    this.themeService.initTheme();
  }
}
