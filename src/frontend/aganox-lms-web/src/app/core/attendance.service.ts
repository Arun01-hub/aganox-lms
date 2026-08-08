import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface AttendanceDay {
  id?: string | null;
  employeeId: string;
  employeeName: string;
  employeeCode: string;
  date: string;
  status: string;
  source: string;
  remarks?: string | null;
  leaveTypeCode?: string | null;
}

export interface AttendanceSummary {
  year: number;
  month: number;
  employeeId: string;
  employeeName: string;
  workingDays: number;
  presentDays: number;
  absentDays: number;
  leaveDays: number;
  lopDays: number;
  paidDays: number;
}

@Injectable({ providedIn: 'root' })
export class AttendanceService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiBaseUrl}/attendance`;

  getMonth(year: number, month: number, departmentId?: string, employeeId?: string) {
    let params = new HttpParams().set('year', year).set('month', month);
    if (departmentId) params = params.set('departmentId', departmentId);
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<AttendanceDay[]>(this.base, { params });
  }

  getSummaries(year: number, month: number, departmentId?: string) {
    let params = new HttpParams().set('year', year).set('month', month);
    if (departmentId) params = params.set('departmentId', departmentId);
    return this.http.get<AttendanceSummary[]>(`${this.base}/summaries`, { params });
  }

  upsert(payload: { employeeId: string; date: string; status: string; remarks?: string }) {
    return this.http.put<AttendanceDay>(this.base, payload);
  }

  syncFromLeave(year: number, month: number, departmentId?: string) {
    return this.http.post<{ synced: number }>(`${this.base}/sync-from-leave`, { year, month, departmentId });
  }
}
