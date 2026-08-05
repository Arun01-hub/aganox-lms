import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { LeaveRequestDetail } from './leave-request.service';

export interface PendingApproval {
  leaveRequestId: string;
  employeeId: string;
  employeeName: string;
  leaveTypeName: string;
  fromDate: string;
  toDate: string;
  numberOfDays: number;
  status: string;
  currentStepOrder: number;
  currentApproverRole: string;
  submittedAt: string;
}

export interface ApprovalHistoryItem {
  leaveRequestId: string;
  employeeName: string;
  leaveTypeName: string;
  fromDate: string;
  toDate: string;
  numberOfDays: number;
  requestStatus: string;
  stepStatus: string;
  approverRole: string;
  approverName?: string | null;
  comments?: string | null;
  actionedAt?: string | null;
}

export interface PagedApprovals<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getPending(page = 1, pageSize = 20) {
    return this.http.get<PagedApprovals<PendingApproval>>(`${this.base}/approvals/pending`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  getHistory(page = 1, pageSize = 20) {
    return this.http.get<PagedApprovals<ApprovalHistoryItem>>(`${this.base}/approvals/history`, {
      params: new HttpParams().set('page', page).set('pageSize', pageSize)
    });
  }

  approve(leaveRequestId: string, comments?: string | null) {
    return this.http.post<LeaveRequestDetail>(`${this.base}/approvals/${leaveRequestId}/approve`, { comments });
  }

  reject(leaveRequestId: string, comments: string) {
    return this.http.post<LeaveRequestDetail>(`${this.base}/approvals/${leaveRequestId}/reject`, { comments });
  }
}
