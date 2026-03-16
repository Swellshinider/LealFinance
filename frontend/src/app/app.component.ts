import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

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
export class AppComponent {}