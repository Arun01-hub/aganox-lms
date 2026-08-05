import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface LeaveType {
  id: string;
  code: string;
  name: string;
  isPaid: boolean;
  requiresApproval: boolean;
  allowHalfDay: boolean;
  allowNegativeBalance: boolean;
  maxDaysPerRequest?: number | null;
  colorHex?: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface LeavePolicy {
  id: string;
  leaveTypeId: string;
  leaveTypeName: string;
  year: number;
  skipWeekends: boolean;
  skipHolidays: boolean;
  carryForwardMaxDays: number;
  yearlyResetEnabled: boolean;
  defaultOpeningBalance: number;
  accrualStartDate?: string | null;
}

export interface LeaveCalendar {
  id: string;
  name: string;
  isDefault: boolean;
  holidayCount: number;
}

export interface Holiday {
  id: string;
  leaveCalendarId: string;
  calendarName: string;
  holidayDate: string;
  name: string;
  isOptional: boolean;
}

export interface LeaveBalance {
  id: string;
  employeeId: string;
  leaveTypeId: string;
  leaveTypeCode: string;
  leaveTypeName: string;
  year: number;
  openingBalance: number;
  credited: number;
  consumed: number;
  adjusted: number;
  remaining: number;
}

export interface LeaveDayPreview {
  numberOfDays: number;
  skippedWeekends: number;
  skippedHolidays: number;
  breakdown: { date: string; counted: boolean; reason?: string | null }[];
}

@Injectable({ providedIn: 'root' })
export class LeaveConfigService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getLeaveTypes(activeOnly = false) {
    return this.http.get<LeaveType[]>(`${this.base}/leave-types`, {
      params: new HttpParams().set('activeOnly', activeOnly)
    });
  }

  createLeaveType(payload: Partial<LeaveType>) {
    return this.http.post<LeaveType>(`${this.base}/leave-types`, payload);
  }

  updateLeaveType(id: string, payload: Partial<LeaveType>) {
    return this.http.put<LeaveType>(`${this.base}/leave-types/${id}`, payload);
  }

  deleteLeaveType(id: string) {
    return this.http.delete<void>(`${this.base}/leave-types/${id}`);
  }

  getPolicies(leaveTypeId?: string, year?: number) {
    let params = new HttpParams();
    if (leaveTypeId) params = params.set('leaveTypeId', leaveTypeId);
    if (year) params = params.set('year', year);
    return this.http.get<LeavePolicy[]>(`${this.base}/leave-policies`, { params });
  }

  upsertPolicy(payload: Record<string, unknown>) {
    return this.http.put<LeavePolicy>(`${this.base}/leave-policies`, payload);
  }

  getCalendars() {
    return this.http.get<LeaveCalendar[]>(`${this.base}/leave-calendars`);
  }

  createCalendar(payload: { name: string; isDefault: boolean }) {
    return this.http.post<LeaveCalendar>(`${this.base}/leave-calendars`, payload);
  }

  getHolidays(calendarId?: string, year?: number) {
    let params = new HttpParams();
    if (calendarId) params = params.set('calendarId', calendarId);
    if (year) params = params.set('year', year);
    return this.http.get<Holiday[]>(`${this.base}/holidays`, { params });
  }

  createHoliday(payload: Record<string, unknown>) {
    return this.http.post<Holiday>(`${this.base}/holidays`, payload);
  }

  deleteHoliday(id: string) {
    return this.http.delete<void>(`${this.base}/holidays/${id}`);
  }

  getEmployeeBalances(employeeId: string, year: number) {
    return this.http.get<LeaveBalance[]>(`${this.base}/leave-balances/employee/${employeeId}`, {
      params: new HttpParams().set('year', year)
    });
  }

  setOpeningBalances(employeeId: string, year: number, balances: { leaveTypeId: string; openingBalance: number }[]) {
    return this.http.put<LeaveBalance[]>(`${this.base}/leave-balances/employee/${employeeId}/opening`, {
      year,
      balances
    });
  }

  previewDays(payload: Record<string, unknown>) {
    return this.http.post<LeaveDayPreview>(`${this.base}/leave-requests/preview-days`, payload);
  }
}
