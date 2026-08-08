import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LeaveRequestDetail, LeaveRequestService } from '../../core/leave-request.service';
import { LocalizedDatePipe } from '../../core/localized-date.pipe';

@Component({
  selector: 'app-leave-request-detail',
  standalone: true,
  imports: [RouterLink, TranslatePipe, LocalizedDatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'leave.detailTitle' | translate }}</h1>
      <a routerLink="/leave/requests" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'common.backToList' | translate }}</a>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    @if (item()) {
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <dl class="row mb-0">
            <dt class="col-sm-3">{{ 'leave.employee' | translate }}</dt><dd class="col-sm-9">{{ item()!.employeeName }}</dd>
            <dt class="col-sm-3">{{ 'leave.leaveType' | translate }}</dt><dd class="col-sm-9">{{ item()!.leaveTypeName }}</dd>
            <dt class="col-sm-3">{{ 'leave.dates' | translate }}</dt><dd class="col-sm-9">{{ item()!.fromDate | localizedDate }} – {{ item()!.toDate | localizedDate }}</dd>
            <dt class="col-sm-3">{{ 'leave.duration' | translate }}</dt><dd class="col-sm-9">{{ 'leave.dayCount' | translate:{ count: item()!.numberOfDays } }}</dd>
            <dt class="col-sm-3">{{ 'common.status' | translate }}</dt><dd class="col-sm-9"><span class="badge text-bg-secondary">{{ ('status.' + item()!.status) | translate }}</span></dd>
            @if (item()!.remarks) {
              <dt class="col-sm-3">{{ 'leave.remarks' | translate }}</dt><dd class="col-sm-9">{{ item()!.remarks }}</dd>
            }
          </dl>

          @if (item()!.approvalSteps.length) {
            <h2 class="h6 mt-4">{{ 'leave.approvalHistory' | translate }}</h2>
            <ul class="list-group list-group-flush">
              @for (step of item()!.approvalSteps; track step.id) {
                <li class="list-group-item px-0 d-flex justify-content-between">
                  <span>{{ step.approverRole }} — {{ step.approverName ?? ('common.unassigned' | translate) }}</span>
                  <span class="badge text-bg-secondary">{{ ('status.' + step.status) | translate }}</span>
                </li>
              }
            </ul>
          }

          @if (item()!.comments.length) {
            <h2 class="h6 mt-4">{{ 'leave.commentsSection' | translate }}</h2>
            @for (c of item()!.comments; track c.id) {
              <div class="border rounded p-2 mb-2">
                <div class="small text-muted">{{ c.authorName }} · {{ c.createdAt }}</div>
                <div>{{ c.comment }}</div>
              </div>
            }
          }
        </div>
        @if (item()!.status === 'Draft' || item()!.status === 'Pending') {
          <div class="card-footer d-flex gap-2">
            @if (item()!.status === 'Draft') {
              <button class="btn btn-aganox btn-sm" type="button" (click)="submit()" [disabled]="busy()">{{ 'leave.submit' | translate }}</button>
            }
            <button class="btn btn-outline-danger btn-sm" type="button" (click)="cancel()" [disabled]="busy()">{{ 'leave.cancelRequest' | translate }}</button>
          </div>
        }
      </div>
    }
  `
})
export class LeaveRequestDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly leaveRequests = inject(LeaveRequestService);
  private readonly translate = inject(TranslateService);

  readonly item = signal<LeaveRequestDetail | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) return;
    this.load(id);
  }

  load(id: string): void {
    this.leaveRequests.get(id).subscribe({
      next: (detail) => this.item.set(detail),
      error: () => this.error.set(this.translate.instant('leave.notFound'))
    });
  }

  submit(): void {
    const id = this.item()?.id;
    if (!id) return;
    this.busy.set(true);
    this.error.set('');
    this.leaveRequests.submit(id).subscribe({
      next: (detail) => { this.item.set(detail); this.busy.set(false); },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('leave.submitFailed'));
      }
    });
  }

  cancel(): void {
    const id = this.item()?.id;
    if (!id) return;
    this.busy.set(true);
    this.error.set('');
    this.leaveRequests.cancel(id).subscribe({
      next: (detail) => { this.item.set(detail); this.busy.set(false); },
      error: (err) => {
        this.busy.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('leave.cancelFailed'));
      }
    });
  }
}
