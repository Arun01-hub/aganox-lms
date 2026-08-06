# Future Enhancements — Aganox LMS

**Version:** 1.0  
**Status:** Roadmap / not implemented in v1

This document captures planned extensions beyond the initial Windows SME release.

---

## Authentication & identity

| Enhancement | Description |
|-------------|-------------|
| **Active Directory / LDAP** | Sign-in with domain accounts; group → role mapping |
| **Azure AD / Entra ID (SSO)** | OpenID Connect for cloud-first orgs |
| **Multi-factor authentication (MFA)** | TOTP or SMS second factor |
| **SAML 2.0** | Enterprise IdP integration |

---

## Multi-tenancy & scale

| Enhancement | Description |
|-------------|-------------|
| **Multi-tenant SaaS** | Tenant id on all entities; subdomain routing |
| **Horizontal API scale** | Stateless API behind load balancer; shared SQL |
| **Read replicas** | Reporting queries off secondary DB |
| **Redis cache** | Session, permission, and dashboard caching |

---

## Database & cloud

| Enhancement | Description |
|-------------|-------------|
| **PostgreSQL provider** | EF Core Npgsql for cross-platform DB |
| **Azure SQL** | Managed SQL with geo-redundancy |
| **AWS RDS SQL Server** | Managed instance deployment |
| **Kubernetes (AKS/EKS)** | Containerized API + Helm chart |

Current v1 targets **SQL Server** (LocalDB/Express/full) on **Windows**.

---

## Notifications & integrations

| Enhancement | Description |
|-------------|-------------|
| **SMTP / SendGrid / Graph mail** | Production email delivery |
| **Microsoft Teams / Slack** | Approval notifications and bots |
| **Calendar sync** | Outlook/Google calendar blocks for approved leave |
| **Webhooks** | HRIS push on approval events |

---

## Product features

| Enhancement | Description |
|-------------|-------------|
| **Mobile app (PWA)** | Offline-friendly progressive web app |
| **Shift / roster leave** | Non-standard work patterns |
| **Delegation** | Acting manager approvals |
| **Bulk import** | Excel employee and balance import |
| **Advanced analytics** | Power BI connector, custom report builder |
| **Localization** | Multi-language UI and email templates |

---

## Operations

| Enhancement | Description |
|-------------|-------------|
| **Windows Service install** | ~~Run API as a service without logged-in user~~ (implemented in v1 server installer) |
| **MSIX packaging** | Store-style deployment |
| **Centralized logging** | OpenTelemetry, Application Insights, ELK |
| **Automated E2E (Playwright)** | Full browser regression in CI |

---

## Contributing priorities

When extending the codebase, preserve:

- Clean Architecture boundaries (Domain → Application → Infrastructure → Api)
- Permission-based authorization (`Permissions.*` policies)
- Vertical slices (API + UI + tests per feature)

For deployment of the current release, see [Deployment Guide](./deployment/README.md).
