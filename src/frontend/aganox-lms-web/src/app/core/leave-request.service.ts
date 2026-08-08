import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface LeaveRequestSummary {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  leaveTypeCode: string;
  fromDate: string;
  toDate: string;
  isHalfDay: boolean;
  halfDayPeriod?: string | null;
  numberOfDays: number;
  status: string;
  remarks?: string | null;
  createdAt: string;
}

export interface LeaveRequestDetail extends LeaveRequestSummary {
  modifiedAt?: string | null;
  approvalSteps: ApprovalStep[];
  comments: LeaveComment[];
}

export interface ApprovalStep {
  id: string;
  stepOrder: number;
  approverRole: string;
  approverEmployeeId?: string | null;
  approverName?: string | null;
  status: string;
  comments?: string | null;
  actionedAt?: string | null;
}

export interface LeaveComment {
  id: string;
  authorName: string;
  comment: string;
  createdAt: string;
}

export interface PagedLeaveRequests {
  items: LeaveRequestSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateLeaveRequestPayload {
  employeeId?: string;
  leaveTypeId: string;
  fromDate: string;
  toDate: string;
  isHalfDay: boolean;
  halfDayPeriod?: string | null;
  remarks?: string | null;
  submit: boolean;
}

@Injectable({ providedIn: 'root' })
export class LeaveRequestService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  list(params: { employeeId?: string; status?: string; page?: number; pageSize?: number } = {}) {
    let httpParams = new HttpParams();
    if (params.employeeId) httpParams = httpParams.set('employeeId', params.employeeId);
    if (params.status) httpParams = httpParams.set('status', params.status);
    if (params.page) httpParams = httpParams.set('page', params.page);
    if (params.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    return this.http.get<PagedLeaveRequests>(`${this.base}/leave-requests`, { params: httpParams });
  }

  get(id: string) {
    return this.http.get<LeaveRequestDetail>(`${this.base}/leave-requests/${id}`);
  }

  create(payload: CreateLeaveRequestPayload) {
    return this.http.post<LeaveRequestDetail>(`${this.base}/leave-requests`, payload);
  }

  update(id: string, payload: CreateLeaveRequestPayload) {
    return this.http.put<LeaveRequestDetail>(`${this.base}/leave-requests/${id}`, payload);
  }

  submit(id: string) {
    return this.http.post<LeaveRequestDetail>(`${this.base}/leave-requests/${id}/submit`, {});
  }

  cancel(id: string) {
    return this.http.post<LeaveRequestDetail>(`${this.base}/leave-requests/${id}/cancel`, {});
  }
}
