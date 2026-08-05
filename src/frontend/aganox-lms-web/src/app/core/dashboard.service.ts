import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface LeaveBalanceSummary {
  leaveTypeCode: string;
  leaveTypeName: string;
  remaining: number;
}

export interface LeaveRequestBrief {
  id: string;
  leaveTypeName: string;
  fromDate: string;
  toDate: string;
  numberOfDays: number;
  status: string;
}

export interface EmployeeDashboard {
  balances: LeaveBalanceSummary[];
  upcomingLeaves: LeaveRequestBrief[];
  pendingRequests: LeaveRequestBrief[];
}

export interface TeamLeaveToday {
  employeeId: string;
  employeeName: string;
  leaveTypeName: string;
  fromDate: string;
  toDate: string;
}

export interface TeamCalendarEntry {
  date: string;
  employeeId: string;
  employeeName: string;
  leaveTypeCode: string;
}

export interface ManagerDashboard {
  pendingApprovalsCount: number;
  teamOnLeaveToday: TeamLeaveToday[];
  teamCalendar: TeamCalendarEntry[];
  pendingTeamRequests: LeaveRequestBrief[];
}

export interface DepartmentStat {
  departmentName: string;
  totalDays: number;
  requestCount: number;
}

export interface MonthlyTrend {
  month: string;
  totalDays: number;
  requestCount: number;
}

export interface AdminDashboard {
  totalEmployees: number;
  activeEmployees: number;
  onLeaveToday: number;
  pendingApprovals: number;
  leavesByDepartment: DepartmentStat[];
  monthlyTrend: MonthlyTrend[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getEmployee(year?: number) {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    return this.http.get<EmployeeDashboard>(`${this.base}/dashboards/employee`, { params });
  }

  getManager(year?: number, month?: number) {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    if (month) params = params.set('month', month);
    return this.http.get<ManagerDashboard>(`${this.base}/dashboards/manager`, { params });
  }

  getAdmin(year?: number) {
    let params = new HttpParams();
    if (year) params = params.set('year', year);
    return this.http.get<AdminDashboard>(`${this.base}/dashboards/admin`, { params });
  }
}
