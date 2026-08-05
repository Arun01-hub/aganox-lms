import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../core/auth.service';
import { LeaveBalance, LeaveConfigService, LeaveType } from '../../core/leave-config.service';
import { EmployeeProfile, OrganizationService } from '../../core/organization.service';

@Component({
  selector: 'app-employee-profile',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    @if (profile()) {
      <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
        <div>
          <h1 class="h4 mb-0">{{ profile()!.fullName }}</h1>
          <p class="text-muted mb-0">{{ profile()!.employeeCode }} · {{ profile()!.designation }}</p>
        </div>
        <a routerLink="/employees" class="btn btn-sm btn-outline-secondary ms-auto">Back to list</a>
        @if (canUpdate()) {
          <a class="btn btn-sm btn-aganox" [routerLink]="['/employees', profile()!.id, 'edit']">Edit</a>
        }
      </div>

      <div class="row g-3">
        <div class="col-lg-7">
          <div class="card border-0 shadow-sm mb-3">
            <div class="card-body">
              <h2 class="h6">Details</h2>
              <dl class="row mb-0">
                <dt class="col-sm-4">Department</dt>
                <dd class="col-sm-8">{{ profile()!.departmentName }}</dd>
                <dt class="col-sm-4">Email</dt>
                <dd class="col-sm-8">{{ profile()!.email }}</dd>
                <dt class="col-sm-4">Phone</dt>
                <dd class="col-sm-8">{{ profile()!.phone ?? '—' }}</dd>
                <dt class="col-sm-4">Manager</dt>
                <dd class="col-sm-8">{{ profile()!.managerName ?? '—' }}</dd>
                <dt class="col-sm-4">Joined</dt>
                <dd class="col-sm-8">{{ profile()!.dateOfJoining }}</dd>
                <dt class="col-sm-4">Status</dt>
                <dd class="col-sm-8">{{ profile()!.status }}</dd>
                <dt class="col-sm-4">Login account</dt>
                <dd class="col-sm-8">
                  @if (profile()!.userId) {
                    {{ profile()!.userEmail ?? profile()!.email }} (linked)
                  } @else {
                    Not linked
                  }
                </dd>
              </dl>
            </div>
          </div>

          @if (canAdjustBalances()) {
            <div class="card border-0 shadow-sm mb-3">
              <div class="card-body">
                <h2 class="h6">Opening balances ({{ balanceYear }})</h2>
                @for (row of balanceRows; track row.leaveTypeId) {
                  <div class="row g-2 align-items-center mb-2">
                    <div class="col-6">{{ row.leaveTypeName }}</div>
                    <div class="col-4"><input class="form-control form-control-sm" type="number" [(ngModel)]="row.openingBalance" /></div>
                    <div class="col-2 small text-muted">{{ row.remaining ?? '—' }} left</div>
                  </div>
                }
                <button class="btn btn-sm btn-aganox mt-2" type="button" (click)="saveBalances()" [disabled]="savingBalances()">
                  Save opening balances
                </button>
                @if (balanceError()) { <div class="text-danger small mt-2">{{ balanceError() }}</div> }
              </div>
            </div>
          }

          @if (canManageUsers() && !profile()!.userId) {
            <div class="card border-0 shadow-sm">
              <div class="card-body">
                <h2 class="h6">Link user account</h2>
                <div class="row g-3">
                  <div class="col-md-6">
                    <label class="form-label">Email</label>
                    <input class="form-control" [(ngModel)]="linkForm.email" />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Initial password</label>
                    <input class="form-control" type="password" [(ngModel)]="linkForm.password" />
                  </div>
                  <div class="col-md-6">
                    <label class="form-label">Role</label>
                    <select class="form-select" [(ngModel)]="linkForm.role">
                      <option value="Employee">Employee</option>
                      <option value="Manager">Manager</option>
                      <option value="HR">HR</option>
                    </select>
                  </div>
                  <div class="col-12">
                    @if (linkError()) {
                      <div class="alert alert-danger py-2">{{ linkError() }}</div>
                    }
                    <button class="btn btn-aganox" type="button" (click)="linkUser()" [disabled]="linking()">
                      Create & link account
                    </button>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>

        <div class="col-lg-5">
          <div class="card border-0 shadow-sm">
            <div class="card-body">
              <h2 class="h6">Direct reports ({{ profile()!.directReports.length }})</h2>
              @if (profile()!.directReports.length) {
                <ul class="list-group list-group-flush">
                  @for (report of profile()!.directReports; track report.id) {
                    <li class="list-group-item px-0">
                      <a [routerLink]="['/employees', report.id]">{{ report.fullName }}</a>
                      <div class="small text-muted">{{ report.designation }}</div>
                    </li>
                  }
                </ul>
              } @else {
                <p class="text-muted mb-0">No direct reports.</p>
              }
            </div>
          </div>
        </div>
      </div>
    } @else if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }
  `
})
export class EmployeeProfileComponent implements OnInit {
  private readonly org = inject(OrganizationService);
  private readonly leave = inject(LeaveConfigService);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly profile = signal<EmployeeProfile | null>(null);
  readonly error = signal<string | null>(null);
  readonly linking = signal(false);
  readonly linkError = signal<string | null>(null);
  readonly savingBalances = signal(false);
  readonly balanceError = signal<string | null>(null);

  balanceYear = new Date().getFullYear();
  balanceRows: { leaveTypeId: string; leaveTypeName: string; openingBalance: number; remaining?: number }[] = [];

  linkForm = { email: '', password: '', role: 'Employee' };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }

    this.org.getEmployeeProfile(id).subscribe({
      next: (item) => {
        this.profile.set(item);
        this.linkForm.email = item.email;
        this.loadBalances(id);
      },
      error: () => this.error.set('Employee not found.')
    });
  }

  loadBalances(employeeId: string): void {
    if (!this.canAdjustBalances()) return;
    this.leave.getLeaveTypes(true).subscribe((types) => {
      this.leave.getEmployeeBalances(employeeId, this.balanceYear).subscribe((balances) => {
        this.balanceRows = types.map((t: LeaveType) => {
          const existing = balances.find((b: LeaveBalance) => b.leaveTypeId === t.id);
          return {
            leaveTypeId: t.id,
            leaveTypeName: t.name,
            openingBalance: existing?.openingBalance ?? 0,
            remaining: existing?.remaining
          };
        });
      });
    });
  }

  canAdjustBalances(): boolean {
    return this.auth.hasPermission('Balances.Adjust');
  }

  canUpdate(): boolean {
    return this.auth.hasPermission('Employees.Update');
  }

  canManageUsers(): boolean {
    return this.auth.hasPermission('Users.Manage');
  }

  saveBalances(): void {
    const id = this.profile()?.id;
    if (!id) return;
    this.savingBalances.set(true);
    this.balanceError.set(null);
    this.leave
      .setOpeningBalances(
        id,
        this.balanceYear,
        this.balanceRows.map((r) => ({ leaveTypeId: r.leaveTypeId, openingBalance: r.openingBalance }))
      )
      .subscribe({
        next: () => {
          this.savingBalances.set(false);
          this.loadBalances(id);
        },
        error: (err) => {
          this.savingBalances.set(false);
          this.balanceError.set(err.error?.errors?.[0] ?? 'Save failed.');
        }
      });
  }

  linkUser(): void {
    const id = this.profile()?.id;
    if (!id) {
      return;
    }

    this.linking.set(true);
    this.linkError.set(null);
    this.org
      .linkEmployeeUser(id, {
        email: this.linkForm.email,
        password: this.linkForm.password,
        role: this.linkForm.role
      })
      .subscribe({
        next: () => {
          this.linking.set(false);
          this.org.getEmployeeProfile(id).subscribe((item) => this.profile.set(item));
        },
        error: (err) => {
          this.linking.set(false);
          this.linkError.set(err.error?.errors?.[0] ?? 'Link failed.');
        }
      });
  }
}
