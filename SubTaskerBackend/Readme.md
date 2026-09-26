# SubTasker Backend

The SubTasker backend is an ASP.NET Core Web API for organizing tasks in a hierarchical structure. Users can create tasks, nest subtasks, group tasks with categories, and associate tasks with tags.

The API uses PostgreSQL for persistence, Entity Framework Core for data access, and JWT bearer tokens for authentication. User registration and login do not require authentication; all other endpoints require a valid JWT.

## Technology

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL 17
- JWT bearer authentication
- xUnit, Moq, ASP.NET Core integration testing, and Testcontainers

## Prerequisites

- .NET 10 SDK
- PostgreSQL 17 for local development, or Docker Desktop for the containerized setup
- Docker Desktop when running API or integration tests, because both test projects use Testcontainers to start PostgreSQL

## Configuration

The API requires these settings:

| Key | Description |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string |
| `Auth:Issuer` | JWT issuer that must match the token issuer |
| `Auth:Audience` | JWT audience that must match the token audience |
| `Auth:SigningKey` | Secret key used to validate JWT signatures |

The development connection string is defined in `appsettings.Development.json`. The signing key should not be committed to source control. Configure it with User Secrets for local development:

```powershell
dotnet user-secrets set "Auth:SigningKey" "replace-with-a-long-development-secret"
```

The project already has a User Secrets ID configured. Environment variables can also be used, for example:

```powershell
$env:Auth__SigningKey = "replace-with-a-long-development-secret"
```

## Run Locally

Start PostgreSQL, then apply the existing migrations and run the API:

```powershell
dotnet ef database update --project SubTaskerBackend
dotnet run --project SubTaskerBackend
```

The API listens on the URL shown by `dotnet run`.

To create a new migration after changing the EF Core model:

```powershell
dotnet ef migrations add DescribeYourChange --project SubTaskerBackend
dotnet ef database update --project SubTaskerBackend
```

## Run with Docker Compose

From the repository root, set the variables used by `docker-compose.yml` and start PostgreSQL and the backend:

```powershell
$env:POSTGRES_PASSWORD = "YourPostgresPassword"
$env:AUTH_SIGNING_KEY = "replace-with-a-long-development-secret"
docker compose up --build
```

The backend is then available at `http://localhost:8080`. Stop the services with:

```powershell
docker compose down
```

To remove the persisted PostgreSQL volume as well:

```powershell
docker compose down -v
```

## Authentication

Register or log in to receive a JWT. Send that token with subsequent requests:

```http
Authorization: Bearer <token>
```

Registration and login are anonymous. The application uses a fallback authorization policy, so new endpoints are protected by default unless they explicitly opt out.

## API Endpoints

All routes are prefixed with `/api`.

### Authentication

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| `POST` | `/api/Auth/register` | Anonymous | Create a user |
| `POST` | `/api/Auth/login` | Anonymous | Authenticate and return a JWT |

Registration body:

```json
{
	"username": "jane",
	"email": "jane@example.com",
	"password": "password123",
	"confirmPassword": "password123"
}
```

Login body:

```json
{
	"email": "jane@example.com",
	"password": "password123"
}
```

### Users

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/User/me` | Get the authenticated user |
| `GET` | `/api/User/{id}` | Get a user by ID |

### Categories

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/Category` | List the authenticated user's categories |
| `GET` | `/api/Category/{id}` | Get a category |
| `POST` | `/api/Category` | Create a category |
| `PATCH` | `/api/Category/{id}` | Update a category |
| `DELETE` | `/api/Category/{id}` | Delete a category |

Category body:

```json
{
	"name": "Work"
}
```

### Tags

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/Tag` | List the authenticated user's tags |
| `GET` | `/api/Tag/{id}` | Get a tag |
| `GET` | `/api/Tag/{tagId}/tasks` | List tasks associated with a tag |
| `POST` | `/api/Tag` | Create a tag |
| `PATCH` | `/api/Tag/{id}` | Update a tag |
| `DELETE` | `/api/Tag/{id}` | Delete a tag |

Tag body:

```json
{
	"name": "urgent"
}
```

### Task Items

| Method | Route | Description |
| --- | --- | --- |
| `GET` | `/api/TaskItem` | List the authenticated user's tasks |
| `GET` | `/api/TaskItem/{id}` | Get a task |
| `GET` | `/api/TaskItem/{id}/tags` | List a task's tags |
| `POST` | `/api/TaskItem` | Create a task or subtask |
| `POST` | `/api/TaskItem/{taskItemId}/tags/{tagId}` | Add a tag to a task |
| `PATCH` | `/api/TaskItem/{id}` | Update a task |
| `DELETE` | `/api/TaskItem/{id}` | Delete a task |
| `DELETE` | `/api/TaskItem/{taskItemId}/tags/{tagId}` | Remove a tag from a task |

Task creation body:

```json
{
	"title": "Prepare report",
	"description": "Summarize this week's results",
	"status": "notStarted",
	"priority": "Medium",
	"dueDate": "2026-10-01T17:00:00Z",
	"categoryId": 1,
	"tagIds": [1],
	"parentTaskId": null
}
```

Set `parentTaskId` to another task's ID to create a subtask. The available status values are `notStarted`, `inProgress`, and `completed`. The available priority values are `Low`, `Medium`, `High`, and `Critical`.

## Responses and Errors

Successful create operations return `201 Created`; successful deletes return `204 No Content`. Validation and application errors are returned using ASP.NET Core Problem Details responses through the global exception handler.

JSON enums are serialized as strings rather than numeric values.

## Tests

Run all tests from the repository root:

```powershell
dotnet test SubTaskerBackend.sln
```

The unit tests do not require Docker. API and integration tests use PostgreSQL containers through Testcontainers, so Docker Desktop must be running when either test project is executed.

## Project Structure

```text
Controllers/   HTTP endpoints
Data/          EF Core DbContext
DTOs/          Request and response contracts
Enums/         Task status and priority values
Exceptions/    HTTP exceptions and global error handling
Mappers/       Model-to-DTO mapping
Migrations/    EF Core database migrations
Models/        Persistent domain models
Services/      Business logic
```
