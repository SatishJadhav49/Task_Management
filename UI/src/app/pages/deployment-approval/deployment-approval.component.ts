import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { CommonService } from '../common.service';
import { ToastService } from '../../shared/services/toast.service';
import { Deployment_Request } from './deployment.model';
import { CommonModule } from '@angular/common';

interface RequestForm {
  featureModule: string;
  changesDescription: string;
  riskChallenge: string;
  changeType: string;
}

const MANAGER_DESIGNATION_ID = 1;
const LEAD_DESIGNATION_ID = 2;
const DEVELOPER_DESIGNATION_ID = 3;

@Component({
  selector: 'app-deployment-approval',
  standalone: true,
  imports: [FormsModule,CommonModule],
  templateUrl: './deployment-approval.component.html',
  styleUrl: './deployment-approval.component.css',
})
export class DeploymentApprovalComponent {
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);
  private readonly router = inject(Router);

  private readonly host = localStorage.getItem('Hostname') || '';
  private readonly plantCode = localStorage.getItem('Plant_Code') || '';
  userId = 0;
  userName = '';
  designationId = 0;

  isManager = false;
  canRaise = false; // developers and team leads raise requests

  changeTypes = ['Minor', 'Major', 'Critical'];
  statusFilters = ['Pending', 'Approved', 'Rejected', 'All'];
  activeStatusFilter = 'Pending';

  // Row whose full details (description, risk, remark) are expanded.
  expandedId = 0;

  showForm = false;
  isSaving = false;
  isLoading = false;

  newRequest: RequestForm = this.createEmptyForm();

  requests: Deployment_Request[] = [];
  currentMode = 'developer';
  private modeSubscription?: Subscription;

  // Reject-with-remark modal
  rejectModalOpen = false;
  rejectTarget: Deployment_Request | null = null;
  rejectRemark = '';
  isDeciding = false;
  readonly remarkMaxLength = 1000;

  ngOnInit() {
    this.designationId = Number(localStorage.getItem('Designation_ID') || '0');
    if (
      this.designationId !== MANAGER_DESIGNATION_ID &&
      this.designationId !== LEAD_DESIGNATION_ID &&
      this.designationId !== DEVELOPER_DESIGNATION_ID
    ) {
      this.router.navigate(['/no-access']);
      return;
    }

    this.userId = Number(localStorage.getItem('Employee_ID') || '0');
    this.userName = localStorage.getItem('Name') || 'Current User';
    this.isManager = this.designationId === MANAGER_DESIGNATION_ID;
    this.canRaise =
      this.designationId === LEAD_DESIGNATION_ID ||
      this.designationId === DEVELOPER_DESIGNATION_ID;

    // Managers always see their approval queue here (they don't raise requests),
    // so the topbar My View / Manager Mode toggle doesn't hide it. Leads and
    // developers follow the toggle: lead = own + team, developer = own only.
    this.modeSubscription = this.commonService.taskViewMode$.subscribe((mode) => {
      this.currentMode = this.isManager ? 'manager' : mode;
      this.loadRequests(this.currentMode);
    });
  }

  ngOnDestroy() {
    this.modeSubscription?.unsubscribe();
  }

  loadRequests(mode: string = this.currentMode) {
    this.isLoading = true;
    this.commonService.getDeploymentRequests(this.userId, mode).subscribe(
      (res: any[]) => {
        this.requests = (res ?? []).map((r: any) => this.mapRequest(r));
        this.isLoading = false;
      },
      () => {
        this.requests = [];
        this.isLoading = false;
        this.toastService.showError('Error', 'Failed to load requests.');
      },
    );
  }

  private mapRequest(r: any): Deployment_Request {
    return {
      Request_ID: Number(r.Request_ID),
      Feature_Module: String(r.Feature_Module ?? ''),
      Changes_Description: String(r.Changes_Description ?? ''),
      Risk_Challenge: r.Risk_Challenge ?? null,
      Change_Type: String(r.Change_Type ?? ''),
      Status: String(r.Status ?? 'Pending'),
      Manager_Remark: r.Manager_Remark ?? null,
      Requested_By: Number(r.Requested_By ?? 0),
      Requested_By_Name: String(r.Requested_By_Name ?? ''),
      Requested_By_Designation: String(r.Requested_By_Designation ?? ''),
      Approver_Manager_ID:
        r.Approver_Manager_ID != null ? Number(r.Approver_Manager_ID) : null,
      Approver_Manager_Name: String(r.Approver_Manager_Name ?? ''),
      Approved_By: r.Approved_By != null ? Number(r.Approved_By) : null,
      Approved_By_Name: String(r.Approved_By_Name ?? ''),
      Approved_Date: r.Approved_Date ?? null,
      Requested_Date: String(r.Requested_Date ?? ''),
    };
  }

  get filteredRequests(): Deployment_Request[] {
    if (this.activeStatusFilter === 'All') {
      return this.requests;
    }
    return this.requests.filter((r) => r.Status === this.activeStatusFilter);
  }

  get pendingCount(): number {
    return this.requests.filter((r) => r.Status === 'Pending').length;
  }

  countByStatus(status: string): number {
    if (status === 'All') {
      return this.requests.length;
    }
    return this.requests.filter((r) => r.Status === status).length;
  }

  setStatusFilter(status: string) {
    this.activeStatusFilter = status;
  }

  toggleExpand(requestId: number) {
    this.expandedId = this.expandedId === requestId ? 0 : requestId;
  }

  // ── Raise a request ───────────────────────────────────────────

  toggleForm() {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.newRequest = this.createEmptyForm();
    }
  }

  isFormValid(): boolean {
    return (
      !!this.newRequest.featureModule.trim() &&
      !!this.newRequest.changesDescription.trim() &&
      !!this.newRequest.changeType
    );
  }

  onSubmit() {
    if (!this.isFormValid()) {
      this.toastService.showError(
        'Validation',
        'Feature/Module, Changes and Change Type are required.',
      );
      return;
    }

    this.isSaving = true;
    this.commonService
      .createDeploymentRequest({
        Feature_Module: this.newRequest.featureModule.trim(),
        Changes_Description: this.newRequest.changesDescription.trim(),
        Risk_Challenge: this.newRequest.riskChallenge.trim() || null,
        Change_Type: this.newRequest.changeType,
        Requested_By: this.userId,
        Plant_Code: this.plantCode,
        Inserted_Host: this.host,
        Inserted_User_ID: this.userId,
      })
      .subscribe(
        (res) => {
          this.isSaving = false;
          this.toastService.showSuccess(
            res?.messageTitle || 'Success',
            res?.messageDetail || 'Deployment request submitted.',
          );
          this.newRequest = this.createEmptyForm();
          this.showForm = false;
          this.loadRequests();
        },
        () => {
          this.isSaving = false;
        },
      );
  }

  withdraw(request: Deployment_Request) {
    if (request.Status !== 'Pending') {
      return;
    }
    this.commonService
      .deleteDeploymentRequest(request.Request_ID, this.userId)
      .subscribe(
        () => {
          this.toastService.showSuccess('Withdrawn', 'Request withdrawn.');
          this.loadRequests();
        },
        () => {},
      );
  }

  // ── Manager decision ──────────────────────────────────────────

  approve(request: Deployment_Request) {
    if (!this.isManager || request.Status !== 'Pending') {
      return;
    }
    this.isDeciding = true;
    this.commonService
      .updateDeploymentDecision(request.Request_ID, {
        Status: 'Approved',
        Manager_Remark: null,
        Updated_User_ID: this.userId,
        Updated_Host: this.host,
      })
      .subscribe(
        (res) => {
          this.isDeciding = false;
          this.toastService.showSuccess(
            'Approved',
            res?.messageDetail || 'Request approved.',
          );
          this.loadRequests();
        },
        () => {
          this.isDeciding = false;
        },
      );
  }

  openRejectModal(request: Deployment_Request) {
    if (!this.isManager || request.Status !== 'Pending') {
      return;
    }
    this.rejectTarget = request;
    this.rejectRemark = '';
    this.rejectModalOpen = true;
  }

  closeRejectModal() {
    this.rejectModalOpen = false;
    this.rejectTarget = null;
    this.rejectRemark = '';
  }

  confirmReject() {
    if (!this.rejectTarget) {
      return;
    }
    if (!this.rejectRemark.trim()) {
      this.toastService.showError(
        'Validation',
        'Please add a remark explaining the rejection.',
      );
      return;
    }

    this.isDeciding = true;
    this.commonService
      .updateDeploymentDecision(this.rejectTarget.Request_ID, {
        Status: 'Rejected',
        Manager_Remark: this.rejectRemark.trim(),
        Updated_User_ID: this.userId,
        Updated_Host: this.host,
      })
      .subscribe(
        (res) => {
          this.isDeciding = false;
          this.toastService.showSuccess(
            'Rejected',
            res?.messageDetail || 'Request rejected.',
          );
          this.closeRejectModal();
          this.loadRequests();
        },
        () => {
          this.isDeciding = false;
        },
      );
  }

  // ── Presentation helpers ──────────────────────────────────────

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Approved':
        return 'bg-green-100 text-green-700';
      case 'Rejected':
        return 'bg-red-100 text-red-700';
      default:
        return 'bg-amber-100 text-amber-700';
    }
  }

  changeTypeBadgeClass(type: string): string {
    switch (type) {
      case 'Critical':
        return 'bg-red-100 text-red-700';
      case 'Major':
        return 'bg-amber-100 text-amber-700';
      case 'Minor':
        return 'bg-emerald-100 text-emerald-700';
      default:
        return 'bg-gray-100 text-gray-700';
    }
  }

  formatDateTime(value: string | null): string {
    if (!value) {
      return '';
    }
    const date = new Date(value);
    if (isNaN(date.getTime())) {
      return value;
    }
    return date.toLocaleString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  formatDate(value: string | null): string {
    if (!value) {
      return '';
    }
    const date = new Date(value);
    if (isNaN(date.getTime())) {
      return value;
    }
    return date.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  }

  formatTime(value: string | null): string {
    if (!value) {
      return '';
    }
    const date = new Date(value);
    if (isNaN(date.getTime())) {
      return '';
    }
    return date.toLocaleTimeString('en-GB', {
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  get listHeading(): string {
    if (this.currentMode === 'manager') {
      return 'Requests awaiting your decision';
    }
    if (this.currentMode === 'lead') {
      return "My team's requests";
    }
    return 'My requests';
  }

  private createEmptyForm(): RequestForm {
    return {
      featureModule: '',
      changesDescription: '',
      riskChallenge: '',
      changeType: '',
    };
  }
}
