import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonService, TeamOption } from '../common.service';
import { ToastService } from '../../shared/services/toast.service';
import { Shift, ShiftRosterMember, ShiftDay } from './shift.model';
import {
  DEVELOPER_DESIGNATION_ID,
  LEAD_DESIGNATION_ID,
  MANAGER_DESIGNATION_ID,
  MES_EXECUTIVE_DESIGNATION_ID,
} from '../../app.constant';

@Component({
  selector: 'app-shift-management',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './shift-management.component.html',
  styleUrl: './shift-management.component.css',
})
export class ShiftManagementComponent {
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  private readonly host = localStorage.getItem('Hostname') || '';
  private readonly userId = Number(localStorage.getItem('Employee_ID') || '0');

  teams: TeamOption[] = [];
  selectedTeamId = 0;

  // Developers can view their team's schedule but not change anything.
  isReadOnly = false;

  shifts: Shift[] = [];
  roster: ShiftRosterMember[] = [];
  isLoading = false;

  weekStartDate: Date = this.mondayOf(new Date());

  // Drag state
  draggingMember: ShiftRosterMember | null = null;
  dragOverShiftId: number | null = null;

  // Day-override modal
  dayEditorOpen = false;
  dayEditorMember: ShiftRosterMember | null = null;
  days: ShiftDay[] = [];
  isDaysLoading = false;

  ngOnInit() {
    const designationId = Number(localStorage.getItem('Designation_ID') || '0');
    if (
      designationId !== MANAGER_DESIGNATION_ID &&
      designationId !== LEAD_DESIGNATION_ID &&
      designationId !== DEVELOPER_DESIGNATION_ID &&
      designationId !== MES_EXECUTIVE_DESIGNATION_ID
    ) {
      this.router.navigate(['/no-access']);
      return;
    }

    // Developers get a read-only view of their team's schedule.
    this.isReadOnly = designationId === DEVELOPER_DESIGNATION_ID;

    if (!this.commonService.userTeams?.length) {
      this.commonService.loadUserTeamsFromStorage();
    }
    this.teams = this.commonService.userTeams ?? [];
    this.selectedTeamId = this.teams.length ? this.teams[0].Team_ID : 0;

    this.commonService.getAllShifts().subscribe(
      (res: any[]) => {
        this.shifts = (res ?? []).map((s: any) => this.mapShift(s));
        this.loadSchedule();
      },
      () => this.toastService.showError('Error', 'Failed to load shifts.'),
    );
  }

  private mapShift(s: any): Shift {
    return {
      Shift_ID: Number(s.Shift_ID),
      Shift_Name: String(s.Shift_Name ?? ''),
      Shift_Code: String(s.Shift_Code ?? ''),
      Start_Time: String(s.Start_Time ?? ''),
      End_Time: String(s.End_Time ?? ''),
      Is_General: !!s.Is_General,
      SORTORDER: Number(s.SORTORDER ?? 0),
    };
  }

  // ── Team / week controls ──────────────────────────────────────

  get hasMultipleTeams(): boolean {
    return this.teams.length > 1;
  }

  get selectedTeamName(): string {
    return (
      this.teams.find((t) => t.Team_ID === Number(this.selectedTeamId))
        ?.Team_Name ?? '—'
    );
  }

  onTeamChange() {
    this.loadSchedule();
  }

  get weekStart(): string {
    return this.toYmd(this.weekStartDate);
  }

  get weekEndDate(): Date {
    const end = new Date(this.weekStartDate);
    end.setDate(end.getDate() + 6);
    return end;
  }

  get weekRangeLabel(): string {
    const opts: Intl.DateTimeFormatOptions = { day: '2-digit', month: 'short' };
    return `${this.weekStartDate.toLocaleDateString('en-GB', opts)} – ${this.weekEndDate.toLocaleDateString('en-GB', opts)}`;
  }

  get isCurrentWeek(): boolean {
    return this.toYmd(this.mondayOf(new Date())) === this.weekStart;
  }

  prevWeek() {
    this.shiftWeek(-7);
  }

  nextWeek() {
    this.shiftWeek(7);
  }

  goToCurrentWeek() {
    this.weekStartDate = this.mondayOf(new Date());
    this.loadSchedule();
  }

  private shiftWeek(days: number) {
    const next = new Date(this.weekStartDate);
    next.setDate(next.getDate() + days);
    this.weekStartDate = next;
    this.loadSchedule();
  }

  // ── Board data ────────────────────────────────────────────────

  loadSchedule() {
    if (!this.selectedTeamId) {
      this.roster = [];
      return;
    }
    this.isLoading = true;
    this.commonService
      .getWeeklySchedule(this.selectedTeamId, this.weekStart)
      .subscribe(
        (res: any[]) => {
          this.roster = (res ?? []).map((m: any) => ({
            Employee_ID: Number(m.Employee_ID),
            Employee_Name: String(m.Employee_Name ?? ''),
            Employee_No: String(m.Employee_No ?? ''),
            Designation_ID: Number(m.Designation_ID ?? 0),
            Designation_Name: String(m.Designation_Name ?? ''),
            Shift_ID: Number(m.Shift_ID ?? 1),
            Shift_Name: String(m.Shift_Name ?? ''),
            Override_Count: Number(m.Override_Count ?? 0),
          }));
          this.isLoading = false;
        },
        () => {
          this.roster = [];
          this.isLoading = false;
          this.toastService.showError('Error', 'Failed to load schedule.');
        },
      );
  }

  membersInShift(shiftId: number): ShiftRosterMember[] {
    return this.roster.filter((m) => m.Shift_ID === shiftId);
  }

  initials(name: string): string {
    return name
      .split(' ')
      .map((w) => w[0])
      .slice(0, 2)
      .join('')
      .toUpperCase();
  }

  // ── Drag & drop ───────────────────────────────────────────────

  onDragStart(member: ShiftRosterMember) {
    if (this.isReadOnly) {
      return;
    }
    this.draggingMember = member;
  }

  onDragEnd() {
    this.draggingMember = null;
    this.dragOverShiftId = null;
  }

  onDragOver(event: DragEvent, shift: Shift) {
    if (this.isReadOnly) {
      return; // no preventDefault -> drop is not allowed
    }
    event.preventDefault(); // allow drop
    this.dragOverShiftId = shift.Shift_ID;
  }

  onDragLeave(shift: Shift) {
    if (this.dragOverShiftId === shift.Shift_ID) {
      this.dragOverShiftId = null;
    }
  }

  onDrop(shift: Shift) {
    const member = this.draggingMember;
    this.dragOverShiftId = null;
    this.draggingMember = null;
    if (this.isReadOnly || !member || member.Shift_ID === shift.Shift_ID) {
      return;
    }
    this.assignShift(member, shift);
  }

  /** Fallback / touch: move via the per-card dropdown. */
  moveToShift(member: ShiftRosterMember, shiftId: number) {
    if (this.isReadOnly) {
      return;
    }
    const shift = this.shifts.find((s) => s.Shift_ID === Number(shiftId));
    if (shift && member.Shift_ID !== shift.Shift_ID) {
      this.assignShift(member, shift);
    }
  }

  private assignShift(member: ShiftRosterMember, shift: Shift) {
    const prevId = member.Shift_ID;
    const prevName = member.Shift_Name;

    // Optimistic update.
    member.Shift_ID = shift.Shift_ID;
    member.Shift_Name = shift.Shift_Name;

    this.commonService
      .saveWeeklyAssignment({
        Employee_ID: member.Employee_ID,
        Team_ID: this.selectedTeamId,
        Week_Start_Date: this.weekStart,
        Shift_ID: shift.Shift_ID,
        Host: this.host,
        User_ID: this.userId,
      })
      .subscribe({
        next: () =>
          this.toastService.showSuccess(
            'Shift updated',
            `${member.Employee_Name} → ${shift.Shift_Name}`,
          ),
        error: () => {
          member.Shift_ID = prevId;
          member.Shift_Name = prevName;
          this.toastService.showError('Error', 'Could not save the shift.');
        },
      });
  }

  // ── Per-day overrides (mid-week changes) ──────────────────────

  openDayEditor(member: ShiftRosterMember) {
    this.dayEditorMember = member;
    this.dayEditorOpen = true;
    this.isDaysLoading = true;
    this.days = [];
    this.commonService
      .getDayOverrides(member.Employee_ID, this.weekStart)
      .subscribe(
        (res: any[]) => {
          this.days = (res ?? []).map((d: any) => ({
            Shift_Date: String(d.Shift_Date ?? ''),
            Day_Label: String(d.Day_Label ?? ''),
            Weekly_Shift_ID: Number(d.Weekly_Shift_ID ?? 1),
            Effective_Shift_ID: Number(d.Effective_Shift_ID ?? 1),
            Is_Override: !!d.Is_Override,
          }));
          this.isDaysLoading = false;
        },
        () => {
          this.isDaysLoading = false;
          this.toastService.showError('Error', 'Failed to load day shifts.');
        },
      );
  }

  onDayShiftChange(day: ShiftDay) {
    if (this.isReadOnly || !this.dayEditorMember) {
      return;
    }
    const member = this.dayEditorMember;
    this.commonService
      .saveDayOverride({
        Employee_ID: member.Employee_ID,
        Team_ID: this.selectedTeamId,
        Shift_Date: day.Shift_Date,
        Shift_ID: Number(day.Effective_Shift_ID),
        Host: this.host,
        User_ID: this.userId,
      })
      .subscribe({
        next: () => {
          day.Is_Override =
            Number(day.Effective_Shift_ID) !== Number(day.Weekly_Shift_ID);
        },
        error: () =>
          this.toastService.showError('Error', 'Could not save the day shift.'),
      });
  }

  closeDayEditor() {
    if (this.dayEditorMember) {
      this.dayEditorMember.Override_Count = this.days.filter(
        (d) => d.Is_Override,
      ).length;
    }
    this.dayEditorOpen = false;
    this.dayEditorMember = null;
    this.days = [];
  }

  columnTheme(shift: Shift): { header: string; dot: string; drop: string } {
    switch (shift.Shift_Code) {
      case 'S1':
        return {
          header: 'bg-emerald-50 text-emerald-700',
          dot: 'bg-emerald-500',
          drop: 'border-emerald-400 bg-emerald-50/60',
        };
      case 'S2':
        return {
          header: 'bg-amber-50 text-amber-700',
          dot: 'bg-amber-500',
          drop: 'border-amber-400 bg-amber-50/60',
        };
      case 'S3':
        return {
          header: 'bg-indigo-50 text-indigo-700',
          dot: 'bg-indigo-500',
          drop: 'border-indigo-400 bg-indigo-50/60',
        };
      default:
        return {
          header: 'bg-gray-100 text-gray-700',
          dot: 'bg-gray-400',
          drop: 'border-gray-400 bg-gray-50',
        };
    }
  }

  shiftName(shiftId: number): string {
    return (
      this.shifts.find((s) => s.Shift_ID === Number(shiftId))?.Shift_Name ?? ''
    );
  }

  formatDayDate(ymd: string): string {
    const parts = ymd.split('-').map(Number);
    if (parts.length !== 3) {
      return ymd;
    }
    const date = new Date(parts[0], parts[1] - 1, parts[2]);
    return date.toLocaleDateString('en-GB', { day: '2-digit', month: 'short' });
  }

  // ── Date helpers ──────────────────────────────────────────────

  private mondayOf(input: Date): Date {
    const date = new Date(input);
    const diff = (date.getDay() + 6) % 7; // Mon=0 .. Sun=6
    date.setDate(date.getDate() - diff);
    date.setHours(0, 0, 0, 0);
    return date;
  }

  private toYmd(date: Date): string {
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
