const apiBaseUrl = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:8080";

type LoginRequest = {
  email: string;
  password: string;
};

type RegisterRequest = {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
};

type LoginResponse = {
  token: string;
};

async function getErrorMessage(response: Response) {
  try {
    const problem = (await response.json()) as { detail?: string; title?: string };
    return problem.detail ?? problem.title ?? `Request failed with status ${response.status}.`;
  } catch {
    return `Request failed with status ${response.status}.`;
  }
}

export async function login(credentials: LoginRequest): Promise<LoginResponse> {
  const response = await fetch(`${apiBaseUrl}/api/Auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials),
  });

  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }

  return response.json() as Promise<LoginResponse>;
}

export async function register(credentials: RegisterRequest) {
  const response = await fetch(`${apiBaseUrl}/api/Auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials),
  });

  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
}