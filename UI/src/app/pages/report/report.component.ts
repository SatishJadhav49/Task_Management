import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonService } from '../common.service';
import { ToastService } from '../../shared/services/toast.service';
import { Activity_Details_List } from '../daily-update/activity.model';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError } from 'rxjs/operators';
import * as XLSX from 'xlsx-js-style';

interface EmployeeOption {
  Employee_ID: number;
  Employee_Name: string;
}

interface DailyUpdateReportRow {
  activityId: number;
  date: string;
  employeeId: number;
  employeeName: string;
  activityType: string;
  description: string;
  addInMail: boolean;
}

interface WeeklyReportRow {
  date: string;
  dateLabel: string;
  inTime: string;
  meeting: string;
  newRequirement: string;
  issues: string;
  changeRequirement: string;
  other: string;
  outTime: string;
}

interface MonthlyReportRow {
  date: string;
  dateLabel: string;
  isFirstOfDate: boolean;
  rowSpan: number;
  activityType: string;
  description: string;
}

type ViewMode = 'daily' | 'weekly' | 'monthly';

@Component({
  selector: 'app-report',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './report.component.html',
  styleUrl: './report.component.css',
})
export class ReportComponent {
  readonly commonService = inject(CommonService);
  readonly toastService = inject(ToastService);

  today = new Date().toISOString().split('T')[0];
  fromDate = this.today;
  toDate = this.today;
  currentUserId = Number(localStorage.getItem('Employee_ID') || '0');

  employees: EmployeeOption[] = [];
  selectedEmployeeId = this.currentUserId;
  rows: DailyUpdateReportRow[] = [];
  isLoading = false;

  viewMode: ViewMode = 'daily';
  weekStartDate = this.getMondayOf(this.today);
  weeklyRows: WeeklyReportRow[] = [];
  isWeeklyLoading = false;

  selectedMonth = this.today.substring(0, 7);
  monthlyRows: MonthlyReportRow[] = [];
  isMonthlyLoading = false;

  currentTaskMode = 'developer';
  private modeSubscription?: Subscription;

  ngOnInit() {
    this.modeSubscription = this.commonService.taskViewContext$.subscribe(({ mode }) => {
      this.currentTaskMode = mode;
      this.selectedEmployeeId = this.currentUserId;
      this.loadEmployees(mode);
    });
  }

  ngOnDestroy() {
    this.modeSubscription?.unsubscribe();
  }

  loadEmployees(mode: string = this.currentTaskMode) {
    this.commonService.getEmployeesByMode(this.currentUserId, mode).subscribe(
      (res: any[]) => {
        const list = (res ?? [])
          .map((item: any) => {
            const employeeId = Number(
              item.Employee_ID ?? item.employeeId ?? item.Emp_ID ?? 0,
            );
            const employeeName = String(
              item.Employee_Name ??
                item.EmployeeName ??
                item.Name ??
                item.Employee_No ??
                `Employee ${employeeId}`,
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

        this.employees = list;
        this.ensureDefaultEmployee();
        this.loadCurrentView();
      },
      () => {
        this.employees = [];
        this.ensureDefaultEmployee();
        this.loadCurrentView();
      },
    );
  }

  ensureDefaultEmployee() {
    if (!this.selectedEmployeeId) {
      this.selectedEmployeeId = this.currentUserId;
    }

    const exists = this.employees.some(
      (employee) => employee.Employee_ID === this.selectedEmployeeId,
    );

    if (!exists && this.currentUserId) {
      this.employees.unshift({
        Employee_ID: this.currentUserId,
        Employee_Name: localStorage.getItem('Name') || 'Current User',
      });
    }
  }

  setViewMode(mode: ViewMode) {
    if (this.viewMode === mode) {
      return;
    }
    this.viewMode = mode;
    this.loadCurrentView();
  }

  loadCurrentView() {
    if (this.viewMode === 'weekly') {
      this.loadWeeklyReport();
    } else if (this.viewMode === 'monthly') {
      this.loadMonthlyReport();
    } else {
      this.loadReport();
    }
  }

  loadReport() {
    if (!this.selectedEmployeeId || !this.fromDate || !this.toDate) {
      this.rows = [];
      return;
    }

    if (this.fromDate > this.toDate) {
      this.toastService.showError(
        'Validation',
        'From date cannot be after To date.',
      );
      return;
    }

    this.isLoading = true;
    this.commonService
      .getActivitiesByDate(this.fromDate, this.toDate, this.selectedEmployeeId)
      .subscribe(
        (activities: Activity_Details_List[]) => {
          const selectedName = this.getEmployeeName(this.selectedEmployeeId);
          this.rows = (activities ?? []).map((activity: any) => ({
            activityId: Number(activity.Activity_ID || 0),
            date: activity.Activity_Date,
            employeeId: Number(activity.Employee_ID || this.selectedEmployeeId),
            employeeName: String(
              activity.Employee_Name ?? activity.EmployeeName ?? selectedName,
            ),
            activityType: String(activity.Activity_Name || ''),
            description: String(activity.Description || ''),
            addInMail: Boolean(activity.Add_In_Mail),
          }));
          this.isLoading = false;
        },
        () => {
          this.rows = [];
          this.isLoading = false;
          this.toastService.showError(
            'Error',
            'Failed to fetch daily update report.',
          );
        },
      );
  }

  getEmployeeName(employeeId: number): string {
    return (
      this.employees.find((employee) => employee.Employee_ID === employeeId)
        ?.Employee_Name || 'Unknown'
    );
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
      Leave: 'bg-red-100 text-red-700',
      Holiday: 'bg-green-100 text-green-700',
    };
    return map[type] ?? 'bg-gray-100 text-gray-600';
  }

  formatDate(dateStr: string): string {
    if (!dateStr) {
      return '';
    }

    const value = dateStr.includes('T') ? dateStr : `${dateStr}T00:00:00`;
    return new Date(value).toLocaleDateString('en-US', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }

  // ── Weekly view ───────────────────────────────────────────────

  get weekEndDate(): string {
    return this.addDays(this.weekStartDate, 7);
  }

  onWeekStartChange() {
    this.weekStartDate = this.getMondayOf(this.weekStartDate);
    this.loadWeeklyReport();
  }

  loadWeeklyReport() {
    if (!this.selectedEmployeeId || !this.weekStartDate) {
      this.weeklyRows = [];
      return;
    }

    const start = this.weekStartDate;
    const end = this.weekEndDate;
    const dates = this.getDateRange(start, end);

    this.isWeeklyLoading = true;

    const activities$ = this.commonService
      .getActivitiesByDate(start, end, this.selectedEmployeeId)
      .pipe(catchError(() => of([] as Activity_Details_List[])));

    forkJoin({
      activities: activities$,
      // statuses: status$.length ? forkJoin(status$) : of([] as (MM_Daily_Status | null)[]),
    }).subscribe({
      next: ({ activities }) => {
        this.weeklyRows = this.buildWeeklyRows(dates, activities ?? []);
        this.isWeeklyLoading = false;
      },
      error: () => {
        this.weeklyRows = this.buildWeeklyRows(dates, []);
        this.isWeeklyLoading = false;
        this.toastService.showError('Error', 'Failed to fetch weekly report.');
      },
    });
  }

  private buildWeeklyRows(
    dates: string[],
    activities: any[],
  ): WeeklyReportRow[] {
    const grouped = new Map<string, any[]>();

    for (const a of activities) {
      const key = (a.Activity_Date || '').split('T')[0];
      if (!key) continue;

      if (!grouped.has(key)) grouped.set(key, []);
      grouped.get(key)!.push(a);
    }

    return dates.map((date) => {
      const dayActivities = grouped.get(date) ?? [];

      const bucket = {
        meeting: [] as string[],
        newRequirement: [] as string[],
        issues: [] as string[],
        changeRequirement: [] as string[],
        other: [] as string[],
      };

      for (const a of dayActivities) {
        const type = String(a.Activity_Name || '').trim();
        const desc = String(a.Description || '').trim();
        if (!desc) continue;

        const target = this.mapActivityToColumn(type);
        bucket[target].push(desc);
      }

      // ✅ Get times from first record (same for that day)
      const firstRecord = dayActivities[0];

      return {
        date,
        dateLabel: this.formatDate(date),

        inTime: this.formatOnlyTime(firstRecord?.In_Time),

        meeting: bucket.meeting.join('\n'),
        newRequirement: bucket.newRequirement.join('\n'),
        issues: bucket.issues.join('\n'),
        changeRequirement: bucket.changeRequirement.join('\n'),
        other: bucket.other.join('\n'),

        outTime: this.formatOnlyTime(firstRecord?.Out_Time),
      };
    });
  }

  private mapActivityToColumn(
    type: string,
  ): 'meeting' | 'newRequirement' | 'issues' | 'changeRequirement' | 'other' {
    const t = type.toLowerCase();
    if (t.includes('meeting')) return 'meeting';
    if (t.includes('new')) return 'newRequirement';
    if (t.includes('issue')) return 'issues';
    if (t.includes('change')) return 'changeRequirement';
    return 'other';
  }

  private formatOnlyTime(dateTime: string | null | undefined): string {
    if (!dateTime) return '';

    // Example input: "2026-03-16 09:00:00.000"
    const timePart = dateTime.split(' ')[1]; // "09:00:00.000"
    if (!timePart) return '';

    const [hourStr, minuteStr] = timePart.split(':');
    let hour = parseInt(hourStr, 10);
    const minute = minuteStr;

    const ampm = hour >= 12 ? 'PM' : 'AM';

    hour = hour % 12;
    hour = hour === 0 ? 12 : hour;

    return `${hour}:${minute} ${ampm}`;
  }

  private getMondayOf(dateStr: string): string {
    const d = new Date(`${dateStr}T00:00:00`);
    if (Number.isNaN(d.getTime())) return dateStr;
    const day = d.getDay();
    const diff = day === 0 ? -6 : 1 - day;
    d.setDate(d.getDate() + diff);
    return d.toISOString().split('T')[0];
  }

  private addDays(dateStr: string, days: number): string {
    const d = new Date(`${dateStr}T00:00:00`);
    d.setDate(d.getDate() + days);
    return d.toISOString().split('T')[0];
  }

  private getDateRange(start: string, end: string): string[] {
    const startDate = new Date(start);
    const endDate = new Date(end);
    const days = Math.ceil(
      (endDate.getTime() - startDate.getTime()) / 86400000,
    );

    return Array.from({ length: days + 1 }, (_, i) => {
      const d = new Date(startDate);
      d.setDate(d.getDate() + i);
      return d.toISOString().split('T')[0];
    });
  }

  // ── Monthly view ──────────────────────────────────────────────

  get monthStartDate(): string {
    return `${this.selectedMonth}-01`;
  }

  get monthEndDate(): string {
    const [y, m] = this.selectedMonth.split('-').map(Number);
    const last = new Date(y, m, 0).getDate();
    return `${this.selectedMonth}-${String(last).padStart(2, '0')}`;
  }

  loadMonthlyReport() {
    if (!this.selectedEmployeeId || !this.selectedMonth) {
      this.monthlyRows = [];
      return;
    }

    this.isMonthlyLoading = true;
    this.commonService
      .getActivitiesByDate(
        this.monthStartDate,
        this.monthEndDate,
        this.selectedEmployeeId,
      )
      .subscribe(
        (activities: Activity_Details_List[]) => {
          this.monthlyRows = this.buildMonthlyRows(activities ?? []);
          this.isMonthlyLoading = false;
        },
        () => {
          this.monthlyRows = [];
          this.isMonthlyLoading = false;
          this.toastService.showError(
            'Error',
            'Failed to fetch monthly report.',
          );
        },
      );
  }

  private buildMonthlyRows(activities: any[]): MonthlyReportRow[] {
    const sorted = [...activities].sort((a, b) => {
      const da = (a.Activity_Date || '').split('T')[0];
      const db = (b.Activity_Date || '').split('T')[0];
      if (da === db) {
        return String(a.Activity_Name || '').localeCompare(
          String(b.Activity_Name || ''),
        );
      }
      return da.localeCompare(db);
    });

    const result: MonthlyReportRow[] = [];
    let prevDate = '';
    const dateCounts = new Map<string, number>();

    for (const a of sorted) {
      const date = (a.Activity_Date || '').split('T')[0];
      if (!date) continue;
      dateCounts.set(date, (dateCounts.get(date) ?? 0) + 1);
    }

    for (const a of sorted) {
      const date = (a.Activity_Date || '').split('T')[0];
      if (!date) continue;
      const isFirst = date !== prevDate;
      result.push({
        date,
        dateLabel: this.formatDate(date),
        isFirstOfDate: isFirst,
        rowSpan: isFirst ? (dateCounts.get(date) ?? 1) : 0,
        activityType: String(a.Activity_Name || ''),
        description: String(a.Description || ''),
      });
      prevDate = date;
    }

    return result;
  }

  downloadExcel(): void {
    if (!this.fromDate || !this.toDate) {
      this.toastService.showError(
        'Validation',
        'Please select date range first.',
      );
      return;
    }

    if (this.fromDate > this.toDate) {
      this.toastService.showError(
        'Validation',
        'From date cannot be after To date.',
      );
      return;
    }

    const exportRows = this.rows
      .filter((row) => {
        const dateOnly = row.date?.split('T')[0] || '';
        return (
          row.employeeId === this.selectedEmployeeId &&
          dateOnly >= this.fromDate &&
          dateOnly <= this.toDate
        );
      })
      .sort((left, right) => {
        const leftDate = left.date?.split('T')[0] || '';
        const rightDate = right.date?.split('T')[0] || '';
        if (leftDate === rightDate) {
          return left.activityType.localeCompare(right.activityType);
        }
        return leftDate.localeCompare(rightDate);
      });

    if (exportRows.length === 0) {
      this.toastService.showError(
        'No Data',
        'No daily update entries available for selected filters.',
      );
      return;
    }

    const header = ['Employee Name', 'Date', 'Activity Type', 'Description'];
    const worksheetData: (string | number)[][] = [header];
    const merges: XLSX.Range[] = [];

    let groupStartRow = 1;
    let currentDate = exportRows[0].date?.split('T')[0] || '';

    exportRows.forEach((row, index) => {
      const dateOnly = row.date?.split('T')[0] || '';
      const worksheetRowIndex = index + 1;

      worksheetData.push([
        row.employeeName,
        dateOnly,
        row.activityType,
        row.description,
      ]);

      if (dateOnly !== currentDate) {
        const prevGroupEndRow = worksheetRowIndex - 1;
        if (prevGroupEndRow > groupStartRow) {
          merges.push({
            s: { r: groupStartRow, c: 0 },
            e: { r: prevGroupEndRow, c: 0 },
          });
          merges.push({
            s: { r: groupStartRow, c: 1 },
            e: { r: prevGroupEndRow, c: 1 },
          });
        }

        groupStartRow = worksheetRowIndex;
        currentDate = dateOnly;
      }
    });

    const lastRowIndex = worksheetData.length - 1;
    if (lastRowIndex > groupStartRow) {
      merges.push({
        s: { r: groupStartRow, c: 0 },
        e: { r: lastRowIndex, c: 0 },
      });
      merges.push({
        s: { r: groupStartRow, c: 1 },
        e: { r: lastRowIndex, c: 1 },
      });
    }

    const worksheet = XLSX.utils.aoa_to_sheet(worksheetData);
    if (merges.length > 0) {
      worksheet['!merges'] = merges;
    }

    worksheet['!cols'] = [{ wch: 24 }, { wch: 14 }, { wch: 22 }, { wch: 70 }];

    const workbook = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Daily Update');

    const employeeName = this.getEmployeeName(this.selectedEmployeeId)
      .replace(/\s+/g, '_')
      .replace(/[^a-zA-Z0-9_]/g, '');
    const fileName = `Daily_Update_${employeeName}_${this.fromDate}_to_${this.toDate}.xlsx`;

    XLSX.writeFile(workbook, fileName);
  }

  monthLabel(monthStr: string): string {
    if (!monthStr) return '';
    const d = new Date(`${monthStr}-01T00:00:00`);
    return d.toLocaleDateString('en-US', { month: 'long', year: 'numeric' });
  }

  downloadWeeklyExcel(): void {
    if (!this.weeklyRows || this.weeklyRows.length === 0) {
      this.toastService.showError('No Data', 'No weekly data available.');
      return;
    }

    const employeeNameRaw = this.getEmployeeName(this.selectedEmployeeId);
    const sheetName = employeeNameRaw.substring(0, 31);

    const formatHeaderDate = (dateStr: string) => {
      const d = new Date(`${dateStr}T00:00:00`);
      return d.toLocaleDateString('en-GB', {
        day: '2-digit',
        month: 'long',
        year: 'numeric',
      });
    };

    const weekStart = formatHeaderDate(this.weekStartDate);
    const weekEnd = formatHeaderDate(this.weekEndDate);

    const getWeekOfMonth = (dateStr: string) => {
      const date = new Date(dateStr);

      const firstDayOfMonth = new Date(date.getFullYear(), date.getMonth(), 1);

      const dayOfMonth = date.getDate();
      const firstDayWeekDay = firstDayOfMonth.getDay(); // 0 (Sun) to 6 (Sat)

      return Math.ceil((dayOfMonth + firstDayWeekDay) / 7);
    };

    // Usage
    const weekNo = getWeekOfMonth(this.weekStartDate);

    // ✅ IMPORTANT: leave first column blank → so header aligns like screenshot
    const data: any[][] = [
      ['', `Week Start Date - ${weekStart}`, '', `Week End Date - ${weekEnd}`],
      ['', 'Week', '', weekNo],
      ['', 'Employee Name', '', employeeNameRaw],
      [],
      [
        'Dates',
        'In Time',
        'Meeting',
        'New Requirement',
        'Issues',
        'Change Requirement',
        'Other',
        'Out Time',
      ],
    ];

    // ✅ Add data rows
    this.weeklyRows.forEach((row) => {
      data.push([
        row.dateLabel,
        row.inTime,
        row.meeting,
        row.newRequirement,
        row.issues,
        row.changeRequirement,
        row.other,
        row.outTime,
      ]);
    });

    const ws: XLSX.WorkSheet = XLSX.utils.aoa_to_sheet(data);

    // ✅ MERGES (ONLY PARTIAL — NOT FULL WIDTH)
    ws['!merges'] = [
      // Row 0
      { s: { r: 0, c: 1 }, e: { r: 0, c: 2 } }, // Start Date

      // Row 1
      { s: { r: 1, c: 1 }, e: { r: 1, c: 2 } }, // Week label

      // Row 2
      { s: { r: 2, c: 1 }, e: { r: 2, c: 2 } }, // Employee label
    ];

    // ✅ Column widths (same as UI)
    ws['!cols'] = [
      { wch: 15 },
      { wch: 10 },
      { wch: 40 },
      { wch: 40 },
      { wch: 25 },
      { wch: 30 },
      { wch: 40 },
      { wch: 12 },
    ];

    // ✅ Styling
    const range = XLSX.utils.decode_range(ws['!ref']!);

    for (let r = 0; r <= range.e.r; r++) {
      for (let c = 0; c <= range.e.c; c++) {
        const ref = XLSX.utils.encode_cell({ r, c });
        const cell = ws[ref];
        if (!cell) continue;

        cell.s = {
          alignment: {
            vertical: 'top',
            horizontal:
              r <= 2
                ? 'center'
                : c === 0 || c === 1 || c === 7
                  ? 'center'
                  : 'left',
            wrapText: true,
          },
          border: {
            top: { style: 'thin' },
            bottom: { style: 'thin' },
            left: { style: 'thin' },
            right: { style: 'thin' },
          },
        };

        // ✅ GREEN HEADER (only used cells)
        if (r === 0) {
          cell.s = {
            ...cell.s,
            font: { bold: true },
            fill: { fgColor: { rgb: 'C6D9B4' } },
          };
        }

        if (r === 1 || r === 2) {
          cell.s = {
            ...cell.s,
            font: { bold: true },
            fill: { fgColor: { rgb: 'E7E6E6' } },
          };
        }

        // ✅ Table header
        if (r === 4) {
          cell.s = {
            ...cell.s,
            font: { bold: true },
            fill: { fgColor: { rgb: 'B8C6D6' } },
          };
        }
      }
    }

    const wb = XLSX.utils.book_new();
    XLSX.utils.book_append_sheet(wb, ws, sheetName);

    const fileName = `Weekly_Report_${employeeNameRaw}_${this.weekStartDate}_to_${this.weekEndDate}.xlsx`;

    XLSX.writeFile(wb, fileName);
  }

  downloadTimesheetExcel(): void {
    this.commonService
      .getActivitiesByDate(
        this.monthStartDate,
        this.monthEndDate,
        this.selectedEmployeeId,
      )
      .subscribe((activities: any[]) => {
        if (!activities || activities.length === 0) {
          this.toastService.showError('No Data', 'No data found');
          return;
        }

        const employeeName = this.getEmployeeName(this.selectedEmployeeId);

        const formatDate = (dateStr: string) => {
          const d = new Date(dateStr);
          return d.toLocaleDateString('en-US', {
            weekday: 'long',
            day: 'numeric',
            month: 'long',
            year: 'numeric',
          });
        };

        const formatTime = (dt: string) => {
          if (!dt) return '';
          const time = dt.split(' ')[1];
          if (!time) return '';

          let [h, m] = time.split(':');
          let hour = parseInt(h, 10);
          const ampm = hour >= 12 ? 'PM' : 'AM';
          hour = hour % 12 || 12;

          return `${hour}:${m} ${ampm}`;
        };

        const calculateHours = (inTime: string, outTime: string) => {
          if (!inTime || !outTime) return '';

          try {
            // ✅ Extract time part
            const inT = inTime.split(' ')[1]; // "08:58:00.000"
            const outT = outTime.split(' ')[1];

            if (!inT || !outT) return '';

            const [inH, inM] = inT.split(':');
            const [outH, outM] = outT.split(':');

            const inMinutes = parseInt(inH) * 60 + parseInt(inM);
            const outMinutes = parseInt(outH) * 60 + parseInt(outM);

            const diff = outMinutes - inMinutes;

            if (diff <= 0) return '';

            const hrs = Math.floor(diff / 60);
            const mins = diff % 60;

            return `${String(hrs).padStart(2, '0')}:${String(mins).padStart(2, '0')}`;
          } catch {
            return '';
          }
        };

        // ✅ GROUP BY DATE
        const grouped = new Map<string, any[]>();

        activities.forEach((a) => {
          const date = (a.Activity_Date || '').split('T')[0];
          if (!grouped.has(date)) grouped.set(date, []);
          grouped.get(date)!.push(a);
        });

        const data: any[][] = [
          [
            'Name',
            'Date',
            'Site',
            'Customer',
            'Work Location',
            'Hours',
            'Start Time',
            'End Time',
            'Type of Activity',
            'Description',
          ],
        ];

        const merges: XLSX.Range[] = [];
        let rowIndex = 1;

        grouped.forEach((items, date) => {
          const first = items[0]; // ✅ common for date
          const startTime = formatTime(first.In_Time);
          const endTime = formatTime(first.Out_Time);
          const hours = calculateHours(first.In_Time, first.Out_Time);

          const startRow = rowIndex;

          items.forEach((a, idx) => {
            data.push([
              idx === 0 ? employeeName : '',
              idx === 0 ? formatDate(date) : '',
              idx === 0 ? 'Nashik' : '',
              idx === 0 ? 'M & M' : '',
              idx === 0 ? 'Mahindra Office' : '',
              idx === 0 ? hours : '',
              idx === 0 ? startTime : '',
              idx === 0 ? endTime : '',
              a.Activity_Name || '',
              a.Description || '',
            ]);

            rowIndex++;
          });

          const endRow = rowIndex - 1;

          // ✅ MERGE A → H (0 to 7)
          if (endRow > startRow) {
            for (let col = 0; col <= 7; col++) {
              merges.push({
                s: { r: startRow, c: col },
                e: { r: endRow, c: col },
              });
            }
          }
        });

        const ws = XLSX.utils.aoa_to_sheet(data);
        ws['!merges'] = merges;

        // ✅ Column widths
        ws['!cols'] = [
          { wch: 20 },
          { wch: 28 },
          { wch: 12 },
          { wch: 12 },
          { wch: 22 },
          { wch: 10 },
          { wch: 12 },
          { wch: 12 },
          { wch: 20 },
          { wch: 70 },
        ];

        // ✅ Styling
        const range = XLSX.utils.decode_range(ws['!ref']!);

        for (let r = 0; r <= range.e.r; r++) {
          for (let c = 0; c <= range.e.c; c++) {
            const ref = XLSX.utils.encode_cell({ r, c });
            const cell = ws[ref];
            if (!cell) continue;

            // ✅ Get activity type from column index 8 (Type of Activity)
            const activityCellRef = XLSX.utils.encode_cell({ r, c: 8 });
            const activityValue =
              ws[activityCellRef]?.v?.toString().toLowerCase() || '';

            let fillColor = null;

            if (activityValue.includes('week off')) {
              fillColor = 'C6E0B4'; // ✅ green (like your screenshot)
            } else if (activityValue.includes('leave')) {
              fillColor = 'FFC7CE'; // ✅ light red
            }

            cell.s = {
              alignment: {
                wrapText: true,
                vertical: c <= 8 ? 'center' : 'top',
                horizontal: c <= 8 ? 'center' : 'left',
              },
              border: {
                top: { style: 'thin' },
                bottom: { style: 'thin' },
                left: { style: 'thin' },
                right: { style: 'thin' },
              },
              ...(fillColor && {
                fill: {
                  fgColor: { rgb: fillColor },
                },
              }),
            };

            // ✅ Header styling
            if (r === 0) {
              cell.s = {
                ...cell.s,
                font: { bold: true },
                fill: { fgColor: { rgb: '8EA9DB' } },
                alignment: {
                  vertical: 'center',
                  horizontal: 'center',
                },
              };
            }
          }
        }
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, employeeName.substring(0, 31));

        XLSX.writeFile(
          wb,
          `Timesheet_${employeeName}_${this.selectedMonth}.xlsx`,
        );
      });
  }
}
