# ParkEase — Multi-Vehicle Parking Management System

A complete, production-shaped parking platform for **Bikes, Cars and Heavy Vehicles**, built with
**.NET 10 Web API + EF Core (SQL Server)** and **Angular 20 (standalone, signals, Tailwind)**.

- Gate staff issue QR passes and settle exits.
- Admins see revenue, audit trails, rate cards, zones/slots and shift reconciliation.
- Super Admins manage branches and admin accounts across the network.
- Drivers open a **public mobile pass** with no login: live timer, live rent, paid receipt.

---

## 1. Quick start

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| Node.js | 20.19+ / 22.12+ / 24+ (tested on 26 LTS) |
| SQL Server | LocalDB (`MSSQLLocalDB`) or any SQL Server instance |
| EF Core CLI | installed locally in `backend/.tools` (already done) |

### Backend

```powershell
cd backend
dotnet run --project src/ParkingApp.API/ParkingApp.API.csproj --launch-profile http
```

- API: <http://localhost:5222>
- Swagger UI: <http://localhost:5222/swagger>
- Health: <http://localhost:5222/health>

On first start the API **applies the `InitialCreate` migration and seeds demo data automatically**.
The connection string lives in `backend/src/ParkingApp.API/appsettings.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=ParkingAppDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Change it if you use a different SQL Server instance.

### Frontend

```powershell
cd frontend
npm install
npm start
```

- App: <http://localhost:4200>
- API base URL is configured in `frontend/src/app/core/api.config.ts` (defaults to `http://localhost:5222/api`).
- CORS on the API already allows `http://localhost:4200`.

### Reset the demo database

```powershell
cd backend
$env:PATH="$PWD\.tools;$env:PATH"
dotnet-ef database drop -f --project src/ParkingApp.Infrastructure --startup-project src/ParkingApp.API
dotnet run --project src/ParkingApp.API/ParkingApp.API.csproj --launch-profile http
```

---

## 2. Seeded login credentials

| Role | Email | Password | Branch |
|---|---|---|---|
| **SuperAdmin** | `superadmin@parking.app` | `Admin@1234` | All branches |
| **Admin** | `admin1@parking.app` | `Admin@1234` | ParkEase Central |
| **Admin** | `admin2@parking.app` | `Admin@1234` | ParkEase Airport |
| **Staff** | `staff1@parking.app` | `Staff@1234` | ParkEase Central |
| **Staff** | `staff2@parking.app` | `Staff@1234` | ParkEase Central |
| **Staff** | `staff3@parking.app` | `Staff@1234` | ParkEase Airport |

The login screen has one-click demo fill buttons for SuperAdmin, Admin and Staff.

### Seeded data

- **2 branches** — ParkEase Central (Bangalore) and ParkEase Airport (Terminal 2).
- **6 zones** — `Zone-B` (bikes), `Zone-C` (cars), `Zone-H` (heavy) per branch.
- **54 slots** — Central: 10 bike + 15 car + 5 heavy; Airport: 8 bike + 12 car + 4 heavy.
- **6 rate cards** — one per vehicle type per branch, with grace period, base slab, extra hourly and 24-hour cap.
- **5 sample sessions** — 4 active vehicles (2 bikes, 2 cars) and 1 completed UPI-paid ticket.
- Every active session already has a public QR token you can open at `/p/{token}`.

---

## 3. Walkthrough

### A. Staff — Gate Workspace (`/staff`)

1. Sign in with `staff1@parking.app` / `Staff@1234`.
2. The top strip shows **live capacity** for Bikes, Cars, Heavy Vehicles and the **shift total**.
3. In **Quick Check-In**:
   - Pick a vehicle type (e.g. Car).
   - Type a plate — it is forced to uppercase (`KA05XY7788`).
   - Optional driver phone.
   - Click a **green available bay** in the visual slot picker (occupied/reserved/maintenance bays are disabled).
   - Press **Check in & issue QR pass**.
4. The **Instant QR Ticket Modal** appears with:
   - Rendered QR code, Zone and Slot/Seat number, plate and entry time.
   - **Copy link**, **Print slip**, **WhatsApp share** (uses the driver phone when supplied).
5. In **Active Vehicles**, search/filter, click **Pass** to open the driver's public page, **Edit** to correct a plate/phone or move the bay, or **Checkout** to settle.
6. Checkout shows the **live rent** and grace-period flag, takes Cash / UPI / Card, finalizes rent, marks the ticket paid/completed and frees the bay.
7. **There is no Delete button for Staff** anywhere in the UI or API.

### B. Admin — Management & Analytics (`/admin`)

1. Sign in with `admin1@parking.app` / `Admin@1234`.
2. **Overview** — total revenue, today's revenue, active vehicles, occupancy ring, revenue mix chart for Bikes vs Cars vs Heavy, and the last 7 days.
3. **Parking logs** — `Active`, `Completed`, `Deleted` tabs with search and type filters.
   - **Audit** opens a slide-over drawer with every field change (`old → new`), editor, reason and timestamp.
   - **Delete** opens a soft-delete modal that requires a non-empty reason. Active tickets release their bay; the row remains queryable and audit-logged.
4. **Configuration**
   - Edit / add / remove **rate cards** per vehicle type.
   - Create zones, add slots, change slot status (`Available`, `Occupied`, `Reserved`, `Maintenance`), delete zones/slots.
5. **Reconciliation** — pick a date to see each staff member's cash / UPI / card collections and shift totals.

### C. Super Admin — Network Control (`/super-admin`)

1. Sign in with `superadmin@parking.app` / `Admin@1234`.
2. Use the **branch switcher** to filter the comparison table (All branches or one location).
3. Compare bays, occupancy, today's revenue, lifetime revenue and live vehicles across branches.
4. Pause / reactivate a branch from the state chip.
5. Create a branch, create an Admin account (branch + name + email + password ≥ 8 chars), and enable/disable admins.

### D. Public mobile pass (`/p/{token}`) — no login

1. Check in any vehicle, then **Copy link** from the QR modal (or click **Pass** on an active row).
2. Open the link on a phone-sized viewport (375–430 px):
   - Big **Zone** and **Slot/Seat** number.
   - Live ticking duration (`HH:MM:SS`) and live running rent, refreshed every 30 seconds.
   - Vehicle plate, entry time, branch address, QR code.
3. After the staff checks the vehicle out, the same URL automatically becomes a **Paid digital receipt** with the total, exit time, duration and payment mode.

---

## 4. RBAC matrix

| Action | Staff | Admin | SuperAdmin |
|---|:--:|:--:|:--:|
| Login / read own token | ✅ | ✅ | ✅ |
| View live slots + capacity | ✅ | ✅ | ✅ |
| Check-in | ✅ | ✅ | ✅ |
| Edit active ticket | ✅ | ✅ | ✅ |
| **Edit completed ticket** | ❌ **403** | ✅ | ✅ |
| Checkout & payment | ✅ | ✅ | ✅ |
| **Soft delete ticket (reason required)** | ❌ **403** | ✅ | ✅ |
| Audit history | ❌ | ✅ | ✅ |
| Analytics / reconciliation / configuration | ❌ | ✅ | ✅ |
| Branch + Admin user management | ❌ | ❌ | ✅ |

Server-side enforcement is in `ParkingService`, `[Authorize(Roles = …)]` attributes and branch scoping from JWT claims.
Admins and Staff are **pinned to the branch in their token** — a tampered `branchId` query parameter is ignored.

---

## 5. Rent engine

`IRentCalculatorService` uses the branch's `RateCard` for the vehicle type:

1. Within **grace period** → `₹0`.
2. Split the stay into 24-hour cycles: each full cycle is charged at `DailyMaxCap`.
3. For the remaining hours (skipped when the stay is an exact multiple of 24h):
   - ≤ `BaseHours` → `BaseRate`.
   - Otherwise → `BaseRate + ceil(extra hours) × AdditionalHourlyRate`, capped at `DailyMaxCap`.
4. Result rounded to 2 decimals.

Live rent is recalculated on every public-ticket request and at checkout. The final checkout amount is authoritative.

---

## 6. API map

### Auth
| Method | Route | Notes |
|---|---|---|
| POST | `/api/auth/login` | Returns JWT with `UserId`, `Role`, `BranchId`, `FullName` |
| GET | `/api/auth/me` | Echoes current identity |

### Parking (Staff / Admin / SuperAdmin)
| Method | Route | Notes |
|---|---|---|
| GET | `/api/parking/slots?branchId=` | Live slots grouped by vehicle type + zone |
| GET | `/api/parking/capacity?branchId=` | Headline capacity strip |
| POST | `/api/parking/check-in` | Occupies bay, issues public token + ticket URL |
| GET | `/api/parking/sessions?status=&includeDeleted=&branchId=` | Master logs |
| GET | `/api/parking/{id}` | Ticket detail |
| GET | `/api/parking/{id}/live-rent` | Live rent preview |
| PUT | `/api/parking/{id}` | Edit vehicle/slot; Staff→Completed = 403; writes AuditLog |
| POST | `/api/parking/{id}/checkout` | Final rent, Paid + Completed, frees bay |
| DELETE | `/api/parking/{id}` | Admin/SuperAdmin only; body `{ "reason": "…" }` |

### Public
| Method | Route | Notes |
|---|---|---|
| GET | `/api/public/tickets/{token}` | Anonymous live pass / paid receipt |

### Admin (Admin / SuperAdmin)
`/api/admin/analytics`, `/api/admin/audit/session/{id}`,
`/api/admin/ratecards` (GET/POST/PUT/DELETE), `/api/admin/zones` (GET/POST/DELETE),
`/api/admin/zones/{id}/slots`, `/api/admin/slots` (POST), `/api/admin/slots/{id}/status`,
`/api/admin/slots/{id}` (DELETE), `/api/admin/reconciliation?date=`,
`/api/admin/users` (SuperAdmin only).

### SuperAdmin
`/api/superadmin/branches` (GET/POST/PUT), `/api/superadmin/branches/summary`,
`/api/superadmin/admins` (GET/POST), `/api/superadmin/admins/{id}/status`.

Swagger is pre-configured with a **Bearer** security scheme — paste the login token to explore.

---

## 7. Architecture

```
backend/
  ParkingApp.slnx
  src/
    ParkingApp.Domain/          Entities + enums
      Entities/                 Branch, AppUser, ParkingZone, ParkingSlot,
                                RateCard, ParkingSession, AuditLog
      Enums/                    UserRole, VehicleType, SlotStatus,
                                SessionStatus, PaymentMode, PaymentStatus, AuditAction
    ParkingApp.Application/     DTOs + service interfaces (no EF dependency)
    ParkingApp.Infrastructure/
      Configurations/           IEntityTypeConfiguration for every entity
      Persistence/              AppDbContext, DbSeeder, DesignTimeDbContextFactory
      Migrations/               20261122044316_InitialCreate
      Services/                 Auth, JWT, RentCalculator, Parking, Public,
                                Admin, SuperAdmin
    ParkingApp.API/
      Controllers/              Auth, Parking, Public, Admin, SuperAdmin
      Extensions/               JWT claim / branch-scoping helpers
      Middleware/               Exception → RFC7807-style JSON
      Program.cs                DI, JWT, Swagger, CORS, migrate + seed

frontend/
  src/app/
    core/                       models, API client, auth signals, jwtInterceptor,
                                roleGuard, toast service
    shared/                     shell (sidebar/bottom nav), QR ticket modal
    pages/                      login, staff, admin, super-admin, public-ticket
    app.routes.ts               functional guards + lazy standalone pages
  tailwind.config.js            dark “control room” theme
```

### Key database rules

- `ParkingSession.PublicToken` — unique index, 40-char random GUID-based value, never a sequential id.
- `ParkingSession.VehicleNumber` — indexed, stored uppercase.
- `ParkingSlot.RowVersion` — SQL Server `rowversion` concurrency token; a simultaneous check-in on the same bay fails with `409 Conflict`.
- `ParkingSession.IsDeleted` — global EF query filter; `AppDbContext.AllSessions` bypasses it for the Deleted tab.
- `AuditLog` — old/new JSON snapshots for every edit and soft delete.
- `RateCards` — unique per `(BranchId, VehicleType)`.
- `Slots` — unique per `(ZoneId, SlotCode)`.

---

## 8. Verification performed

| Check | Result |
|---|---|
| `dotnet build ParkingApp.slnx` | ✅ **0 errors** (8 NuGet advisory warnings on a transitive `System.Security.Cryptography.Pkcs` 9.0.4) |
| EF migration `InitialCreate` | ✅ Generated in `ParkingApp.Infrastructure/Migrations` |
| `ng build` (production) | ✅ **0 errors**, no CommonJS warnings |
| Runtime login (SuperAdmin/Admin/Staff) | ✅ JWT issued with role + branch |
| Capacity / sessions / public ticket | ✅ Seeded data returned correctly |
| Check-in → edit → checkout → public receipt | ✅ Live pass flips to `Completed` with payment mode |
| Staff `DELETE /api/parking/{id}` | ✅ **403 Forbidden** |
| Staff edit completed ticket | ✅ **403 Forbidden** (service-level rule) |
| Admin delete with empty reason | ✅ **400 Bad Request** |
| Admin soft delete with reason | ✅ **204**, row retained with `isDeleted`, slot freed, audit entry written |
| Audit trail after edit + delete | ✅ Both `Updated` and `SoftDeleted` entries returned |
| Cross-branch slot check-in | ✅ Blocked (`409`, "belongs to another branch") |
| Cross-branch ticket read/edit/checkout | ✅ **404** (session hidden by branch scope) |
| Cross-branch audit lookup | ✅ **404** (Admin is pinned to its JWT branch) |
| Admin analytics + reconciliation | ✅ Revenue, 7-day series, per-staff cash/UPI/card totals |
| SuperAdmin branch summary | ✅ Both branches returned with occupancy and revenue |

### Useful build commands

> On Windows the running API locks its output DLLs. Stop the API (Ctrl+C) before rebuilding, or build to a separate output path.

```powershell
# Backend
cd backend
dotnet build ParkingApp.slnx

# Add a migration (local tool is in backend/.tools)
$env:PATH="$PWD\.tools;$env:PATH"
dotnet-ef migrations add <Name> --project src/ParkingApp.Infrastructure --startup-project src/ParkingApp.API --output-dir Migrations

# Frontend
cd frontend
npm run build
```

---

## 9. Demo tips

- Use two browser windows: Staff gate on one, the public `/p/{token}` pass on the other. Check the vehicle out and watch the pass flip to a paid receipt.
- Edit a plate as Staff, then open **Admin → Parking logs → Audit** to see the `old → new` diff.
- Try to edit a **completed** ticket as Staff via Swagger — the API returns `403`.
- Try `DELETE /api/parking/{id}` as Staff — `403`; as Admin with an empty reason — `400`; with a reason — soft delete + audit entry + bay released.
- Change a car's rate card in **Configuration**, then re-open a live public pass to see the running rent use the new card.
