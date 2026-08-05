import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LeaveRequestDetail, LeaveRequestService } from '../../core/leave-request.service';

@Component({
  selector: 'app-leave-request-detail',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">Leave request</h1>
      <a routerLink="/leave/requests" class="btn btn-sm btn-outline-secondary ms-auto">Back to list</a>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    @if (item()) {
      <div class="card border-0 shadow-sm">
        <div class="card-body">
          <dl class="row mb-0">
            <dt class="col-sm-3">Employee</dt><dd class="col-sm-9">{{ item()!.employeeName }}</dd>
            <dt class="col-sm-3">Leave type</dt><dd class="col-sm-9">{{ item()!.leaveTypeName }}</dd>
            <dt class="col-sm-3">Dates</dt><dd class="col-sm-9">{{ item()!.fromDate }} – {{ item()!.toDate }}</dd>
            <dt class="col-sm-3">Duration</dt><dd class="col-sm-9">{{ item()!.numberOfDays }} day(s)</dd>
            <dt class="col-sm-3">Status</dt><dd class="col-sm-9"><span class="badge text-bg-secondary">{{ item()!.status }}</span></dd>
            @if (item()!.remarks) {
              <dt class="col-sm-3">Remarks</dt><dd class="col-sm-9">{{ item()!.remarks }}</dd>
            }
          </dl>

          @if (item()!.approvalSteps?.length) {
            <h2 class="h6 mt-4">Approval history</h2>
            <ul class="list-group list-group-flush">
              @for (step of item()!.approvalSteps; track step.id) {
                <li class="list-group-item px-0 d-flex justify-content-between">
                  <span>{{ step.approverRole }} — {{ step.approverName ?? 'Unassigned' }}</span>
                  <span class="badge text-bg-secondary">{{ step.status }}</span>
                </li>
              }
            </ul>
          }

          @if (item()!.comments?.length) {
            <h2 class="h6 mt-4">Comments</h2>
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
              <button class="btn btn-aganox btn-sm" type="button" (click)="submit()" [disabled]="busy()">Submit</button>
            }
            <button class="btn btn-outline-danger btn-sm" type="button" (click)="cancel()" [disabled]="busy()">Cancel request</button>
          </div>
        }
      </div>
    }
  `
})
export class LeaveRequestDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly leaveRequests = inject(LeaveRequestService);

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
      error: () => this.error.set('Leave request not found.')
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
        this.error.set(err.error?.errors?.[0] ?? 'Submit failed.');
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
        this.error.set(err.error?.errors?.[0] ?? 'Cancel failed.');
      }
    });
  }
}
