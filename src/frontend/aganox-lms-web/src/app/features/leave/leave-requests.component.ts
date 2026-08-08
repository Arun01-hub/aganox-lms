import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LeaveRequestService, LeaveRequestSummary } from '../../core/leave-request.service';
import { LocalizedDatePipe } from '../../core/localized-date.pipe';

@Component({
  selector: 'app-leave-requests',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe, LocalizedDatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">{{ 'leave.myRequestsTitle' | translate }}</h1>
      <a routerLink="/leave/apply" class="btn btn-sm btn-aganox ms-auto">{{ 'leave.applyForLeave' | translate }}</a>
    </div>

    <div class="card border-0 shadow-sm mb-3">
      <div class="card-body row g-2 align-items-end">
        <div class="col-md-3">
          <label class="form-label">{{ 'common.status' | translate }}</label>
          <select class="form-select" [(ngModel)]="statusFilter" (change)="load()">
            <option value="">{{ 'common.all' | translate }}</option>
            <option value="Draft">{{ 'status.Draft' | translate }}</option>
            <option value="Pending">{{ 'status.Pending' | translate }}</option>
            <option value="Approved">{{ 'status.Approved' | translate }}</option>
            <option value="Rejected">{{ 'status.Rejected' | translate }}</option>
            <option value="Cancelled">{{ 'status.Cancelled' | translate }}</option>
          </select>
        </div>
        <div class="col-md-2">
          <button class="btn btn-outline-secondary w-100" type="button" (click)="load()">{{ 'common.refresh' | translate }}</button>
        </div>
      </div>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    <div class="card border-0 shadow-sm">
      <table class="table mb-0 align-middle">
        <thead>
          <tr>
            <th>{{ 'common.type' | translate }}</th>
            <th>{{ 'leave.dates' | translate }}</th>
            <th>{{ 'common.days' | translate }}</th>
            <th>{{ 'common.status' | translate }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (r of items(); track r.id) {
            <tr>
              <td>{{ r.leaveTypeName }}</td>
              <td>{{ r.fromDate | localizedDate }} – {{ r.toDate | localizedDate }}</td>
              <td>{{ r.numberOfDays }}</td>
              <td><span class="badge text-bg-secondary">{{ ('status.' + r.status) | translate }}</span></td>
              <td class="text-end">
                <a class="btn btn-sm btn-outline-primary" [routerLink]="['/leave/requests', r.id]">{{ 'common.view' | translate }}</a>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-muted text-center py-4">{{ 'leave.noRequests' | translate }}</td></tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class LeaveRequestsComponent implements OnInit {
  private readonly leaveRequests = inject(LeaveRequestService);
  private readonly translate = inject(TranslateService);
  readonly items = signal<LeaveRequestSummary[]>([]);
  readonly error = signal('');
  statusFilter = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.error.set('');
    this.leaveRequests.list({ status: this.statusFilter || undefined, pageSize: 50 }).subscribe({
      next: (page) => this.items.set(page.items),
      error: (err) => this.error.set(err.error?.errors?.[0] ?? this.translate.instant('leave.loadFailed'))
    });
  }
}
