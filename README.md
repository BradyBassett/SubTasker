# SubTasker
SubTasker is a web-based application designed to help users organize and manage tasks in a hierarchical structure. Each task can have multiple subtasks, and these subtasks can themselves have further nested tasks, allowing users to break down projects into sections, subsections, and actionable steps.

## Production TODO

### Authentication and Frontend

- [ ] Replace the default `NEXT_PUBLIC_API_URL` localhost fallback with a required production environment variable.
- [ ] Add authenticated routing so the root route sends signed-in users to the application and unauthenticated users to `/auth`.
- [ ] Redirect users to the application after a successful login and to the sign-in page after logout or token expiration.
- [ ] Replace `localStorage` JWT storage with a secure, `HttpOnly`, `Secure`, `SameSite` cookie strategy or a backend-for-frontend session.
- [ ] Add token expiration handling and a logout flow.
- [ ] Implement the forgot-password flow, or remove the current placeholder link.
- [ ] Add real Terms and Privacy Policy pages, or remove the placeholder links.
- [ ] Add client-side handling for registration success, duplicate email/username errors, and password confirmation errors.

### Backend and Deployment

- [ ] Replace localhost-only CORS origins with the production frontend origin and keep development origins environment-specific.
- [ ] Serve the frontend and API over HTTPS and configure secure headers.
- [ ] Move the database password, JWT signing key, issuer, and audience to deployment secrets or environment configuration.
- [ ] Remove hardcoded development credentials and Auth0 development URLs from production Compose configuration.
- [ ] Use a production database with backups, restore testing, and a migration step in the deployment process.
- [ ] Add API health checks, structured logging, monitoring, and alerting.
- [ ] Review global error responses to ensure they do not expose sensitive implementation details.
- [ ] Run the API and database containers with production resource limits and least-privilege settings.

### Verification

- [ ] Add end-to-end tests covering registration, login, invalid credentials, token expiration, logout, and protected routes.
- [ ] Add a production-like smoke test for frontend-to-API communication and CORS.
- [ ] Run dependency and container vulnerability scans before release.
- [ ] Document production environment variables, deployment steps, rollback steps, and required secrets.
