import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface Department {
  id: string;
  name: string;
  code: string;
  isActive: boolean;
  employeeCount: number;
}

export interface EmployeeSummary {
  id: string;
  employeeCode: string;
  fullName: string;
  departmentId: string;
  departmentName: string;
  designation: string;
  dateOfJoining: string;
  status: string;
  email: string;
  phone?: string | null;
  managerId?: string | null;
  managerName?: string | null;
  userId?: string | null;
}

export interface EmployeeProfile extends EmployeeSummary {
  userEmail?: string | null;
  directReports: EmployeeSummary[];
}

export interface PagedEmployees {
  items: EmployeeSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface EmployeeListParams {
  search?: string;
  departmentId?: string;
  status?: string;
  managerId?: string;
  sortBy?: string;
  sortDir?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class OrganizationService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getDepartments(activeOnly = false) {
    const params = new HttpParams().set('activeOnly', activeOnly);
    return this.http.get<Department[]>(`${this.base}/departments`, { params });
  }

  createDepartment(payload: { name: string; code: string; isActive: boolean }) {
    return this.http.post<Department>(`${this.base}/departments`, payload);
  }

  updateDepartment(id: string, payload: { name: string; code: string; isActive: boolean }) {
    return this.http.put<Department>(`${this.base}/departments/${id}`, payload);
  }

  deleteDepartment(id: string) {
    return this.http.delete<void>(`${this.base}/departments/${id}`);
  }

  getEmployees(params: EmployeeListParams = {}) {
    let httpParams = new HttpParams();
    Object.entries(params).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        httpParams = httpParams.set(key, String(value));
      }
    });
    return this.http.get<PagedEmployees>(`${this.base}/employees`, { params: httpParams });
  }

  getEmployee(id: string) {
    return this.http.get<EmployeeSummary>(`${this.base}/employees/${id}`);
  }

  getEmployeeProfile(id: string) {
    return this.http.get<EmployeeProfile>(`${this.base}/employees/${id}/profile`);
  }

  createEmployee(payload: Record<string, unknown>) {
    return this.http.post<EmployeeSummary>(`${this.base}/employees`, payload);
  }

  updateEmployee(id: string, payload: Record<string, unknown>) {
    return this.http.put<EmployeeSummary>(`${this.base}/employees/${id}`, payload);
  }

  deleteEmployee(id: string) {
    return this.http.delete<void>(`${this.base}/employees/${id}`);
  }

  linkEmployeeUser(
    id: string,
    payload: { userId?: string; email?: string; password?: string; role?: string }
  ) {
    return this.http.post<EmployeeSummary>(`${this.base}/employees/${id}/link-user`, payload);
  }
}
