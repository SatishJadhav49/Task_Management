import { Component, Input, Output, EventEmitter } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { DEVELOPER_DESIGNATION_ID, LEAD_DESIGNATION_ID, MANAGER_DESIGNATION_ID, MES_EXECUTIVE_DESIGNATION_ID } from '../../app.constant';

interface NavItem {
  label: string;
  route: string;
  icon: string;
}


@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
  @Input() isOpen = true;
  @Output() closeSidebar = new EventEmitter<void>();
  Employee_Name: string = localStorage.getItem('Name') || 'User Name';
  Designation: string = localStorage.getItem('Designation') || 'Designation';

  get initials(): string {
    return this.Employee_Name.split(' ').map(w => w[0]).slice(0, 2).join('').toUpperCase();
  }

  private readonly designationId = Number(localStorage.getItem('Designation_ID') || '0');

  private readonly baseNavItems: NavItem[] = [
    {
      label: 'Dashboard',
      route: '/dashboard',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
            </svg>`
    },
    {
      label: 'Tasks Tracking',
      route: '/my-tasks',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
            </svg>`
    },
    {
      label: 'Daily Update',
      route: '/daily-update',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>`
    },
     {
      label: 'Reports',
      route: '/reports',
      icon: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 17v-2m-2-4H7m10 4h-2M7 13h10M7 17h10M7 9h10M7 5h10" />
            </svg>`
    }
  ];

  // Visible to team leads and managers.
  private readonly shiftItem: NavItem = {
    label: 'Shift Management',
    route: '/shift-management',
    icon: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>`
  };

  // Visible to managers, team leads and developers (not executive).
  private readonly deploymentItem: NavItem = {
    label: 'Deployment Approval',
    route: '/deployment-approval',
    icon: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
          </svg>`
  };

  private readonly managerOnlyItem: NavItem = {
    label: 'User Management',
    route: '/user-management',
    icon: `<svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7zm6-3l2 2m0 0l2-2m-2 2v-4" />
          </svg>`
  };

  // Managers/leads schedule shifts; developers get a read-only view.
  private readonly shiftDesignationIds = [
    MANAGER_DESIGNATION_ID,
    LEAD_DESIGNATION_ID,
    DEVELOPER_DESIGNATION_ID,
    MES_EXECUTIVE_DESIGNATION_ID
  ];

  // Deployment approval is for managers, leads and developers alike.
  private readonly deploymentDesignationIds = [
    MANAGER_DESIGNATION_ID,
    LEAD_DESIGNATION_ID,
    DEVELOPER_DESIGNATION_ID,
  ];

  readonly navItems: NavItem[] = [
    ...this.baseNavItems,
    ...(this.shiftDesignationIds.includes(this.designationId)
      ? [this.shiftItem]
      : []),
    ...(this.deploymentDesignationIds.includes(this.designationId)
      ? [this.deploymentItem]
      : []),
    ...(this.designationId === MANAGER_DESIGNATION_ID
      ? [this.managerOnlyItem]
      : []),
  ];
}
