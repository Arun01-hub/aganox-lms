import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { AuthService } from '../../core/auth.service';
import {
  AdminDashboard,
  DashboardService,
  EmployeeDashboard,
  LeaveBalanceSummary,
  ManagerDashboard
} from '../../core/dashboard.service';
import { LocalizedDatePipe } from '../../core/localized-date.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, TranslatePipe, LocalizedDatePipe],
  template: `
    <header class="mb-4">
      <h1 class="h3 mb-1">{{ 'dashboard.title' | translate }}</h1>
      <p class="text-muted mb-0">{{ 'dashboard.welcome' | translate:{ name: auth.user()?.userName } }}</p>
    </header>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    @if (employee()) {
      <div class="gov-section dashboard-panel mb-4">
        <div class="gov-section-head">{{ 'dashboard.myLeave' | translate }}</div>
        <div class="gov-section-body">
          <div class="row g-3 mb-3">
            @for (b of employee()!.balances; track b.leaveTypeCode) {
              <div class="col-md-6 col-lg-4">
                <div class="card border-0 shadow-sm h-100 dashboard-balance-card" [attr.data-leave-code]="b.leaveTypeCode">
                  <div class="card-body">
                    <div class="d-flex align-items-center gap-2 mb-1">
                      <span class="badge rounded-pill dashboard-leave-badge" [attr.data-leave-code]="b.leaveTypeCode">{{ b.leaveTypeCode }}</span>
                      <div class="text-muted small">{{ leaveTypeLabel(b) }}</div>
                    </div>
                    <div class="gov-stat-value">{{ b.remaining }}</div>
                    <div class="small text-muted">{{ 'dashboard.daysRemaining' | translate }}</div>
                  </div>
                </div>
              </div>
            } @empty {
              <div class="col-12">
                <div class="alert alert-light border mb-0">{{ 'dashboard.noLeaveBalances' | translate }}</div>
              </div>
            }
          </div>
          <div class="row g-3">
            <div class="col-lg-6">
              <div class="card border-0 shadow-sm">
                <div class="card-header bg-white fw-semibold">{{ 'dashboard.upcomingLeave' | translate }}</div>
                <ul class="list-group list-group-flush">
                  @for (l of employee()!.upcomingLeaves; track l.id) {
                    <li class="list-group-item">{{ l.fromDate | localizedDate }} – {{ l.toDate | localizedDate }} · {{ l.leaveTypeName }}</li>
                  } @empty {
                    <li class="list-group-item text-muted">{{ 'dashboard.noUpcomingLeave' | translate }}</li>
                  }
                </ul>
              </div>
            </div>
            <div class="col-lg-6">
              <div class="card border-0 shadow-sm">
                <div class="card-header bg-white fw-semibold d-flex justify-content-between">
                  <span>{{ 'dashboard.pendingRequests' | translate }}</span>
                  <a routerLink="/leave/apply" class="btn btn-sm btn-aganox">{{ 'dashboard.applyLeave' | translate }}</a>
                </div>
                <ul class="list-group list-group-flush">
                  @for (l of employee()!.pendingRequests; track l.id) {
                    <li class="list-group-item d-flex justify-content-between">
                      <span>{{ l.fromDate | localizedDate }} · {{ l.leaveTypeName }}</span>
                      <span class="badge text-bg-secondary">{{ ('status.' + l.status) | translate }}</span>
                    </li>
                  } @empty {
                    <li class="list-group-item text-muted">{{ 'dashboard.noPendingRequests' | translate }}</li>
                  }
                </ul>
              </div>
            </div>
          </div>
        </div>
      </div>
    }

    @if (manager() || admin()) {
      <div class="gov-section dashboard-panel">
        <div class="gov-section-head">{{ 'dashboard.organization' | translate }}</div>
        <div class="gov-section-body">
          @if (admin()) {
            <div class="row g-3 mb-3">
              <div class="col-md-3">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-body">
                    <div class="text-muted small">{{ 'dashboard.employees' | translate }}</div>
                    <div class="gov-stat-value">{{ admin()!.totalEmployees }}</div>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-body">
                    <div class="text-muted small">{{ 'dashboard.active' | translate }}</div>
                    <div class="gov-stat-value">{{ admin()!.activeEmployees }}</div>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-body">
                    <div class="text-muted small">{{ 'dashboard.onLeaveToday' | translate }}</div>
                    <div class="gov-stat-value">{{ admin()!.onLeaveToday }}</div>
                  </div>
                </div>
              </div>
              <div class="col-md-3">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-body">
                    <div class="text-muted small">{{ 'dashboard.pendingApprovals' | translate }}</div>
                    <div class="gov-stat-value">{{ admin()!.pendingApprovals }}</div>
                  </div>
                </div>
              </div>
            </div>
          }

          @if (manager()) {
            <div class="row g-3 mb-3">
              <div class="col-md-4">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-body">
                    <div class="text-muted small">{{ 'dashboard.pendingApprovals' | translate }}</div>
                    <div class="gov-stat-value">{{ manager()!.pendingApprovalsCount }}</div>
                    <a routerLink="/approvals" class="small">{{ 'dashboard.goToInbox' | translate }}</a>
                  </div>
                </div>
              </div>
              <div class="col-md-8">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-header bg-white fw-semibold">{{ 'dashboard.onLeaveToday' | translate }}</div>
                  <ul class="list-group list-group-flush">
                    @for (t of manager()!.teamOnLeaveToday; track t.employeeId) {
                      <li class="list-group-item">{{ t.employeeName }} · {{ t.leaveTypeName }}</li>
                    } @empty {
                      <li class="list-group-item text-muted">{{ 'dashboard.fullTeamAvailable' | translate }}</li>
                    }
                  </ul>
                </div>
              </div>
            </div>
            <div class="card border-0 shadow-sm mb-3">
              <div class="card-header bg-white fw-semibold">{{ 'dashboard.teamCalendar' | translate }}</div>
              <div class="card-body">
                <div class="d-flex flex-wrap gap-1">
                  @for (entry of manager()!.teamCalendar; track entry.date + entry.employeeId) {
                    <span class="badge text-bg-light border" [title]="entry.employeeName">
                      {{ entry.date.slice(8) }} {{ entry.leaveTypeCode.charAt(0) }}
                    </span>
                  } @empty {
                    <span class="text-muted">{{ 'dashboard.noTeamLeaveMonth' | translate }}</span>
                  }
                </div>
              </div>
            </div>
          }

          @if (admin()) {
            <div class="row g-3">
              <div class="col-lg-6">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-header bg-white fw-semibold">{{ 'dashboard.leaveByDepartment' | translate }}</div>
                  <div class="card-body">
                    @for (d of admin()!.leavesByDepartment; track d.departmentName) {
                      <div class="mb-2">
                        <div class="d-flex justify-content-between small"><span>{{ d.departmentName }}</span><span>{{ d.totalDays }}d</span></div>
                        <div class="progress" style="height:8px">
                          <div class="progress-bar bg-primary" [style.width.%]="barWidth(d.totalDays)"></div>
                        </div>
                      </div>
                    } @empty {
                      <p class="text-muted mb-0">{{ 'dashboard.noApprovedLeaveData' | translate }}</p>
                    }
                  </div>
                </div>
              </div>
              <div class="col-lg-6">
                <div class="card border-0 shadow-sm h-100">
                  <div class="card-header bg-white fw-semibold">{{ 'dashboard.monthlyTrend' | translate }}</div>
                  <div class="card-body">
                    @for (m of admin()!.monthlyTrend; track m.month) {
                      <div class="mb-2">
                        <div class="d-flex justify-content-between small"><span>{{ m.month }}</span><span>{{ 'dashboard.reqCount' | translate:{ days: m.totalDays, count: m.requestCount } }}</span></div>
                        <div class="progress" style="height:8px">
                          <div class="progress-bar bg-success" [style.width.%]="barWidth(m.totalDays)"></div>
                        </div>
                      </div>
                    } @empty {
                      <p class="text-muted mb-0">{{ 'dashboard.noTrendData' | translate }}</p>
                    }
                  </div>
                </div>
              </div>
            </div>
            @if (auth.hasPermission('Reports.View')) {
              <div class="mt-3">
                <a routerLink="/reports" class="btn btn-outline-primary btn-sm">{{ 'dashboard.openReports' | translate }}</a>
              </div>
            }
          }
        </div>
      </div>
    }
  `
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly dashboard = inject(DashboardService);
  private readonly translate = inject(TranslateService);

  readonly employee = signal<EmployeeDashboard | null>(null);
  readonly manager = signal<ManagerDashboard | null>(null);
  readonly admin = signal<AdminDashboard | null>(null);
  readonly error = signal('');
  private maxBar = 1;

  ngOnInit(): void {
    const year = new Date().getFullYear();
    const month = new Date().getMonth() + 1;

    this.dashboard.getEmployee(year).subscribe({
      next: (data) => this.employee.set(data),
      error: () => undefined
    });

    if (this.auth.hasPermission('Leave.ViewTeam') || this.auth.hasRole('Manager')) {
      this.dashboard.getManager(year, month).subscribe({
        next: (data) => this.manager.set(data),
        error: () => undefined
      });
    }

    if (this.auth.hasPermission('Leave.ViewAll') || this.auth.hasRole('HR') || this.auth.hasRole('Administrator')) {
      this.dashboard.getAdmin(year).subscribe({
        next: (data) => {
          this.admin.set(data);
          this.maxBar = Math.max(
            1,
            ...data.leavesByDepartment.map((x) => x.totalDays),
            ...data.monthlyTrend.map((x) => x.totalDays)
          );
        },
        error: (err) => this.error.set(err.error?.errors?.[0] ?? this.translate.instant('dashboard.adminLoadFailed'))
      });
    }
  }

  barWidth(value: number): number {
    return Math.min(100, (value / this.maxBar) * 100);
  }

  leaveTypeLabel(balance: LeaveBalanceSummary): string {
    const key =
      balance.leaveTypeCode === 'CL'
        ? 'dashboard.casualLeave'
        : balance.leaveTypeCode === 'EL'
          ? 'dashboard.earnedLeave'
          : null;
    return key ? this.translate.instant(key) : balance.leaveTypeName;
  }
}
