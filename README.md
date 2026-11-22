# ParkEase

Multi-vehicle parking management system (Bikes • Cars • Heavy Vehicles) with a .NET 10 Web API,
EF Core + SQL Server, JWT RBAC, Swagger and an Angular 20 + Tailwind frontend.

**Start here → [WALKTHROUGH.md](./WALKTHROUGH.md)** for startup commands, seeded credentials,
role walkthroughs, the API map and verification results.

```
backend/   ASP.NET Core Web API (Domain / Application / Infrastructure / API)
frontend/  Angular standalone app (signals, functional guards/interceptors, Tailwind)
```

Quick start:

```powershell
# Terminal 1
cd backend
dotnet run --project src/ParkingApp.API/ParkingApp.API.csproj --launch-profile http

# Terminal 2
cd frontend
npm install
npm start
```

- App: http://localhost:4200
- Swagger: http://localhost:5222/swagger
- Demo SuperAdmin: `superadmin@parking.app` / `Admin@1234`
- Demo Admin: `admin1@parking.app` / `Admin@1234`
- Demo Staff: `staff1@parking.app` / `Staff@1234`
