import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/password-flow.component').then((m) => m.ForgotPasswordComponent)
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/password-flow.component').then((m) => m.ResetPasswordComponent)
  },
  {
    path: 'change-password',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/auth/password-flow.component').then((m) => m.ChangePasswordComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent
          )
      },
      {
        path: 'departments',
        loadComponent: () =>
          import('./features/organization/departments.component').then((m) => m.DepartmentsComponent)
      },
      {
        path: 'employees',
        loadComponent: () =>
          import('./features/organization/employees.component').then((m) => m.EmployeesComponent)
      },
      {
        path: 'employees/new',
        loadComponent: () =>
          import('./features/organization/employee-form.component').then((m) => m.EmployeeFormComponent)
      },
      {
        path: 'employees/:id/edit',
        loadComponent: () =>
          import('./features/organization/employee-form.component').then((m) => m.EmployeeFormComponent)
      },
      {
        path: 'employees/:id',
        loadComponent: () =>
          import('./features/organization/employee-profile.component').then((m) => m.EmployeeProfileComponent)
      },
      {
        path: 'leave/types',
        loadComponent: () =>
          import('./features/leave/leave-types.component').then((m) => m.LeaveTypesComponent)
      },
      {
        path: 'leave/policies',
        loadComponent: () =>
          import('./features/leave/leave-types.component').then((m) => m.LeavePoliciesComponent)
      },
      {
        path: 'leave/holidays',
        loadComponent: () =>
          import('./features/leave/leave-holidays.component').then((m) => m.LeaveHolidaysComponent)
      },
      {
        path: 'leave/preview',
        loadComponent: () =>
          import('./features/leave/leave-holidays.component').then((m) => m.LeavePreviewComponent)
      },
      {
        path: 'leave/requests',
        loadComponent: () =>
          import('./features/leave/leave-requests.component').then((m) => m.LeaveRequestsComponent)
      },
      {
        path: 'leave/apply',
        loadComponent: () =>
          import('./features/leave/leave-request-form.component').then((m) => m.LeaveRequestFormComponent)
      },
      {
        path: 'leave/requests/:id',
        loadComponent: () =>
          import('./features/leave/leave-request-detail.component').then((m) => m.LeaveRequestDetailComponent)
      },
      {
        path: 'approvals',
        loadComponent: () =>
          import('./features/approvals/approvals-inbox.component').then((m) => m.ApprovalsInboxComponent)
      },
      {
        path: 'approvals/history',
        loadComponent: () =>
          import('./features/approvals/approvals-inbox.component').then((m) => m.ApprovalsHistoryComponent)
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports.component').then((m) => m.ReportsComponent)
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/profile/user-profile.component').then((m) => m.UserProfileComponent)
      },
      {
        path: 'attendance',
        loadComponent: () =>
          import('./features/attendance/attendance.component').then((m) => m.AttendanceComponent)
      },
      {
        path: 'payroll/components',
        loadComponent: () =>
          import('./features/payroll/payroll-config.component').then((m) => m.PayrollConfigComponent)
      },
      {
        path: 'payroll/salaries',
        loadComponent: () =>
          import('./features/payroll/payroll-salaries.component').then((m) => m.PayrollSalariesComponent)
      },
      {
        path: 'payroll/runs',
        loadComponent: () =>
          import('./features/payroll/payroll-runs.component').then((m) => m.PayrollRunsComponent)
      },
      {
        path: 'payroll/salary-entry',
        loadComponent: () =>
          import('./features/payroll/salary-entry.component').then((m) => m.SalaryEntryComponent)
      },
      {
        path: 'payroll/payslips/:runId',
        loadComponent: () =>
          import('./features/payroll/payroll-payslips.component').then((m) => m.PayrollPayslipsComponent)
      },
      {
        path: 'payroll/reports',
        loadComponent: () =>
          import('./features/payroll/payroll-reports.component').then((m) => m.PayrollReportsComponent)
      },
      {
        path: 'payroll/arrears',
        loadComponent: () =>
          import('./features/payroll/payroll-arrears.component').then((m) => m.PayrollArrearsComponent)
      },
      {
        path: 'admin',
        loadComponent: () =>
          import('./features/admin/admin.component').then((m) => m.AdminComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
