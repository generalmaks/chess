import { Routes } from '@angular/router';

import { authGuard } from './core/auth-guard';
import { guestGuard } from './core/guest-guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home').then((m) => m.Home),
    canActivate: [authGuard]
  },
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
    canActivate: [guestGuard]
  },
  {
    path: 'register',
    loadComponent: () => import('./pages/register/register').then((m) => m.Register),
    canActivate: [guestGuard]
  },
  {
    path: 'play',
    loadComponent: () => import('./pages/play/play').then((m) => m.Play),
    canActivate: [authGuard]
  },
  {
    path: 'game/:gameId',
    loadComponent: () => import('./pages/game/game').then((m) => m.Game),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
