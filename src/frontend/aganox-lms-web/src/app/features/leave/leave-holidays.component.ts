import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Holiday, LeaveCalendar, LeaveConfigService } from '../../core/leave-config.service';

@Component({
  selector: 'app-leave-holidays',
  standalone: true,
  imports: [RouterLink, FormsModule],
  template: `
    <div class="d-flex align-items-center mb-4 gap-2 flex-wrap">
      <h1 class="h4 mb-0">Calendars & holidays</h1>
      <a routerLink="/leave/types" class="btn btn-sm btn-outline-secondary ms-auto">Leave types</a>
    </div>

    <div class="row g-3">
      <div class="col-lg-4">
        <div class="card border-0 shadow-sm">
          <div class="card-body">
            <h2 class="h6">Calendars</h2>
            <ul class="list-group list-group-flush">
              @for (c of calendars(); track c.id) {
                <li class="list-group-item px-0 d-flex justify-content-between align-items-center">
                  <button class="btn btn-link p-0" type="button" (click)="selectCalendar(c.id)">{{ c.name }}</button>
                  @if (c.isDefault) { <span class="badge text-bg-primary">Default</span> }
                </li>
              }
            </ul>
            <div class="mt-3 d-flex gap-2">
              <input class="form-control form-control-sm" [(ngModel)]="newCalendarName" placeholder="New calendar" />
              <button class="btn btn-sm btn-aganox" type="button" (click)="addCalendar()">Add</button>
            </div>
          </div>
        </div>
      </div>
      <div class="col-lg-8">
        <div class="card border-0 shadow-sm mb-3">
          <div class="card-body row g-2 align-items-end">
            <div class="col-md-3"><label class="form-label">Year</label><input class="form-control" type="number" [(ngModel)]="year" (change)="loadHolidays()" /></div>
            <div class="col-md-4"><label class="form-label">Date</label><input class="form-control" type="date" [(ngModel)]="holidayForm.date" /></div>
            <div class="col-md-3"><label class="form-label">Name</label><input class="form-control" [(ngModel)]="holidayForm.name" /></div>
            <div class="col-md-2"><button class="btn btn-aganox w-100" type="button" (click)="addHoliday()" [disabled]="!selectedCalendarId">Add</button></div>
          </div>
        </div>
        <div class="card border-0 shadow-sm">
          <table class="table mb-0">
            <thead><tr><th>Date</th><th>Name</th><th></th></tr></thead>
            <tbody>
              @for (h of holidays(); track h.id) {
                <tr>
                  <td>{{ h.holidayDate }}</td>
                  <td>{{ h.name }}</td>
                  <td class="text-end"><button class="btn btn-sm btn-outline-danger" type="button" (click)="removeHoliday(h)">Delete</button></td>
                </tr>
              } @empty {
                <tr><td colspan="3" class="text-muted text-center py-3">No holidays for this year.</td></tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    </div>
  `
})
export class LeaveHolidaysComponent implements OnInit {
  private readonly leave = inject(LeaveConfigService);
  readonly calendars = signal<LeaveCalendar[]>([]);
  readonly holidays = signal<Holiday[]>([]);
  selectedCalendarId = '';
  year = new Date().getFullYear();
  newCalendarName = '';
  holidayForm = { date: '', name: '' };

  ngOnInit(): void { this.loadCalendars(); }

  loadCalendars(): void {
    this.leave.getCalendars().subscribe((items) => {
      this.calendars.set(items);
      if (!this.selectedCalendarId && items.length) {
        this.selectedCalendarId = items.find((c) => c.isDefault)?.id ?? items[0].id;
        this.loadHolidays();
      }
    });
  }

  selectCalendar(id: string): void { this.selectedCalendarId = id; this.loadHolidays(); }

  loadHolidays(): void {
    if (!this.selectedCalendarId) return;
    this.leave.getHolidays(this.selectedCalendarId, this.year).subscribe((items) => this.holidays.set(items));
  }

  addCalendar(): void {
    if (!this.newCalendarName.trim()) return;
    this.leave.createCalendar({ name: this.newCalendarName.trim(), isDefault: false }).subscribe(() => {
      this.newCalendarName = '';
      this.loadCalendars();
    });
  }

  addHoliday(): void {
    if (!this.selectedCalendarId || !this.holidayForm.date || !this.holidayForm.name.trim()) return;
    this.leave.createHoliday({
      leaveCalendarId: this.selectedCalendarId,
      holidayDate: this.holidayForm.date,
      name: this.holidayForm.name.trim(),
      isOptional: false
    }).subscribe(() => {
      this.holidayForm = { date: '', name: '' };
      this.loadHolidays();
    });
  }

  removeHoliday(h: Holiday): void {
    this.leave.deleteHoliday(h.id).subscribe(() => this.loadHolidays());
  }
}

@Component({
  selector: 'app-leave-preview',
  standalone: true,
  imports: [FormsModule],
  template: `
    <h1 class="h4 mb-4">Leave day preview</h1>
    <div class="card border-0 shadow-sm">
      <div class="card-body row g-3">
        <div class="col-md-3"><label class="form-label">Leave type</label>
          <select class="form-select" [(ngModel)]="leaveTypeId">@for (t of types(); track t.id) { <option [value]="t.id">{{ t.name }}</option> }</select>
        </div>
        <div class="col-md-2"><label class="form-label">From</label><input class="form-control" type="date" [(ngModel)]="fromDate" /></div>
        <div class="col-md-2"><label class="form-label">To</label><input class="form-control" type="date" [(ngModel)]="toDate" /></div>
        <div class="col-md-2 d-flex align-items-end"><div class="form-check"><input type="checkbox" class="form-check-input" [(ngModel)]="isHalfDay" id="hd" /><label class="form-check-label" for="hd">Half day</label></div></div>
        <div class="col-md-3 d-flex align-items-end"><button class="btn btn-aganox" type="button" (click)="preview()">Calculate</button></div>
      </div>
      @if (result()) {
        <div class="card-footer">
          <strong>{{ result()!.numberOfDays }} day(s)</strong>
          <span class="text-muted ms-2">Weekends skipped: {{ result()!.skippedWeekends }}, Holidays skipped: {{ result()!.skippedHolidays }}</span>
        </div>
      }
    </div>
  `
})
export class LeavePreviewComponent implements OnInit {
  private readonly leave = inject(LeaveConfigService);
  readonly types = signal<{ id: string; name: string }[]>([]);
  readonly result = signal<{ numberOfDays: number; skippedWeekends: number; skippedHolidays: number } | null>(null);
  leaveTypeId = '';
  fromDate = '';
  toDate = '';
  isHalfDay = false;

  ngOnInit(): void {
    this.leave.getLeaveTypes(true).subscribe((items) => {
      this.types.set(items);
      if (items.length) this.leaveTypeId = items[0].id;
    });
  }

  preview(): void {
    this.leave.previewDays({ leaveTypeId: this.leaveTypeId, fromDate: this.fromDate, toDate: this.toDate, isHalfDay: this.isHalfDay }).subscribe((r) => this.result.set(r));
  }
}
