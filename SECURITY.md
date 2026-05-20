# Security Policy

## Supported Versions

Security updates are provided for:

- `main` branch (latest production-ready code)
- latest tagged release (if tags are used for deployment)

Older snapshots and custom forks are not supported unless explicitly agreed.

## Reporting a Vulnerability

Please do **not** report security issues in public GitHub issues.

Use one of these private channels:

1. GitHub private vulnerability reporting (`Security` tab -> `Report a vulnerability`)
2. Direct maintainer contact (private channel already used for this project)

If private reporting is not available in your repository UI, contact the maintainer privately and include the details below.

## What to Include

Please include:

- affected component and file/path
- steps to reproduce
- impact (what can an attacker do)
- proof of concept (if available)
- suggested fix or mitigation (optional)

## Response Targets

- Initial acknowledgement: within 72 hours
- Triage and severity assessment: within 7 days
- Fix timeline:
  - Critical: as soon as possible, target <= 7 days
  - High: target <= 14 days
  - Medium/Low: scheduled in normal release cycle

## Disclosure Policy

- We prefer coordinated disclosure.
- Please wait for a fix or mitigation before public disclosure.
- After remediation, we may publish a summary/changelog entry.

## Scope

In scope:

- authentication and authorization flaws
- sensitive data exposure
- injection vulnerabilities
- privilege escalation
- insecure defaults in deployment/runtime configuration

Out of scope (unless chained with a real security impact):

- cosmetic UI bugs
- pure availability issues without security impact
- missing best-practice headers with no exploit path
