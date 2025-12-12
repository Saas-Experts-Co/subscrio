using Microsoft.EntityFrameworkCore;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Database.Entities;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of ISubscriptionRepository
/// Equivalent to TypeScript DrizzleSubscriptionRepository
/// Note: Computed status is calculated in C# instead of using database view
/// </summary>
public class EfSubscriptionRepository : ISubscriptionRepository
{
    private readonly SubscrioDbContext _dbContext;

    public EfSubscriptionRepository(SubscrioDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private static string ComputeStatus(SubscriptionEntity entity)
    {
        var now = DateHelper.Now();

        if (entity.CancellationDate.HasValue && entity.CancellationDate.Value > now)
            return "cancellation_pending";
        if (entity.CancellationDate.HasValue && entity.CancellationDate.Value <= now)
            return "cancelled";
        if (entity.ExpirationDate.HasValue && entity.ExpirationDate.Value <= now)
            return "expired";
        if (entity.ActivationDate.HasValue && entity.ActivationDate.Value > now)
            return "pending";
        if (entity.TrialEndDate.HasValue && entity.TrialEndDate.Value > now)
            return "trial";
        return "active";
    }

    private async Task<List<FeatureOverride>> LoadFeatureOverridesAsync(long subscriptionId)
    {
        var records = await _dbContext.SubscriptionFeatureOverrides
            .Where(sfo => sfo.SubscriptionId == subscriptionId)
            .ToListAsync();

        return records.Select(r => new FeatureOverride
        {
            FeatureId = r.FeatureId,
            Value = r.Value,
            Type = Enum.Parse<OverrideType>(r.OverrideType, true),
            CreatedAt = r.CreatedAt
        }).ToList();
    }

    public async Task<Subscription> SaveAsync(Subscription subscription)
    {
        long savedSubscriptionId;
        if (subscription.Id == null)
        {
            // Insert new entity
            var entity = new SubscriptionEntity
            {
                Key = subscription.Key,
                CustomerId = subscription.CustomerId ?? throw new InvalidOperationException("CustomerId is required"),
                PlanId = subscription.PlanId ?? throw new InvalidOperationException("PlanId is required"),
                BillingCycleId = subscription.Props.BillingCycleId,
                IsArchived = subscription.Props.IsArchived,
                ActivationDate = subscription.Props.ActivationDate,
                ExpirationDate = subscription.Props.ExpirationDate,
                CancellationDate = subscription.Props.CancellationDate,
                TrialEndDate = subscription.Props.TrialEndDate,
                CurrentPeriodStart = subscription.Props.CurrentPeriodStart,
                CurrentPeriodEnd = subscription.Props.CurrentPeriodEnd,
                StripeSubscriptionId = subscription.Props.StripeSubscriptionId,
                Metadata = subscription.Props.Metadata,
                CreatedAt = subscription.Props.CreatedAt,
                UpdatedAt = subscription.Props.UpdatedAt,
                TransitionedAt = subscription.Props.TransitionedAt
            };

            _dbContext.Subscriptions.Add(entity);
            await _dbContext.SaveChangesAsync();

            savedSubscriptionId = entity.Id;

            // Insert feature overrides
            if (subscription.Props.FeatureOverrides.Count > 0)
            {
                var overrideEntities = subscription.Props.FeatureOverrides.Select(ov => new SubscriptionFeatureOverrideEntity
                {
                    SubscriptionId = savedSubscriptionId,
                    FeatureId = ov.FeatureId,
                    Value = ov.Value,
                    OverrideType = ov.Type.ToString().ToLower(),
                    CreatedAt = ov.CreatedAt
                }).ToList();

                _dbContext.SubscriptionFeatureOverrides.AddRange(overrideEntities);
                await _dbContext.SaveChangesAsync();
            }
        }
        else
        {
            // Update existing entity
            savedSubscriptionId = subscription.Id.Value;

            var entity = await _dbContext.Subscriptions.FindAsync(savedSubscriptionId);
            if (entity == null)
            {
                throw new InvalidOperationException($"Subscription with id {savedSubscriptionId} not found");
            }

            entity.Key = subscription.Key;
            entity.CustomerId = subscription.CustomerId ?? throw new InvalidOperationException("CustomerId is required");
            entity.PlanId = subscription.PlanId ?? throw new InvalidOperationException("PlanId is required");
            entity.BillingCycleId = subscription.Props.BillingCycleId;
            entity.IsArchived = subscription.Props.IsArchived;
            entity.ActivationDate = subscription.Props.ActivationDate;
            entity.ExpirationDate = subscription.Props.ExpirationDate;
            entity.CancellationDate = subscription.Props.CancellationDate;
            entity.TrialEndDate = subscription.Props.TrialEndDate;
            entity.CurrentPeriodStart = subscription.Props.CurrentPeriodStart;
            entity.CurrentPeriodEnd = subscription.Props.CurrentPeriodEnd;
            entity.StripeSubscriptionId = subscription.Props.StripeSubscriptionId;
            entity.Metadata = subscription.Props.Metadata;
            entity.UpdatedAt = subscription.Props.UpdatedAt;
            entity.TransitionedAt = subscription.Props.TransitionedAt;

            // Delete existing feature overrides
            var existingOverrides = await _dbContext.SubscriptionFeatureOverrides
                .Where(sfo => sfo.SubscriptionId == savedSubscriptionId)
                .ToListAsync();
            _dbContext.SubscriptionFeatureOverrides.RemoveRange(existingOverrides);

            // Insert new feature overrides
            if (subscription.Props.FeatureOverrides.Count > 0)
            {
                var overrideEntities = subscription.Props.FeatureOverrides.Select(ov => new SubscriptionFeatureOverrideEntity
                {
                    SubscriptionId = savedSubscriptionId,
                    FeatureId = ov.FeatureId,
                    Value = ov.Value,
                    OverrideType = ov.Type.ToString().ToLower(),
                    CreatedAt = ov.CreatedAt
                }).ToList();

                _dbContext.SubscriptionFeatureOverrides.AddRange(overrideEntities);
            }

            await _dbContext.SaveChangesAsync();
        }

        return await LoadSubscriptionByIdAsync(savedSubscriptionId);
    }

    private async Task<Subscription> LoadSubscriptionByIdAsync(long id)
    {
        var record = await _dbContext.Subscriptions.FindAsync(id);
        if (record == null)
        {
            throw new InvalidOperationException($"Subscription with id '{id}' not found after save");
        }

        var featureOverrides = await LoadFeatureOverridesAsync(id);
        var computedStatus = ComputeStatus(record);

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            customer_id = record.CustomerId,
            plan_id = record.PlanId,
            billing_cycle_id = record.BillingCycleId,
            activation_date = record.ActivationDate,
            expiration_date = record.ExpirationDate,
            cancellation_date = record.CancellationDate,
            trial_end_date = record.TrialEndDate,
            current_period_start = record.CurrentPeriodStart,
            current_period_end = record.CurrentPeriodEnd,
            stripe_subscription_id = record.StripeSubscriptionId,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt,
            is_archived = record.IsArchived,
            transitioned_at = record.TransitionedAt,
            computed_status = computedStatus
        };

        return SubscriptionMapper.ToDomain(raw, featureOverrides);
    }

    public async Task<Subscription?> FindByIdAsync(long id)
    {
        var record = await _dbContext.Subscriptions.FindAsync(id);
        if (record == null) return null;

        var featureOverrides = await LoadFeatureOverridesAsync(id);
        var computedStatus = ComputeStatus(record);

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            customer_id = record.CustomerId,
            plan_id = record.PlanId,
            billing_cycle_id = record.BillingCycleId,
            activation_date = record.ActivationDate,
            expiration_date = record.ExpirationDate,
            cancellation_date = record.CancellationDate,
            trial_end_date = record.TrialEndDate,
            current_period_start = record.CurrentPeriodStart,
            current_period_end = record.CurrentPeriodEnd,
            stripe_subscription_id = record.StripeSubscriptionId,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt,
            is_archived = record.IsArchived,
            transitioned_at = record.TransitionedAt,
            computed_status = computedStatus
        };

        return SubscriptionMapper.ToDomain(raw, featureOverrides);
    }

    public async Task<Subscription?> FindByKeyAsync(string key)
    {
        var record = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.Key == key);

        if (record == null) return null;

        var featureOverrides = await LoadFeatureOverridesAsync(record.Id);
        var computedStatus = ComputeStatus(record);

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            customer_id = record.CustomerId,
            plan_id = record.PlanId,
            billing_cycle_id = record.BillingCycleId,
            activation_date = record.ActivationDate,
            expiration_date = record.ExpirationDate,
            cancellation_date = record.CancellationDate,
            trial_end_date = record.TrialEndDate,
            current_period_start = record.CurrentPeriodStart,
            current_period_end = record.CurrentPeriodEnd,
            stripe_subscription_id = record.StripeSubscriptionId,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt,
            is_archived = record.IsArchived,
            transitioned_at = record.TransitionedAt,
            computed_status = computedStatus
        };

        return SubscriptionMapper.ToDomain(raw, featureOverrides);
    }

    public async Task<Subscription?> FindByStripeIdAsync(string stripeId)
    {
        // Only find active (non-archived) subscriptions by Stripe ID
        var record = await _dbContext.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeId && !s.IsArchived);

        if (record == null) return null;

        var featureOverrides = await LoadFeatureOverridesAsync(record.Id);
        var computedStatus = ComputeStatus(record);

        dynamic raw = new
        {
            id = record.Id,
            key = record.Key,
            customer_id = record.CustomerId,
            plan_id = record.PlanId,
            billing_cycle_id = record.BillingCycleId,
            activation_date = record.ActivationDate,
            expiration_date = record.ExpirationDate,
            cancellation_date = record.CancellationDate,
            trial_end_date = record.TrialEndDate,
            current_period_start = record.CurrentPeriodStart,
            current_period_end = record.CurrentPeriodEnd,
            stripe_subscription_id = record.StripeSubscriptionId,
            metadata = record.Metadata,
            created_at = record.CreatedAt,
            updated_at = record.UpdatedAt,
            is_archived = record.IsArchived,
            transitioned_at = record.TransitionedAt,
            computed_status = computedStatus
        };

        return SubscriptionMapper.ToDomain(raw, featureOverrides);
    }

    public async Task<IReadOnlyList<(Subscription Subscription, Customer? Customer)>> FindAllAsync(SubscriptionFilterDto? filters = null)
    {
        var query = _dbContext.Subscriptions
            .Join(_dbContext.Customers,
                s => s.CustomerId,
                c => c.Id,
                (s, c) => new { Subscription = s, Customer = c })
            .AsQueryable();

        if (filters != null)
        {
            // Note: customerId, planIds, billingCycleId are resolved at service layer
            // Here we filter by isArchived and status (computed)
            if (filters.IsArchived.HasValue)
            {
                query = query.Where(x => x.Subscription.IsArchived == filters.IsArchived.Value);
            }

            // Date range filters
            if (filters.ActivationDateFrom.HasValue)
            {
                query = query.Where(x => x.Subscription.ActivationDate >= filters.ActivationDateFrom.Value);
            }
            if (filters.ActivationDateTo.HasValue)
            {
                query = query.Where(x => x.Subscription.ActivationDate <= filters.ActivationDateTo.Value);
            }
            if (filters.ExpirationDateFrom.HasValue)
            {
                query = query.Where(x => x.Subscription.ExpirationDate >= filters.ExpirationDateFrom.Value);
            }
            if (filters.ExpirationDateTo.HasValue)
            {
                query = query.Where(x => x.Subscription.ExpirationDate <= filters.ExpirationDateTo.Value);
            }

            // HasStripeId filter
            if (filters.HasStripeId.HasValue)
            {
                if (filters.HasStripeId.Value)
                {
                    query = query.Where(x => x.Subscription.StripeSubscriptionId != null);
                }
                else
                {
                    query = query.Where(x => x.Subscription.StripeSubscriptionId == null);
                }
            }

            // HasTrial filter
            if (filters.HasTrial.HasValue)
            {
                if (filters.HasTrial.Value)
                {
                    query = query.Where(x => x.Subscription.TrialEndDate != null);
                }
                else
                {
                    query = query.Where(x => x.Subscription.TrialEndDate == null);
                }
            }

            // Apply sorting
            var sortBy = filters.SortBy ?? "createdAt";
            var sortOrder = filters.SortOrder ?? "desc";

            query = sortBy switch
            {
                "activationDate" => sortOrder == "desc"
                    ? query.OrderByDescending(x => x.Subscription.ActivationDate)
                    : query.OrderBy(x => x.Subscription.ActivationDate),
                "expirationDate" => sortOrder == "desc"
                    ? query.OrderByDescending(x => x.Subscription.ExpirationDate)
                    : query.OrderBy(x => x.Subscription.ExpirationDate),
                "currentPeriodStart" => sortOrder == "desc"
                    ? query.OrderByDescending(x => x.Subscription.CurrentPeriodStart)
                    : query.OrderBy(x => x.Subscription.CurrentPeriodStart),
                "currentPeriodEnd" => sortOrder == "desc"
                    ? query.OrderByDescending(x => x.Subscription.CurrentPeriodEnd)
                    : query.OrderBy(x => x.Subscription.CurrentPeriodEnd),
                _ => sortOrder == "desc"
                    ? query.OrderByDescending(x => x.Subscription.CreatedAt)
                    : query.OrderBy(x => x.Subscription.CreatedAt)
            };

            // Apply pagination
            if (filters.Limit > 0)
            {
                query = query.Take(filters.Limit);
            }
            if (filters.Offset > 0)
            {
                query = query.Skip(filters.Offset);
            }
        }
        else
        {
            query = query.OrderByDescending(x => x.Subscription.CreatedAt);
        }

        var records = await query.ToListAsync();

        var results = new List<(Subscription, Customer?)>();
        foreach (var record in records)
        {
            var featureOverrides = await LoadFeatureOverridesAsync(record.Subscription.Id);
            var computedStatus = ComputeStatus(record.Subscription);

            // Filter by status if specified (after computing status)
            if (filters?.Status != null)
            {
                if (computedStatus != filters.Status.ToLower())
                {
                    continue;
                }
            }

            dynamic raw = new
            {
                id = record.Subscription.Id,
                key = record.Subscription.Key,
                customer_id = record.Subscription.CustomerId,
                plan_id = record.Subscription.PlanId,
                billing_cycle_id = record.Subscription.BillingCycleId,
                activation_date = record.Subscription.ActivationDate,
                expiration_date = record.Subscription.ExpirationDate,
                cancellation_date = record.Subscription.CancellationDate,
                trial_end_date = record.Subscription.TrialEndDate,
                current_period_start = record.Subscription.CurrentPeriodStart,
                current_period_end = record.Subscription.CurrentPeriodEnd,
                stripe_subscription_id = record.Subscription.StripeSubscriptionId,
                metadata = record.Subscription.Metadata,
                created_at = record.Subscription.CreatedAt,
                updated_at = record.Subscription.UpdatedAt,
                is_archived = record.Subscription.IsArchived,
                transitioned_at = record.Subscription.TransitionedAt,
                computed_status = computedStatus
            };

            var subscription = SubscriptionMapper.ToDomain(raw, featureOverrides);

            // Map customer
            Customer? customer = null;
            if (record.Customer != null)
            {
                dynamic customerRaw = new
                {
                    id = record.Customer.Id,
                    key = record.Customer.Key,
                    display_name = record.Customer.DisplayName,
                    email = record.Customer.Email,
                    external_billing_id = record.Customer.ExternalBillingId,
                    status = record.Customer.Status,
                    metadata = record.Customer.Metadata,
                    created_at = record.Customer.CreatedAt,
                    updated_at = record.Customer.UpdatedAt
                };
                customer = CustomerMapper.ToDomain(customerRaw);
            }

            results.Add((subscription, customer));
        }

        return results;
    }

    public async Task<IReadOnlyList<Subscription>> FindByCustomerIdAsync(long customerId, SubscriptionFilterDto? filters = null)
    {
        var query = _dbContext.Subscriptions
            .Where(s => s.CustomerId == customerId)
            .AsQueryable();

        var records = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var subscriptions = new List<Subscription>();
        foreach (var record in records)
        {
            var featureOverrides = await LoadFeatureOverridesAsync(record.Id);
            var computedStatus = ComputeStatus(record);

            // Filter by status if specified
            if (filters?.Status != null)
            {
                if (computedStatus != filters.Status.ToLower())
                {
                    continue;
                }
            }

            dynamic raw = new
            {
                id = record.Id,
                key = record.Key,
                customer_id = record.CustomerId,
                plan_id = record.PlanId,
                billing_cycle_id = record.BillingCycleId,
                activation_date = record.ActivationDate,
                expiration_date = record.ExpirationDate,
                cancellation_date = record.CancellationDate,
                trial_end_date = record.TrialEndDate,
                current_period_start = record.CurrentPeriodStart,
                current_period_end = record.CurrentPeriodEnd,
                stripe_subscription_id = record.StripeSubscriptionId,
                metadata = record.Metadata,
                created_at = record.CreatedAt,
                updated_at = record.UpdatedAt,
                is_archived = record.IsArchived,
                transitioned_at = record.TransitionedAt,
                computed_status = computedStatus
            };

            subscriptions.Add(SubscriptionMapper.ToDomain(raw, featureOverrides));
        }

        return subscriptions;
    }

    public async Task<IReadOnlyList<Subscription>> FindByIdsAsync(IReadOnlyList<long> ids)
    {
        if (ids.Count == 0) return new List<Subscription>();

        var records = await _dbContext.Subscriptions
            .Where(s => ids.Contains(s.Id))
            .ToListAsync();

        var subscriptions = new List<Subscription>();
        foreach (var record in records)
        {
            var featureOverrides = await LoadFeatureOverridesAsync(record.Id);
            var computedStatus = ComputeStatus(record);

            dynamic raw = new
            {
                id = record.Id,
                key = record.Key,
                customer_id = record.CustomerId,
                plan_id = record.PlanId,
                billing_cycle_id = record.BillingCycleId,
                activation_date = record.ActivationDate,
                expiration_date = record.ExpirationDate,
                cancellation_date = record.CancellationDate,
                trial_end_date = record.TrialEndDate,
                current_period_start = record.CurrentPeriodStart,
                current_period_end = record.CurrentPeriodEnd,
                stripe_subscription_id = record.StripeSubscriptionId,
                metadata = record.Metadata,
                created_at = record.CreatedAt,
                updated_at = record.UpdatedAt,
                is_archived = record.IsArchived,
                transitioned_at = record.TransitionedAt,
                computed_status = computedStatus
            };

            subscriptions.Add(SubscriptionMapper.ToDomain(raw, featureOverrides));
        }

        return subscriptions;
    }

    public async Task DeleteAsync(long id)
    {
        var entity = await _dbContext.Subscriptions.FindAsync(id);
        if (entity != null)
        {
            _dbContext.Subscriptions.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task<Subscription?> FindActiveByCustomerAndPlanAsync(long customerId, long planId)
    {
        var records = await _dbContext.Subscriptions
            .Where(s => s.CustomerId == customerId && s.PlanId == planId && !s.IsArchived)
            .Take(100)
            .ToListAsync();

        foreach (var record in records)
        {
            var computedStatus = ComputeStatus(record);
            if (computedStatus == "active" || computedStatus == "trial")
            {
                var featureOverrides = await LoadFeatureOverridesAsync(record.Id);

                dynamic raw = new
                {
                    id = record.Id,
                    key = record.Key,
                    customer_id = record.CustomerId,
                    plan_id = record.PlanId,
                    billing_cycle_id = record.BillingCycleId,
                    activation_date = record.ActivationDate,
                    expiration_date = record.ExpirationDate,
                    cancellation_date = record.CancellationDate,
                    trial_end_date = record.TrialEndDate,
                    current_period_start = record.CurrentPeriodStart,
                    current_period_end = record.CurrentPeriodEnd,
                    stripe_subscription_id = record.StripeSubscriptionId,
                    metadata = record.Metadata,
                    created_at = record.CreatedAt,
                    updated_at = record.UpdatedAt,
                    is_archived = record.IsArchived,
                    transitioned_at = record.TransitionedAt,
                    computed_status = computedStatus
                };

                return SubscriptionMapper.ToDomain(raw, featureOverrides);
            }
        }

        return null;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        return await _dbContext.Subscriptions.AnyAsync(s => s.Id == id);
    }

    public async Task<bool> HasSubscriptionsForPlanAsync(long planId)
    {
        return await _dbContext.Subscriptions
            .AnyAsync(s => s.PlanId == planId);
    }

    public async Task<bool> HasSubscriptionsForBillingCycleAsync(long billingCycleId)
    {
        return await _dbContext.Subscriptions
            .AnyAsync(s => s.BillingCycleId == billingCycleId);
    }

    public async Task<IReadOnlyList<Subscription>> FindExpiredWithTransitionPlansAsync(int limit = 1000)
    {
        // Query expired subscriptions that are not archived
        // and whose plan has a transition requirement
        var records = await _dbContext.Subscriptions
            .Join(_dbContext.Plans,
                s => s.PlanId,
                p => p.Id,
                (s, p) => new { Subscription = s, Plan = p })
            .Where(x => !x.Subscription.IsArchived &&
                       x.Plan.OnExpireTransitionToBillingCycleId != null)
            .OrderByDescending(x => x.Subscription.ExpirationDate)
            .Take(limit)
            .Select(x => x.Subscription)
            .ToListAsync();

        var subscriptions = new List<Subscription>();
        foreach (var record in records)
        {
            var computedStatus = ComputeStatus(record);
            // Only include expired subscriptions
            if (computedStatus != "expired")
            {
                continue;
            }

            var featureOverrides = await LoadFeatureOverridesAsync(record.Id);

            dynamic raw = new
            {
                id = record.Id,
                key = record.Key,
                customer_id = record.CustomerId,
                plan_id = record.PlanId,
                billing_cycle_id = record.BillingCycleId,
                activation_date = record.ActivationDate,
                expiration_date = record.ExpirationDate,
                cancellation_date = record.CancellationDate,
                trial_end_date = record.TrialEndDate,
                current_period_start = record.CurrentPeriodStart,
                current_period_end = record.CurrentPeriodEnd,
                stripe_subscription_id = record.StripeSubscriptionId,
                metadata = record.Metadata,
                created_at = record.CreatedAt,
                updated_at = record.UpdatedAt,
                is_archived = record.IsArchived,
                transitioned_at = record.TransitionedAt,
                computed_status = computedStatus
            };

            subscriptions.Add(SubscriptionMapper.ToDomain(raw, featureOverrides));
        }

        return subscriptions;
    }
}

