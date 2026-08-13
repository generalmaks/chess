import { Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { Auth } from '../../core/auth';

@Component({
  selector: 'app-home',
  imports: [RouterLink],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly auth = inject(Auth);
  private readonly router = inject(Router);

  protected logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }
}
