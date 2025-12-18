import { Component, ChangeDetectionStrategy, signal, inject } from '@angular/core';
import { LeadsPageComponent } from './pages/leads-page/leads-page.component';
import { AccountsPageComponent } from './pages/accounts-page/accounts-page.component';
import { OpportunitiesPageComponent } from './pages/opportunities-page/opportunities-page.component';
import { TasksPageComponent } from './pages/tasks-page/tasks-page.component';
import { DocumentsPageComponent } from './pages/documents-page/documents-page.component';
import { AdminPageComponent } from './pages/admin-page/admin-page.component';
import { DashboardPageComponent } from './pages/dashboard-page/dashboard-page.component';
import { LoginPageComponent } from './pages/login-page/login-page.component';
import { ProfilePageComponent } from './pages/profile-page/profile-page.component';
import { AuthService } from './services/auth.service';
import { UserService } from './services/user.service';
import { User } from './models/user.model';
import { ThemeService } from './services/theme.service';

type View = 'dashboard' | 'leads' | 'accounts' | 'opportunities' | 'tasks' | 'documents' | 'admin' | 'profile';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DashboardPageComponent, LeadsPageComponent, AccountsPageComponent, OpportunitiesPageComponent, TasksPageComponent, DocumentsPageComponent, AdminPageComponent, LoginPageComponent, ProfilePageComponent],
})
export class AppComponent {
  private authService = inject(AuthService);
  private userService = inject(UserService);
  themeService = inject(ThemeService);

  readonly allUsers = this.userService.users;
  readonly currentUser = this.authService.currentUser;

  readonly currentView = signal<View>('dashboard');
  readonly userMenuOpen = signal(false);

  setView(view: View) {
    this.currentView.set(view);
    this.userMenuOpen.set(false);
  }



  logout() {
    this.authService.logout();
    this.userMenuOpen.set(false);
  }
}