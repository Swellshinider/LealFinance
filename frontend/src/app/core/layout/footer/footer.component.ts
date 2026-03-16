import { Component } from '@angular/core';

/**
 * Application footer containing static branding text.
 */
@Component({
  selector: 'app-footer',
  standalone: true,
  templateUrl: './footer.component.html',
  styleUrl: './footer.component.scss'
})
export class FooterComponent {
  /** Current calendar year displayed in footer. */
  public readonly year = new Date().getFullYear();
}
