# Subscrio · The Entitlement Engine

**The missing layer in your SaaS stack: The entitlement engine that translates subscriptions into feature access.**

Every time a user clicks a button, creates a resource, or calls an API endpoint, your application asks: "Is this customer allowed to do this?" Subscrio is the definitive answer.

## The Problem You're Solving

**Right now, you have two disconnected systems:**

1. **Billing Platform** (Stripe, Paddle) - Handles payments and invoices
2. **Your Application** - Enforces what users can actually do

**The gap:** Who translates "Pro Plan" into actionable permissions throughout your app?

Currently, you're doing this with scattered conditional statements across dozens of files, checking plan names and hardcoding feature limits.

**This creates massive problems:**
- Change a plan? Requires code deployment
- Custom deals? Engineers build one-off override logic  
- Multiple products? Conditional statements become unmaintainable
- Sales flexibility? Product team can't experiment without engineering
- Vendor lock-in? You're forced to parse your billing system's data structures

## The Solution

**Subscrio is the entitlement layer your SaaS application is missing.**

It's not feature flags for gradual rollouts. It's not a billing system for processing payments. It's the authoritative system between them that knows exactly what each customer is entitled to access.

### How It Works

**1. Define Your Business Model (Once)**
Configure products, features, and plans through a simple API. Set up your business model once and let Subscrio handle the complexity.

**2. Enforce Entitlements Throughout Your App**
Query feature values for customers in real-time. Check limits, permissions, and access rights without hardcoded conditional logic.

**3. Business Teams Control Configuration**
Sales teams can grant custom overrides, product teams can experiment with new plans, and customer success can handle exceptions—all without requiring engineering deployments.

## Why Subscrio Wins

**vs. Building In-House:**

✅ Saves 120+ hours of development  
✅ Production-tested with audit trails  
✅ No technical debt as your business model evolves  

**vs. Feature Flags (LaunchDarkly, Split):**

✅ Feature flags roll out new code gradually  
✅ Subscrio manages what customers paid for and can access  
✅ Different problems, different solutions  

**vs. Billing Systems (Stripe, Paddle):**  

✅ Billing handles payments and invoices  
✅ Subscrio translates subscriptions into feature entitlements  
✅ Tightly integrated, not competing  

## Key Benefits

✅ **Zero Configuration**: Works out of the box with sensible defaults  
✅ **Feature Resolution**: Automatic hierarchy (subscription → plan → default)  
✅ **Multiple Subscriptions**: Customers can have multiple active subscriptions  
✅ **Trial Management**: Built-in trial period handling  
✅ **Override System**: Temporary and permanent feature overrides  
✅ **Status Calculation**: Dynamic subscription status based on dates  
✅ **Production Ready**: Battle-tested with comprehensive error handling  
✅ **Type Safety**: Full TypeScript support with compile-time validation  
✅ **Business Flexibility**: Change plans and grant exceptions without deployments  

## Core Concepts

**Features** - Standalone capabilities that can be toggled, limited, or configured (e.g., "max projects", "team collaboration", "API calls per hour")

**Products** - Collections of features that represent your business offerings (e.g., "Project Management", "Analytics Dashboard")

**Plans** - Pricing tiers within a product that define feature values (e.g., "Free", "Pro", "Enterprise")

**Billing Cycles** - How often customers are charged for a plan (e.g., monthly, yearly)

**Customers** - Your application's users, identified by your system's user ID

**Subscriptions** - Active relationships between customers and plans, with dynamic status calculation

**Feature Resolution Hierarchy:**
1. Subscription Override (highest priority)
2. Plan Value  
3. Feature Default (fallback)

**Subscription Status** - Calculated dynamically based on dates and cancellation state