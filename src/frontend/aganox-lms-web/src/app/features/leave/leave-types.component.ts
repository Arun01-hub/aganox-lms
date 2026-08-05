import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LeaveConfigService, LeavePolicy, LeaveType } from '../../core/leave-config.service';

@Component({
  selector: 'app-leave-types',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">Leave types</h1>
      <a routerLink="/leave/policies" class="btn btn-sm btn-outline-secondary">Policies</a>
      <a routerLink="/leave/holidays" class="btn btn-sm btn-outline-secondary">Holidays</a>
    </div>

    <div class="card border-0 shadow-sm mb-4">
      <div class="card-body row g-3">
        <div class="col-md-2"><input class="form-control" placeholder="Code" [(ngModel)]="form.code" /></div>
        <div class="col-md-3"><input class="form-control" placeholder="Name" [(ngModel)]="form.name" /></div>
        <div class="col-md-2"><input class="form-control" type="number" placeholder="Sort" [(ngModel)]="form.sortOrder" /></div>
        <div class="col-md-2"><input class="form-control" placeholder="#Color" [(ngModel)]="form.colorHex" /></div>
        <div class="col-md-3 d-flex align-items-center gap-2">
          <div class="form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="form.isActive" id="ltActive" /><label class="form-check-label" for="ltActive">Active</label></div>
          <button class="btn btn-aganox ms-auto" type="button" (click)="save()">{{ editingId() ? 'Update' : 'Add' }}</button>
        </div>
      </div>
      @if (error()) { <div class="card-footer text-danger small">{{ error() }}</div> }
    </div>

    <div class="card border-0 shadow-sm">
      <table class="table mb-0">
        <thead><tr><th>Code</th><th>Name</th><th>Half-day</th><th>Active</th><th></th></tr></thead>
        <tbody>
          @for (t of types(); track t.id) {
            <tr>
              <td><code>{{ t.code }}</code></td>
              <td><span class="badge me-1" [style.background]="t.colorHex ?? '#ccc'">&nbsp;</span>{{ t.name }}</td>
              <td>{{ t.allowHalfDay ? 'Yes' : 'No' }}</td>
              <td>{{ t.isActive ? 'Yes' : 'No' }}</td>
              <td class="text-end">
                <button class="btn btn-sm btn-outline-primary me-1" type="button" (click)="edit(t)">Edit</button>
                <button class="btn btn-sm btn-outline-danger" type="button" (click)="remove(t)">Delete</button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class LeaveTypesComponent implements OnInit {
  private readonly leave = inject(LeaveConfigService);
  readonly types = signal<LeaveType[]>([]);
  readonly editingId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  form = { code: '', name: '', sortOrder: 0, colorHex: '#2F6FED', isActive: true, isPaid: true, requiresApproval: true, allowHalfDay: true, allowNegativeBalance: false };

  ngOnInit(): void { this.load(); }
  load(): void { this.leave.getLeaveTypes().subscribe((items) => this.types.set(items)); }
  edit(t: LeaveType): void {
    this.editingId.set(t.id);
    this.form = { code: t.code, name: t.name, sortOrder: t.sortOrder, colorHex: t.colorHex ?? '#2F6FED', isActive: t.isActive, isPaid: t.isPaid, requiresApproval: t.requiresApproval, allowHalfDay: t.allowHalfDay, allowNegativeBalance: t.allowNegativeBalance };
  }
  save(): void {
    const payload = { ...this.form, maxDaysPerRequest: null };
    const req = this.editingId() ? this.leave.updateLeaveType(this.editingId()!, payload) : this.leave.createLeaveType(payload);
    req.subscribe({ next: () => { this.editingId.set(null); this.form = { code: '', name: '', sortOrder: 0, colorHex: '#2F6FED', isActive: true, isPaid: true, requiresApproval: true, allowHalfDay: true, allowNegativeBalance: false }; this.load(); }, error: (e) => this.error.set(e.error?.errors?.[0] ?? 'Save failed') });
  }
  remove(t: LeaveType): void {
    if (!confirm(`Delete leave type "${t.name}"?`)) return;
    this.leave.deleteLeaveType(t.id).subscribe({ next: () => this.load(), error: (e) => this.error.set(e.error?.errors?.[0] ?? 'Delete failed') });
  }
}

@Component({
  selector: 'app-leave-policies',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">Leave policies</h1>
      <a routerLink="/leave/types" class="btn btn-sm btn-outline-secondary ms-auto">Leave types</a>
    </div>
    <div class="card border-0 shadow-sm">
      <div class="card-body row g-3">
        <div class="col-md-4">
          <label class="form-label">Leave type</label>
          <select class="form-select" [(ngModel)]="selectedTypeId" (change)="load()">
            @for (t of types(); track t.id) { <option [value]="t.id">{{ t.name }}</option> }
          </select>
        </div>
        <div class="col-md-2"><label class="form-label">Year</label><input class="form-control" type="number" [(ngModel)]="year" (change)="load()" /></div>
      </div>
      @if (policy()) {
        <div class="card-body border-top row g-3">
          <div class="col-md-3 form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="form.skipWeekends" id="sw" /><label class="form-check-label" for="sw">Skip weekends</label></div>
          <div class="col-md-3 form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="form.skipHolidays" id="sh" /><label class="form-check-label" for="sh">Skip holidays</label></div>
          <div class="col-md-3"><label class="form-label">Default opening balance</label><input class="form-control" type="number" [(ngModel)]="form.defaultOpeningBalance" /></div>
          <div class="col-md-3"><label class="form-label">Carry forward max</label><input class="form-control" type="number" [(ngModel)]="form.carryForwardMaxDays" /></div>
          <div class="col-12"><button class="btn btn-aganox" type="button" (click)="save()">Save policy</button></div>
        </div>
      }
    </div>
  `
})
export class LeavePoliciesComponent implements OnInit {
  private readonly leave = inject(LeaveConfigService);
  readonly types = signal<LeaveType[]>([]);
  readonly policy = signal<LeavePolicy | null>(null);
  selectedTypeId = '';
  year = new Date().getFullYear();
  form = { skipWeekends: true, skipHolidays: true, defaultOpeningBalance: 0, carryForwardMaxDays: 0, yearlyResetEnabled: true };

  ngOnInit(): void {
    this.leave.getLeaveTypes(true).subscribe((items) => {
      this.types.set(items);
      if (items.length) { this.selectedTypeId = items[0].id; this.load(); }
    });
  }
  load(): void {
    if (!this.selectedTypeId) return;
    this.leave.getPolicies(this.selectedTypeId, this.year).subscribe((items) => {
      const p = items[0] ?? null;
      this.policy.set(p);
      if (p) this.form = { skipWeekends: p.skipWeekends, skipHolidays: p.skipHolidays, defaultOpeningBalance: p.defaultOpeningBalance, carryForwardMaxDays: p.carryForwardMaxDays, yearlyResetEnabled: p.yearlyResetEnabled };
    });
  }
  save(): void {
    this.leave.upsertPolicy({ leaveTypeId: this.selectedTypeId, year: this.year, ...this.form, accrualStartDate: null }).subscribe(() => this.load());
  }
}
