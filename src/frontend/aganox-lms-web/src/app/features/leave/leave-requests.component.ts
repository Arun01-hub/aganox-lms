import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { LeaveRequestService, LeaveRequestSummary } from '../../core/leave-request.service';

@Component({
  selector: 'app-leave-requests',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">My leave requests</h1>
      <a routerLink="/leave/apply" class="btn btn-sm btn-aganox ms-auto">Apply for leave</a>
    </div>

    <div class="card border-0 shadow-sm mb-3">
      <div class="card-body row g-2 align-items-end">
        <div class="col-md-3">
          <label class="form-label">Status</label>
          <select class="form-select" [(ngModel)]="statusFilter" (change)="load()">
            <option value="">All</option>
            <option value="Draft">Draft</option>
            <option value="Pending">Pending</option>
            <option value="Approved">Approved</option>
            <option value="Rejected">Rejected</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
        <div class="col-md-2">
          <button class="btn btn-outline-secondary w-100" type="button" (click)="load()">Refresh</button>
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
            <th>Type</th>
            <th>Dates</th>
            <th>Days</th>
            <th>Status</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (r of items(); track r.id) {
            <tr>
              <td>{{ r.leaveTypeName }}</td>
              <td>{{ r.fromDate }} – {{ r.toDate }}</td>
              <td>{{ r.numberOfDays }}</td>
              <td><span class="badge text-bg-secondary">{{ r.status }}</span></td>
              <td class="text-end">
                <a class="btn btn-sm btn-outline-primary" [routerLink]="['/leave/requests', r.id]">View</a>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-muted text-center py-4">No leave requests yet.</td></tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class LeaveRequestsComponent implements OnInit {
  private readonly leaveRequests = inject(LeaveRequestService);
  readonly items = signal<LeaveRequestSummary[]>([]);
  readonly error = signal('');
  statusFilter = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.error.set('');
    this.leaveRequests.list({ status: this.statusFilter || undefined, pageSize: 50 }).subscribe({
      next: (page) => this.items.set(page.items),
      error: (err) => this.error.set(err.error?.errors?.[0] ?? 'Failed to load leave requests.')
    });
  }
}
