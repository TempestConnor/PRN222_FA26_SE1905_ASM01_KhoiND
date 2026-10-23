# AGENTS.md — PRN222 N-Tier Solution Template (MVC · Razor Pages · Blazor · SignalR · Worker Service)

This file tells AI coding agents (and humans) how to build PRN222 assignment solutions. Every
solution shares one N-tier back end (**Entities → Repositories → Services**) and adds one or more
hosts: **ASP.NET Core MVC, Razor Pages, Blazor, SignalR and a Worker Service**.

The file is **generic** and **self-contained**: every piece of code an agent needs to write for
the shared layers is here, and nothing depends on another solution being present. The patterns
come from the course's reference solution. "Reference value" columns show what a placeholder
looked like there (domain `Vegan`). Fixes to the reference are marked **[FIX]**.

---

## 0. Phase plan (read first)

| Phase | Solution contains | Who does what |
|---|---|---|
| **1 — Assignment01** | `Entities`, `Repositories`, `Services` + **`MvcWebApp`** | **Human:** runs the SQL script and generates Entities + DbContext with **EF Core Power Tools** (§3.1). **Agent:** configures the DbContext, writes **all** of §3.2–§3.3, then the MVC host (§4.1). |
| **2+ — later assignments** | The three shared projects (copied) + the host(s) the brief asks for: Razor Pages (§4.2), SignalR (§4.3), Blazor (§4.4), Worker Service (§4.5) | **Human:** copies the shared project folders into a new empty solution (§3.4). **Agent:** adds the host projects. **The shared layers are not rewritten.** |

Because Phase 1 output gets copied into every later solution, **the shared layers must be
complete and correct the first time.** That means no `NotImplementedException` stubs, no TODOs,
and a solution that builds with 0 errors. See the definition of done in §3.5.

**Every host must build and run on its own.** A later solution might contain only one host, so
no host may require another host to exist. SignalR connections between hosts are optional, and
are switched off when the hub URL isn't configured (§4.3).

---

## 1. Placeholders

Replace these everywhere (file names, namespaces, class names, folders):

| Placeholder | Meaning | Reference value |
|---|---|---|
| `{Domain}` | Solution/domain prefix | `Vegan` |
| `{Suffix}` | Student / owner code appended to projects and classes | `DuyLD` |
| `{Db}` | SQL Server database name | `VeganSupportDB` |
| `{DbContext}` | EF Core context class | `VeganSupportDBContext` |
| `{Entity}` | Main entity class, **exactly as generated** | `VeganPlaceDuyLd` |
| `{EntityBase}` | `{Entity}` without the suffix, used in repo/service names | `VeganPlace` |
| `{Parent}` / `{ParentBase}` | Lookup/parent entity of `{Entity}` (FK on `{Entity}`) | `PlaceCategoryDuyLd` / `PlaceCategory` |
| `{Child}` / `{ChildBase}` | Child entity of `{Entity}` (FK to `{Entity}`), optional | `PlaceFoodDuyLd` / `PlaceFood` |
| `{EntityId}` / `{ParentId}` | Generated primary-key properties | `VeganPlaceDuyLdid` / `PlaceCategoryDuyLdid` |
| `{Key}` | CLR type of the primary key (`int` or `long`) | `long` |
| `{EntitySet}` / `{ParentSet}` | Generated `DbSet` property names | `VeganPlaceDuyLds` / `PlaceCategoryDuyLds` |
| `{ParentName}` | Display column of `{Parent}` (shown in dropdowns) | `CategoryName` |
| `{Search1}` | String column searched with `Contains` | `PlaceName` |
| `{Search2}` | Numeric column searched with `>=` | `Rating` |
| `{Search3}` | Second string column searched with `Contains` (may be on `{Parent}`) | `Description` |
| `{EntityPlural}` | Controller / page folder name | `VeganPlaces` |

Lowercase forms such as `{search1}` and `{entity}` are the camelCase parameter or message
versions of the same names.

- **Class names:** repository and service classes put `{Suffix}` after the role. For example,
  `VeganPlace` + `Repository` + `DuyLD` gives `VeganPlaceRepositoryDuyLD`. Use the suffix
  casing the brief gives.
- **Search fields:** the assignment brief usually names them. If it doesn't, use the entity's main
  name column, a numeric column, and a descriptive string column.
- **Generated names:** EF casing turns table `VeganPlaceDuyLD` into class `VeganPlaceDuyLd`, and
  column `VeganPlaceDuyLDId` into property `VeganPlaceDuyLdid`. **Use the generated names
  as-is.** Never "fix" their casing. Before writing code, open `Models/*.cs` and the DbContext
  to get the real names.

---

## 2. Solution layout

```
{Solution}.slnx
├── {Domain}.Entities.{Suffix}/        # class library — EF models (generated)
├── {Domain}.Repositories.{Suffix}/    # class library — DbContext + repositories
├── {Domain}.Services.{Suffix}/        # class library — business services + shared contracts
│   ── hosts: include only the ones the current assignment needs ──
├── {Domain}.MvcWebApp.{Suffix}/       # ASP.NET Core MVC     (Assignment01)
├── {Domain}.RazorWebApp.{Suffix}/     # Razor Pages          (default SignalR hub host)
├── {Domain}.BlazorWebApp.{Suffix}/    # Blazor Web App, Interactive Server
└── {Domain}.WorkerService.{Suffix}/   # Worker Service, periodic background job
```

### Dependency graph (must not be violated)

```
Entities ◄── Repositories ◄── Services ◄──┬── MvcWebApp
   ▲                                      ├── RazorWebApp   (hosts the SignalR hub)
   │                                      ├── BlazorWebApp  ··optional SignalR client··► hub
   └──────────── (all hosts) ─────────────└── WorkerService ··optional SignalR client··► hub
```

- Hosts reference only **Services** and **Entities**. They never reference Repositories or the
  DbContext directly.
- Services reference Repositories. Repositories reference Entities.
- No host references another host project. Hosts talk to each other only through SignalR (HTTP).

### Project types / SDKs

| Project | `dotnet new` template | SDK | Packages | Project refs |
|---|---|---|---|---|
| Entities | `classlib` | `Microsoft.NET.Sdk` | `Microsoft.EntityFrameworkCore.SqlServer` | — |
| Repositories | `classlib` | `Microsoft.NET.Sdk` | `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Configuration.Json` | Entities |
| Services | `classlib` | `Microsoft.NET.Sdk` | — | Repositories |
| MvcWebApp | `mvc` | `Microsoft.NET.Sdk.Web` | `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.VisualStudio.Web.CodeGeneration.Design` (scaffolding) | Services, Entities |
| RazorWebApp | `webapp` | `Microsoft.NET.Sdk.Web` | same as MVC | Services, Entities |
| BlazorWebApp | `blazor --interactivity Server` | `Microsoft.NET.Sdk.Web` | `Microsoft.AspNetCore.SignalR.Client` | Services, Entities |
| WorkerService | `worker` | `Microsoft.NET.Sdk.Worker` | `Microsoft.AspNetCore.SignalR.Client` | Services, Entities |

All projects use `<TargetFramework>net10.0</TargetFramework>`, `<Nullable>enable</Nullable>` and
`<ImplicitUsings>enable</ImplicitUsings>`. Keep **one** framework version across the solution.
The reference mixes net8.0 and net10.0; don't copy that. The major version of the EF Core and
`Microsoft.Extensions.*` packages must match the target framework major (10.x for net10.0). Add
packages with `dotnet add package` rather than editing the XML by hand.

---

## 3. Shared layers (Phase 1 — Assignment01)

### 3.1 Entities + DbContext — generated by the human

1. Run the SQL script on SQL Server to create `{Db}`. This is database-first: the database is
   the source of truth, and no EF migrations are used.
2. Create the three class libraries and add the project references and packages from §2.
3. In VS, right-click `{Domain}.Entities.{Suffix}`, choose **EF Core Power Tools → Reverse
   Engineer**, select all tables, then pick these options:

   | Option | Value |
   |---|---|
   | Context name | `{DbContext}` |
   | Namespace | `{Domain}.Entities.{Suffix}` (models land in `.Models`) |
   | EntityTypes path | `Models` |
   | What to generate | **EntityTypes & DbContext** |
   | Pluralize / singularize (Humanizer) | ✅ |
   | Use table and column names from the database | ❌ |
   | Use DataAnnotation attributes | ❌ (fluent API only) |
   | Include connection string in generated code | ❌ **[FIX]** (see §3.2 step 2) |
   | Use C# nullable reference types | ❌ (generates `#nullable disable`) |
   | Use DateOnly / TimeOnly | ✅ |

   These choices are saved to `efpt.config.json`. Keep that file so regenerating gives the same output.
4. Output is `Models/{Entity}.cs`: `public partial class`, a `// <auto-generated>` header,
   `virtual` navigation properties, and collections initialised to `new List<T>()`.
   **Never hand-edit generated files.** Extra members go in a separate `partial class` file.

**Fallback (CLI, same layout).** Use this only if EF Core Power Tools isn't available:

```bash
dotnet tool install --global dotnet-ef
dotnet add {Domain}.Entities.{Suffix} package Microsoft.EntityFrameworkCore.Design
dotnet ef dbcontext scaffold "Data Source=localhost;Initial Catalog={Db};User ID=sa;Password=<pwd>;Encrypt=False" \
  Microsoft.EntityFrameworkCore.SqlServer \
  --project {Domain}.Entities.{Suffix} \
  --output-dir Models --namespace {Domain}.Entities.{Suffix}.Models \
  --context {DbContext} \
  --context-dir ../{Domain}.Repositories.{Suffix}/DBContext \
  --context-namespace {Domain}.Repositories.{Suffix} \
  --no-onconfiguring --force
```

The CLI puts the DbContext straight into Repositories, so skip step 1 of §3.2. It emits nullable
annotations (`= null!;`) instead of `#nullable disable`. Those annotations make navigation
properties implicitly `[Required]` in MVC/Razor model binding, so see the `ModelState.Remove`
note in §4.1.

### 3.2 Repositories — written by the agent

```
{Domain}.Repositories.{Suffix}/
├── Base/GenericRepository.cs
├── DBContext/{DbContext}.cs                  # generated (moved here)
├── DBContext/{DbContext}.Configuration.cs    # hand-written partial [FIX]
├── {EntityBase}Repository{Suffix}.cs         # main entity
├── {ParentBase}Repository{Suffix}.cs         # parent entity
├── {ChildBase}Repository{Suffix}.cs          # child entity (if any)
└── SystemUserAccountRepository.cs            # if the DB has System.UserAccount
```

**Step 1: move the DbContext.** EF Core Power Tools writes `{DbContext}.cs` into the Entities
project. Move it to `Repositories/DBContext/` and set its namespace to
`{Domain}.Repositories.{Suffix}`. Add `using {Domain}.Entities.{Suffix}.Models;` if it is missing.
Change nothing else in that file.

**Step 2: connection config, in its own partial file.** This survives regeneration:

```csharp
// DBContext/{DbContext}.Configuration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace {Domain}.Repositories.{Suffix};

public partial class {DbContext}
{
    public static string GetConnectionString(string connectionStringName)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        string connectionString = config.GetConnectionString(connectionStringName);
        return connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(GetConnectionString("DefaultConnection"))
                          .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }
    }
}
```

⚠️ The connection string is read from `appsettings.json` **in the running executable's
folder**. So *every host* needs `ConnectionStrings:DefaultConnection` in its own
`appsettings.json` (§5). The Web and Worker SDKs copy that file to the output automatically.
Class libraries have no `appsettings.json` of their own.

If the generated file still contains an `OnConfiguring` (because "Include connection string" was
left ticked), you'll get a duplicate-member compile error. Regenerate with the option unticked;
don't edit the generated file.

**Step 3: `Base/GenericRepository.cs`.** Write exactly this, which is the reference version plus
`long`-key overloads **[FIX]**:

```csharp
using Microsoft.EntityFrameworkCore;

namespace {Domain}.Repositories.{Suffix}.Base
{
    public class GenericRepository<T> where T : class
    {
        protected {DbContext} _context;

        public GenericRepository()
        {
            _context ??= new {DbContext}();
        }

        public GenericRepository({DbContext} context)
        {
            _context = context;
        }

        #region Read

        public List<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        public async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public T? GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public T? GetById(long id)
        {
            return _context.Set<T>().Find(id);
        }

        public async Task<T?> GetByIdAsync(long id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public T? GetById(string code)
        {
            return _context.Set<T>().Find(code);
        }

        public async Task<T?> GetByIdAsync(string code)
        {
            return await _context.Set<T>().FindAsync(code);
        }

        public T? GetById(Guid code)
        {
            return _context.Set<T>().Find(code);
        }

        public async Task<T?> GetByIdAsync(Guid code)
        {
            return await _context.Set<T>().FindAsync(code);
        }

        #endregion

        #region Write (saves immediately)

        public void Create(T entity)
        {
            _context.Add(entity);
            _context.SaveChanges();
        }

        public async Task<int> CreateAsync(T entity)
        {
            _context.Add(entity);
            return await _context.SaveChangesAsync();
        }

        public void Update(T entity)
        {
            _context.ChangeTracker.Clear();
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
            _context.SaveChanges();
        }

        public async Task<int> UpdateAsync(T entity)
        {
            _context.ChangeTracker.Clear();
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
            return await _context.SaveChangesAsync();
        }

        public bool Remove(T entity)
        {
            _context.Remove(entity);
            _context.SaveChanges();
            return true;
        }

        public async Task<bool> RemoveAsync(T entity)
        {
            _context.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Unit of work (stage several changes, then Save once)

        public void PrepareCreate(T entity)
        {
            _context.Add(entity);
        }

        public void PrepareUpdate(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
        }

        public void PrepareRemove(T entity)
        {
            _context.Remove(entity);
        }

        public int Save()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion
    }
}
```

How the members behave:
- **Contexts:** each repository instance owns one DbContext, created by its parameterless
  constructor. Queries don't track entities by default.
- **`Update`:** it calls `ChangeTracker.Clear()` first, so an entity bound from a form can be
  saved even after other reads on the same context.
- **`Find`/`FindAsync`:** these always track the entity they return, and they never load
  navigations. Entity repositories hide them to add `.Include(...)`.
- **Key types:** `GetById(int)` won't find a `long` key, so use the overload whose type matches
  `{Key}`.

**Step 4: one repository per entity.** Both constructors are required. Hide (`new`) the generic
reads to add `.Include(...)`.

Main entity, with navigations and search:

```csharp
using Microsoft.EntityFrameworkCore;
using {Domain}.Entities.{Suffix}.Models;
using {Domain}.Repositories.{Suffix}.Base;

namespace {Domain}.Repositories.{Suffix}
{
    public class {EntityBase}Repository{Suffix} : GenericRepository<{Entity}>
    {
        public {EntityBase}Repository{Suffix}()
        {
            _context ??= new {DbContext}();
        }

        public {EntityBase}Repository{Suffix}({DbContext} context)
        {
            _context = context;
        }

        public new async Task<List<{Entity}>> GetAllAsync()
        {
            return await _context.{EntitySet}
                .Include(e => e.{Parent})
                .Include(e => e.{Child}s)                 // omit if no child
                .ToListAsync();
        }

        public new async Task<{Entity}?> GetByIdAsync({Key} id)
        {
            return await _context.{EntitySet}
                .Include(e => e.{Parent})
                .Include(e => e.{Child}s)                 // omit if no child
                .FirstOrDefaultAsync(e => e.{EntityId} == id);
        }

        public async Task<List<{Entity}>> SearchAsync(string {search1}, decimal {search2}, string {search3})
        {
            return await _context.{EntitySet}
                .Include(e => e.{Parent})
                .Where(e =>
                    (e.{Search1}.Contains({search1}) || string.IsNullOrEmpty({search1}))
                    && (e.{Search3}.Contains({search3}) || string.IsNullOrEmpty({search3}))
                    && (e.{Search2} >= {search2} || {search2} == 0))
                .ToListAsync();
        }
    }
}
```

- **Search idiom:** each filter takes the form `(column matches || filter is empty)`, so an empty
  filter matches every row.
- **`{Search3}` on the parent:** if it's a parent column, write `e.{Parent}.{Search3}.Contains(...)`.
- **Types:** match the parameter type to the column type (`decimal`, `int`, `double`).
- **Collection name:** check the generated name of the child collection (`{Child}s`) in the
  model class.

Parent entity, used for dropdowns and its own CRUD:

```csharp
using Microsoft.EntityFrameworkCore;
using {Domain}.Entities.{Suffix}.Models;
using {Domain}.Repositories.{Suffix}.Base;

namespace {Domain}.Repositories.{Suffix}
{
    public class {ParentBase}Repository{Suffix} : GenericRepository<{Parent}>
    {
        public {ParentBase}Repository{Suffix}()
        {
            _context ??= new {DbContext}();
        }

        public {ParentBase}Repository{Suffix}({DbContext} context)
        {
            _context = context;
        }

        public new async Task<List<{Parent}>> GetAllAsync()
        {
            return await _context.{ParentSet}.Include(p => p.{Entity}s).ToListAsync();
        }

        public new async Task<{Parent}?> GetByIdAsync({Key} id)
        {
            return await _context.{ParentSet}
                .Include(p => p.{Entity}s)
                .FirstOrDefaultAsync(p => p.{ParentId} == id);
        }
    }
}
```

Child entity, or any entity with no custom queries:

```csharp
using {Domain}.Entities.{Suffix}.Models;
using {Domain}.Repositories.{Suffix}.Base;

namespace {Domain}.Repositories.{Suffix}
{
    public class {ChildBase}Repository{Suffix} : GenericRepository<{Child}>
    {
        public {ChildBase}Repository{Suffix}() => _context ??= new {DbContext}();
        public {ChildBase}Repository{Suffix}({DbContext} context) => _context = context;
    }
}
```

User accounts (only if the DB has `System.UserAccount`). Log in by whichever column the brief
names. The reference uses `Email`; the others are `UserName`, `Phone` and `EmployeeCode`.

```csharp
using Microsoft.EntityFrameworkCore;
using {Domain}.Entities.{Suffix}.Models;
using {Domain}.Repositories.{Suffix}.Base;

namespace {Domain}.Repositories.{Suffix}
{
    public class SystemUserAccountRepository : GenericRepository<SystemUserAccount>
    {
        public SystemUserAccountRepository() => _context ??= new {DbContext}();
        public SystemUserAccountRepository({DbContext} context) => _context = context;

        public async Task<SystemUserAccount?> GetUserAccount(string userName, string password)
        {
            return await _context.SystemUserAccounts
                .FirstOrDefaultAsync(u => u.Email == userName && u.Password == password && u.IsActive);
        }
    }
}
```

### 3.3 Services — written by the agent

```
{Domain}.Services.{Suffix}/
├── I{EntityBase}Service{Suffix}.cs   + {EntityBase}Service{Suffix}.cs   # full CRUD + search
├── I{ParentBase}Service{Suffix}.cs   + {ParentBase}Service{Suffix}.cs   # at least GetAll/GetById (dropdowns)
├── ISystemUserAccountService.cs      + SystemUserAccountService.cs      # login
├── Dtos/{EntityBase}Dto.cs           # flat records for SignalR (may be added in Phase 2)
└── Realtime/HubContract.cs           # shared SignalR names     (may be added in Phase 2)
```

Rules for every service:
- **Constructor:** a parameterless constructor creates its own repositories with `??= new`.
  There's no constructor injection, which keeps services usable both with and without DI.
- **Error handling:** every public method wraps its body in `try/catch` and rethrows
  `new Exception("Error <doing X>. " + ex.Message, ex)`.
- **Interface layout:** list query methods first, then mutation methods.
- **No stubs:** every interface member is fully implemented, and nothing outside the interface
  is public.

Main entity interface:

```csharp
using {Domain}.Entities.{Suffix}.Models;

namespace {Domain}.Services.{Suffix}
{
    public interface I{EntityBase}Service{Suffix}
    {
        // Queries: read database
        Task<List<{Entity}>> GetAllAsync();
        Task<{Entity}?> GetByIdAsync({Key} id);
        Task<List<{Entity}>> GetAllAsync(string? {search1}, decimal? {search2}, string? {search3});

        // Mutations: write database
        Task<int> CreateAsync({Entity} item);
        Task<int> UpdateAsync({Entity} item);
        Task<bool> DeleteAsync({Key} id);
    }
}
```

Main entity implementation:

```csharp
using {Domain}.Entities.{Suffix}.Models;
using {Domain}.Repositories.{Suffix};

namespace {Domain}.Services.{Suffix}
{
    public class {EntityBase}Service{Suffix} : I{EntityBase}Service{Suffix}
    {
        private readonly {EntityBase}Repository{Suffix} _repository;

        public {EntityBase}Service{Suffix}()
            => _repository ??= new {EntityBase}Repository{Suffix}();

        public async Task<List<{Entity}>> GetAllAsync()
        {
            try
            {
                return await _repository.GetAllAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving all {entity}s. " + ex.Message, ex);
            }
        }

        public async Task<{Entity}?> GetByIdAsync({Key} id)
        {
            try
            {
                return await _repository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw new Exception("Error retrieving {entity}. " + ex.Message, ex);
            }
        }

        public async Task<List<{Entity}>> GetAllAsync(string? {search1}, decimal? {search2}, string? {search3})
        {
            try
            {
                return await _repository.SearchAsync(
                    {search1} ?? string.Empty,
                    {search2} ?? 0,
                    {search3} ?? string.Empty);
            }
            catch (Exception ex)
            {
                throw new Exception("Error searching {entity}s. " + ex.Message, ex);
            }
        }

        public async Task<int> CreateAsync({Entity} item)
        {
            try
            {
                return await _repository.CreateAsync(item);
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating {entity}. " + ex.Message, ex);
            }
        }

        public async Task<int> UpdateAsync({Entity} item)
        {
            try
            {
                return await _repository.UpdateAsync(item);
            }
            catch (Exception ex)
            {
                throw new Exception("Error updating {entity}. " + ex.Message, ex);
            }
        }

        public async Task<bool> DeleteAsync({Key} id)
        {
            try
            {
                var item = await _repository.GetByIdAsync(id);
                if (item == null)
                {
                    throw new KeyNotFoundException($"{Entity} with ID {id} not found.");
                }

                // FKs are generated with DeleteBehavior.ClientSetNull on required columns,
                // so deleting a row that still has children fails. Block it with a clear message.
                if (item.{Child}s.Any())                  // omit if no child
                {
                    throw new InvalidOperationException(
                        $"{Entity} with ID {id} still has {item.{Child}s.Count} related {child}(s).");
                }

                return await _repository.RemoveAsync(item);
            }
            catch (Exception ex)
            {
                throw new Exception("Error deleting {entity}. " + ex.Message, ex);
            }
        }
    }
}
```

**Parent service.** It needs at least `GetAllAsync()` and `GetByIdAsync({Key})`, wrapped the same
way. Add full CRUD (same pattern as above) if the brief asks for it. Its `DeleteAsync` blocks
deletion while `{Entity}s.Any()`, using the same pattern.

**User account service:**

```csharp
using {Domain}.Entities.{Suffix}.Models;

namespace {Domain}.Services.{Suffix}
{
    public interface ISystemUserAccountService
    {
        Task<SystemUserAccount?> GetUserAccount(string userName, string password);
    }
}
```

```csharp
using {Domain}.Entities.{Suffix}.Models;
using {Domain}.Repositories.{Suffix};

namespace {Domain}.Services.{Suffix}
{
    public class SystemUserAccountService : ISystemUserAccountService
    {
        private readonly SystemUserAccountRepository _repository;

        public SystemUserAccountService()
            => _repository ??= new SystemUserAccountRepository();

        public async Task<SystemUserAccount?> GetUserAccount(string userName, string password)
        {
            try
            {
                return await _repository.GetUserAccount(userName, password);
            }
            catch (Exception ex)
            {
                throw new Exception("Error in GetUserAccount. " + ex.Message, ex);
            }
        }
    }
}
```

**Shared SignalR contracts** (needed from the SignalR assignment on; fine to add in Phase 1).
Every host references Services, so the hub's method names and the DTOs sent over the wire live
here. Hosts must never use them as scattered string literals.

```csharp
namespace {Domain}.Services.{Suffix}.Realtime
{
    public static class HubContract
    {
        public const string Path = "/hubs/{domain-lower}";
        // server → client
        public const string ItemChanged  = "ItemChanged";   // (string action, {EntityBase}Dto item)
        public const string Notification = "Notification";  // (string message)
        // client → server
        public const string Broadcast    = "Broadcast";     // (string message)
    }
}
```

```csharp
using {Domain}.Entities.{Suffix}.Models;

namespace {Domain}.Services.{Suffix}.Dtos
{
    public record {EntityBase}Dto({Key} Id, string {Search1}, string? {ParentName}, decimal? {Search2});

    public static class {EntityBase}DtoMapper
    {
        public static {EntityBase}Dto ToDto(this {Entity} e)
            => new(e.{EntityId}, e.{Search1}, e.{Parent}?.{ParentName}, e.{Search2});
    }
}
```

Convert entities to DTOs before sending them over SignalR. Generated entities have navigations in
both directions (`{Entity}.{Parent}.{Entity}s`), so serializing an entity directly to JSON loops
forever.

### 3.4 Copying the shared layers into a new solution (Phase 2+)

1. Create the new empty solution: `dotnet new sln --format slnx -n {NewSolution}`.
2. Copy the folders `{Domain}.Entities.{Suffix}`, `{Domain}.Repositories.{Suffix}` and
   `{Domain}.Services.{Suffix}`. **Leave out `bin/`, `obj/` and `.vs/`.**
3. `dotnet sln add` each `.csproj`, then add the new host projects and their project references (§2).
4. **Don't rename namespaces or classes while copying.** If the brief requires a different
   `{Suffix}`, rename projects, folders and namespaces in one pass, then rebuild.
5. Give each new host an `appsettings.json` containing `ConnectionStrings:DefaultConnection` (§5).
6. Run `dotnet build {NewSolution}.slnx` before writing any host code.

### 3.5 Phase 1 definition of done (agent checklist)

- [ ] `dotnet build` succeeds for the solution with 0 errors, and no new warnings in hand-written files.
- [ ] Generated `Models/*.cs` and `{DbContext}.cs` are unedited, apart from the moved DbContext's namespace.
- [ ] `{DbContext}.Configuration.cs` exists, and there is no hard-coded connection string anywhere.
- [ ] `GenericRepository<T>` matches §3.2 Step 3 exactly.
- [ ] Every table has a repository with both constructors.
- [ ] The main entity's repository has `GetAllAsync`, `GetByIdAsync({Key})` and `SearchAsync`,
      all using `Include`.
- [ ] The main entity's service implements all six interface members. The parent service has
      `GetAllAsync` and `GetByIdAsync`. The user account service has `GetUserAccount`.
- [ ] No `NotImplementedException`, no `TODO`, and no public members outside the interfaces in services.
- [ ] No `Console.WriteLine` and no host-specific code (HTTP, MVC, SignalR) in the shared layers.
- [ ] The MVC host (§4.1) can log in, list and search, create, edit and delete the main entity
      against the real database.

---

## 4. Hosts

### 4.0 Dependency injection (all hosts)

The services build their own dependencies, so they work without DI. In the hosts, **also
register them** so that controllers, pages, hubs, components and workers can inject them:

```csharp
builder.Services.AddScoped<I{EntityBase}Service{Suffix}, {EntityBase}Service{Suffix}>();
builder.Services.AddScoped<I{ParentBase}Service{Suffix}, {ParentBase}Service{Suffix}>();
builder.Services.AddScoped<ISystemUserAccountService, SystemUserAccountService>();
```

In **Blazor** use `AddTransient` instead. A circuit scope lives as long as the browser tab, and
a long-lived DbContext breaks when two operations overlap.

### 4.1 MvcWebApp — `{Domain}.MvcWebApp.{Suffix}` (Assignment01)

```
Controllers/
  HomeController.cs
  AccountController.cs                 # Login / Logout / Forbidden
  {EntityPlural}Controller.cs          # CRUD + search
Models/
  LoginViewModel.cs
  ErrorViewModel.cs                    # from template
Views/
  Shared/_Layout.cshtml                # nav: entity links + login/logout
  Shared/_ValidationScriptsPartial.cshtml
  Account/Login.cshtml, Forbidden.cshtml
  {EntityPlural}/Index|Create|Edit|Details|Delete.cshtml
appsettings.json                       # ConnectionStrings:DefaultConnection
```

**Scaffolding.** In VS, right-click Controllers and choose Add → New Scaffolded Item → **MVC
Controller with views, using Entity Framework**. Pick model `{Entity}` and context `{DbContext}`.
Keep the generated **views**, but rewrite the controller to use the services, as below. Then
delete the `AddDbContext` line the scaffolder adds to `Program.cs`, and remove any
`using {Domain}.Repositories...` it adds. The controller must not reference the DbContext.

`Controllers/{EntityPlural}Controller.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using {Domain}.Entities.{Suffix}.Models;
using {Domain}.Services.{Suffix};

namespace {Domain}.MvcWebApp.{Suffix}.Controllers
{
    [Authorize]
    public class {EntityPlural}Controller : Controller
    {
        private readonly I{EntityBase}Service{Suffix} _service;
        private readonly I{ParentBase}Service{Suffix} _parentService;

        public {EntityPlural}Controller(I{EntityBase}Service{Suffix} service, I{ParentBase}Service{Suffix} parentService)
        {
            _service = service;
            _parentService = parentService;
        }

        // GET: {EntityPlural}?{search1}=..&{search2}=..&{search3}=..
        public async Task<IActionResult> Index(string? {search1}, decimal? {search2}, string? {search3})
        {
            ViewData["{Search1}"] = {search1};
            ViewData["{Search2}"] = {search2};
            ViewData["{Search3}"] = {search3};
            var items = await _service.GetAllAsync({search1}, {search2}, {search3});
            return View(items);
        }

        public async Task<IActionResult> Details({Key}? id)
        {
            if (id == null) return NotFound();
            var item = await _service.GetByIdAsync(id.Value);
            if (item == null) return NotFound();
            return View(item);
        }

        public async Task<IActionResult> Create()
        {
            await LoadParentsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create({Entity} item)
        {
            ModelState.Remove(nameof({Entity}.{Parent}));      // navigation is not posted
            if (!ModelState.IsValid)
            {
                await LoadParentsAsync(item.{ParentId});
                return View(item);
            }
            await _service.CreateAsync(item);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit({Key}? id)
        {
            if (id == null) return NotFound();
            var item = await _service.GetByIdAsync(id.Value);
            if (item == null) return NotFound();
            await LoadParentsAsync(item.{ParentId});
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit({Key} id, {Entity} item)
        {
            if (id != item.{EntityId}) return NotFound();
            ModelState.Remove(nameof({Entity}.{Parent}));
            if (!ModelState.IsValid)
            {
                await LoadParentsAsync(item.{ParentId});
                return View(item);
            }
            item.{Parent} = null;                                // don't let EF touch the parent row
            await _service.UpdateAsync(item);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete({Key}? id)
        {
            if (id == null) return NotFound();
            var item = await _service.GetByIdAsync(id.Value);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed({Key} id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // e.g. "still has N related children" — show it on the Delete view
                ViewData["Error"] = ex.InnerException?.Message ?? ex.Message;
                var item = await _service.GetByIdAsync(id);
                return item == null ? RedirectToAction(nameof(Index)) : View(item);
            }
        }

        private async Task LoadParentsAsync(object? selected = null)
        {
            ViewData["{ParentId}"] = new SelectList(
                await _parentService.GetAllAsync(), "{ParentId}", "{ParentName}", selected);
        }
    }
}
```

View notes:
- **Index:** add a GET search form above the table. Its inputs are named exactly `{search1}`,
  `{search2}` and `{search3}`, and their `value` comes from `ViewData`. Show `item.{Parent}.{ParentName}`
  in its own column.
- **Create and Edit:** keep the scaffolded `<select asp-for="{ParentId}" asp-items="ViewBag.{ParentId}">`.
  Edit also needs a hidden input for `{EntityId}`, plus hidden inputs for any columns the form
  doesn't show but the update must keep, such as `CreatedAt`. Without them those columns are
  overwritten with defaults.
- **Delete:** render `ViewData["Error"]` in an `alert-danger` block when it's set.
- **Scaffolded `[Bind(...)]` lists:** if you keep them, they must include every column the form
  posts, including the hidden inputs.

`Models/LoginViewModel.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace {Domain}.MvcWebApp.{Suffix}.Models
{
    public class LoginViewModel
    {
        [Required] public string UserName { get; set; } = string.Empty;
        [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
    }
}
```

`Controllers/AccountController.cs`:

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using {Domain}.MvcWebApp.{Suffix}.Models;
using {Domain}.Services.{Suffix};

namespace {Domain}.MvcWebApp.{Suffix}.Controllers
{
    public class AccountController : Controller
    {
        private readonly ISystemUserAccountService _accountService;

        public AccountController(ISystemUserAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var account = await _accountService.GetUserAccount(model.UserName, model.Password);
            if (account == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, account.FullName),
                new(ClaimTypes.Email, account.Email),
                new(ClaimTypes.Role, account.RoleId.ToString())
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);
            return RedirectToAction("Index", "{EntityPlural}");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Forbidden() => View();
    }
}
```

Roles: if the brief limits actions by `RoleId`, use `[Authorize(Roles = "1,2")]` on the
controller or on individual actions. The role claim is the `RoleId` as a string.

`Program.cs`:

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using {Domain}.Services.{Suffix};

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Account/Login";
        o.AccessDeniedPath = "/Account/Forbidden";
        o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });
// service registrations (§4.0)

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}")
   .WithStaticAssets();

app.Run();
```

### 4.2 RazorWebApp — `{Domain}.RazorWebApp.{Suffix}`

```
Pages/
  Shared/_Layout.cshtml
  Account/Login.cshtml(.cs), Logout.cshtml(.cs), Forbidden.cshtml(.cs)
  {EntityPlural}/Index|Create|Edit|Details|Delete.cshtml(.cs)
Hubs/{Domain}Hub.cs                    # when the assignment includes SignalR (§4.3)
appsettings.json
```

- **Scaffolding:** generate the CRUD pages with **Razor Pages using Entity Framework (CRUD)**
  against `{DbContext}`. Then **replace every `_context` usage with the injected service**, and
  delete the scaffolded `AddDbContext` line.
- **Same logic as MVC:** each PageModel does what the matching MVC action in §4.1 does.
  - `OnGetAsync` replaces the GET actions.
  - `OnPostAsync` replaces the POST actions.
  - The form model is a `[BindProperty] public {Entity} {EntityBase} { get; set; }` property.
  - The `ModelState.Remove` key becomes `"{EntityBase}.{Parent}"`.
  - Dropdowns go in `ViewData["{ParentId}"]`.
- **Search:** the Index page takes its filters as `[BindProperty(SupportsGet = true)]` properties
  and calls the service's search overload.
- **Login:** the same claims logic as `AccountController.Login`, inside `Login.cshtml.cs`'s
  `OnPostAsync`.
- **`Program.cs`:** call `AddRazorPages()` and set up cookie auth (same options as MVC), then
  `UseAuthentication()` → `UseAuthorization()` → `MapRazorPages().WithStaticAssets()`. Protect
  folders with
  `AddRazorPages(o => o.Conventions.AuthorizeFolder("/{EntityPlural}"))`.

### 4.3 SignalR (hub inside a web host)

The hub lives **inside a web host**. By default that's the RazorWebApp; use the MVC app if the
SignalR assignment is MVC-based. Its own pages are the main clients. The Blazor and Worker hosts
can connect to it as optional external clients when they're in the same solution.

```csharp
// Hubs/{Domain}Hub.cs
using Microsoft.AspNetCore.SignalR;
using {Domain}.Services.{Suffix}.Realtime;

public class {Domain}Hub : Hub
{
    // Called by other clients (Blazor, Worker, browser JS) to fan a message out to everyone.
    public Task Broadcast(string message)
        => Clients.All.SendAsync(HubContract.Notification, message);
}
```

`Program.cs` additions: `builder.Services.AddSignalR();` and
`app.MapHub<{Domain}Hub>(HubContract.Path);`.

- **Server push after writes:** inject `IHubContext<{Domain}Hub>` into the PageModel or
  controller. After each successful Create, Edit or Delete, send
  `await _hub.Clients.All.SendAsync(HubContract.ItemChanged, "created", item.ToDto());`.
- **Browser client:**
  1. Add `@microsoft/signalr` via LibMan to `wwwroot/lib/microsoft/signalr/`.
  2. Reference `dist/browser/signalr.min.js` in `_Layout`.
  3. In `wwwroot/js/site.js`:
     - build the connection with
       `new signalR.HubConnectionBuilder().withUrl("/hubs/{domain-lower}").withAutomaticReconnect().build()`;
     - handle `ItemChanged` by reloading the list, or by updating the row in the DOM;
     - handle `Notification` by showing a toast.
- **Authorization:** leave the hub **anonymous** (no `[Authorize]`) unless the brief requires
  otherwise. External .NET clients can't reuse the browser's login cookie.

### 4.4 BlazorWebApp — `{Domain}.BlazorWebApp.{Suffix}`

```
Components/
  App.razor, Routes.razor, _Imports.razor
  Layout/MainLayout.razor, NavMenu.razor
  Pages/{EntityPlural}/Index.razor     # list + search (+ live updates if HubUrl set)
  Pages/{EntityPlural}/Create.razor, Edit.razor, Details.razor, Delete.razor
appsettings.json                       # ConnectionStrings + optional "HubUrl"
```

- **Render mode:** put `@rendermode InteractiveServer` on interactive pages.
- **Data access:** inject services directly (`@inject I{EntityBase}Service{Suffix} Service`).
  Blazor Server runs on the server, so it doesn't need an HTTP API.
- **Forms:** use `EditForm` with `DataAnnotationsValidator`, and an `InputSelect` filled from
  `I{ParentBase}Service{Suffix}.GetAllAsync()` for the parent.
- **Optional live updates:** connect only when `HubUrl` is configured, and implement `IAsyncDisposable`:

```csharp
protected override async Task OnInitializedAsync()
{
    _items = await Service.GetAllAsync();

    var hubUrl = Config["HubUrl"];
    if (string.IsNullOrWhiteSpace(hubUrl)) return;          // standalone mode

    _hub = new HubConnectionBuilder().WithUrl(hubUrl).WithAutomaticReconnect().Build();
    _hub.On<string, {EntityBase}Dto>(HubContract.ItemChanged, async (action, dto) =>
    {
        _items = await Service.GetAllAsync();
        await InvokeAsync(StateHasChanged);                 // always marshal back to the renderer
    });
    await _hub.StartAsync();
}

public async ValueTask DisposeAsync()
{
    if (_hub is not null) await _hub.DisposeAsync();
}
```

### 4.5 WorkerService — `{Domain}.WorkerService.{Suffix}`

```
Program.cs
Worker.cs                 # BackgroundService
WorkerOptions.cs          # IntervalSeconds, HubUrl (optional)
appsettings.json          # ConnectionStrings + "Worker": { "IntervalSeconds": 30, "HubUrl": "" }
```

What it does: on a fixed interval it asks the Services layer for rows changed since the last tick,
logs them, and pushes a summary through the hub if `HubUrl` is set. Use the generated `CreatedAt`
column, or whatever business rule the brief gives, such as inactive or expired rows.

```csharp
public class WorkerOptions
{
    public int IntervalSeconds { get; set; } = 30;
    public string? HubUrl { get; set; }
}

public class Worker(IServiceScopeFactory scopes, IOptions<WorkerOptions> opt, ILogger<Worker> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        HubConnection? hub = null;
        if (!string.IsNullOrWhiteSpace(opt.Value.HubUrl))
        {
            hub = new HubConnectionBuilder().WithUrl(opt.Value.HubUrl).WithAutomaticReconnect().Build();
            await ConnectWithRetryAsync(hub, ct);    // hub host may start after the worker
        }

        var lastCheck = DateTime.Now;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(opt.Value.IntervalSeconds));
        while (await timer.WaitForNextTickAsync(ct))
        {
            try
            {
                using var scope = scopes.CreateScope();          // fresh service + DbContext per tick
                var svc = scope.ServiceProvider.GetRequiredService<I{EntityBase}Service{Suffix}>();
                var fresh = (await svc.GetAllAsync()).Where(e => e.CreatedAt > lastCheck).ToList();
                lastCheck = DateTime.Now;

                log.LogInformation("{Count} new item(s) since last check", fresh.Count);
                if (fresh.Count > 0 && hub is not null)
                    await hub.InvokeAsync(HubContract.Broadcast, $"{fresh.Count} new item(s)", ct);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Worker tick failed");          // never kill the loop
            }
        }

        if (hub is not null) await hub.DisposeAsync();
    }

    private async Task ConnectWithRetryAsync(HubConnection hub, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await hub.StartAsync(ct); return; }
            catch (Exception ex) { log.LogWarning(ex, "Hub not reachable, retrying in 5s"); }
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
        }
    }
}
```

`Program.cs` uses `Host.CreateApplicationBuilder(args)`, binds the options with
`builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"))`, adds the
service registrations (§4.0) and calls `AddHostedService<Worker>()`.

---

## 5. Configuration

Every host's `appsettings.json` contains:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=localhost;Initial Catalog={Db};Persist Security Info=True;User ID=sa;Password=<pwd>;Encrypt=False"
}
```

Optional keys: Blazor `HubUrl`; Worker `Worker:IntervalSeconds` and `Worker:HubUrl`. A hub URL is
the hub host's **https** `applicationUrl` (from its `Properties/launchSettings.json`) followed by
`HubContract.Path`. Leave it empty to run the host standalone.

## 6. Build & run

```bash
dotnet build {Solution}.slnx
dotnet run --project {Domain}.MvcWebApp.{Suffix} --launch-profile https
# Multi-host solutions — start order: SQL Server → hub host → Blazor → Worker
dotnet run --project {Domain}.RazorWebApp.{Suffix} --launch-profile https
dotnet run --project {Domain}.BlazorWebApp.{Suffix} --launch-profile https
dotnet run --project {Domain}.WorkerService.{Suffix}
```

In Visual Studio, set the startup project. For several hosts, go to Solution → Configure Startup
Projects and choose **Multiple startup projects**. Trust the dev certificate once with
`dotnet dev-certs https --trust`.

SignalR smoke test: open the hub host in two browser tabs. Create an item in one tab and the other
should update without a reload. If Blazor or the Worker is configured with `HubUrl`, their updates
and notifications should appear too.

---

## 7. Agent rules

1. **Dependency graph:** follow §2. If a host needs data, add a method to the **Service**
   interface and implementation, backed by a Repository method. Never query the DbContext from a
   host, and remove any `AddDbContext` the scaffolder adds.
2. **Shared layers are Phase 1 output.** In Phase 2+, change them only to add members. Never
   rename or remove existing ones, because earlier solutions and hosts depend on them.
3. **Conventions:** new code follows the patterns in §3:
   - `{Suffix}` naming
   - `Base/` and `DBContext/` folders
   - `new`-hidden reads that add `.Include`
   - try/catch-rethrow in services
   - interfaces list queries before mutations
4. **Generated files:** don't edit `// <auto-generated>` files. Regenerate them, or add a `partial` class.
5. **Standalone hosts:** every host builds and runs on its own. Cross-host SignalR is optional
   and controlled by configuration.
6. **SignalR:** method names come only from `HubContract`, and payloads are DTOs.
7. **Blazor:** hub callbacks must call `InvokeAsync(StateHasChanged)`, and components that open a
   hub connection implement `IAsyncDisposable`.
8. **Worker:** loops must catch exceptions on each tick and honour the `CancellationToken`.
9. **Build:** run `dotnet build` on the solution after every change, and fix any warnings you introduced.

### Known issues in the reference, and what this template does instead

| Reference | This template |
|---|---|
| Mixed target frameworks (net8.0 libraries, net10.0 web app) | One TFM (net10.0) everywhere |
| Hard-coded `OnConfiguring` edited inside the generated DbContext | Separate partial `{DbContext}.Configuration.cs` with an `IsConfigured` guard |
| Web app `appsettings.json` has no `ConnectionStrings`, so the DbContext throws at runtime | Every host has `DefaultConnection` |
| `GetById` only has `int`/`string`/`Guid` overloads, so `long` keys fail | `long` overloads added |
| `NotImplementedException` stubs (`RemoveAsync(Task<...>)`, `GetByIdAsync(object...)`, `GetByIdAsync(int)`) | Every member implemented, and no extra public members |
| `RemoveAsync` takes a `Task<T>` instead of an awaited `T` | Removed |
| Delete fails with a raw FK error when child rows exist | Service checks for children and throws `InvalidOperationException` with a clear message, which the MVC Delete view displays |
| `GetUserAccount` compares plaintext passwords | Kept, because the course DB stores them that way, but passwords are never logged, put in claims, or exposed in DTOs |
