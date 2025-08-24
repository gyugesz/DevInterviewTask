1) API futtatása (Todo.Api)
A) Parancssorból (PowerShell)
Nyiss egy PowerShellt a repo gyökerében:
#NuGet restore (egyszer)
dotnet restore

# (Opcionális) EF migrációk alkalmazása
# Ha a migrációk ugyanebben a projektben vannak:
dotnet tool update -g dotnet-ef
dotnet ef database update --project .\API\Todo.Api\Todo.Api.csproj

# Ha a migrációk külön projektben vannak (pl. Infrastructure),
# használd az --startup-project kapcsolót:
# dotnet ef database update `
#   --project .\API\Todo.Infrastructure\Todo.Infrastructure.csproj `
#   --startup-project .\API\Todo.Api\Todo.Api.csproj

# API indítása
dotnet run --project .\API\Todo.Api\Todo.Api.csproj

2) WPF kliens futtatása (TodoWpfClient)
A) Visual Studio-ból (ajánlott)
Nyisd meg a TodoWpfClient projektet (vagy solutiont).
F5 / Start.

B) Parancssorból
dotnet build .\TodoWpfClient\TodoWpfClient.csproj
dotnet run   --project .\TodoWpfClient\TodoWpfClient.csproj

Használat:
A felső mezőben állítsd be az API Base URL-t (pl. http://localhost:5055), majd Mentés → Frissítés.
A lista csak az elvégzetlen teendőket mutatja.
Hozzáadás alul:
Cím, Leírás és Prioritás (kötelező)
Kész gomb: a tétel eltűnik a listából (mert elvégzettnek számít).
Rendezés: a jobb felső legördülőből (Cím A→Z, Z→A, Létrehozás régi/új elöl).



