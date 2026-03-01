# Subscrio Cursor Rules Index

This directory contains comprehensive Cursor rules for the Subscrio project. These rules enforce architectural patterns, coding standards, and best practices derived from the requirements document.

## Rule Files

### Core Architecture Rules

1. **[ddd-rules.mdc](./ddd-rules.mdc)** - Domain-Driven Design fundamentals (ALWAYS APPLIED)
   - Monorepo & build configuration
   - Layer separation (Domain, Application, Infrastructure)
   - Admin app process model (direct import, not REST)
   - Security & authentication patterns

2. **[id-generation.mdc](./id-generation.mdc)** - UUIDv7 ID generation (ALWAYS APPLIED)
   - CRITICAL: All IDs must be UUIDv7
   - Generator utility implementation
   - Database schema patterns
   - Why UUIDv7 over UUID v4

3. **[mapper-pattern.mdc](./mapper-pattern.mdc)** - Three-layer data transformation (ALWAYS APPLIED)
   - Record ⇄ Domain ⇄ DTO transformations
   - Naming conventions (snake_case ↔ camelCase)
   - Type conversions (Date ↔ ISO string, enum ↔ string)
   - Mapper structure and location

4. **[repository-pattern.mdc](./repository-pattern.mdc)** - Repository abstraction (ALWAYS APPLIED)
   - CRITICAL: No direct database access from Application layer
   - Interface definition in Application layer
   - Implementation in Infrastructure layer
   - Dependency injection patterns

5. **[entity-structure.mdc](./entity-structure.mdc)** - Domain entity patterns (ALWAYS APPLIED)
   - Entity base class structure
   - Business logic in entities
   - Value objects (enums, statuses)
   - Domain services for multi-entity operations

### Data & Validation Rules

6. **[dto-validation.mdc](./dto-validation.mdc)** - DTO and Zod validation (ALWAYS APPLIED)
   - CRITICAL: All public APIs use DTOs with Zod validation
   - Create/Update/Filter DTO patterns
   - Validation in application services
   - Error handling for validation failures

7. **[naming-conventions.mdc](./naming-conventions.mdc)** - Naming standards (ALWAYS APPLIED)
   - Database: snake_case (PostgreSQL standard)
   - TypeScript: camelCase for variables/properties
   - Classes/Types: PascalCase
   - Files and directories naming
   - Foreign key patterns

### Business Logic Rules

8. **[feature-resolution.mdc](./feature-resolution.mdc)** - Feature value hierarchy (ALWAYS APPLIED)
   - CRITICAL: Resolution order: Subscription Override > Plan Value > Feature Default
   - Multiple active subscriptions handling
   - Override types (permanent vs temporary)
   - Renewal processing and plan transitions

### Integration Rules

9. **[stripe-integration.mdc](./stripe-integration.mdc)** - Stripe integration patterns (ALWAYS APPLIED)
   - CRITICAL: Implementor verifies webhooks, Subscrio processes events
   - Webhook endpoint implementation
   - Event processing patterns
   - Price mapping (plan + cycle → Stripe price ID)
   - Customer and subscription syncing

### Testing & Quality Rules

10. **[testing-patterns.mdc](./testing-patterns.mdc)** - E2E testing patterns (ALWAYS APPLIED)
    - CRITICAL: E2E tests with real PostgreSQL, NO MOCKS
    - Test database setup/teardown
    - Test all public API methods
    - Vitest configuration
    - Fixture helpers and test structure

### Error Handling & Admin Architecture

11. **[error-handling.mdc](./error-handling.mdc)** - Error handling patterns (ALWAYS APPLIED)
    - Custom error classes (ValidationError, NotFoundError, ConflictError, etc.)
    - Error usage in services and entities
    - REST API error mapping
    - Admin app error handling

12. **[admin-architecture.mdc](./admin-architecture.mdc)** - Admin app architecture (ALWAYS APPLIED)
    - CRITICAL: Direct library import, not REST API
    - Singleton Subscrio instance pattern
    - React Query integration
    - Type-safe component patterns
    - When to use REST vs direct import

### Operational Rules

13. **[database.mdc](./database.mdc)** - Database management safety (CONDITIONAL)
    - CRITICAL: Never run reset-db.js without permission
    - Safe vs dangerous operations
    - Migration commands
    - Production protection

14. **[building-app.mdc](./building-app.mdc)** - Build and run rules (ALWAYS APPLIED)
    - Never run the application directly from terminal
    - Build only for testing errors
    - Prevent hidden terminal processes

## Quick Reference

### Most Critical Rules

These rules must NEVER be violated:

1. **All IDs are UUIDv7** - Never use UUID v4 or other versions
2. **Three-layer transformation** - Always use mappers: Record ⇄ Domain ⇄ DTO
3. **Repository pattern** - Application layer NEVER directly accesses database
4. **Feature resolution hierarchy** - Override > Plan > Default (in that order)
5. **Stripe webhook verification** - Implementor verifies, library processes
6. **E2E testing** - Real database, no mocks
7. **Admin uses direct import** - Not REST API
8. **Zod validation** - All public APIs validate with Zod schemas

### Naming Quick Reference

- **Database**: `snake_case` (e.g., `display_name`, `created_at`)
- **TypeScript**: `camelCase` (e.g., `displayName`, `createdAt`)
- **Classes/Types**: `PascalCase` (e.g., `Product`, `ProductDto`)
- **Interfaces**: `I` prefix (e.g., `IProductRepository`)
- **Enums**: `PascalCase` name, lowercase values (e.g., `ProductStatus.Active = 'active'`)
- **Constants**: `SCREAMING_SNAKE_CASE` (e.g., `MAX_LENGTH`)

### Error Types

- `ValidationError` - Input validation failed
- `NotFoundError` - Resource doesn't exist
- `ConflictError` - Duplicate resource
- `DomainError` - Business rule violation
- `AuthError` - Authentication failure
- `ConfigurationError` - Config issues

### File Structure

```
packages/
├── core/                                # @subscrio/core
│   └── src/
│       ├── domain/
│       │   ├── entities/                # Product.ts, Plan.ts
│       │   ├── value-objects/           # ProductStatus.ts
│       │   └── services/                # FeatureValueResolver.ts
│       ├── application/
│       │   ├── services/                # ProductManagementService.ts
│       │   ├── dtos/                    # ProductDto.ts
│       │   ├── mappers/                 # ProductMapper.ts
│       │   └── repositories/            # IProductRepository.ts (interfaces)
│       └── infrastructure/
│           ├── database/
│           │   ├── schema.ts            # Drizzle schema
│           │   └── drizzle.ts
│           ├── repositories/            # DrizzleProductRepository.ts (implementations)
│           └── utils/
│               └── uuid.ts              # UUIDv7 generator
├── api/                                 # @subscrio/api (REST wrapper)
│   └── src/
│       ├── routes/
│       ├── middleware/
│       └── index.ts
└── admin/                               # @subscrio/admin (React app)
    └── src/
        ├── lib/
        │   └── subscrio.ts              # Singleton instance
        ├── hooks/                       # React Query hooks
        ├── pages/
        └── components/
```

## How to Use These Rules

1. **Starting New Features**: Review relevant rules first
2. **Code Review**: Check compliance with these patterns
3. **Debugging Issues**: Verify rules are being followed
4. **Onboarding**: Read rules in order listed above

## Rule Priority

When rules conflict (rare), priority order:

1. Security rules (authentication, database safety)
2. Data integrity rules (UUIDs, validation)
3. Architecture rules (DDD, repositories)
4. Code style rules (naming, formatting)

## Updating Rules

When adding new patterns to the requirements document:

1. Create/update relevant rule file
2. Add to this index
3. Mark as `alwaysApply: true` if critical
4. Include code examples
5. Document common mistakes

## Testing Rule Compliance

```bash
# Type checking (enforces many rules)
npm typecheck

# Linting
npm lint

# Run tests (validates behavior matches rules)
npm test

# Build (ensures no import cycles, etc.)
npm build
```

## Common Violations and Fixes

| Violation | Fix |
|-----------|-----|
| Using `crypto.randomUUID()` | Use `generateId()` from uuid util |
| Direct database access in service | Use repository interface |
| snake_case in TypeScript | Use camelCase, let mapper handle conversion |
| Not validating DTO | Add Zod schema and validate |
| Checking plan before override | Check subscription override first |
| Admin calling REST API | Import subscrio singleton directly |
| Mocking database in tests | Use real database with setup/teardown |
| Generic `Error` class | Use specific error class |

## Questions?

If you're unsure about a pattern:

1. Check the specific rule file
2. Look at the code examples in rules
3. Review the requirements.md document
4. Ask in team chat

---

**Last Updated**: Based on requirements.md v1.0

**Rule Files**: 14 total (12 always applied, 2 conditional)

**Coverage**: Architecture, data patterns, validation, testing, integration, errors, admin app

