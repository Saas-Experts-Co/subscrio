using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Subscrio.Core.Domain.ValueObjects;

namespace Subscrio.Core.Infrastructure.Database.Extensions;

public static class ModelBuilderExtensions
{
    public static PropertyBuilder<Dictionary<string, object>?> AsJsonColumn(this PropertyBuilder<Dictionary<string, object>?> builder, DatabaseType databaseType)
    {
        if (databaseType == DatabaseType.PostgreSQL)
        {
            return builder.HasColumnType("jsonb");
        }
        else
        {
            // SQL Server uses NVARCHAR(MAX) for JSON
            return builder.HasColumnType("nvarchar(max)");
        }
    }

    public static PropertyBuilder<DateTime> WithDefaultTimestamp(this PropertyBuilder<DateTime> builder, DatabaseType databaseType)
    {
        if (databaseType == DatabaseType.PostgreSQL)
        {
            return builder.HasDefaultValueSql("NOW()");
        }
        else
        {
            return builder.HasDefaultValueSql("GETUTCDATE()");
        }
    }

    public static PropertyBuilder<DateTime?> WithDefaultTimestampNullable(this PropertyBuilder<DateTime?> builder, DatabaseType databaseType)
    {
        if (databaseType == DatabaseType.PostgreSQL)
        {
            return builder.HasDefaultValueSql("NOW()");
        }
        else
        {
            return builder.HasDefaultValueSql("GETUTCDATE()");
        }
    }

    public static PropertyBuilder<DateTime?> AsTimestampTz(this PropertyBuilder<DateTime?> builder, DatabaseType databaseType)
    {
        if (databaseType == DatabaseType.PostgreSQL)
        {
            return builder.HasColumnType("timestamptz");
        }
        else
        {
            return builder.HasColumnType("datetimeoffset");
        }
    }

    public static PropertyBuilder<DateTime> AsTimestampTz(this PropertyBuilder<DateTime> builder, DatabaseType databaseType)
    {
        if (databaseType == DatabaseType.PostgreSQL)
        {
            return builder.HasColumnType("timestamptz");
        }
        else
        {
            return builder.HasColumnType("datetimeoffset");
        }
    }
}

