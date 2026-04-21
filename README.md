# PetHelp.AdminOnboarding

App Razor/Blazor separada para revisar, aprobar o rechazar solicitudes de onboarding de refugios sin depender de que `PetHelp.Blazor.Server` esté corriendo.

## Ubicación

- App separada: `C:\Users\agust\source\repos\PetHelp.AdminOnboarding`
- Librerías reutilizadas desde PetHelp:
  - `C:\Users\agust\source\repos\Pethelp\src\PetHelp.Core`
  - `C:\Users\agust\source\repos\Pethelp\src\PetHelp.Domain`
  - `C:\Users\agust\source\repos\Pethelp\src\PetHelp.Infrastructure`

## Qué hace

- Lista solicitudes de `ShelterOnboardingSubmissions`
- Muestra detalle, historial y adjuntos de `MediaAssets`
- Genera URLs SAS temporales para PDFs/fotos en el contenedor privado `onboarding`
- Aprueba o rechaza solicitudes con transacción
- Crea o actualiza `ShelterProfiles` al aprobar
- Registra decisiones en `OnboardingReviewDecisions`

## Configuración local

La app ya trae en `appsettings.Development.json` la conexión local a SQL Server:

```json
"ConnectionStrings": {
  "Default": "Server=localhost;Database=pethelp;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;Connection Timeout=30;"
}
```

Faltan dos secretos para correrla completa:

1. `Auth0:ClientSecret`
2. `Blob:ConnectionString`

Configúralos con:

```powershell
cd C:\Users\agust\source\repos\PetHelp.AdminOnboarding
dotnet user-secrets set "Auth0:ClientSecret" "TU_SECRET"
dotnet user-secrets set "Blob:ConnectionString" "TU_BLOB_CONNECTION_STRING"
```

Si quieres restringir quién puede usar la app, agrega correos permitidos:

```powershell
dotnet user-secrets set "AdminAccess:AllowedEmails:0" "tu-correo@dominio.com"
```

Si no defines `AdminAccess:AllowedEmails`, cualquier usuario autenticado en Auth0 podrá entrar.

## Ejecutar

```powershell
cd C:\Users\agust\source\repos\PetHelp.AdminOnboarding
dotnet run
```

## Flujo de aprobación

Al aprobar:

- `ShelterOnboardingSubmissions.Status = Approved`
- actualiza `StaffNotes`
- actualiza `Updated`
- crea o actualiza `ShelterProfiles` usando `ApplicantUserId`
- deja `IsActive = true`
- inserta fila en `OnboardingReviewDecisions`

Al rechazar:

- `ShelterOnboardingSubmissions.Status = Rejected`
- actualiza `StaffNotes`
- actualiza `Updated`
- inserta fila en `OnboardingReviewDecisions`

## Validación hecha

Se validó con:

```powershell
dotnet build C:\Users\agust\source\repos\PetHelp.AdminOnboarding\PetHelp.AdminOnboarding.slnx
```
