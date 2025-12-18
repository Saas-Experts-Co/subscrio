# Subscrio .NET Core Library

A .NET implementation of the Subscrio subscription management library, providing comprehensive features for managing products, plans, features, customers, subscriptions, and billing cycles.

## Supported .NET Versions

This library supports the following .NET versions:
- **.NET 8.0** (LTS - supported until November 2026)
- **.NET 9.0**
- **.NET 10.0**

The library is multi-targeted, meaning a single NuGet package contains builds for all supported frameworks. The appropriate build will be automatically selected based on your project's target framework.

## Features

- **Product Management** - Create and manage products with features
- **Feature Management** - Define features with different value types (toggle, numeric, text)
- **Plan Management** - Create plans with feature values and billing cycles
- **Customer Management** - Manage customer records and external IDs
- **Subscription Management** - Handle subscriptions with feature overrides
- **Billing Cycle Management** - Define billing cycles (monthly, yearly, etc.)
- **Feature Resolution** - Hierarchical feature value resolution (Subscription Override > Plan Value > Feature Default)
- **Stripe Integration** - Process Stripe webhooks and manage Stripe subscriptions
- **Configuration Sync** - Import/export configuration from JSON

## Installation

Add the NuGet package to your project:

```xml
<PackageReference Include="Subscrio.Core" Version="1.0.0" />
```

Or using the .NET CLI:

```bash
dotnet add package Subscrio.Core
```

## Quick Start

### Manual Instantiation

```csharp
using Subscrio.Core;
using Subscrio.Core.Config;

// Load configuration
var config = ConfigLoader.Load();

// Create Subscrio instance
var subscrio = new Subscrio(config);

// Install schema (first time only)
await subscrio.InstallSchemaAsync("your-admin-passphrase");

// Use the library
var product = await subscrio.Products.CreateProductAsync(new CreateProductDto(
    Key: "my-product",
    DisplayName: "My Product"
));
```

## Dependency Injection

Subscrio supports dependency injection and can be registered in your DI container. This is the recommended approach for web applications and provides better lifetime management.

### Registration

#### ASP.NET Core Web API

```csharp
using Subscrio.Core;
using Subscrio.Core.Config;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var config = ConfigLoader.Load();

// Register Subscrio with Scoped lifetime (recommended for web apps)
builder.Services.AddSubscrio(config, ServiceLifetime.Scoped);

var app = builder.Build();

// Use in controllers
app.MapGet("/products", async (Subscrio subscrio) =>
{
    var products = await subscrio.Products.ListProductsAsync();
    return Results.Ok(products);
});

app.Run();
```

#### Console Application with DI

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Subscrio.Core;
using Subscrio.Core.Config;

var config = ConfigLoader.Load();

var services = new ServiceCollection();
services.AddSubscrio(config, ServiceLifetime.Scoped);

var serviceProvider = services.BuildServiceProvider();

// Use scoped service
using var scope = serviceProvider.CreateScope();
var subscrio = scope.ServiceProvider.GetRequiredService<Subscrio>();

var products = await subscrio.Products.ListProductsAsync();
```

#### Controller Injection

```csharp
using Microsoft.AspNetCore.Mvc;
using Subscrio.Core;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly Subscrio _subscrio;

    public ProductsController(Subscrio subscrio)
    {
        _subscrio = subscrio; // Injected by DI
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductDto>>> GetProducts()
    {
        var products = await _subscrio.Products.ListProductsAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        var product = await _subscrio.Products.CreateProductAsync(dto);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
    }
}
```

### Service Lifetime Options

The `AddSubscrio()` method accepts a `ServiceLifetime` parameter:

#### Scoped (Recommended for Web Applications)

```csharp
services.AddSubscrio(config, ServiceLifetime.Scoped);
```

- **When to use**: ASP.NET Core web applications
- **Behavior**: One `Subscrio` instance per HTTP request
- **Benefits**: 
  - Each request gets a fresh `Subscrio` instance
  - Each instance creates its own `DbContext`
  - Change tracker is clean for each request
  - No tracking conflicts between requests
  - Proper resource cleanup after request completes

#### Transient

```csharp
services.AddSubscrio(config, ServiceLifetime.Transient);
```

- **When to use**: Console applications, background services, or when you need a new instance each time
- **Behavior**: New `Subscrio` instance every time it's requested
- **Benefits**: Maximum isolation, each operation gets a fresh instance

#### Singleton

```csharp
services.AddSubscrio(config, ServiceLifetime.Singleton);
```

- **When to use**: Rarely recommended - only if you want a single instance for the entire application lifetime
- **Behavior**: One `Subscrio` instance shared across the entire application
- **Warning**: Can cause EF Core tracking conflicts if multiple operations use the same instance
- **Not recommended** for web applications

### How Subscrio Creates DbContext

Subscrio creates its own `DbContext` internally when instantiated. This means:

- **With Scoped lifetime**: Each scope (HTTP request) gets a new `Subscrio` → new `DbContext` → clean change tracker
- **With Transient lifetime**: Each request gets a new `Subscrio` → new `DbContext` → clean change tracker
- **With Singleton lifetime**: Single `Subscrio` → single `DbContext` → potential tracking conflicts

**Best Practice**: Use `ServiceLifetime.Scoped` for web applications to ensure proper isolation and resource management.

## Configuration

### Configuration Structure

```csharp
var config = new SubscrioConfig
{
    Database = new DatabaseConfig
    {
        ConnectionString = "Host=localhost;Port=5432;Database=subscrio;Username=postgres;Password=password",
        DatabaseType = DatabaseType.PostgreSQL, // or DatabaseType.SqlServer
        Ssl = false,
        PoolSize = 10
    },
    Stripe = new StripeConfig
    {
        SecretKey = "sk_test_..." // Optional
    }
};
```

### Loading Configuration

#### From Environment Variables

```csharp
var config = ConfigLoader.Load();
```

This reads from:
- `DATABASE_URL` - Database connection string
- `DATABASE_TYPE` - "PostgreSQL" or "SqlServer"
- `STRIPE_SECRET_KEY` - Stripe secret key (optional)

#### From Custom Source

```csharp
var config = new SubscrioConfig
{
    Database = new DatabaseConfig
    {
        ConnectionString = "your-connection-string",
        DatabaseType = DatabaseType.PostgreSQL
    }
};
```

## Database Setup

### First-Time Installation

```csharp
var subscrio = new Subscrio(config);
await subscrio.InstallSchemaAsync("your-secure-admin-passphrase");
```

### Verify Schema

```csharp
var version = await subscrio.VerifySchemaAsync();
if (version == null)
{
    await subscrio.InstallSchemaAsync("your-admin-passphrase");
}
```

### Run Migrations

```csharp
var migrationsApplied = await subscrio.MigrateAsync();
Console.WriteLine($"Applied {migrationsApplied} migrations");
```

## Usage Examples

### Create a Product with Features

```csharp
// Create product
var product = await subscrio.Products.CreateProductAsync(new CreateProductDto(
    Key: "saas-platform",
    DisplayName: "SaaS Platform"
));

// Create features
var maxProjects = await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
    Key: "max-projects",
    DisplayName: "Max Projects",
    ValueType: FeatureValueType.Numeric,
    DefaultValue: "10"
));

var ganttCharts = await subscrio.Features.CreateFeatureAsync(new CreateFeatureDto(
    Key: "gantt-charts",
    DisplayName: "Gantt Charts",
    ValueType: FeatureValueType.Toggle,
    DefaultValue: "false"
));

// Associate features with product
await subscrio.Products.AssociateFeatureAsync(product.Key, maxProjects.Key);
await subscrio.Products.AssociateFeatureAsync(product.Key, ganttCharts.Key);
```

### Create Plans with Feature Values

```csharp
// Create plan
var basicPlan = await subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
    ProductKey: product.Key,
    Key: "basic",
    DisplayName: "Basic Plan"
));

var proPlan = await subscrio.Plans.CreatePlanAsync(new CreatePlanDto(
    ProductKey: product.Key,
    Key: "pro",
    DisplayName: "Pro Plan"
));

// Set feature values for plans
await subscrio.Plans.SetFeatureValueAsync(proPlan.Key, maxProjects.Key, "50");
await subscrio.Plans.SetFeatureValueAsync(proPlan.Key, ganttCharts.Key, "true");
```

### Create Billing Cycles

```csharp
// Create monthly billing cycle
var monthly = await subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
    PlanKey: proPlan.Key,
    Key: "pro-monthly",
    DisplayName: "Pro Monthly",
    DurationValue: 1,
    DurationUnit: "months"
));

// Create yearly billing cycle
var yearly = await subscrio.BillingCycles.CreateBillingCycleAsync(new CreateBillingCycleDto(
    PlanKey: proPlan.Key,
    Key: "pro-yearly",
    DisplayName: "Pro Yearly",
    DurationValue: 1,
    DurationUnit: "years"
));
```

### Create Customers and Subscriptions

```csharp
// Create customer
var customer = await subscrio.Customers.CreateCustomerAsync(new CreateCustomerDto(
    ExternalId: "customer-123",
    DisplayName: "John Doe"
));

// Create subscription
var subscription = await subscrio.Subscriptions.CreateSubscriptionAsync(new CreateSubscriptionDto(
    Key: "sub-001",
    CustomerKey: customer.Key,
    BillingCycleKey: monthly.Key
));

// Add feature override
await subscrio.Subscriptions.AddFeatureOverrideAsync(
    subscription.Key,
    maxProjects.Key,
    "100",
    OverrideType.Permanent
);
```

### Check Feature Values

```csharp
// Check if feature is enabled
var hasGanttCharts = await subscrio.FeatureChecker.IsEnabledAsync(
    customer.ExternalId,
    ganttCharts.Key
);

// Get feature value
var maxProjectsValue = await subscrio.FeatureChecker.GetValueAsync(
    customer.ExternalId,
    maxProjects.Key
);

// Get all features
var allFeatures = await subscrio.FeatureChecker.GetAllFeaturesAsync(customer.ExternalId);
```

## Feature Resolution Hierarchy

Feature values are resolved in this order (highest to lowest priority):

1. **Subscription Override** - Feature value set directly on the subscription
2. **Plan Value** - Feature value set on the plan
3. **Feature Default** - Default value defined on the feature

This allows fine-grained control over feature access per customer while maintaining sensible defaults.

## Stripe Integration

### Process Stripe Webhooks

```csharp
// In your webhook endpoint (after verifying Stripe signature)
await subscrio.Stripe.ProcessStripeEventAsync(stripeEvent);
```

### Create Stripe Subscription

```csharp
var subscription = await subscrio.Stripe.CreateStripeSubscriptionAsync(
    customerExternalId: customer.ExternalId,
    planKey: proPlan.Key,
    renewalCycleKey: monthly.Key
);
```

## Best Practices

1. **Use Dependency Injection** - Register Subscrio with `AddSubscrio()` in web applications
2. **Scoped Lifetime** - Use `ServiceLifetime.Scoped` for web apps to avoid tracking conflicts
3. **Schema Installation** - Run `InstallSchemaAsync()` once during application startup
4. **Error Handling** - Handle `ValidationException`, `NotFoundException`, `ConflictException` appropriately
5. **Feature Keys** - Use lowercase alphanumeric keys with hyphens (e.g., `max-projects`)
6. **Customer External IDs** - Use your own customer identifiers for easy integration

## Support

- See [tests/README.md](tests/README.md) for testing guide
- See [requirements/requirements.md](../../requirements/requirements.md) for full specifications

