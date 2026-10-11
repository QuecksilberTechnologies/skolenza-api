# Skolenza Employee Agent

Lightweight Windows tray agent for configured, consented employee screenshots. It starts at Windows user login, captures JPEG images at a server-provided randomized interval, stores pending captures in a SQLite outbox, encrypts each local image with AES-GCM, protects the queue key and agent credential with Windows DPAPI, and synchronizes automatically when the API becomes reachable.

## Build and publish

```powershell
dotnet restore .\skolenza.employeeagent\skolenza.employeeagent.csproj
dotnet publish .\skolenza.employeeagent\skolenza.employeeagent.csproj -c Release -r win-x64 --self-contained false
```

The target PC requires the matching .NET Desktop Runtime when using a framework-dependent publish. Installer packaging and code signing are deployment work and are not performed by this repository setup.

## One-time enrollment

1. An authorized administrator configures the Tenant policy and registers an agent through the API.
2. The registration response returns `agentInstanceId` and `agentCredential` exactly once.
3. On the target Windows user account, run the published executable once:

```powershell
.\Skolenza.EmployeeAgent.exe --api-url "https://api.example.com/" --agent-instance-id "00000000-0000-0000-0000-000000000000" --credential "one-time-registration-response-value"
```

Do not store the real credential in scripts, source control, screenshots, shell history, tickets or documentation. The bootstrap command protects it under the current Windows user with DPAPI. Start the executable normally after setup; it registers itself under the current user's Windows `Run` key.

## Local data

Data is stored under `%LocalAppData%\Skolenza.EmployeeAgent`. Screenshot bytes are never stored in SQLite or as plaintext image files. The database contains only queue metadata. A server acknowledgment is required before an encrypted local capture is deleted.

The agent does not contain Tenant IDs, Employee IDs, permission IDs, intervals, retention limits, image quality or storage limits. The agent credential resolves Tenant, Employee, enrollment and policy on the server.
