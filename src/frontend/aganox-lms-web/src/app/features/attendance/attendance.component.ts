import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { forkJoin } from 'rxjs';
import { AttendanceDay, AttendanceService, AttendanceSummary } from '../../core/attendance.service';
import { AuthService } from '../../core/auth.service';
import { LeaveBalance, LeaveConfigService, LeaveType } from '../../core/leave-config.service';
import { LeaveRequestService } from '../../core/leave-request.service';
import { Department, EmployeeSummary, OrganizationService } from '../../core/organization.service';

interface CalendarCell {
  date: string;
  day: number;
  inMonth: boolean;
  status?: string;
  source?: string;
  remarks?: string | null;
  leaveTypeCode?: string | null;
}

@Component({
  selector: 'app-attendance',
  standalone: true,
  imports: [FormsModule, TranslatePipe, DecimalPipe],
  template: `
    <div class="d-flex align-items-center mb-3 gap-2 flex-wrap">
      <h1 class="h4 mb-0">{{ 'attendance.title' | translate }}</h1>
      <div class="ms-auto d-flex gap-2 flex-wrap align-items-center">
        @if (canManage()) {
          <button class="btn btn-sm btn-outline-secondary" type="button" (click)="syncLeave()" [disabled]="busy()">
            {{ 'attendance.syncLeave' | translate }}
          </button>
        }
      </div>
    </div>

    @if (message()) { <div class="alert alert-success py-2">{{ message() }}</div> }
    @if (error()) { <div class="alert alert-danger py-2">{{ error() }}</div> }

    <div class="row g-3">
      <div class="col-lg-8">
        <div class="gov-section">
          <div class="gov-section-head d-flex flex-wrap align-items-center gap-2">
            <span><i class="bi bi-calendar3"></i> {{ 'attendance.calendar' | translate }}</span>
            <div class="ms-auto d-flex gap-2 align-items-center flex-wrap">
              <button type="button" class="btn btn-sm btn-outline-secondary" (click)="shiftMonth(-1)">&lsaquo;</button>
              <strong>{{ monthLabel() }}</strong>
              <button type="button" class="btn btn-sm btn-outline-secondary" (click)="shiftMonth(1)">&rsaquo;</button>
            </div>
          </div>
          <div class="gov-section-body">
            <div class="row g-2 mb-3">
              <div class="col-md-4">
                <label class="form-label small mb-0">{{ 'employees.department' | translate }}</label>
                <select class="form-select form-select-sm" [(ngModel)]="departmentId" (change)="onDepartmentChange()">
                  <option value="">{{ 'common.all' | translate }}</option>
                  @for (d of departments(); track d.id) {
                    <option [value]="d.id">{{ d.name }}</option>
                  }
                </select>
              </div>
              <div class="col-md-5">
                <label class="form-label small mb-0">{{ 'attendance.selectEmployee' | translate }}</label>
                <select class="form-select form-select-sm" [(ngModel)]="selectedEmployeeId" (change)="onEmployeeChange()">
                  @for (e of employees(); track e.id) {
                    <option [value]="e.id">{{ e.fullName }} ({{ e.employeeCode }})</option>
                  }
                </select>
              </div>
              @if (selectedSummary()) {
                <div class="col-md-3 small text-muted d-flex align-items-end pb-1">
                  {{ 'attendance.paidDays' | translate }}: <strong class="ms-1">{{ selectedSummary()!.paidDays }}</strong>
                </div>
              }
            </div>

            <div class="att-calendar-legend mb-2">
              @for (s of statusLegend; track s) {
                <span class="att-cal-chip" [class]="statusClass(s)">{{ ('attendance.status' + s) | translate }}</span>
              }
            </div>

            <div class="att-calendar">
              <div class="att-calendar-weekdays">
                @for (wd of weekDays; track wd) {
                  <div class="att-calendar-weekday">{{ ('weekdays.' + wd) | translate }}</div>
                }
              </div>
              @for (week of calendarWeeks(); track $index) {
                <div class="att-calendar-week">
                  @for (cell of week; track cell.date) {
                    <button
                      type="button"
                      class="att-calendar-day"
                      [class.att-cal-outside]="!cell.inMonth"
                      [class.att-cal-selected]="cell.date === selectedDate()"
                      [class]="cell.inMonth && cell.status ? statusClass(cell.status) : ''"
                      [disabled]="!cell.inMonth || !selectedEmployeeId"
                      (click)="selectDay(cell)"
                    >
                      <span class="att-calendar-day-num">{{ cell.day }}</span>
                      @if (cell.inMonth && cell.status) {
                        <span class="att-calendar-day-status">{{ statusShort(cell.status, cell.leaveTypeCode) }}</span>
                      }
                    </button>
                  }
                </div>
              }
            </div>
          </div>
        </div>

        @if (canManage() && selectedDate()) {
          <div class="gov-section mt-3">
            <div class="gov-section-head"><i class="bi bi-pencil-square"></i> {{ 'attendance.markDay' | translate:{ date: selectedDate() } }}</div>
            <div class="gov-section-body row g-2 align-items-end">
              <div class="col-md-3">
                <label class="form-label">{{ 'common.status' | translate }}</label>
                <select class="form-select form-select-sm" [(ngModel)]="editStatus">
                  @for (s of editableStatuses; track s) {
                    <option [value]="s">{{ ('attendance.status' + s) | translate }}</option>
                  }
                </select>
              </div>
              <div class="col-md-5">
                <label class="form-label">{{ 'attendance.remarks' | translate }}</label>
                <input class="form-control form-control-sm" [(ngModel)]="editRemarks" />
              </div>
              <div class="col-md-2">
                <button class="btn btn-sm btn-aganox w-100" type="button" (click)="saveDay()" [disabled]="busy()">
                  {{ 'attendance.saveDay' | translate }}
                </button>
              </div>
            </div>
          </div>
        }
      </div>

      <div class="col-lg-4">
        <div class="gov-section mb-3">
          <div class="gov-section-head"><i class="bi bi-wallet2"></i> {{ 'attendance.leaveBalances' | translate }}</div>
          <div class="gov-section-body">
            @if (!selectedEmployeeId) {
              <p class="text-muted small mb-0">{{ 'attendance.selectEmployeeHint' | translate }}</p>
            } @else if (balances().length === 0) {
              <p class="text-muted small mb-0">{{ 'common.noData' | translate }}</p>
            } @else {
              <table class="table table-sm mb-0">
                <thead>
                  <tr>
                    <th>{{ 'leave.leaveType' | translate }}</th>
                    <th class="text-end">{{ 'attendance.remaining' | translate }}</th>
                  </tr>
                </thead>
                <tbody>
                  @for (b of balances(); track b.leaveTypeId) {
                    <tr>
                      <td>{{ b.leaveTypeName }}</td>
                      <td class="text-end fw-semibold">{{ b.remaining | number:'1.0-1' }}</td>
                    </tr>
                  }
                </tbody>
              </table>
            }
          </div>
        </div>

        @if (canApplyLeave() && selectedEmployeeId) {
          <div class="gov-section">
            <div class="gov-section-head"><i class="bi bi-calendar-plus"></i> {{ 'attendance.applyLeave' | translate }}</div>
            <div class="gov-section-body row g-2">
              <div class="col-12">
                <label class="form-label">{{ 'leave.leaveType' | translate }}</label>
                <select class="form-select form-select-sm" [(ngModel)]="leaveForm.leaveTypeId">
                  @for (t of leaveTypes(); track t.id) {
                    <option [value]="t.id">{{ t.name }}</option>
                  }
                </select>
              </div>
              <div class="col-6">
                <label class="form-label">{{ 'common.from' | translate }}</label>
                <input type="date" class="form-control form-control-sm" [(ngModel)]="leaveForm.fromDate" (change)="previewLeave()" />
              </div>
              <div class="col-6">
                <label class="form-label">{{ 'common.to' | translate }}</label>
                <input type="date" class="form-control form-control-sm" [(ngModel)]="leaveForm.toDate" (change)="previewLeave()" />
              </div>
              <div class="col-12">
                <div class="form-check">
                  <input type="checkbox" class="form-check-input" id="attHalfDay" [(ngModel)]="leaveForm.isHalfDay" (change)="previewLeave()" />
                  <label class="form-check-label" for="attHalfDay">{{ 'leave.halfDayLabel' | translate }}</label>
                </div>
              </div>
              <div class="col-12">
                <label class="form-label">{{ 'leave.remarks' | translate }}</label>
                <textarea class="form-control form-control-sm" rows="2" [(ngModel)]="leaveForm.remarks"></textarea>
              </div>
              @if (leavePreviewDays() !== null) {
                <div class="col-12 small text-muted">
                  {{ 'leave.calculatedDuration' | translate }}
                  <strong>{{ 'leave.daysCount' | translate:{ count: leavePreviewDays() } }}</strong>
                </div>
              }
              <div class="col-12 d-flex gap-2">
                <button class="btn btn-sm btn-outline-secondary" type="button" (click)="applyLeave(false)" [disabled]="busy()">
                  {{ 'leave.saveDraft' | translate }}
                </button>
                <button class="btn btn-sm btn-aganox" type="button" (click)="applyLeave(true)" [disabled]="busy()">
                  {{ 'leave.submitForApproval' | translate }}
                </button>
              </div>
            </div>
          </div>
        }
      </div>
    </div>
  `
})
export class AttendanceComponent implements OnInit {
  private readonly attendance = inject(AttendanceService);
  private readonly org = inject(OrganizationService);
  private readonly leaveConfig = inject(LeaveConfigService);
  private readonly leaveRequests = inject(LeaveRequestService);
  private readonly translate = inject(TranslateService);
  private readonly auth = inject(AuthService);

  readonly departments = signal<Department[]>([]);
  readonly employees = signal<EmployeeSummary[]>([]);
  readonly summaries = signal<AttendanceSummary[]>([]);
  readonly monthDays = signal<AttendanceDay[]>([]);
  readonly balances = signal<LeaveBalance[]>([]);
  readonly leaveTypes = signal<LeaveType[]>([]);
  readonly busy = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly selectedDate = signal('');
  readonly leavePreviewDays = signal<number | null>(null);

  readonly weekDays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
  readonly statusLegend = ['Present', 'Absent', 'HalfDay', 'OnLeave', 'Holiday', 'WeekOff'];
  readonly editableStatuses = ['Present', 'Absent', 'HalfDay', 'OnLeave'];

  year = new Date().getFullYear();
  month = new Date().getMonth() + 1;
  departmentId = '';
  selectedEmployeeId = '';
  editStatus = 'Present';
  editRemarks = '';

  leaveForm = {
    leaveTypeId: '',
    fromDate: '',
    toDate: '',
    isHalfDay: false,
    remarks: ''
  };

  readonly dayMap = computed(() => {
    const map = new Map<string, AttendanceDay>();
    for (const d of this.monthDays()) {
      map.set(d.date, d);
    }
    return map;
  });

  readonly selectedSummary = computed(() =>
    this.summaries().find((s) => s.employeeId === this.selectedEmployeeId) ?? null);

  readonly calendarWeeks = computed(() => this.buildCalendarWeeks(this.year, this.month, this.dayMap()));

  ngOnInit(): void {
    this.leaveConfig.getLeaveTypes(true).subscribe((types) => {
      this.leaveTypes.set(types);
      if (types.length) this.leaveForm.leaveTypeId = types[0].id;
    });
    this.org.getDepartments(true).subscribe((items) => this.departments.set(items));
    this.loadEmployees();
  }

  monthLabel(): string {
    const d = new Date(this.year, this.month - 1, 1);
    return d.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
  }

  canManage(): boolean {
    return this.auth.hasPermission('Attendance.Manage');
  }

  canApplyLeave(): boolean {
    return this.auth.hasPermission('Leave.Apply') && this.auth.hasPermission('Leave.ViewAll');
  }

  canViewBalances(): boolean {
    return this.auth.hasPermission('Balances.View');
  }

  onDepartmentChange(): void {
    this.loadEmployees();
  }

  onEmployeeChange(): void {
    this.loadEmployeeData();
  }

  shiftMonth(delta: number): void {
    let m = this.month + delta;
    let y = this.year;
    if (m < 1) { m = 12; y--; }
    if (m > 12) { m = 1; y++; }
    this.month = m;
    this.year = y;
    this.loadSummaries();
    if (this.selectedEmployeeId) this.loadEmployeeData();
  }

  loadEmployees(): void {
    this.org.getEmployees({
      departmentId: this.departmentId || undefined,
      status: 'Active',
      pageSize: 500,
      sortBy: 'fullName'
    }).subscribe({
      next: (result) => {
        this.employees.set(result.items);
        if (result.items.length && !result.items.some((e) => e.id === this.selectedEmployeeId)) {
          this.selectedEmployeeId = result.items[0].id;
        }
        this.loadSummaries();
        if (this.selectedEmployeeId) this.loadEmployeeData();
      },
      error: () => this.error.set(this.translate.instant('attendance.loadFailed'))
    });
  }

  loadSummaries(): void {
    this.attendance.getSummaries(this.year, this.month, this.departmentId || undefined).subscribe({
      next: (items) => this.summaries.set(items),
      error: () => this.error.set(this.translate.instant('attendance.loadFailed'))
    });
  }

  loadEmployeeData(): void {
    if (!this.selectedEmployeeId) return;
    this.error.set('');
    const requests: { days: ReturnType<AttendanceService['getMonth']> } = {
      days: this.attendance.getMonth(this.year, this.month, this.departmentId || undefined, this.selectedEmployeeId)
    };
    if (this.canViewBalances()) {
      forkJoin({
        days: requests.days,
        balances: this.leaveConfig.getEmployeeBalances(this.selectedEmployeeId, this.year)
      }).subscribe({
        next: ({ days, balances }) => {
          this.monthDays.set(days);
          this.balances.set(balances);
        },
        error: () => this.error.set(this.translate.instant('attendance.loadFailed'))
      });
    } else {
      requests.days.subscribe({
        next: (days) => this.monthDays.set(days),
        error: () => this.error.set(this.translate.instant('attendance.loadFailed'))
      });
    }
  }

  selectDay(cell: CalendarCell): void {
    if (!cell.inMonth) return;
    this.selectedDate.set(cell.date);
    this.leaveForm.fromDate = cell.date;
    this.leaveForm.toDate = cell.date;
    const day = this.dayMap().get(cell.date);
    if (day && this.editableStatuses.includes(day.status)) {
      this.editStatus = day.status;
      this.editRemarks = day.remarks ?? '';
    } else if (day?.status === 'Holiday' || day?.status === 'WeekOff') {
      this.editStatus = 'Present';
      this.editRemarks = '';
    } else {
      this.editStatus = 'Present';
      this.editRemarks = day?.remarks ?? '';
    }
    this.previewLeave();
  }

  statusClass(status: string): string {
    return `att-cal-${status.charAt(0).toLowerCase()}${status.slice(1)}`;
  }

  statusShort(status: string, leaveTypeCode?: string | null): string {
    if (leaveTypeCode) {
      return leaveTypeCode;
    }
    const key = `attendance.short${status}`;
    const translated = this.translate.instant(key);
    return translated !== key ? translated : status.slice(0, 3);
  }

  saveDay(): void {
    if (!this.selectedEmployeeId || !this.selectedDate() || !this.canManage()) return;
    this.busy.set(true);
    this.message.set('');
    this.attendance.upsert({
      employeeId: this.selectedEmployeeId,
      date: this.selectedDate(),
      status: this.editStatus,
      remarks: this.editRemarks || undefined
    }).subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set(this.translate.instant('attendance.saved'));
        this.loadSummaries();
        this.loadEmployeeData();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  previewLeave(): void {
    if (!this.leaveForm.leaveTypeId || !this.leaveForm.fromDate || !this.leaveForm.toDate) {
      this.leavePreviewDays.set(null);
      return;
    }
    this.leaveConfig.previewDays({
      employeeId: this.selectedEmployeeId,
      leaveTypeId: this.leaveForm.leaveTypeId,
      fromDate: this.leaveForm.fromDate,
      toDate: this.leaveForm.toDate,
      isHalfDay: this.leaveForm.isHalfDay
    }).subscribe({
      next: (r) => this.leavePreviewDays.set(r.numberOfDays),
      error: () => this.leavePreviewDays.set(null)
    });
  }

  applyLeave(submit: boolean): void {
    if (!this.selectedEmployeeId || !this.leaveForm.leaveTypeId || !this.leaveForm.fromDate || !this.leaveForm.toDate) {
      return;
    }
    this.busy.set(true);
    this.message.set('');
    this.error.set('');
    this.leaveRequests.create({
      employeeId: this.selectedEmployeeId,
      leaveTypeId: this.leaveForm.leaveTypeId,
      fromDate: this.leaveForm.fromDate,
      toDate: this.leaveForm.toDate,
      isHalfDay: this.leaveForm.isHalfDay,
      halfDayPeriod: this.leaveForm.isHalfDay ? 'FirstHalf' : null,
      remarks: this.leaveForm.remarks || null,
      submit
    }).subscribe({
      next: (created) => {
        if (created.status === 'Approved' && this.canManage()) {
          this.markLeaveOnCalendar(this.leaveForm.fromDate, this.leaveForm.toDate, created.isHalfDay);
        } else {
          this.busy.set(false);
          this.message.set(this.translate.instant(submit ? 'attendance.leaveSubmitted' : 'attendance.leaveDraftSaved'));
          if (this.canViewBalances()) this.loadEmployeeData();
        }
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.saveFailed'));
      }
    });
  }

  private markLeaveOnCalendar(from: string, to: string, isHalfDay: boolean): void {
    const dates = this.enumerateDates(from, to);
    const status = isHalfDay ? 'HalfDay' : 'OnLeave';
    let pending = dates.length;
    if (pending === 0) {
      this.busy.set(false);
      return;
    }
    for (const date of dates) {
      const day = this.dayMap().get(date);
      if (day?.status === 'WeekOff' || day?.status === 'Holiday') {
        pending--;
        if (pending === 0) this.afterLeaveMarked(true);
        continue;
      }
      this.attendance.upsert({
        employeeId: this.selectedEmployeeId,
        date,
        status,
        remarks: this.translate.instant('attendance.leaveSyncRemark')
      }).subscribe({
        next: () => {
          pending--;
          if (pending === 0) this.afterLeaveMarked(true);
        },
        error: () => {
          pending--;
          if (pending === 0) this.afterLeaveMarked(false);
        }
      });
    }
  }

  private afterLeaveMarked(attendanceOk: boolean): void {
    this.busy.set(false);
    this.message.set(this.translate.instant(
      attendanceOk ? 'attendance.leaveApplied' : 'attendance.leaveAppliedPartial'));
    this.loadSummaries();
    this.loadEmployeeData();
  }

  syncLeave(): void {
    this.busy.set(true);
    this.message.set('');
    this.attendance.syncFromLeave(this.year, this.month, this.departmentId || undefined).subscribe({
      next: (r) => {
        this.busy.set(false);
        this.message.set(this.translate.instant('attendance.synced', { count: r.synced }));
        this.loadSummaries();
        if (this.selectedEmployeeId) this.loadEmployeeData();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('attendance.syncFailed'));
      }
    });
  }

  private buildCalendarWeeks(year: number, month: number, dayMap: Map<string, AttendanceDay>): CalendarCell[][] {
    const first = new Date(year, month - 1, 1);
    const lastDay = new Date(year, month, 0).getDate();
    const startPad = (first.getDay() + 6) % 7;
    const cells: CalendarCell[] = [];

    for (let i = 0; i < startPad; i++) {
      const d = new Date(year, month - 1, -startPad + i + 1);
      cells.push(this.makeCell(d, false, dayMap));
    }
    for (let day = 1; day <= lastDay; day++) {
      const d = new Date(year, month - 1, day);
      cells.push(this.makeCell(d, true, dayMap));
    }
    while (cells.length % 7 !== 0) {
      const d = new Date(year, month, cells.length - startPad - lastDay + 1);
      cells.push(this.makeCell(d, false, dayMap));
    }

    const weeks: CalendarCell[][] = [];
    for (let i = 0; i < cells.length; i += 7) {
      weeks.push(cells.slice(i, i + 7));
    }
    return weeks;
  }

  private makeCell(d: Date, inMonth: boolean, dayMap: Map<string, AttendanceDay>): CalendarCell {
    const date = this.toIsoDate(d);
    const entry = dayMap.get(date);
    return {
      date,
      day: d.getDate(),
      inMonth,
      status: entry?.status,
      source: entry?.source,
      remarks: entry?.remarks,
      leaveTypeCode: entry?.leaveTypeCode
    };
  }

  private toIsoDate(d: Date): string {
    const y = d.getFullYear();
    const m = (d.getMonth() + 1).toString().padStart(2, '0');
    const day = d.getDate().toString().padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private enumerateDates(from: string, to: string): string[] {
    const dates: string[] = [];
    const start = new Date(from + 'T00:00:00');
    const end = new Date(to + 'T00:00:00');
    for (let d = new Date(start); d <= end; d.setDate(d.getDate() + 1)) {
      dates.push(this.toIsoDate(d));
    }
    return dates;
  }
}
