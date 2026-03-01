# Running Tests

## Connection String

Tests connect to PostgreSQL using this connection string:

```
postgresql://postgres:postgres@localhost:5432/postgres
```

**User**: `postgres`  
**Password**: `postgres`  
**Host**: `localhost`  
**Port**: `5432`  
**Database**: `postgres`

## Prerequisites

1. **PostgreSQL must be running** on localhost:5432
2. **User `postgres`** must exist with password `postgres`
3. **User must have permission** to CREATE and DROP databases

## Running Tests

```bash
# Just run tests (PostgreSQL must be running)
npm test

# Watch mode
npm run test:watch

# With coverage
npm run test:coverage

# Debug mode - keep test databases after completion
npm run test:debug
```

## Debugging with Preserved Test Databases

When debugging failing tests, you can preserve the test database to inspect its state after tests complete:

```bash
# Run tests and keep databases
npm run test:debug

# Or set the environment variable directly
$env:KEEP_TEST_DB = "true"
npm test

# Or on Unix/Mac
KEEP_TEST_DB=true npm test
```

After the tests complete, you'll see output like:

```
🔍 Test database preserved for debugging:
   Database: subscrio_test_a1b2c3d4e5f6g7h8
   Connection: postgresql://postgres:postgres@localhost:5432/subscrio_test_a1b2c3d4e5f6g7h8
   To connect: psql postgresql://postgres:postgres@localhost:5432/subscrio_test_a1b2c3d4e5f6g7h8
   To drop: DROP DATABASE subscrio_test_a1b2c3d4e5f6g7h8;
```

You can then connect to the database using `psql` or any PostgreSQL client to inspect:
- Table contents
- Data relationships
- Constraint violations
- Actual vs expected state

**Important:** Remember to manually drop test databases when done debugging, or use the cleanup utility (see below).

## Cleaning Up Test Databases

If you've preserved test databases for debugging, clean them up when done:

```typescript
// In Node.js/TypeScript
import { cleanupDanglingTestDatabases } from './tests/setup/database';
await cleanupDanglingTestDatabases();
```

Or manually in `psql`:
```sql
-- List all test databases
SELECT datname FROM pg_database WHERE datname LIKE 'subscrio_test_%';

-- Drop a specific one
DROP DATABASE subscrio_test_a1b2c3d4e5f6g7h8;
```

## Custom Credentials

If your PostgreSQL uses different credentials, set the environment variable:

```bash
# PowerShell
$env:TEST_DATABASE_URL = "postgresql://your_user:your_password@localhost:5432/postgres"
npm test

# Or create packages/core/.env file
TEST_DATABASE_URL=postgresql://your_user:your_password@localhost:5432/postgres
```

## What Tests Do

1. **Global Setup** (once at start):
   - Connect to the PostgreSQL instance at TEST_DATABASE_URL
   - Create **one** unique temporary database (e.g., `subscrio_test_a1b2c3d4`)
   - Install the Subscrio schema in that database
   - Create a shared Subscrio instance

2. **Test Execution**:
   - All 9 test files share the same database
   - Tests run sequentially (single-threaded) to avoid conflicts

3. **Global Teardown** (once at end):
   - Drop the temporary database

This ensures:
- ✅ Each test run starts with a clean database
- ✅ Tests don't interfere with your development data
- ✅ No manual cleanup needed
- ✅ **Fast tests** - only 1 database created instead of 9

## Troubleshooting

### "password authentication failed"

Your PostgreSQL doesn't use `postgres/postgres` credentials. Either:

1. Set TEST_DATABASE_URL with your actual credentials
2. Or create a `postgres` user with password `postgres`:
   ```sql
   CREATE USER postgres WITH PASSWORD 'postgres' SUPERUSER;
   ```

### "Connection refused"

PostgreSQL isn't running. Start it:
- **Windows**: Check Services or use `pg_ctl start`
- **Mac**: `brew services start postgresql`
- **Linux**: `sudo systemctl start postgresql`

### "Permission denied to create database"

The user needs CREATEDB permission:
```sql
ALTER USER postgres CREATEDB;
```

