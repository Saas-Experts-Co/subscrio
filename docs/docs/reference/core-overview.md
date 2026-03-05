# Subscrio Core API Reference

Complete documentation of all data structures and methods exposed by Subscrio Core (TypeScript: `core.typescript`, .NET: `core.dotnet`).

## Main Class

### Subscrio

=== "TypeScript"
    ```typescript
    import { Subscrio } from 'core.typescript';

    const subscrio = new Subscrio({
      database: {
        connectionString: process.env.DATABASE_URL
      }
    });
    ```

=== ".NET"
    Direct construction:

    ```csharp
    using Subscrio.Core;

    var subscrio = new Subscrio(new SubscrioConfig
    {
        Database = new DatabaseConfig
        {
            ConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL") ?? ""
        }
    });
    ```

    **Dependency injection (ASP.NET Core / generic host):** Register Subscrio with the service collection so it is resolved per scope (recommended for web apps). Add `using Subscrio.Core.DependencyInjection;` and call `services.AddSubscrio(config, ServiceLifetime.Scoped)`. Then inject `Subscrio` in controllers or minimal API handlers. Use `ServiceLifetime.Scoped` for web apps; use `Transient` for console or background services. See [Getting Started](getting-started.md) for a full bootstrap and DI example.

## Method Catalog

=== "TypeScript"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `installSchema` | Creates every Subscrio database table, seeds configuration rows, and optionally writes the admin passphrase hash | `Promise<void>` |
    | `migrate` | Runs pending database migrations to update the schema to the latest version | `Promise<number>` |
    | `verifySchema` | Confirms whether the Subscrio schema is already installed and returns the current schema version | `Promise<string \| null>` |
    | `dropSchema` | Removes every table created by Subscrio (for local development resets or automated tests) | `Promise<void>` |
    | `close` | Closes the database connection pool | `Promise<void>` |

=== ".NET"
    | Method | Description | Returns |
    | --- | --- | --- |
    | `InstallSchemaAsync` | Creates every Subscrio database table, seeds configuration rows, and optionally writes the admin passphrase hash | `Task` |
    | `MigrateAsync` | Runs pending database migrations to update the schema to the latest version | `Task<int>` |
    | `VerifySchemaAsync` | Confirms whether the Subscrio schema is already installed and returns the current schema version | `Task<string?>` |
    | `DropSchemaAsync` | Removes every table created by Subscrio (for local development resets or automated tests) | `Task` |
    | `Dispose` | Closes the database connection pool | `void` |

## Method Reference

### Constructor

#### Description
 Instantiates the core library, initializes the database connection, and wires every repository and service so callers can use Subscrio synchronously after construction.

#### Signature

=== "TypeScript"
    ```typescript
    new Subscrio(config: SubscrioConfig)
    ```

=== ".NET"
    ```csharp
    new Subscrio(SubscrioConfig config)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `config` | [`SubscrioConfig`](#configuration-object) | Yes | Database connection plus optional passphrase, Stripe, and logging overrides. |

#### Input Properties

- [`SubscrioConfig`](#configuration-object) – high-level shape that includes the `database`, `stripe`, and `logging` objects defined later in this page.

#### Returns

=== "TypeScript"
    Creates a new `Subscrio` instance that exposes the services listed in the “Public Services” table above.

=== ".NET"
    Creates a new `Subscrio` instance that exposes the services listed in the "Public Services" table above.

#### Return Properties

=== "TypeScript"
    - `Subscrio` – instance with properties such as `products`, `plans`, `featureChecker`, etc.

=== ".NET"
    - `Subscrio` – instance with properties such as `Products`, `Plans`, `FeatureChecker`, etc.

#### Expected Results

- Initializes a Postgres database connection using `config.database`.
- Constructs repository instances and wires each service with its dependencies.
- Keeps a shared schema installer for schema management helpers.

#### Potential Errors

| Error | When |
| --- | --- |
| `ConfigurationError` | Thrown downstream if `config` is invalid or a database connection cannot be established. |

#### Example

=== "TypeScript"
    ```typescript
    const subscrio = new Subscrio({
      database: { connectionString: process.env.DATABASE_URL! },
      adminPassphrase: process.env.ADMIN_PASSPHRASE
    });
    ```

=== ".NET"
    ```csharp
    var subscrio = new Subscrio(new SubscrioConfig
    {
        Database = new DatabaseConfig { ConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")! },
        AdminPassphrase = Environment.GetEnvironmentVariable("ADMIN_PASSPHRASE")
    });
    ```

#### Configuration object

`Subscrio` consumes a strongly typed config. Only `database.connectionString` (TypeScript) / `Database.ConnectionString` (.NET) is required; every other field is optional.

=== "TypeScript"
    Config is defined in `src/config/types.ts`:

    ```typescript
    export interface SubscrioConfig {
      database: {
        connectionString: string;
        ssl?: boolean;
        poolSize?: number;
      };
      adminPassphrase?: string;
      stripe?: { secretKey: string };
      logging?: { level: 'debug' | 'info' | 'warn' | 'error' };
    }
    ```

=== ".NET"
    Config is defined in `Subscrio.Core.Config`:

    ```csharp
    public class SubscrioConfig
    {
        public required DatabaseConfig Database { get; init; }
        public string? AdminPassphrase { get; init; }
        public StripeConfig? Stripe { get; init; }
        public LoggingConfig? Logging { get; init; }
    }

    public class DatabaseConfig
    {
        public required string ConnectionString { get; init; }
        public bool Ssl { get; init; }
        public int PoolSize { get; init; } = 10;
    }
    ```

##### `database` object

=== "TypeScript"
    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `connectionString` | `string` | Yes | Full Postgres URI (`postgresql://user:pass@host:port/db`). |
    | `ssl` | `boolean` | No | Forces SSL when running outside trusted networks. |
    | `poolSize` | `number` | No | Custom pg pool size; defaults to driver preset. |

=== ".NET"
    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `ConnectionString` | `string` | Yes | Full Postgres connection string. |
    | `Ssl` | `bool` | No | Forces SSL when running outside trusted networks. |
    | `PoolSize` | `int` | No | Custom pool size; defaults to 10. |

##### `adminPassphrase`

Optional override for the admin passphrase hash stored during `installSchema()` / `InstallSchemaAsync()`. If omitted you can pass the passphrase directly to the install method.

##### `stripe` object

=== "TypeScript"
    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `secretKey` | `string` | Yes | Server-side Stripe secret used by helpers like `createStripeSubscription`. |

=== ".NET"
    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `SecretKey` | `string` | Yes | Server-side Stripe secret used by helpers like `CreateStripeSubscriptionAsync`. |

##### `logging` object

=== "TypeScript"
    | Field | Type | Required | Description |
    | --- | --- | --- | --- |
    | `level` | `'debug' \| 'info' \| 'warn' \| 'error'` | Yes | Sets global log verbosity for Subscrio internals. |

=== ".NET"
    | Property | Type | Required | Description |
    | --- | --- | --- | --- |
    | `Level` | `string` | Yes | `"debug"`, `"info"`, `"warn"`, or `"error"` – sets global log verbosity. |

### installSchema

#### Description
 Creates every Subscrio database table, seeds configuration rows, and optionally writes the admin passphrase hash when setting up a fresh environment.

#### Signature

=== "TypeScript"
    ```typescript
    installSchema(adminPassphrase?: string): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task InstallSchemaAsync(string? adminPassphrase = null)
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| `adminPassphrase` | `string` | No | Optional override that supersedes `config.adminPassphrase`. |

#### Input Properties

- `adminPassphrase` – plain text string that will be hashed before being stored in `system_config`.

#### Returns

=== "TypeScript"
    `Promise<void>` – resolves when the schema is fully installed.

=== ".NET"
    `Task` – completes when the schema is fully installed.

#### Return Properties

=== "TypeScript"
    - `void`

=== ".NET"
    - None (`Task` returns no value)

#### Expected Results

- Runs the schema installer to create all tables, extensions, and seed configuration rows.
- Stores the admin passphrase hash when provided.

#### Potential Errors

| Error | When |
| --- | --- |
| `ConfigurationError` | Database connection unavailable or migration prerequisites missing. |
| `DomainError` | Passphrase validation fails the policy enforced by the installer. |

#### Example

=== "TypeScript"
    ```typescript
    await subscrio.installSchema('super-secret-passphrase');
    ```

=== ".NET"
    ```csharp
    await subscrio.InstallSchemaAsync("super-secret-passphrase");
    ```

### migrate

#### Description
Runs pending database migrations to update the schema to the latest version. Migrations are tracked via `schema_version` in the `system_config` table, so only pending migrations are applied.

#### Signature

=== "TypeScript"
    ```typescript
    migrate(): Promise<number>
    ```

=== ".NET"
    ```csharp
    Task<int> MigrateAsync()
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| _None_ |  |  |  |

#### Input Properties

- None.

#### Returns

=== "TypeScript"
    `Promise<number>` – resolves to the number of migrations applied.

=== ".NET"
    `Task<int>` – resolves to the number of migrations applied.

#### Return Properties

=== "TypeScript"
    - `number` – count of migrations that were applied (0 if database is up to date).

=== ".NET"
    - `int` – count of migrations that were applied (0 if database is up to date).

#### Expected Results

- Checks current schema version from `system_config`.
- Runs only pending migrations (those with version numbers greater than current).
- Updates `schema_version` in `system_config` after each migration.
- Returns count of migrations applied.

#### Potential Errors

| Error | When |
| --- | --- |
| `ConfigurationError` | Database connection unavailable or migration fails. |

#### Example

=== "TypeScript"
    ```typescript
    const migrationsApplied = await subscrio.migrate();
    if (migrationsApplied > 0) {
      console.log(`Applied ${migrationsApplied} migration(s)`);
    } else {
      console.log('Database is up to date');
    }
    ```

    Or via CLI: `npm run migrate` or `npx subscrio migrate`

=== ".NET"
    ```csharp
    var migrationsApplied = await subscrio.MigrateAsync();
    if (migrationsApplied > 0)
    {
        Console.WriteLine($"Applied {migrationsApplied} migration(s)");
    }
    else
    {
        Console.WriteLine("Database is up to date");
    }
    ```

### verifySchema

#### Description
 Confirms whether the Subscrio schema is already installed and returns the current schema version. Returns `null` if the schema is not installed, allowing callers to decide whether to run `installSchema()` or proceed with normal operations.

#### Signature

=== "TypeScript"
    ```typescript
    verifySchema(): Promise<string | null>
    ```

=== ".NET"
    ```csharp
    Task<string?> VerifySchemaAsync()
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| _None_ |  |  |  |

#### Input Properties

- None.

#### Returns

=== "TypeScript"
    `Promise<string | null>` – resolves to the current schema version (e.g., `"1.1.0"`) when installed, or `null` if not installed.

=== ".NET"
    `Task<string?>` – resolves to the current schema version (e.g., `"1.1.0"`) when installed, or `null` if not installed.

#### Return Properties

=== "TypeScript"
    - `string | null` – the schema version string if installed, or `null` if not installed.

=== ".NET"
    - `string?` – the schema version string if installed, or `null` if not installed.

#### Expected Results

- Executes lightweight checks on required tables and indexes via the schema installer.
- If schema exists, retrieves and returns the current schema version from `system_config`.
- Returns `null` if schema is not installed or version cannot be determined.

#### Potential Errors

| Error | When |
| --- | --- |
| `ConfigurationError` | Database connection is unavailable. |

#### Example

=== "TypeScript"
    ```typescript
    const version = await subscrio.verifySchema();
    if (version === null) {
      console.warn('Subscrio schema missing – run installSchema() first.');
    } else {
      console.log(`Schema version: ${version}`);
    }
    ```

=== ".NET"
    ```csharp
    var version = await subscrio.VerifySchemaAsync();
    if (version == null)
    {
        Console.WriteLine("Subscrio schema missing – run InstallSchemaAsync() first.");
    }
    else
    {
        Console.WriteLine($"Schema version: {version}");
    }
    ```

### dropSchema

#### Description
 Removes every table created by Subscrio. Intended for local development resets or automated tests.

#### Signature

=== "TypeScript"
    ```typescript
    dropSchema(): Promise<void>
    ```

=== ".NET"
    ```csharp
    Task DropSchemaAsync()
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| _None_ |  |  |  |

#### Input Properties

- None.

#### Returns

=== "TypeScript"
    `Promise<void>` – resolves after the installer drops all managed tables.

=== ".NET"
    `Task` – completes after the installer drops all managed tables.

#### Return Properties

=== "TypeScript"
    - `void`

=== ".NET"
    - None

#### Expected Results

- Drops every Subscrio-owned table via the installer. This is destructive and meant for local resets/tests.

#### Potential Errors

| Error | When |
| --- | --- |
| `ConfigurationError` | Database refuses the drop (permissions, locks). |

#### Example

=== "TypeScript"
    ```typescript
    if (process.env.NODE_ENV === 'test') {
      await subscrio.dropSchema();
    }
    ```

=== ".NET"
    ```csharp
    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing")
    {
        await subscrio.DropSchemaAsync();
    }
    ```

### close

#### Description
 Closes the shared database connection pool so the process can exit cleanly.

#### Signature

=== "TypeScript"
    ```typescript
    close(): Promise<void>
    ```

=== ".NET"
    ```csharp
    void Dispose()
    ```

#### Inputs

| Name | Type | Required | Description |
| --- | --- | --- | --- |
| _None_ |  |  |  |

#### Input Properties

- None.

#### Returns

=== "TypeScript"
    `Promise<void>` – resolves after all connections are closed.

=== ".NET"
    `void` – `Dispose()` is synchronous; use `await using` for proper cleanup.

#### Return Properties

=== "TypeScript"
    - `void`

=== ".NET"
    - None

#### Expected Results

- Closes database connections and releases resources.

#### Potential Errors

| Error | When |
| --- | --- |
| `ConfigurationError` | Database connection has already been torn down unexpectedly. |

#### Example

=== "TypeScript"
    ```typescript
    await subscrio.close();
    ```

=== ".NET"
    ```csharp
    subscrio.Dispose();
    // Or use: await using var subscrio = new Subscrio(config);
    ```

---

## Service Reference Index

All service-level documentation now lives in dedicated markdown files so each method, DTO, error, and example can be described in depth. The following table shows where to find those references:

| Service | Scope | Reference |
| --- | --- | --- |
| ProductManagementService | Product CRUD, feature associations | [`products.md`](./products.md) |
| FeatureManagementService | Global feature definitions | [`features.md`](./features.md) |
| PlanManagementService | Plans, feature values, transitions | [`plans.md`](./plans.md) |
| BillingCycleManagementService | Billing cadence + price mappings | [`billing-cycles.md`](./billing-cycles.md) |
| CustomerManagementService | Customer lifecycle | [`customers.md`](./customers.md) |
| SubscriptionManagementService | Subscriptions, overrides, batch jobs | [`subscriptions.md`](./subscriptions.md) |
| FeatureCheckerService | Runtime feature resolution APIs | [`feature-checker.md`](./feature-checker.md) |
| StripeIntegrationService | Stripe webhook processing & helpers | [`stripe-integration.md`](./stripe-integration.md) |

> Every service doc follows a standard structure that standardizes sections for usage, inputs/outputs, DTOs, expected results, errors, and working examples.

## Additional Reference Guides

- `subscriptions.md` covers CRUD APIs, DTOs, overrides, and lifecycle automation APIs.
- `subscription-lifecycle.md` fully documents how each status is calculated (with diagrams) and how transitions work.
- `relationships.md` centralizes the product/plan/feature/billing-cycle/customer relationships, the feature resolution hierarchy, and the customer key conventions.
- `products.md`, `plans.md`, `features.md`, and `billing-cycles.md` document CRUD flows, DTOs, and association helpers for each domain surface.
- `feature-checker.md` explains the subscription override → plan value → feature default resolution order in depth.
- `customers.md` details how caller-supplied customer keys map to internal IDs and where they are required.
- `stripe-integration.md` contains the full Stripe workflow, including where signature verification must happen before calling `processStripeEvent()`.
