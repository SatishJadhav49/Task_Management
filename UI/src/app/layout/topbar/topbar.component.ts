import {
  Component,
  Output,
  EventEmitter,
  inject,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonService, TeamOption } from '../../pages/common.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.css'
})
export class TopbarComponent {
  @Output() toggleSidebar = new EventEmitter<void>();

  readonly commonService = inject(CommonService);
  designationId = Number(localStorage.getItem('Designation_ID') || '3');
  isRoleModeEnabled = false;
  selectedTeamId = 0;
  private roleModeSubscription?: Subscription;

  ngOnInit() {
    this.commonService.initializeRoleFlagsFromDesignation(this.designationId);
    this.selectedTeamId = this.commonService.selectedTeamId;

    if (this.designationId === 1) {
      this.roleModeSubscription = this.commonService.isManagerMode$.subscribe(
        (isEnabled) => {
          this.isRoleModeEnabled = isEnabled;
        },
      );
      return;
    }

    if (this.designationId === 2) {
      this.roleModeSubscription = this.commonService.isLeadMode$.subscribe(
        (isEnabled) => {
          this.isRoleModeEnabled = isEnabled;
        },
      );
      return;
    }

    this.isRoleModeEnabled = false;
  }

  ngOnDestroy() {
    this.roleModeSubscription?.unsubscribe();
  }

  get canToggleRoleMode(): boolean {
    return this.designationId === 1 || this.designationId === 2;
  }

  get roleToggleLabel(): string {
    return this.designationId === 1 ? 'Manager Mode' : 'Team Lead Mode';
  }

  get roleToggleHint(): string {
    return this.isRoleModeEnabled ? 'Team View' : 'My View';
  }

  onRoleToggle() {
    const nextState = !this.isRoleModeEnabled;
    this.commonService.toggleRoleMode(nextState, this.designationId);
  }

  // ── Team switcher (manager mode only) ─────────────────────────

  get userTeams(): TeamOption[] {
    return this.commonService.userTeams;
  }

  /** Show the team selector only when a manager is in team view. */
  get showTeamSelector(): boolean {
    return (
      this.designationId === 1 &&
      this.isRoleModeEnabled &&
      this.userTeams.length > 0
    );
  }

  get hasMultipleTeams(): boolean {
    return this.userTeams.length > 1;
  }

  get selectedTeamName(): string {
    if (!this.selectedTeamId) {
      return 'All Teams';
    }
    const team = this.userTeams.find(
      (t) => t.Team_ID === Number(this.selectedTeamId),
    );
    return team?.Team_Name ?? 'All Teams';
  }

  onTeamChange() {
    this.commonService.setSelectedTeam(Number(this.selectedTeamId));
  }
}
