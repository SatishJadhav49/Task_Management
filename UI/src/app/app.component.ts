import { Component } from '@angular/core';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { SidebarComponent } from './layout/sidebar/sidebar.component';
import { TopbarComponent } from './layout/topbar/topbar.component';
import { ToastContainerComponent } from './shared/components/toast-container.component';
import { filter } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    SidebarComponent,
    TopbarComponent,
    ToastContainerComponent,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent {
  isSidebarOpen = true;
  showMainLayout = false;
  toggleSidebar() {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  constructor(
    private router: Router
  ) {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event: NavigationEnd) => {
        // Show main layout only for protected routes
        this.showMainLayout = this.shouldShowMainLayout(event.url);
      });
  }

   shouldShowMainLayout(url: string): boolean {
    // Don't show main layout for auth routes and no-menu-access
    const authRoutes = ['/', '/auth', '/no-access', '/no-menu-access'];
    return !authRoutes.includes(url);
  }
}