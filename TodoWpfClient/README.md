# TodoWpfClient

Egyszerű WPF kliens a teendők kezeléséhez. Csak az elvégzetlen teendőket listázza, lehet új teendőt felvenni és készre jelölni.

## Követelmények
- .NET 8 SDK
- Futó Todo API (pl. `https://localhost:5001` vagy `http://localhost:5000`)

## Futtatás
```bash
dotnet build
dotnet run
```

Az alkalmazásban a tetején állítható az **API Base URL**, majd **Mentés** és **Frissítés**.

## API kompatibilitás
A kliens rugalmas az endpointokra/properirekre:
- GET: `/todos` vagy `/api/todos`
- POST: `/todos` (body: `{ title }` vagy `{ title, isCompleted:false }`)
- Készre jelölés: először `PUT /todos/{id}/complete`, majd `PATCH /todos/{id}` `{ isCompleted: true }`, végül `PUT /todos/{id}` teljes entitással.

A válaszban elfogadott property nevek: `id|todoId|guid`, `title|name|text`, `isCompleted|completed|done`, `createdAt|created`.

