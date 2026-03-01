# Changelog

## [0.1.16] - 2025-12-17

### Added
- Enhanced Stripe integration with subscription linking and checkout session creation
- `isArchived` property to `SubscriptionDto` for better subscription state tracking
- Customer object and `isArchived` filter to subscription queries for improved filtering capabilities

### Changed
- Improved Stripe subscription period and metadata handling for more reliable synchronization
- Clarified Stripe Checkout session parameter usage in documentation

### Fixed
- 


## [0.1.15] - 2025-11-21

### Fixed
- Enhanced validation for `onExpireTransitionToBillingCycleKey` to ensure the referenced billing cycle exists across all plans within the same product, preventing invalid transition configurations

### Changed
- Enhanced subscription lifecycle documentation with comprehensive trial scenarios and best practices, including detailed guidance on trial end behaviors and billing period configuration

## [0.1.14] - 2025-11-19

### Added
- 

### Changed
- 

### Fixed
- 


## [0.1.13] - 2025-11-19

### Added
- Database migration functionality for managing schema changes
- Subscription transition handling when subscriptions expire, allowing automatic transitions to specified billing cycles
- Schema updates to support subscription expiration transitions

### Fixed
- Validation for `onExpireTransitionToBillingCycleKey` parameter in subscription management to prevent invalid transitions


## [0.1.13] - 2025-01-27

### Added
- Database migration functionality for managing schema changes
- Subscription transition handling when subscriptions expire, allowing automatic transitions to specified billing cycles
- Schema updates to support subscription expiration transitions

### Fixed
- Validation for `onExpireTransitionToBillingCycleKey` parameter in subscription management to prevent invalid transitions

## [0.1.12] - 2025-11-17

### Added
- 

### Changed
- Moved tables to their own schema

### Fixed
- 


## [0.1.11] - 2025-11-17

### Added
- ConfigSyncDto and ConfigSyncService integration into Subscrio for configuration synchronization

### Changed
- Updated Vite configuration to include additional external dependencies

### Removed
- CONFIG_SYNC.md documentation file

## [0.1.10] - 2025-11-15

### Added
- Automatic Stripe customer synchronization and broader event mapping coverage, plus refreshed integration docs to clarify setup end to end.
- Modular core API reference documentation and a rewritten getting started guide to accelerate onboarding.

### Changed
- Standardized all reference content into modular service files with consistent formatting, including expanded TypeScript service guidance and refined billing cycle docs.

### Removed
- API key management feature and its supporting documentation, reflecting the streamlined direct-import architecture.
- Legacy monolithic documentation directory, replaced by the new reference structure.

## [0.1.9] - 2025-11-13

### Changed

* Standardized entities and repositories to use numeric IDs internally for more consistent type handling.
* Refactored repository ID handling and removed redundant checks to simplify the code and reduce edge-case failures.
* Updated API documentation to reference public keys instead of internal IDs where appropriate, clarifying how clients should interact with the API.
* Updated `package.json` metadata and scripts in preparation for the 0.1.9 release.


All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.4] - 2025-11-13

### Added
- Archived state for customers and subscriptions with filtering and deletion enforcement
- Status enums and database column for billing cycles
- Shared utilities for feature validation, time handling, and constants
- Base repository and service abstractions with environment config helpers
- Audit report document with findings and remediation plan
- Expanded README, API reference, and demo scripts
- MIT license, `.npmignore`, and npm metadata updates

### Changed
- Renamed package from `@subscrio/core` to `@saas-experts/subscrio`
- Removed `inactive` status; use `archived` instead
- Prevent deletion of entities with associations or active subscriptions
- Made keys and foreign-key identifiers immutable in update DTOs
- Switched to `npm` and improved CLI output
- Optimized feature checks and plan lookups with caching

### Deprecated
- None.

### Removed
- Legacy code paths and deprecated assets from published package
- Manual SQL sanitization helpers; using Drizzle's parameterized queries
- `inactive` status value from DTOs and docs

### Fixed
- Hardened deletion and association rules to prevent orphaned states
- Improved test coverage for subscriptions, features, and SQL injection

### Security
- Strengthened SQL injection defenses with parameterized queries

## [0.1.0] - 2024-12-19

### Added
- Initial release of @subscrio/core TypeScript library
- Complete subscription management system with feature flags
- PostgreSQL integration with Drizzle ORM
- Stripe webhook processing and integration
- Feature resolution hierarchy (subscription overrides → plan values → defaults)
- Comprehensive TypeScript type definitions
- ESM and CommonJS dual package support
- Full test suite with E2E testing

### Features
- **Product Management**: Create and manage subscription products
- **Feature Flags**: Granular feature control with numeric, toggle, and text values
- **Plan Management**: Flexible subscription plans with feature value overrides
- **Customer Management**: Customer lifecycle and external ID mapping
- **Subscription Lifecycle**: Complete subscription management from trial to renewal
- **Billing Cycles**: Flexible billing periods (monthly, annual, custom)
- **Feature Resolution**: Smart hierarchy for determining feature access
- **Stripe Integration**: Webhook processing and payment synchronization
- **TypeScript Support**: Full type safety and excellent developer experience

### Technical Details
- Built with TypeScript 5.0+
- Uses Drizzle ORM for database operations
- Supports PostgreSQL 12+
- Node.js 18+ required
- MIT License
