import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LeaveConfigService, LeaveType } from '../../core/leave-config.service';
import { LeaveRequestService } from '../../core/leave-request.service';

@Component({
  selector: 'app-leave-request-form',
  standalone: true,
  imports: [RouterLink, FormsModule, TranslatePipe],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2">
      <h1 class="h4 mb-0">{{ 'leave.applyTitle' | translate }}</h1>
      <a routerLink="/leave/requests" class="btn btn-sm btn-outline-secondary ms-auto">{{ 'common.backToList' | translate }}</a>
    </div>

    @if (error()) {
      <div class="alert alert-danger">{{ error() }}</div>
    }

    <div class="card border-0 shadow-sm">
      <div class="card-body row g-3">
        <div class="col-md-4">
          <label class="form-label">{{ 'leave.leaveType' | translate }}</label>
          <select class="form-select" [(ngModel)]="form.leaveTypeId">
            @for (t of types(); track t.id) {
              <option [value]="t.id">{{ t.name }}</option>
            }
          </select>
        </div>
        <div class="col-md-3">
          <label class="form-label">{{ 'common.from' | translate }}</label>
          <input class="form-control" type="date" [(ngModel)]="form.fromDate" (change)="preview()" />
        </div>
        <div class="col-md-3">
          <label class="form-label">{{ 'common.to' | translate }}</label>
          <input class="form-control" type="date" [(ngModel)]="form.toDate" (change)="preview()" />
        </div>
        <div class="col-md-2 d-flex align-items-end">
          <div class="form-check">
            <input type="checkbox" class="form-check-input" [(ngModel)]="form.isHalfDay" id="halfDay" (change)="preview()" />
            <label class="form-check-label" for="halfDay">{{ 'leave.halfDayLabel' | translate }}</label>
          </div>
        </div>
        @if (form.isHalfDay) {
          <div class="col-md-3">
            <label class="form-label">{{ 'leave.halfDayPeriod' | translate }}</label>
            <select class="form-select" [(ngModel)]="form.halfDayPeriod">
              <option value="FirstHalf">{{ 'halfDay.FirstHalf' | translate }}</option>
              <option value="SecondHalf">{{ 'halfDay.SecondHalf' | translate }}</option>
            </select>
          </div>
        }
        <div class="col-12">
          <label class="form-label">{{ 'leave.remarks' | translate }}</label>
          <textarea class="form-control" rows="2" [(ngModel)]="form.remarks"></textarea>
        </div>
        @if (previewDays() !== null) {
          <div class="col-12">
            <span class="text-muted">{{ 'leave.calculatedDuration' | translate }} <strong>{{ 'leave.daysCount' | translate:{ count: previewDays() } }}</strong></span>
          </div>
        }
        <div class="col-12 d-flex gap-2">
          <button class="btn btn-outline-secondary" type="button" (click)="save(false)" [disabled]="saving()">{{ 'leave.saveDraft' | translate }}</button>
          <button class="btn btn-aganox" type="button" (click)="save(true)" [disabled]="saving()">{{ 'leave.submitForApproval' | translate }}</button>
        </div>
      </div>
    </div>
  `
})
export class LeaveRequestFormComponent implements OnInit {
  private readonly leaveConfig = inject(LeaveConfigService);
  private readonly leaveRequests = inject(LeaveRequestService);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);

  readonly types = signal<LeaveType[]>([]);
  readonly error = signal('');
  readonly saving = signal(false);
  readonly previewDays = signal<number | null>(null);

  form = {
    leaveTypeId: '',
    fromDate: '',
    toDate: '',
    isHalfDay: false,
    halfDayPeriod: 'FirstHalf',
    remarks: ''
  };

  ngOnInit(): void {
    this.leaveConfig.getLeaveTypes(true).subscribe((items) => {
      this.types.set(items);
      if (items.length) this.form.leaveTypeId = items[0].id;
    });
  }

  preview(): void {
    if (!this.form.leaveTypeId || !this.form.fromDate || !this.form.toDate) {
      this.previewDays.set(null);
      return;
    }
    this.leaveConfig.previewDays({
      leaveTypeId: this.form.leaveTypeId,
      fromDate: this.form.fromDate,
      toDate: this.form.toDate,
      isHalfDay: this.form.isHalfDay
    }).subscribe({
      next: (r) => this.previewDays.set(r.numberOfDays),
      error: () => this.previewDays.set(null)
    });
  }

  save(submit: boolean): void {
    this.error.set('');
    this.saving.set(true);
    this.leaveRequests.create({
      leaveTypeId: this.form.leaveTypeId,
      fromDate: this.form.fromDate,
      toDate: this.form.toDate,
      isHalfDay: this.form.isHalfDay,
      halfDayPeriod: this.form.isHalfDay ? this.form.halfDayPeriod : null,
      remarks: this.form.remarks.trim() || null,
      submit
    }).subscribe({
      next: (created) => {
        this.saving.set(false);
        void this.router.navigate(['/leave/requests', created.id]);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.errors?.[0] ?? this.translate.instant('leave.saveFailed'));
      }
    });
  }
}
