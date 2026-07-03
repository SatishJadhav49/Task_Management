import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonService } from '../common.service';
import { ToastService } from '../../shared/services/toast.service';
import { MM_Task_Details, Task_Details_List } from './task.model';
import { Subscription } from 'rxjs';

interface TaskForm {
  taskDescription: string;
  priority: string;
  dueDate: string;
  status: string;
  assignedEmployeeId: number;
}

interface EmployeeOption {
  Employee_ID: number;
  Employee_Name: string;
}

@Component({
  selector: 'app-my-tasks',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './my-tasks.component.html',
  styleUrl: './my-tasks.component.css'
})
export class MyTasksComponent {
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);

  today = new Date().toISOString().split('T')[0];

  activeStatusFilter = 'In Progress';
  statusFilters = ['All', 'To Do', 'In Progress', 'Completed', 'Overdue'];
  activePriorityFilter = 'All';
  priorityFilters = ['All', 'High', 'Medium', 'Low'];
  priorities = ['High', 'Medium', 'Low'];
  statuses = ['To Do', 'In Progress', 'Completed'];

  showForm = false;
  isEdit = false;
  editingTaskId = 0;
  userId = 0;
  userName = '';
  isManager = false;

  // Manager remark modal state
  remarkModalOpen = false;
  remarkTaskRef: Task_Details_List | null = null;
  remarkText = '';
  remarkSaving = false;
  readonly remarkMaxLength = 1000;

  newTask: TaskForm = {
    taskDescription: '',
    priority: '',
    dueDate: '',
    status: 'To Do',
    assignedEmployeeId: 0,
  };

  tasks: Task_Details_List[] = [];
  employees: EmployeeOption[] = [];
  currentTaskMode = 'developer';
  private modeSubscription?: Subscription;

  ngOnInit() {
    this.userId = parseInt(localStorage.getItem('Employee_ID') || '0');
    this.userName = localStorage.getItem('Name') || 'Current User';
    this.isManager = Number(localStorage.getItem('Designation_ID') || '0') === 1;
    this.newTask.assignedEmployeeId = this.userId;
    this.modeSubscription = this.commonService.taskViewContext$.subscribe(({ mode }) => {
      this.currentTaskMode = mode;
      this.loadEmployees(mode);
      this.loadTasks(mode);
    });
  }

  ngOnDestroy() {
    this.modeSubscription?.unsubscribe();
  }

  loadEmployees(mode: string = this.currentTaskMode) {
    this.commonService.getEmployeesByMode(this.userId, mode).subscribe(
      (res: any[]) => {
        const parsed = (res ?? [])
          .map((item: any) => {
            const employeeId = Number(item.Employee_ID ?? item.employeeId ?? item.Emp_ID ?? 0);
            const employeeName = String(
              item.Employee_Name ?? item.EmployeeName ?? item.Name ?? item.Employee_No ?? `Employee ${employeeId}`,
            );

            if (!employeeId) {
              return null;
            }

            return {
              Employee_ID: employeeId,
              Employee_Name: employeeName,
            } as EmployeeOption;
          })
          .filter((emp): emp is EmployeeOption => !!emp);

        this.employees = parsed;
        this.ensureCurrentEmployeeInList();
        this.resetAssignedEmployeeToSelf();
      },
      () => {
        this.employees = [];
        this.ensureCurrentEmployeeInList();
        this.resetAssignedEmployeeToSelf();
      }
    );
  }

  private resetAssignedEmployeeToSelf() {
    if (this.userId) {
      this.newTask.assignedEmployeeId = this.userId;
    }
  }

  ensureCurrentEmployeeInList() {
    if (!this.userId) {
      return;
    }

    const exists = this.employees.some((emp) => emp.Employee_ID === this.userId);
    if (!exists) {
      this.employees.unshift({
        Employee_ID: this.userId,
        Employee_Name: this.userName,
      });
    }

    if (!this.newTask.assignedEmployeeId) {
      this.newTask.assignedEmployeeId = this.userId;
    }
  }

  loadTasks(mode: string = this.currentTaskMode) {
    this.commonService.getTasksByEmployee(this.userId, mode).subscribe(
      (res: Task_Details_List[]) => {
        this.tasks = res ?? [];
      },
      () => {
        this.tasks = [];
        this.toastService.showError('Error', 'Failed to fetch tasks.');
      }
    );
  }

  openNewTaskForm() {
    this.isEdit = false;
    this.showForm = true;
    this.editingTaskId = 0;
    this.newTask = {
      taskDescription: '',
      priority: '',
      dueDate: '',
      status: 'To Do',
      assignedEmployeeId: this.userId,
    };
  }

  get filteredTasks(): Task_Details_List[] {
    return this.tasks.filter((task) => {
      const matchesStatus = this.activeStatusFilter === 'All'
        ? true
        : this.activeStatusFilter === 'Overdue'
          ? this.isOverdueTask(task)
          : task.Status === this.activeStatusFilter;

      const matchesPriority = this.activePriorityFilter === 'All'
        ? true
        : task.Priority === this.activePriorityFilter;

      return matchesStatus && matchesPriority;
    });
  }

  onSave() {
    const payload: MM_Task_Details = {
      Task_ID: this.editingTaskId,
      Employee_ID: Number(this.newTask.assignedEmployeeId || this.userId),
      Due_Date: this.newTask.dueDate,
      Task_Description: this.newTask.taskDescription,
      Priority: this.newTask.priority,
      Status: this.isEdit ? this.newTask.status : 'To Do',
      Plant_Code: localStorage.getItem('Plant_Code') || '',
      Inserted_Host: localStorage.getItem('Hostname') || '',
      Inserted_User_ID: parseInt(localStorage.getItem('Employee_ID') || '0'),
      Updated_Host: localStorage.getItem('Hostname') || '',
      Updated_User_ID: parseInt(localStorage.getItem('Employee_ID') || '0'),
    };

    const request = this.isEdit
      ? this.commonService.updateTask(payload)
      : this.commonService.addNewTask(payload);

    request.subscribe(
      (res) => {
        this.toastService.showSuccess(res.messageTitle, res.messageDetail);
        this.resetForm();
        this.loadTasks(this.currentTaskMode);
      },
      () => this.toastService.showError(
        'Error',
        this.isEdit ? 'Failed to update task.' : 'Failed to save task.'
      )
    );
  }

  startEdit(task: Task_Details_List) {
    const dueDate = task.Due_Date ? task.Due_Date.split('T')[0] : '';

    this.isEdit = true;
    this.showForm = true;
    this.editingTaskId = Number(task.Task_ID);
    this.newTask = {
      taskDescription: task.Task_Description,
      priority: task.Priority,
      dueDate,
      status: task.Status || 'To Do',
      assignedEmployeeId: Number(task.Employee_ID || this.userId),
    };
  }

  removeTask(task: Task_Details_List) {
    

    this.commonService.deleteTask(Number(task.Task_ID)).subscribe(
      (res) => {
        this.toastService.showSuccess(res.messageTitle, res.messageDetail);

        if (this.isEdit && this.editingTaskId === Number(task.Task_ID)) {
          this.resetForm();
        }

        this.loadTasks(this.currentTaskMode);
      },
      () => {
        this.toastService.showError('Error', 'Failed to delete task.');
      }
    );
  }

  updateTaskStatus(task: Task_Details_List, status: string) {
    if (!status || task.Status === status) {
      return;
    }

    const previousStatus = task.Status;
    task.Status = status;

    const payload: MM_Task_Details = {
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
    };

    this.commonService.updateTask(payload).subscribe(
      (res) => {
        if (this.isEdit && this.editingTaskId === Number(task.Task_ID)) {
          this.newTask.status = status;
        }

        this.toastService.showSuccess(res.messageTitle, res.messageDetail);
        this.loadTasks(this.currentTaskMode);
      },
      () => {
        task.Status = previousStatus;
        if (this.isEdit && this.editingTaskId === Number(task.Task_ID)) {
          this.newTask.status = previousStatus;
        }
        this.toastService.showError('Error', 'Failed to update task status.');
      }
    );
  }

  resetForm() {
    this.isEdit = false;
    this.showForm = false;
    this.editingTaskId = 0;
    this.newTask = {
      taskDescription: '',
      priority: '',
      dueDate: '',
      status: 'To Do',
      assignedEmployeeId: this.userId,
    };
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

  priorityClass(p: string) {
    return p === 'High'   ? 'bg-red-100 text-red-700' :
           p === 'Medium' ? 'bg-yellow-100 text-yellow-700' :
                            'bg-green-100 text-green-700';
  }

  statusClass(s: string) {
    return s === 'Completed'   ? 'bg-green-100 text-green-700'  :
           s === 'In Progress' ? 'bg-blue-100 text-blue-700'    :
           s === 'Overdue'     ? 'bg-red-100 text-red-700'      :
                                 'bg-gray-100 text-gray-600';
  }

  statusSelectClass(s: string) {
    return s === 'Completed'
      ? 'border-green-200 bg-green-50 text-green-700'
      : s === 'In Progress'
        ? 'border-blue-200 bg-blue-50 text-blue-700'
        : 'border-gray-200 bg-gray-50 text-gray-700';
  }

  // ── Manager remark ─────────────────────────────────────────────

  openRemarkModal(task: Task_Details_List) {
    if (!this.isManager) {
      return;
    }
    this.remarkTaskRef = task;
    this.remarkText = task.Manager_Remark ?? '';
    this.remarkModalOpen = true;
  }

  closeRemarkModal() {
    if (this.remarkSaving) {
      return;
    }
    this.remarkModalOpen = false;
    this.remarkTaskRef = null;
    this.remarkText = '';
  }

  clearRemark() {
    this.remarkText = '';
  }

  saveRemark() {
    if (!this.remarkTaskRef) {
      return;
    }

    const payload = {
      Manager_Remark: this.remarkText.trim(),
      Updated_User_ID: this.userId,
      Updated_Host: localStorage.getItem('Hostname') || '',
    };

    this.remarkSaving = true;
    this.commonService
      .updateTaskRemark(Number(this.remarkTaskRef.Task_ID), payload)
      .subscribe(
        (res) => {
          this.remarkSaving = false;
          this.toastService.showSuccess(
            res?.messageTitle || 'Success',
            res?.messageDetail || 'Manager remark updated.',
          );
          this.remarkModalOpen = false;
          this.remarkTaskRef = null;
          this.loadTasks(this.currentTaskMode);
        },
        () => {
          this.remarkSaving = false;
        },
      );
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
}
