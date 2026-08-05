import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../environments/environment';

export interface AppSetting {
  key: string;
  value: string;
}

export interface RolePermissions {
  roleId: string;
  roleName: string;
  description?: string | null;
  permissions: string[];
}

export interface AuditLog {
  id: number;
  entityName: string;
  entityId: string;
  action: string;
  oldValues?: string | null;
  newValues?: string | null;
  userId?: string | null;
  timestamp: string;
}

export interface ActivityLog {
  id: number;
  userId?: string | null;
  action: string;
  details?: string | null;
  timestamp: string;
}

export interface BackupHistory {
  id: string;
  filePath: string;
  status: string;
  message?: string | null;
  createdAt: string;
}

export interface Paged<T> {
  items: T[];
  totalCount: number;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBaseUrl;

  getSettings() {
    return this.http.get<AppSetting[]>(`${this.base}/admin/settings`);
  }

  updateSettings(settings: AppSetting[]) {
    return this.http.put<AppSetting[]>(`${this.base}/admin/settings`, { settings });
  }

  getPermissions() {
    return this.http.get<RolePermissions[]>(`${this.base}/admin/permissions`);
  }

  updateRolePermissions(roleId: string, permissions: string[]) {
    return this.http.put<RolePermissions>(`${this.base}/admin/roles/${roleId}/permissions`, { permissions });
  }

  getAuditLogs(page = 1) {
    return this.http.get<Paged<AuditLog>>(`${this.base}/admin/audit-logs`, {
      params: new HttpParams().set('page', page).set('pageSize', 20)
    });
  }

  getActivityLogs(page = 1) {
    return this.http.get<Paged<ActivityLog>>(`${this.base}/admin/activity-logs`, {
      params: new HttpParams().set('page', page).set('pageSize', 20)
    });
  }

  getBackups() {
    return this.http.get<Paged<BackupHistory>>(`${this.base}/admin/backups`);
  }

  createBackup() {
    return this.http.post<{ backupId: string; filePath: string; status: string }>(`${this.base}/admin/backup`, {});
  }

  restoreBackup(backupFilePath: string) {
    return this.http.post<{ message: string }>(`${this.base}/admin/restore`, { backupFilePath });
  }
}
