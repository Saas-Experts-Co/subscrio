using Subscrio.Core.Application.Constants;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Domain.Base;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Domain.Entities;

public class ProductProps
{
    public required string Key { get; init; }
    public required string DisplayName { get; set; }
    public string? Description { get; init; }
    public required ProductStatus Status { get; set; }
    public Dictionary<string, object?>? Metadata { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime UpdatedAt { get; set; }
}

public class Product : Entity<ProductProps>
{
    public string Key => Props.Key;

    public string DisplayName => Props.DisplayName;

    public ProductStatus Status => Props.Status;

    public void Archive()
    {
        Props.Status = ProductStatus.Archived;
        Props.UpdatedAt = DateHelper.Now();
    }

    public void Unarchive()
    {
        Props.Status = ProductStatus.Active;
        Props.UpdatedAt = DateHelper.Now();
    }

    public bool CanDelete()
    {
        return Props.Status == ProductStatus.Archived;
    }

    public void UpdateDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < ApplicationConstants.MinDisplayNameLength)
        {
            throw new DomainException($"Display name cannot be empty. Product key: {Key}");
        }
        if (name.Length > ApplicationConstants.MaxDisplayNameLength)
        {
            throw new DomainException($"Display name cannot exceed {ApplicationConstants.MaxDisplayNameLength} characters. Product key: {Key}, provided length: {name.Length}");
        }
        Props.DisplayName = name;
        Props.UpdatedAt = DateHelper.Now();
    }
}

