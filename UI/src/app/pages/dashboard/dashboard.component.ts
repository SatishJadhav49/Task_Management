import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CommonService } from '../common.service';
import { ToastService } from '../../shared/services/toast.service';
import { Task_Details_List } from '../my-tasks/task.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);

  today = new Date().toISOString().split('T')[0];
  userId = 0;
  Employee_Name = "User";
  statuses = ['To Do', 'In Progress', 'Completed'];

  stats = [
    { label: 'Total Tasks', value: 0, icon: 'total', color: 'bg-blue-50 text-blue-600', border: 'border-blue-100' },
    { label: 'In Progress', value: 0,  icon: 'progress', color: 'bg-yellow-50 text-yellow-600', border: 'border-yellow-100' },
    { label: 'Completed',   value: 0, icon: 'done', color: 'bg-green-50 text-green-600', border: 'border-green-100' },
    { label: 'Overdue',     value: 0,  icon: 'overdue', color: 'bg-red-50 text-red-600', border: 'border-red-100' },
  ];

  allTasks: Task_Details_List[] = [];
  currentTaskMode = 'developer';
  private modeSubscription?: Subscription;

  ngOnInit() {
    this.userId = parseInt(localStorage.getItem('Employee_ID') || '0');
    this.Employee_Name = localStorage.getItem('Name') || 'User';
    this.modeSubscription = this.commonService.taskViewContext$.subscribe(({ mode }) => {
      this.currentTaskMode = mode;
      this.loadDashboardData(mode);
    });
  }

  ngOnDestroy() {
    this.modeSubscription?.unsubscribe();
  }

  loadDashboardData(mode: string = this.currentTaskMode) {
    this.commonService.getTasksByEmployee(this.userId, mode).subscribe(
      (tasks: Task_Details_List[]) => {
        this.allTasks = tasks ?? [];
        this.updateStats();
      },
      () => {
        this.allTasks = [];
        this.updateStats();
        this.toastService.showError('Error', 'Failed to fetch dashboard data.');
      }
    );
  }

  get recentTasks(): Task_Details_List[] {
    return this.allTasks
      .filter((task) =>  task.Status === 'In Progress')
      .sort((left, right) => left.Due_Date.localeCompare(right.Due_Date));
  }

  updateStats() {
    const overdueCount = this.allTasks.filter((task) => this.isOverdueTask(task)).length;
    const inProgressCount = this.allTasks.filter((task) => task.Status === 'In Progress').length;
    const completedCount = this.allTasks.filter((task) => task.Status === 'Completed').length;

    this.stats = [
      { label: 'Total Tasks', value: this.allTasks.length, icon: 'total', color: 'bg-blue-50 text-blue-600', border: 'border-blue-100' },
      { label: 'In Progress', value: inProgressCount, icon: 'progress', color: 'bg-yellow-50 text-yellow-600', border: 'border-yellow-100' },
      { label: 'Completed', value: completedCount, icon: 'done', color: 'bg-green-50 text-green-600', border: 'border-green-100' },
      { label: 'Overdue', value: overdueCount, icon: 'overdue', color: 'bg-red-50 text-red-600', border: 'border-red-100' },
    ];
  }

  isOverdueTask(task: Task_Details_List): boolean {
    if (task.Status === 'Completed' || !task.Due_Date) {
      return false;
    }

    const dueDateOnly = task.Due_Date.split('T')[0];
    return dueDateOnly < this.today;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const dt = dateStr.includes('T') ? new Date(dateStr) : new Date(dateStr + 'T00:00:00');
    return dt.toLocaleDateString('en-US', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  hasRemark(task: Task_Details_List): boolean {
    return !!(task.Manager_Remark && task.Manager_Remark.trim().length > 0);
  }

  formatRemarkMeta(task: Task_Details_List): string {
    const author = task.Remark_Updated_By_Name || 'Manager';
    if (!task.Remark_Updated_Date) {
      return author;
    }
    const dt = new Date(task.Remark_Updated_Date);
    if (Number.isNaN(dt.getTime())) {
      return author;
    }
    return `${author} · ${dt.toLocaleDateString('en-US', { day: 'numeric', month: 'short', year: 'numeric' })}`;
  }

  updateTaskStatus(task: Task_Details_List, status: string) {
    if (!status || task.Status === status) {
      return;
    }

    const previousStatus = task.Status;
    task.Status = status;
    this.updateStats();

    this.commonService.updateTask({
      Task_ID: Number(task.Task_ID),
      Employee_ID: Number(task.Employee_ID || this.userId),
      Due_Date: task.Due_Date ? task.Due_Date.split('T')[0] : '',
      Task_Description: task.Task_Description,
      Priority: task.Priority,
      Status: status,
      Plant_Code: localStorage.getItem('Plant_Code') || '',
      Inserted_Host: localStorage.getItem('Hostname') || '',
      Inserted_User_ID: parseInt(localStorage.getItem('Employee_ID') || '0'),
      Updated_Host: localStorage.getItem('Hostname') || '',
      Updated_User_ID: parseInt(localStorage.getItem('Employee_ID') || '0'),
    }).subscribe(
      (res) => {
        this.toastService.showSuccess(res.messageTitle, res.messageDetail);
        this.loadDashboardData(this.currentTaskMode);
      },
      () => {
        task.Status = previousStatus;
        this.updateStats();
        this.toastService.showError('Error', 'Failed to update task status.');
      }
    );
  }

  priorityClass(p: string) {
    return p === 'High' ? 'bg-red-100 text-red-700' :
           p === 'Medium' ? 'bg-yellow-100 text-yellow-700' :
           'bg-green-100 text-green-700';
  }

  statusClass(s: string) {
    return s === 'Completed'   ? 'bg-green-100 text-green-700' :
           s === 'In Progress' ? 'bg-blue-100 text-blue-700'   :
           s === 'Overdue'     ? 'bg-red-100 text-red-700'     :
           'bg-gray-100 text-gray-600';
  }

  statusSelectClass(s: string) {
    return s === 'Completed'
      ? 'border-green-200 bg-green-50 text-green-700'
      : s === 'In Progress'
        ? 'border-blue-200 bg-blue-50 text-blue-700'
        : 'border-gray-200 bg-gray-50 text-gray-700';
  }
}
