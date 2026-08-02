# ER Diagram — Aganox LMS

**Version:** 1.0  
**Companion:** [02-database-design.md](./02-database-design.md)

---

## 1. Conceptual model

```mermaid
flowchart TB
  subgraph Org
    Dept[Department]
    Emp[Employee]
    Cal[Leave Calendar]
    Hol[Holiday]
  end

  subgraph Config
    LT[Leave Type]
    Pol[Leave Policy]
  end

  subgraph Runtime
    LB[Leave Balance]
    Adj[Balance Adjustment]
    LR[Leave Request]
    Step[Approval Step]
    Cmt[Request Comment]
  end

  subgraph Security
    User[User]
    Role[Role]
    Perm[Permission]
  end

  subgraph CrossCut
    Ntf[Notification]
    Aud[Audit Log]
  end

  Dept --> Emp
  Emp -->|manager| Emp
  Emp --> User
  Cal --> Emp
  Cal --> Hol
  LT --> Pol
  Emp --> LB
  LT --> LB
  LB --> Adj
  Emp --> LR
  LT --> LR
  LR --> Step
  LR --> Cmt
  User --> Role
  Role --> Perm
  LR --> Ntf
  User --> Aud
```

---

## 2. Logical ER (normalized)

```mermaid
erDiagram
  DEPARTMENTS ||--o{ EMPLOYEES : "has"
  EMPLOYEES ||--o{ EMPLOYEES : "manages"
  LEAVE_CALENDARS ||--o{ EMPLOYEES : "assigned"
  LEAVE_CALENDARS ||--o{ HOLIDAYS : "includes"
  USERS ||--o| EMPLOYEES : "linked"
  USERS ||--o{ USER_ROLES : ""
  ROLES ||--o{ USER_ROLES : ""
  ROLES ||--o{ ROLE_PERMISSIONS : ""
  PERMISSIONS ||--o{ ROLE_PERMISSIONS : ""

  LEAVE_TYPES ||--o{ LEAVE_POLICIES : "yearly"
  LEAVE_TYPES ||--o{ LEAVE_BALANCES : ""
  LEAVE_TYPES ||--o{ LEAVE_REQUESTS : ""
  EMPLOYEES ||--o{ LEAVE_BALANCES : ""
  EMPLOYEES ||--o{ LEAVE_REQUESTS : ""
  LEAVE_BALANCES ||--o{ LEAVE_BALANCE_ADJUSTMENTS : ""
  LEAVE_REQUESTS ||--o{ LEAVE_APPROVAL_STEPS : ""
  LEAVE_REQUESTS ||--o{ LEAVE_REQUEST_COMMENTS : ""
  LEAVE_REQUESTS ||--o{ NOTIFICATIONS : ""
  USERS ||--o{ REFRESH_TOKENS : ""
  USERS ||--o{ AUDIT_LOGS : ""
  USERS ||--o{ ACTIVITY_LOGS : ""
```

---

## 3. Entity cardinality cheat-sheet

| Entity A | Relation | Entity B |
|----------|----------|----------|
| Department | 1:N | Employee |
| Employee | 1:N | Employee (direct reports) |
| Employee | 1:0..1 | User |
| LeaveCalendar | 1:N | Holiday |
| LeaveType | 1:N | LeavePolicy (by year) |
| Employee + LeaveType + Year | 1:1 | LeaveBalance |
| LeaveBalance | 1:N | LeaveBalanceAdjustment |
| Employee | 1:N | LeaveRequest |
| LeaveRequest | 1:N | LeaveApprovalStep |
| Role | N:N | Permission |

---

## 4. Status state machine (LeaveRequest)

```mermaid
stateDiagram-v2
  [*] --> Draft
  Draft --> Pending: Submit
  Draft --> Cancelled: Cancel
  Pending --> Approved: Final approver OK
  Pending --> Rejected: Any step Reject
  Pending --> Cancelled: Cancel
  Pending --> Pending: Edit after return / re-submit
  Approved --> Cancelled: Cancel with auth
  Approved --> Pending: Material edit → re-approval
  Rejected --> Draft: Revise
  Rejected --> [*]
  Cancelled --> [*]
```

---

## 5. Approval chain (default)

```mermaid
sequenceDiagram
  participant E as Employee
  participant S as System
  participant M as Manager
  participant H as HR

  E->>S: Submit leave
  S->>S: Calc days, overlap, balance check
  S->>S: Create steps Manager→HR
  S->>M: Notify pending
  M->>S: Approve + comment
  S->>H: Notify pending
  H->>S: Approve + comment
  S->>S: Status=Approved; deduct balance
  S->>E: Notify approved
```

---

## Approval checkpoint

Approve ER and leave state machine; changes after coding start require migrations.
