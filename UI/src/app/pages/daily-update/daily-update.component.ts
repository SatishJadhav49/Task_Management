import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  Activity_Details_List,
  Activity_Type_List,
  MM_Activity_Details,
  MM_Daily_Status,
} from './activity.model';
import { CommonService } from '../common.service';
import { ToastService } from '../../shared/services/toast.service';

interface DailyEntry {
  activityId: number;
  activityTypeId: number;
  date: string;
  activityType: string;
  description: string;
  addInMail: boolean;
}

interface DailyRemarkOption {
  value: string;
  label: string;
  isWorking: boolean;
}

interface DailyStatusForm {
  statusId: number;
  date: string;
  inTime: string;
  outTime: string;
  status: string;
  remark: string;
  isWorking: boolean;
}

@Component({
  selector: 'app-daily-update',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './daily-update.component.html',
  styleUrl: './daily-update.component.css',
})
export class DailyUpdateComponent {
  today = new Date().toISOString().split('T')[0];
  showDailyStatusPanel = false;

  // Date range filter — defaults to today only
  filterFrom: string = this.today;
  filterTo: string = this.today;

  activityTypes: Activity_Type_List[] = [];

  // New entry form model
  newEntry: DailyEntry = {
    activityId: 0,
    activityTypeId: 0,
    date: this.today,
    activityType: '',
    description: '',
    addInMail: false,
  };

  entries: DailyEntry[] = [];

  readonly remarkOptions: DailyRemarkOption[] = [
    { value: 'working', label: 'Working', isWorking: true },
    { value: 'leave', label: 'Leave', isWorking: false },
    { value: 'holiday', label: 'Holiday', isWorking: false },
    { value: 'weeklyoff', label: 'Weekly Off', isWorking: false },
    { value: 'com-off', label: 'Comp Off', isWorking: false },
  ];

  dailyStatus: DailyStatusForm = this.createDefaultDailyStatus(this.today);
  isDailyStatusEdit = false;
  isDailyStatusLoading = false;
  isDailyStatusSaving = false;

  isEdit = false;

  User_ID:number = 0;

  // Inject services
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);
  ngOnInit() {
    this.commonService.getActivityTypes().subscribe((types) => {
      this.activityTypes = types;
    });
    this.User_ID = parseInt(localStorage.getItem('Employee_ID') || '0');
    this.getActivitiesForDate();
    this.loadDailyStatusForDate(this.dailyStatus.date);
  }

  toggleDailyStatusPanel() {
    this.showDailyStatusPanel = !this.showDailyStatusPanel;
  }

  closeDailyStatusPanel() {
    this.showDailyStatusPanel = false;
  }

  onSave() {
    const saveData: MM_Activity_Details = {
      Activity_ID: this.newEntry.activityId ?? 0,
      Activity_Type_ID: Number(this.newEntry.activityType),
      Description: this.newEntry.description,
      Activity_Date: this.newEntry.date,
      Add_In_Mail: this.newEntry.addInMail,
      Employee_ID: this.User_ID,
      Updated_Host: localStorage.getItem('Hostname') || '',
      Updated_User_ID: parseInt(localStorage.getItem('Employee_ID') || '0'),
      Plant_Code: localStorage.getItem('Plant_Code') || '',
      Inserted_Host: localStorage.getItem('Hostname') || '',
      Inserted_User_ID: parseInt(localStorage.getItem('Employee_ID') || '0'),
    };
    if (this.isEdit) {
      this.commonService.updateActivity(saveData).subscribe(
        (res) => {
          this.toastService.showSuccess(res.messageTitle, res.messageDetail);
          this.resetForm();
          this.getActivitiesForDate();
        },
        () =>
          this.toastService.showError('Error', 'Failed to update activity.'),
      );
    } else {
      this.commonService.addNewActivity(saveData).subscribe(
        (res) => {
          this.toastService.showSuccess(res.messageTitle, res.messageDetail);
          this.resetForm();
          this.getActivitiesForDate();
        },
        () => this.toastService.showError('Error', 'Failed to save activity.'),
      );
    }
  }

  resetForm() {
    this.isEdit = false;
    this.newEntry = {
      activityId: 0,
      activityTypeId: 0,
      date: this.today,
      activityType: '',
      description: '',
      addInMail: false,
    };
  }

  getActivitiesForDate() {
    this.commonService.getActivitiesByDate(this.filterFrom,this.filterTo, this.User_ID).subscribe(
      (activities: Activity_Details_List[]) => {
        console.log('Activities for date', this.filterFrom, activities);
        this.entries = activities.map((a) => ({
          activityId: a.Activity_ID,
          activityTypeId: a.Activity_Type_ID,
          date: a.Activity_Date,
          activityType: a.Activity_Name,
          description: a.Description,
          addInMail: a.Add_In_Mail,
        }));
        console.log(this.entries);

      },
      (error) => {
        console.error('Error fetching activities:', error);
        this.toastService.showError('Error', 'Failed to fetch activities for the selected date.');
      },
    );
  }

  createDefaultDailyStatus(date: string): DailyStatusForm {
    return {
      statusId: 0,
      date,
      inTime: '',
      outTime: '',
      status: 'Present',
      remark: 'working',
      isWorking: true,
    };
  }

  onDateRangeChanged() {
    this.getActivitiesForDate();
    this.dailyStatus.date = this.filterFrom;
    this.loadDailyStatusForDate(this.dailyStatus.date);
  }

  resetDateFiltersToToday() {
    this.filterFrom = this.today;
    this.filterTo = this.today;
    this.dailyStatus.date = this.today;
    this.getActivitiesForDate();
    this.loadDailyStatusForDate(this.today);
  }

  onDailyStatusDateChange() {
    this.loadDailyStatusForDate(this.dailyStatus.date);
  }

  onRemarkChange() {
    const selected = this.remarkOptions.find(
      (option) => option.value === this.dailyStatus.remark,
    );
    const isWorking = selected?.isWorking ?? false;

    this.dailyStatus.isWorking = isWorking;
    this.dailyStatus.status = isWorking ? 'Present' : 'Not Working';

    if (!isWorking) {
      this.dailyStatus.inTime = '';
      this.dailyStatus.outTime = '';
    }
  }

  loadDailyStatusForDate(date: string) {
    if (!date || !this.User_ID) {
      return;
    }

    this.isDailyStatusLoading = true;
    this.commonService.getDailyStatusByDate(date, this.User_ID).subscribe(
      (response) => {
        this.isDailyStatusLoading = false;
        if (!response) {
          this.isDailyStatusEdit = false;
          this.dailyStatus = this.createDefaultDailyStatus(date);
          return;
        }

        this.isDailyStatusEdit = true;
        this.dailyStatus = {
          statusId: response.Status_ID,
          date: response.Date?.split('T')[0] || date,
          inTime: this.toTimeInputValue(response.In_Time),
          outTime: this.toTimeInputValue(response.Out_Time),
          status: response.Status || 'Present',
          remark: (response.Remark || 'working').toLowerCase(),
          isWorking: response.Is_Working,
        };
      },
      () => {
        this.isDailyStatusLoading = false;
        this.isDailyStatusEdit = false;
        this.dailyStatus = this.createDefaultDailyStatus(date);
      },
    );
  }

  onSaveDailyStatus() {
    if (!this.dailyStatus.date || !this.dailyStatus.remark) {
      this.toastService.showError('Validation', 'Please select date and remark.');
      return;
    }

    if (
      this.dailyStatus.isWorking &&
      (!this.dailyStatus.inTime || !this.dailyStatus.outTime)
    ) {
      this.toastService.showError(
        'Validation',
        'Please provide both in time and out time for working day.',
      );
      return;
    }

    // if (
    //   this.dailyStatus.isWorking &&
    //   this.dailyStatus.inTime >= this.dailyStatus.outTime
    // ) {
    //   this.toastService.showError(
    //     'Validation',
    //     'Out time must be greater than in time.',
    //   );
    //   return;
    // }

    const employeeId = parseInt(localStorage.getItem('Employee_ID') || '0');
    const inDateTime = this.dailyStatus.isWorking
      ? this.combineDateAndTime(this.dailyStatus.date, this.dailyStatus.inTime)
      : '';
    const outDateTime = this.dailyStatus.isWorking
      ? this.combineDateAndTime(this.dailyStatus.date, this.dailyStatus.outTime)
      : '';

    const payload: MM_Daily_Status = {
      Status_ID: this.dailyStatus.statusId,
      Employee_ID: employeeId,
      Date: this.dailyStatus.date,
      In_Time: inDateTime,
      Out_Time: outDateTime,
      Status: this.dailyStatus.status,
      Remark: this.dailyStatus.remark,
      Is_Working: this.dailyStatus.isWorking,
      Inserted_Host: localStorage.getItem('Hostname') || '',
      Inserted_User_ID: employeeId,
      Updated_Host: localStorage.getItem('Hostname') || '',
      Updated_User_ID: employeeId,
      Plant_Code: localStorage.getItem('Plant_Code') || '',
    };

    this.isDailyStatusSaving = true;
    this.commonService.saveDailyStatus(payload).subscribe(
      (res) => {
        this.isDailyStatusSaving = false;
        this.toastService.showSuccess(res.messageTitle, res.messageDetail);
        this.loadDailyStatusForDate(this.dailyStatus.date);
      },
      () => {
        this.isDailyStatusSaving = false;
        this.toastService.showError('Error', 'Failed to save daily status.');
      },
    );
  }

  remarkBadgeClass(remark: string): string {
    const map: Record<string, string> = {
      working: 'bg-green-100 text-green-700',
      leave: 'bg-amber-100 text-amber-700',
      holiday: 'bg-indigo-100 text-indigo-700',
      weeklyoff: 'bg-slate-100 text-slate-700',
      'com-off': 'bg-teal-100 text-teal-700',
    };
    return map[remark] ?? 'bg-gray-100 text-gray-600';
  }

  remarkLabel(value: string): string {
    return (
      this.remarkOptions.find((option) => option.value === value)?.label ||
      value
    );
  }

  private combineDateAndTime(date: string, time: string): string {
    if (!date || !time) {
      return '';
    }
    return `${date}T${time}:00`;
  }

  private toTimeInputValue(value: string): string {
    if (!value) {
      return '';
    }

    const normalized = value.trim();
    if (!normalized) {
      return '';
    }

    if (normalized.includes('T')) {
      return normalized.split('T')[1]?.slice(0, 5) || '';
    }

    if (normalized.includes(' ')) {
      return normalized.split(' ')[1]?.slice(0, 5) || '';
    }

    return normalized.slice(0, 5);
  }

  get filteredEntries(): DailyEntry[] {
    return this.entries.filter(
      (e) => e.date >= this.filterFrom && e.date <= this.filterTo,
    );
  }

  removeEntry(entry: DailyEntry) {
    this.commonService.deleteActivity(entry.activityId).subscribe(
      (res) => {
        this.toastService.showSuccess(res.messageTitle, res.messageDetail);
        this.getActivitiesForDate();
      },
      () => {
        this.toastService.showError('Error', 'Failed to delete activity.');
      },
    );
  }

  startEdit(entry: DailyEntry) {
    this.isEdit = true;
    this.newEntry = {
      activityId: entry.activityId,
      activityTypeId: entry.activityTypeId,
      date: entry.date,
      activityType: String(entry.activityTypeId),
      description: entry.description,
      addInMail: entry.addInMail,
    };
  }

  cancelEdit() {
    this.resetForm();
  }

  activityBadgeClass(type: string): string {
    const map: Record<string, string> = {
      'New Development': 'bg-blue-100 text-blue-700',
      Issue: 'bg-red-100 text-red-700',
      Change: 'bg-orange-100 text-orange-700',
      POC: 'bg-purple-100 text-purple-700',
      Meeting: 'bg-yellow-100 text-yellow-700',
      KT: 'bg-teal-100 text-teal-700',
      Support: 'bg-green-100 text-green-700',
      Other: 'bg-gray-100 text-gray-700',
      Leave:'bg-red-100 text-red-700',
      Holiday:'bg-green-100 text-green-700'
    };
    return map[type] ?? 'bg-gray-100 text-gray-600';
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr + 'T00:00:00').toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }
}
