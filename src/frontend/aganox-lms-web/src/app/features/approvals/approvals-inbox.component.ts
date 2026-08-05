import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ApprovalService, PendingApproval } from '../../core/approval.service';

@Component({
  selector: 'app-approvals-inbox',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">Approvals inbox</h1>
      <a routerLink="/approvals/history" class="btn btn-sm btn-outline-secondary ms-auto">History</a>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }
    @if (message()) {
      <div class="alert alert-success">{{ message() }}</div>
    }

    <div class="card border-0 shadow-sm">
      <table class="table mb-0 align-middle">
        <thead>
          <tr>
            <th>Employee</th>
            <th>Leave</th>
            <th>Dates</th>
            <th>Step</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (item of items(); track item.leaveRequestId) {
            <tr>
              <td>{{ item.employeeName }}</td>
              <td>{{ item.leaveTypeName }} ({{ item.numberOfDays }}d)</td>
              <td>{{ item.fromDate }} – {{ item.toDate }}</td>
              <td><span class="badge text-bg-warning">{{ item.currentApproverRole }}</span></td>
              <td class="text-end">
                <a class="btn btn-sm btn-outline-primary me-1" [routerLink]="['/leave/requests', item.leaveRequestId]">View</a>
                <button class="btn btn-sm btn-aganox me-1" type="button" (click)="openAction(item, 'approve')">Approve</button>
                <button class="btn btn-sm btn-outline-danger" type="button" (click)="openAction(item, 'reject')">Reject</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-muted text-center py-4">No pending approvals.</td></tr>
          }
        </tbody>
      </table>
    </div>

    @if (activeItem()) {
      <div class="modal show d-block" tabindex="-1" style="background: rgba(0,0,0,.4)">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h2 class="modal-title h5">{{ action() === 'approve' ? 'Approve' : 'Reject' }} leave</h2>
              <button type="button" class="btn-close" (click)="closeAction()"></button>
            </div>
            <div class="modal-body">
              <p class="mb-2">{{ activeItem()!.employeeName }} — {{ activeItem()!.fromDate }} to {{ activeItem()!.toDate }}</p>
              <label class="form-label">Comments{{ action() === 'reject' ? ' (required)' : '' }}</label>
              <textarea class="form-control" rows="3" [(ngModel)]="comments"></textarea>
            </div>
            <div class="modal-footer">
              <button class="btn btn-outline-secondary" type="button" (click)="closeAction()">Cancel</button>
              <button class="btn btn-aganox" type="button" (click)="confirmAction()" [disabled]="busy()">
                {{ action() === 'approve' ? 'Approve' : 'Reject' }}
              </button>
            </div>
          </div>
        </div>
      </div>
    }
  `
})
export class ApprovalsInboxComponent implements OnInit {
  private readonly approvals = inject(ApprovalService);

  readonly items = signal<PendingApproval[]>([]);
  readonly error = signal('');
  readonly message = signal('');
  readonly activeItem = signal<PendingApproval | null>(null);
  readonly action = signal<'approve' | 'reject'>('approve');
  readonly busy = signal(false);
  comments = '';

  ngOnInit(): void { this.load(); }

  load(): void {
    this.error.set('');
    this.approvals.getPending().subscribe({
      next: (page) => this.items.set(page.items),
      error: () => this.error.set('Failed to load pending approvals.')
    });
  }

  openAction(item: PendingApproval, action: 'approve' | 'reject'): void {
    this.activeItem.set(item);
    this.action.set(action);
    this.comments = '';
  }

  closeAction(): void {
    this.activeItem.set(null);
    this.comments = '';
  }

  confirmAction(): void {
    const item = this.activeItem();
    if (!item) return;
    if (this.action() === 'reject' && !this.comments.trim()) {
      this.error.set('Comments are required to reject.');
      return;
    }

    this.busy.set(true);
    this.error.set('');
    const req = this.action() === 'approve'
      ? this.approvals.approve(item.leaveRequestId, this.comments.trim() || null)
      : this.approvals.reject(item.leaveRequestId, this.comments.trim());

    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.message.set(this.action() === 'approve' ? 'Leave approved.' : 'Leave rejected.');
        this.closeAction();
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? 'Action failed.');
      }
    });
  }
}

@Component({
  selector: 'app-approvals-history',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">Approval history</h1>
      <a routerLink="/approvals" class="btn btn-sm btn-outline-secondary ms-auto">Back to inbox</a>
    </div>

    <div class="card border-0 shadow-sm">
      <table class="table mb-0">
        <thead>
          <tr>
            <th>Employee</th>
            <th>Leave</th>
            <th>Step</th>
            <th>Outcome</th>
            <th>When</th>
          </tr>
        </thead>
        <tbody>
          @for (h of history(); track h.leaveRequestId + h.actionedAt) {
            <tr>
              <td>{{ h.employeeName }}</td>
              <td>{{ h.leaveTypeName }} ({{ h.fromDate }} – {{ h.toDate }})</td>
              <td>{{ h.approverRole }}</td>
              <td>{{ h.stepStatus }}</td>
              <td>{{ h.actionedAt ?? '—' }}</td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-muted text-center py-4">No approval history yet.</td></tr>
          }
        </tbody>
      </table>
    </div>
  `
})
export class ApprovalsHistoryComponent implements OnInit {
  private readonly approvals = inject(ApprovalService);
  readonly history = signal<import('../../core/approval.service').ApprovalHistoryItem[]>([]);

  ngOnInit(): void {
    this.approvals.getHistory().subscribe((page) => this.history.set(page.items));
  }
}
