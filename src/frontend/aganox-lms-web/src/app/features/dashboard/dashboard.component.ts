import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import {
  AdminDashboard,
  DashboardService,
  EmployeeDashboard,
  ManagerDashboard
} from '../../core/dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink],
  template: `
    <header class="mb-4">
      <h1 class="h3 mb-1">Dashboard</h1>
      <p class="text-muted mb-0">Welcome, {{ auth.user()?.userName }}</p>
    </header>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    @if (employee()) {
      <section class="mb-4">
        <h2 class="h5 mb-3">My leave</h2>
        <div class="row g-3 mb-3">
          @for (b of employee()!.balances; track b.leaveTypeCode) {
            <div class="col-md-4">
              <div class="card border-0 shadow-sm h-100">
                <div class="card-body">
                  <div class="text-muted small">{{ b.leaveTypeName }}</div>
                  <div class="fs-3 fw-semibold">{{ b.remaining }}</div>
                  <div class="small text-muted">days remaining</div>
                </div>
              </div>
            </div>
          }
        </div>
        <div class="row g-3">
          <div class="col-lg-6">
            <div class="card border-0 shadow-sm">
              <div class="card-header bg-white fw-semibold">Upcoming leave</div>
              <ul class="list-group list-group-flush">
                @for (l of employee()!.upcomingLeaves; track l.id) {
                  <li class="list-group-item">{{ l.fromDate }} – {{ l.toDate }} · {{ l.leaveTypeName }}</li>
                } @empty {
                  <li class="list-group-item text-muted">No upcoming approved leave.</li>
                }
              </ul>
            </div>
          </div>
          <div class="col-lg-6">
            <div class="card border-0 shadow-sm">
              <div class="card-header bg-white fw-semibold d-flex justify-content-between">
                <span>Pending requests</span>
                <a routerLink="/leave/apply" class="btn btn-sm btn-aganox">Apply</a>
              </div>
              <ul class="list-group list-group-flush">
                @for (l of employee()!.pendingRequests; track l.id) {
                  <li class="list-group-item d-flex justify-content-between">
                    <span>{{ l.fromDate }} · {{ l.leaveTypeName }}</span>
                    <span class="badge text-bg-secondary">{{ l.status }}</span>
                  </li>
                } @empty {
                  <li class="list-group-item text-muted">No pending requests.</li>
                }
              </ul>
            </div>
          </div>
        </div>
      </section>
    }

    @if (manager()) {
      <section class="mb-4">
        <h2 class="h5 mb-3">Team overview</h2>
        <div class="row g-3 mb-3">
          <div class="col-md-4">
            <div class="card border-0 shadow-sm">
              <div class="card-body">
                <div class="text-muted small">Pending approvals</div>
                <div class="fs-3 fw-semibold">{{ manager()!.pendingApprovalsCount }}</div>
                <a routerLink="/approvals" class="small">Go to inbox</a>
              </div>
            </div>
          </div>
          <div class="col-md-8">
            <div class="card border-0 shadow-sm">
              <div class="card-header bg-white fw-semibold">On leave today</div>
              <ul class="list-group list-group-flush">
                @for (t of manager()!.teamOnLeaveToday; track t.employeeId) {
                  <li class="list-group-item">{{ t.employeeName }} · {{ t.leaveTypeName }}</li>
                } @empty {
                  <li class="list-group-item text-muted">Full team available today.</li>
                }
              </ul>
            </div>
          </div>
        </div>
        <div class="card border-0 shadow-sm">
          <div class="card-header bg-white fw-semibold">Team calendar (this month)</div>
          <div class="card-body">
            <div class="d-flex flex-wrap gap-1">
              @for (entry of manager()!.teamCalendar; track entry.date + entry.employeeId) {
                <span class="badge text-bg-light border" [title]="entry.employeeName">
                  {{ entry.date.slice(8) }} {{ entry.leaveTypeCode.charAt(0) }}
                </span>
              } @empty {
                <span class="text-muted">No team leave this month.</span>
              }
            </div>
          </div>
        </div>
      </section>
    }

    @if (admin()) {
      <section>
        <h2 class="h5 mb-3">Organization</h2>
        <div class="row g-3 mb-3">
          <div class="col-md-3"><div class="card border-0 shadow-sm"><div class="card-body"><div class="text-muted small">Employees</div><div class="fs-4 fw-semibold">{{ admin()!.totalEmployees }}</div></div></div></div>
          <div class="col-md-3"><div class="card border-0 shadow-sm"><div class="card-body"><div class="text-muted small">Active</div><div class="fs-4 fw-semibold">{{ admin()!.activeEmployees }}</div></div></div></div>
          <div class="col-md-3"><div class="card border-0 shadow-sm"><div class="card-body"><div class="text-muted small">On leave today</div><div class="fs-4 fw-semibold">{{ admin()!.onLeaveToday }}</div></div></div></div>
          <div class="col-md-3"><div class="card border-0 shadow-sm"><div class="card-body"><div class="text-muted small">Pending approvals</div><div class="fs-4 fw-semibold">{{ admin()!.pendingApprovals }}</div></div></div></div>
        </div>
        <div class="row g-3">
          <div class="col-lg-6">
            <div class="card border-0 shadow-sm">
              <div class="card-header bg-white fw-semibold">Leave by department</div>
              <div class="card-body">
                @for (d of admin()!.leavesByDepartment; track d.departmentName) {
                  <div class="mb-2">
                    <div class="d-flex justify-content-between small"><span>{{ d.departmentName }}</span><span>{{ d.totalDays }}d</span></div>
                    <div class="progress" style="height:8px">
                      <div class="progress-bar bg-primary" [style.width.%]="barWidth(d.totalDays)"></div>
                    </div>
                  </div>
                } @empty {
                  <p class="text-muted mb-0">No approved leave data yet.</p>
                }
              </div>
            </div>
          </div>
          <div class="col-lg-6">
            <div class="card border-0 shadow-sm">
              <div class="card-header bg-white fw-semibold">Monthly trend</div>
              <div class="card-body">
                @for (m of admin()!.monthlyTrend; track m.month) {
                  <div class="mb-2">
                    <div class="d-flex justify-content-between small"><span>{{ m.month }}</span><span>{{ m.totalDays }}d · {{ m.requestCount }} req</span></div>
                    <div class="progress" style="height:8px">
                      <div class="progress-bar bg-success" [style.width.%]="barWidth(m.totalDays)"></div>
                    </div>
                  </div>
                } @empty {
                  <p class="text-muted mb-0">No trend data yet.</p>
                }
              </div>
            </div>
          </div>
        </div>
        @if (auth.hasPermission('Reports.View')) {
          <div class="mt-3"><a routerLink="/reports" class="btn btn-outline-primary btn-sm">Open reports</a></div>
        }
      </section>
    }
  `
})
export class DashboardComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly dashboard = inject(DashboardService);

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
        error: (err) => this.error.set(err.error?.errors?.[0] ?? 'Failed to load admin dashboard.')
      });
    }
  }

  barWidth(value: number): number {
    return Math.min(100, (value / this.maxBar) * 100);
  }
}
