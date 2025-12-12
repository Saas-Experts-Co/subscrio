using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Domain.Entities;

namespace Subscrio.Core.Application.Repositories;

public interface ISubscriptionRepository
{
    Task<Subscription> SaveAsync(Subscription subscription);
    Task<Subscription?> FindByIdAsync(long id);
    Task<Subscription?> FindByKeyAsync(string key);
    Task<List<Subscription>> FindByCustomerIdAsync(long customerId, SubscriptionFilterDto? filters = null);
    Task<Subscription?> FindByStripeIdAsync(string stripeSubscriptionId);
    Task<List<(Subscription Subscription, Customer? Customer)>> FindAllAsync(SubscriptionFilterDto? filters = null);
    Task<List<Subscription>> FindByIdsAsync(List<long> ids);
    Task DeleteAsync(long id);
    Task<bool> ExistsAsync(long id);

    // Find active subscription for customer and plan combination
    Task<Subscription?> FindActiveByCustomerAndPlanAsync(long customerId, long planId);

    // Foreign key checks
    Task<bool> HasSubscriptionsForPlanAsync(long planId);
    Task<bool> HasSubscriptionsForBillingCycleAsync(long billingCycleId);

    // Find expired subscriptions with transition plans (for transition processing)
    Task<List<Subscription>> FindExpiredWithTransitionPlansAsync(int? limit = null);
}

