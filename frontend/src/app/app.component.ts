import { HttpClient } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';

interface PingResponse {
  message: string;
}

/**
 * Root component used to validate backend/frontend connectivity.
 */
@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.component.html',
  styleUrl: './app.scss'
})
export class AppComponent implements OnInit {
  /**
   * UI message returned by the backend ping endpoint.
   */
  public message = 'Loading...';

  public constructor(private readonly httpClient: HttpClient) {}

  /**
   * Loads the ping response from the backend API.
   */
  public ngOnInit(): void {
    this.httpClient.get<PingResponse>('http://localhost:5216/api/ping').subscribe({
      next: (response: PingResponse) => {
        this.message = response.message;
      },
      error: () => {
        this.message = 'Could not reach LealFinance API.';
      }
    });
  }
}