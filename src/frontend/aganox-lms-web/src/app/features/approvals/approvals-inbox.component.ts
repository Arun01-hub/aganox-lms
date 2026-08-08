import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ApprovalService, PendingApproval } from '../../core/approval.service';
import { LocalizedDatePipe } from '../../core/localized-date.pipe';

@Component({
  selector: 'app-approvals-inbox',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe, LocalizedDatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">{{ 'approvals.inboxTitle' | translate }}</h1>
      <a routerLink="/approvals/history" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'approvals.history' | translate }}</a>
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
            <th>{{ 'leave.employee' | translate }}</th>
            <th>{{ 'approvals.leave' | translate }}</th>
            <th>{{ 'leave.dates' | translate }}</th>
            <th>{{ 'approvals.step' | translate }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          @for (item of items(); track item.leaveRequestId) {
            <tr>
              <td>{{ item.employeeName }}</td>
              <td>{{ item.leaveTypeName }} ({{ item.numberOfDays }}d)</td>
              <td>{{ item.fromDate | localizedDate }} – {{ item.toDate | localizedDate }}</td>
              <td><span class="badge text-bg-warning">{{ item.currentApproverRole }}</span></td>
              <td class="text-end">
                <a class="btn btn-sm btn-outline-primary me-1" [routerLink]="['/leave/requests', item.leaveRequestId]">{{ 'common.view' | translate }}</a>
                <button class="btn btn-sm btn-aganox me-1" type="button" (click)="openAction(item, 'approve')">{{ 'approvals.approve' | translate }}</button>
                <button class="btn btn-sm btn-outline-danger" type="button" (click)="openAction(item, 'reject')">{{ 'approvals.reject' | translate }}</button>
              </td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-muted text-center py-4">{{ 'approvals.noPending' | translate }}</td></tr>
          }
        </tbody>
      </table>
    </div>

    @if (activeItem()) {
      <div class="modal show d-block" tabindex="-1" style="background: rgba(0,0,0,.4)">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header">
              <h2 class="modal-title h5">{{ (action() === 'approve' ? 'approvals.approveLeave' : 'approvals.rejectLeave') | translate }}</h2>
              <button type="button" class="btn-close" (click)="closeAction()"></button>
            </div>
            <div class="modal-body">
              <p class="mb-2">{{ activeItem()!.employeeName }} — {{ activeItem()!.fromDate | localizedDate }} {{ 'common.to' | translate }} {{ activeItem()!.toDate | localizedDate }}</p>
              <label class="form-label">{{ action() === 'reject' ? ('common.commentsRequired' | translate) : ('common.comments' | translate) }}</label>
              <textarea class="form-control" rows="3" [(ngModel)]="comments"></textarea>
            </div>
            <div class="modal-footer">
              <button class="btn btn-outline-secondary" type="button" (click)="closeAction()">{{ 'common.cancel' | translate }}</button>
              <button class="btn btn-aganox" type="button" (click)="confirmAction()" [disabled]="busy()">
                {{ (action() === 'approve' ? 'approvals.approve' : 'approvals.reject') | translate }}
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
  private readonly translate = inject(TranslateService);

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
      error: () => this.error.set(this.translate.instant('approvals.loadFailed'))
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
      this.error.set(this.translate.instant('approvals.commentsRequiredReject'));
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
        this.message.set(this.action() === 'approve' ? this.translate.instant('approvals.approved') : this.translate.instant('approvals.rejected'));
        this.closeAction();
        this.load();
      },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('common.actionFailed'));
      }
    });
  }
}

@Component({
  selector: 'app-approvals-history',
  standalone: true,
  imports: [RouterLink, TranslatePipe, LocalizedDatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'approvals.historyTitle' | translate }}</h1>
      <a routerLink="/approvals" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'approvals.backToInbox' | translate }}</a>
    </div>

    <div class="card border-0 shadow-sm">
      <table class="table mb-0">
        <thead>
          <tr>
            <th>{{ 'leave.employee' | translate }}</th>
            <th>{{ 'approvals.leave' | translate }}</th>
            <th>{{ 'approvals.step' | translate }}</th>
            <th>{{ 'approvals.outcome' | translate }}</th>
            <th>{{ 'common.when' | translate }}</th>
          </tr>
        </thead>
        <tbody>
          @for (h of history(); track h.leaveRequestId + h.actionedAt) {
            <tr>
              <td>{{ h.employeeName }}</td>
              <td>{{ h.leaveTypeName }} ({{ h.fromDate | localizedDate }} – {{ h.toDate | localizedDate }})</td>
              <td>{{ h.approverRole }}</td>
              <td>{{ ('status.' + h.stepStatus) | translate }}</td>
              <td>{{ h.actionedAt ?? ('common.dash' | translate) }}</td>
            </tr>
          } @empty {
            <tr><td colspan="5" class="text-muted text-center py-4">{{ 'approvals.noHistory' | translate }}</td></tr>
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
