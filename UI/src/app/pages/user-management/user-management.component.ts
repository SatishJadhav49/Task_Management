import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonService, TeamOption } from '../common.service';
import { ToastService } from '../../shared/services/toast.service';

interface DesignationOption {
  Designation_ID: number;
  Designation_Name: string;
}

interface EmployeeOption {
  Employee_ID: number;
  Employee_Name: string;
}

interface EmployeeRow {
  Employee_ID: number;
  Employee_Name: string;
  Employee_No: string;
  Email_Address: string;
  Designation_ID: number;
  Designation_Name: string;
  Reporting_Manager_ID: number | null;
  Reporting_Manager_Name: string;
  Team_Lead_ID: number | null;
  Team_Lead_Name: string;
  Team_IDs: number[];
  Team_Names: string;
}

interface UserForm {
  employeeName: string;
  employeeNo: string;
  designationId: number;
  reportingManagerId: number;
  teamLeadId: number | null;
  teamIds: number[];
}

const MANAGER_DESIGNATION_ID = 1;
const LEAD_DESIGNATION_ID = 2;
const DEVELOPER_DESIGNATION_ID = 3;

@Component({
  selector: 'app-user-management',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './user-management.component.html',
  styleUrl: './user-management.component.css',
})
export class UserManagementComponent {
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  designations: DesignationOption[] = [];
  managers: EmployeeOption[] = [];
  teamLeads: EmployeeOption[] = [];
  employees: EmployeeRow[] = [];
  teams: TeamOption[] = [];

  designationFilter = 0;

  isSaving = false;
  isLoading = false;
  isEdit = false;
  editingEmployeeId = 0;

  newUser: UserForm = this.createEmptyForm();

  ngOnInit() {
    const designationId = Number(localStorage.getItem('Designation_ID') || '0');
    if (designationId !== MANAGER_DESIGNATION_ID) {
      this.router.navigate(['/no-access']);
      return;
    }

    this.loadDesignations();
    this.loadTeams();
    this.loadEmployees();
  }

  loadTeams() {
    this.commonService.getAllTeams().subscribe(
      (res: TeamOption[]) => {
        this.teams = (res ?? []).map((team: any) => ({
          Team_ID: Number(team.Team_ID),
          Team_Name: String(team.Team_Name ?? ''),
        }));
      },
      () => {
        this.teams = [];
        this.toastService.showError('Error', 'Failed to load teams.');
      },
    );
  }

  loadDesignations() {
    this.commonService.getAllDesignations().subscribe(
      (res: any[]) => {
        this.designations = (res ?? []).map((item: any) => ({
          Designation_ID: Number(item.Designation_ID),
          Designation_Name: String(item.Designation_Name ?? ''),
        }));
      },
      () => {
        this.designations = [];
        this.toastService.showError('Error', 'Failed to load designations.');
      },
    );
  }

  loadEmployees() {
    this.isLoading = true;
    this.commonService.getEmployees().subscribe(
      (res: any[]) => {
        const list = (res ?? []).map((item: any) => this.mapEmployeeRow(item));
        this.employees = list;
        this.managers = list
          .filter((e) => e.Designation_ID === MANAGER_DESIGNATION_ID)
          .map((e) => ({ Employee_ID: e.Employee_ID, Employee_Name: e.Employee_Name }));
        this.teamLeads = list
          .filter((e) => e.Designation_ID === LEAD_DESIGNATION_ID)
          .map((e) => ({ Employee_ID: e.Employee_ID, Employee_Name: e.Employee_Name }));
        this.isLoading = false;
      },
      () => {
        this.employees = [];
        this.managers = [];
        this.teamLeads = [];
        this.isLoading = false;
        this.toastService.showError('Error', 'Failed to load employees.');
      },
    );
  }

  private mapEmployeeRow(item: any): EmployeeRow {
    return {
      Employee_ID: Number(item.Employee_ID),
      Employee_Name: String(item.Employee_Name ?? ''),
      Employee_No: String(item.Employee_No ?? ''),
      Email_Address: String(item.Email_Address ?? ''),
      Designation_ID: Number(item.Designation_ID ?? 0),
      Designation_Name: String(item.Designation_Name ?? ''),
      Reporting_Manager_ID:
        item.Reporting_Manager_ID != null ? Number(item.Reporting_Manager_ID) : null,
      Reporting_Manager_Name: String(item.Reporting_Manager_Name ?? ''),
      Team_Lead_ID: item.Team_Lead_ID != null ? Number(item.Team_Lead_ID) : null,
      Team_Lead_Name: String(item.Team_Lead_Name ?? ''),
      Team_IDs: Array.isArray(item.Team_IDs)
        ? item.Team_IDs.map((id: any) => Number(id))
        : [],
      Team_Names: String(item.Team_Names ?? ''),
    };
  }

  get filteredEmployees(): EmployeeRow[] {
    if (!this.designationFilter) {
      return this.employees;
    }
    return this.employees.filter(
      (e) => e.Designation_ID === Number(this.designationFilter),
    );
  }

  startEdit(row: EmployeeRow) {
    this.isEdit = true;
    this.editingEmployeeId = row.Employee_ID;
    this.newUser = {
      employeeName: row.Employee_Name,
      employeeNo: row.Employee_No,
      designationId: row.Designation_ID,
      reportingManagerId: row.Reporting_Manager_ID ?? 0,
      teamLeadId: row.Team_Lead_ID ?? null,
      teamIds: [...row.Team_IDs],
    };
  }

  onSave() {
    if (!this.isFormValid()) {
      this.toastService.showError(
        'Validation',
        'Please fill all required fields.',
      );
      return;
    }

    const employeeNo = this.newUser.employeeNo.trim();
    const teamLeadValue =
      this.showTeamLead && this.newUser.teamLeadId && this.newUser.teamLeadId > 0
        ? Number(this.newUser.teamLeadId)
        : null;

    const basePayload = {
      Employee_Name: this.newUser.employeeName.trim(),
      Employee_No: employeeNo,
      Email_Address: `${employeeNo}@mahindra.com`,
      Designation_ID: Number(this.newUser.designationId),
      Reporting_Manager_ID: Number(this.newUser.reportingManagerId),
      Team_Lead_ID: teamLeadValue,
      Team_IDs: this.newUser.teamIds.map((id) => Number(id)),
      Audit_Type_Id: Number(localStorage.getItem('Audit_Type_Id') || '0'),
      Plant_ID: Number(localStorage.getItem('Plant_ID') || '0'),
      Plant_Code: localStorage.getItem('Plant_Code') || '',
      Inserted_Host: localStorage.getItem('Hostname') || '',
      Inserted_User_ID: Number(localStorage.getItem('Employee_ID') || '0'),
      Updated_Host: localStorage.getItem('Hostname') || '',
      Updated_User_ID: Number(localStorage.getItem('Employee_ID') || '0'),
      Shop_ID: 0,
      Model_ID: 0,
    };

    this.isSaving = true;
    const request$ = this.isEdit
      ? this.commonService.updateEmployee(this.editingEmployeeId, [basePayload])
      : this.commonService.createEmployee([basePayload]);

    request$.subscribe(
      (res) => {
        this.isSaving = false;
        this.toastService.showSuccess(
          res?.messageTitle || 'Success',
          res?.messageDetail ||
            (this.isEdit ? 'User updated successfully.' : 'User created successfully.'),
        );
        this.resetForm();
        this.loadEmployees();
      },
      () => {
        this.isSaving = false;
      },
    );
  }

  resetForm() {
    this.isEdit = false;
    this.editingEmployeeId = 0;
    this.newUser = this.createEmptyForm();
  }

  onDesignationChange() {
    if (!this.showTeamLead) {
      this.newUser.teamLeadId = null;
    }
    // Non-managers belong to a single team — keep only the first selection.
    if (!this.isManagerDesignation && this.newUser.teamIds.length > 1) {
      this.newUser.teamIds = [this.newUser.teamIds[0]];
    }
  }

  get showTeamLead(): boolean {
    return Number(this.newUser.designationId) === DEVELOPER_DESIGNATION_ID;
  }

  /** Managers can belong to multiple teams; everyone else picks one. */
  get isManagerDesignation(): boolean {
    return Number(this.newUser.designationId) === MANAGER_DESIGNATION_ID;
  }

  isTeamSelected(teamId: number): boolean {
    return this.newUser.teamIds.includes(Number(teamId));
  }

  toggleTeam(teamId: number) {
    const id = Number(teamId);
    if (this.newUser.teamIds.includes(id)) {
      this.newUser.teamIds = this.newUser.teamIds.filter((t) => t !== id);
    } else {
      this.newUser.teamIds = [...this.newUser.teamIds, id];
    }
  }

  /** Single-team dropdown model for non-manager designations. */
  get singleTeamId(): number {
    return this.newUser.teamIds[0] ?? 0;
  }

  set singleTeamId(value: number) {
    const id = Number(value);
    this.newUser.teamIds = id > 0 ? [id] : [];
  }

  isFormValid(): boolean {
    return (
      !!this.newUser.employeeName.trim() &&
      !!this.newUser.employeeNo.trim() &&
      this.newUser.designationId > 0 &&
      this.newUser.reportingManagerId > 0 &&
      this.newUser.teamIds.length > 0
    );
  }

  designationBadgeClass(id: number): string {
    switch (id) {
      case MANAGER_DESIGNATION_ID:
        return 'bg-purple-100 text-purple-700';
      case LEAD_DESIGNATION_ID:
        return 'bg-blue-100 text-blue-700';
      case DEVELOPER_DESIGNATION_ID:
        return 'bg-green-100 text-green-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  }

  private createEmptyForm(): UserForm {
    return {
      employeeName: '',
      employeeNo: '',
      designationId: 0,
      reportingManagerId: 0,
      teamLeadId: null,
      teamIds: [],
    };
  }
}
