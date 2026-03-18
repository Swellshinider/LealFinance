import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

/** Supported UI theme modes. */
export type ThemeMode = 'light' | 'dark';

/**
 * Handles global theme mode state and persistence.
 * The active mode is applied to the document body with a CSS class.
 */
@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly storageKey = 'lealfinance-theme-mode';
  private readonly document = inject(DOCUMENT);

  /** Active theme mode signal used by view components. */
  public readonly mode = signal<ThemeMode>('light');

  /**
   * Initializes the theme mode from localStorage and user preference.
   */
  public initTheme(): void {
    const storedMode = this.getStoredMode();

    if (storedMode) {
      this.setTheme(storedMode);
      return;
    }

    const preferredMode = this.prefersDarkMode() ? 'dark' : 'light';
    this.setTheme(preferredMode);
  }

  /**
   * Toggles between light and dark mode.
   */
  public toggleTheme(): void {
    const nextMode: ThemeMode = this.mode() === 'dark' ? 'light' : 'dark';
    this.setTheme(nextMode);
  }

  /**
   * Applies and persists a specific theme mode.
   * @param mode Theme mode to activate.
   */
  public setTheme(mode: ThemeMode): void {
    this.mode.set(mode);
    this.applyThemeClass(mode);
    this.persistMode(mode);
  }

  private getStoredMode(): ThemeMode | null {
    try {
      const value = globalThis.localStorage?.getItem(this.storageKey);
      return value === 'dark' || value === 'light' ? value : null;
    } catch {
      return null;
    }
  }

  private persistMode(mode: ThemeMode): void {
    try {
      globalThis.localStorage?.setItem(this.storageKey, mode);
    } catch {
      // Ignore browser storage limitations.
    }
  }

  private prefersDarkMode(): boolean {
    return globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }

  private applyThemeClass(mode: ThemeMode): void {
    const body = this.document.body;
    body.classList.toggle('theme-dark', mode === 'dark');
    body.classList.toggle('theme-light', mode === 'light');
    body.setAttribute('data-theme', mode);
  }
}
