import { inject, Injectable } from '@angular/core';
import {
  BehaviorSubject,
  catchError,
  combineLatest,
  distinctUntilChanged,
  map,
  Observable,
} from 'rxjs';
import { ApiRequestService } from '../shared/services/api-request.service';
import {
  Activity_Type_List,
  MM_Activity_Details,
  MM_Daily_Status,
} from './daily-update/activity.model';
import { MM_Task_Details, Task_Details_List } from './my-tasks/task.model';

export interface TeamOption {
  Team_ID: number;
  Team_Name: string;
}

export interface TaskViewContext {
  mode: string;
  teamId: number;
}

@Injectable({
  providedIn: 'root',
})
export class CommonService {
  readonly apiRequest = inject(ApiRequestService);

  public Is_Developer_Mode: boolean = true;
  public Is_Lead_Mode: boolean = false;
  public Is_Manager_Mode: boolean = false;

  /** Teams the logged-in user belongs to (loaded from localStorage after auth). */
  public userTeams: TeamOption[] = [];
  /** Currently selected team in manager mode. 0 = All Teams. */
  public selectedTeamId = 0;

  private readonly developerModeSubject = new BehaviorSubject<boolean>(
    this.Is_Developer_Mode,
  );
  private readonly leadModeSubject = new BehaviorSubject<boolean>(
    this.Is_Lead_Mode,
  );
  private readonly managerModeSubject = new BehaviorSubject<boolean>(
    this.Is_Manager_Mode,
  );
  private readonly selectedTeamSubject = new BehaviorSubject<number>(
    this.selectedTeamId,
  );

  readonly isDeveloperMode$ = this.developerModeSubject.asObservable();
  readonly isLeadMode$ = this.leadModeSubject.asObservable();
  readonly isManagerMode$ = this.managerModeSubject.asObservable();
  readonly selectedTeamId$ = this.selectedTeamSubject.asObservable();
  readonly taskViewMode$ = combineLatest([
    this.isDeveloperMode$,
    this.isLeadMode$,
    this.isManagerMode$,
  ]).pipe(
    map(([isDeveloper, isLead, isManager]) => {
      if (isManager) {
        return 'manager';
      }
      if (isLead) {
        return 'lead';
      }
      if (isDeveloper) {
        return 'developer';
      }
      return 'developer';
    }),
    distinctUntilChanged(),
  );

  /**
   * Mode + selected team together. Pages that load mode-dependent data should
   * subscribe to this so a team change in the topbar reloads them too.
   */
  readonly taskViewContext$: Observable<TaskViewContext> = combineLatest([
    this.taskViewMode$,
    this.selectedTeamId$,
  ]).pipe(
    map(([mode, teamId]) => ({ mode, teamId })),
    distinctUntilChanged(
      (prev, curr) => prev.mode === curr.mode && prev.teamId === curr.teamId,
    ),
  );

  loadUserTeamsFromStorage(): void {
    try {
      this.userTeams = (
        JSON.parse(localStorage.getItem('Teams') || '[]') as any[]
      ).map((team) => ({
        Team_ID: Number(team.Team_ID),
        Team_Name: String(team.Team_Name ?? ''),
      }));
    } catch {
      this.userTeams = [];
    }

    // Single team -> preselect it; multiple teams -> default to All Teams.
    this.selectedTeamId =
      this.userTeams.length === 1 ? this.userTeams[0].Team_ID : 0;
    this.selectedTeamSubject.next(this.selectedTeamId);
  }

  setSelectedTeam(teamId: number): void {
    this.selectedTeamId = Number(teamId) || 0;
    this.selectedTeamSubject.next(this.selectedTeamId);
  }

  getCurrentTaskViewMode(): string {
    if (this.Is_Manager_Mode) {
      return 'manager';
    }
    if (this.Is_Lead_Mode) {
      return 'lead';
    }
    return 'developer';
  }

  private emitRoleFlags(): void {
    this.developerModeSubject.next(this.Is_Developer_Mode);
    this.leadModeSubject.next(this.Is_Lead_Mode);
    this.managerModeSubject.next(this.Is_Manager_Mode);
  }

  initializeRoleFlagsFromDesignation(designationId?: number): void {
    const id = designationId ?? Number(localStorage.getItem('Designation_ID'));

    this.loadUserTeamsFromStorage();

    this.Is_Developer_Mode = true;
    this.Is_Lead_Mode = false;
    this.Is_Manager_Mode = false;

    if (id === 1 || id === 2) {
      // Manager/Lead starts in self view by default.
      this.Is_Developer_Mode = true;
    }

    this.emitRoleFlags();
  }

  toggleRoleMode(enabled: boolean, designationId?: number): void {
    const id = designationId ?? Number(localStorage.getItem('Designation_ID'));

    if (id === 1) {
      this.Is_Manager_Mode = enabled;
      this.Is_Lead_Mode = false;
      this.Is_Developer_Mode = !enabled;
      this.emitRoleFlags();
      return;
    }

    if (id === 2) {
      this.Is_Lead_Mode = enabled;
      this.Is_Manager_Mode = false;
      this.Is_Developer_Mode = !enabled;
      this.emitRoleFlags();
      return;
    }

    // Developer cannot switch to team mode.
    this.Is_Developer_Mode = true;
    this.Is_Lead_Mode = false;
    this.Is_Manager_Mode = false;
    this.emitRoleFlags();
  }

  getActivityTypes(): Observable<Activity_Type_List[]> {
    return this.apiRequest.get('MM_Activity_Details/GetAllActivityTypes');
  }

  addNewActivity(activity: any): Observable<any> {
    return this.apiRequest.post(
      'MM_Activity_Details/CreateActivityDetail',
      activity,
    );
  }

  getActivitiesByDate(
    filterFrom: string,
    filterTo: string,
    employeeId: number,
  ): Observable<any> {
    return this.apiRequest.get(
      `MM_Activity_Details/GetActivitiesByDate?filterFrom=${filterFrom}&filterTo=${filterTo}&employeeId=${employeeId}`,
    );
  }

  deleteActivity(activityId: number): Observable<any> {
    return this.apiRequest.delete(
      `MM_Activity_Details/DeleteActivityDetail/${activityId}`,
      {},
    );
  }

  updateActivity(activity: MM_Activity_Details): Observable<any> {
    return this.apiRequest.put(
      'MM_Activity_Details/UpdateActivityDetail',
      activity.Activity_ID,
      activity,
    );
  }

  getDailyStatusByDate(
    date: string,
    employeeId: number,
  ): Observable<MM_Daily_Status | null> {
    return this.apiRequest
      .get(
        `MM_Daily_Status/GetDailyStatusByDate?date=${date}&employeeId=${employeeId}`,
      )
      .pipe(
        map((response: MM_Daily_Status[] | MM_Daily_Status | null) => {
          if (Array.isArray(response)) {
            return response[0] ?? null;
          }
          return response ?? null;
        }),
      );
  }

  saveDailyStatus(status: MM_Daily_Status): Observable<any> {
    if (status.Status_ID && status.Status_ID > 0) {
      return this.apiRequest.put(
        'MM_Daily_Status/UpdateDailyStatus',
        status.Status_ID,
        status,
      );
    }
    return this.apiRequest.post('MM_Daily_Status/CreateDailyStatus', status);
  }

  // ── Task Methods ──────────────────────────────────────────────
  addNewTask(task: MM_Task_Details): Observable<any> {
    return this.apiRequest.post('MM_Task_Details/CreateTaskDetail', task);
  }

  updateTask(task: MM_Task_Details): Observable<any> {
    return this.apiRequest.put(
      'MM_Task_Details/UpdateTaskDetail',
      task.Task_ID,
      task,
    );
  }

  deleteTask(taskId: number): Observable<any> {
    return this.apiRequest.delete(
      `MM_Task_Details/DeleteTaskDetail/${taskId}`,
      {},
    );
  }

  updateTaskRemark(
    taskId: number,
    payload: {
      Manager_Remark: string;
      Updated_User_ID: number;
      Updated_Host: string;
    },
  ): Observable<any> {
    return this.apiRequest.post(
      `MM_Task_Details/UpdateManagerRemark/${taskId}`,
      payload,
    );
  }

  getTasksByEmployee(
    employeeId: number,
    mode?: string,
  ): Observable<Task_Details_List[]> {
    const taskMode = mode ?? this.getCurrentTaskViewMode();
    return this.apiRequest.get(
      `MM_Task_Details/GetTasksByEmployee?employeeId=${employeeId}&mode=${taskMode}${this.teamQueryParam(taskMode)}`,
    );
  }

  /** teamId only matters in manager mode; 0 = all of the manager's teams. */
  private teamQueryParam(mode: string): string {
    return mode === 'manager' ? `&teamId=${this.selectedTeamId}` : '';
  }

  getEmployees(): Observable<any[]> {
    return this.apiRequest
      .get('MM_EmployeeMaster/GetAllEmployees')
      .pipe(
        catchError(() =>
          this.apiRequest.get('MM_EmployeeMaster/GetAllEmployee'),
        ),
      );
  }

  getEmployeesByMode(
    employeeId: number,
    mode?: string,
  ): Observable<any[]> {
    const viewMode = mode ?? this.getCurrentTaskViewMode();
    return this.apiRequest.get(
      `MM_EmployeeMaster/GetEmployeesByMode?employeeId=${employeeId}&mode=${viewMode}${this.teamQueryParam(viewMode)}`,
    );
  }

  getAllDesignations(): Observable<any[]> {
    return this.apiRequest.get('MM_EmployeeMaster/GetAllDesignations');
  }

  getAllTeams(): Observable<TeamOption[]> {
    return this.apiRequest.get('MM_EmployeeMaster/GetAllTeams');
  }

  createEmployee(payload: any[]): Observable<any> {
    return this.apiRequest.post('MM_EmployeeMaster/CreateEmployee', payload);
  }

  // ── Shift Management ──────────────────────────────────────────
  getAllShifts(): Observable<any[]> {
    return this.apiRequest.get('MM_Shift/GetAllShifts');
  }

  getWeeklySchedule(teamId: number, weekStart: string): Observable<any[]> {
    return this.apiRequest.get(
      `MM_Shift/GetWeeklySchedule?teamId=${teamId}&weekStart=${weekStart}`,
    );
  }

  saveWeeklyAssignment(payload: {
    Employee_ID: number;
    Team_ID: number;
    Week_Start_Date: string;
    Shift_ID: number;
    Host: string;
    User_ID: number;
  }): Observable<any> {
    return this.apiRequest.post('MM_Shift/SaveWeeklyAssignment', payload);
  }

  getDayOverrides(employeeId: number, weekStart: string): Observable<any[]> {
    return this.apiRequest.get(
      `MM_Shift/GetDayOverrides?employeeId=${employeeId}&weekStart=${weekStart}`,
    );
  }

  saveDayOverride(payload: {
    Employee_ID: number;
    Team_ID: number;
    Shift_Date: string;
    Shift_ID: number;
    Host: string;
    User_ID: number;
  }): Observable<any> {
    return this.apiRequest.post('MM_Shift/SaveDayOverride', payload);
  }

  updateEmployee(employeeId: number, payload: any[]): Observable<any> {
    return this.apiRequest.post(
      `MM_EmployeeMaster/UpdateEmployee/${employeeId}`,
      payload,
    );
  }
}
