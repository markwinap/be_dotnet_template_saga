# Security Policy

## Supported versions

The following versions of the Order Service template receive security updates:

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a vulnerability

If you discover a security vulnerability, please do **not** open a public issue.

Instead, either:

- Open a private security advisory via the repository's GitHub Security tab, or
- Email the maintainers at the contact address listed on their GitHub profile with a clear subject line.

Include as much detail as possible:

- A description of the vulnerability and the affected code path.
- Steps to reproduce the issue.
- The possible impact.
- Suggested remediation, if known.

## Response expectations

- We will acknowledge receipt within 5 business days.
- We will provide an initial assessment within 10 business days.
- We will coordinate a fix and disclose the issue responsibly once a patch is available.

## Security notes

- The default `Jwt:Secret` in `appsettings.json` is a placeholder and must be changed before any production deployment.
- The `/api/auth/dev-token` endpoint is for local development only and should be disabled or restricted in production.
