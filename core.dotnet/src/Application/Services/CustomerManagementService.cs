using FluentValidation;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Utils;

namespace Subscrio.Core.Application.Services;

public class CustomerManagementService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<CreateCustomerDto> _createCustomerValidator;
    private readonly IValidator<UpdateCustomerDto> _updateCustomerValidator;
    private readonly IValidator<CustomerFilterDto> _customerFilterValidator;

    public CustomerManagementService(
        ICustomerRepository customerRepository,
        IValidator<CreateCustomerDto> createCustomerValidator,
        IValidator<UpdateCustomerDto> updateCustomerValidator,
        IValidator<CustomerFilterDto> customerFilterValidator)
    {
        _customerRepository = customerRepository;
        _createCustomerValidator = createCustomerValidator;
        _updateCustomerValidator = updateCustomerValidator;
        _customerFilterValidator = customerFilterValidator;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        // Validate input
        var validationResult = await _createCustomerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                $"Invalid customer data for key '{dto.Key}': {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}",
                validationResult.Errors
            );
        }

        // Check if key already exists
        var existing = await _customerRepository.FindByKeyAsync(dto.Key);
        if (existing != null)
        {
            throw new ConflictException($"Customer with key '{dto.Key}' already exists");
        }

        // Check if external billing ID already exists (if provided)
        if (dto.ExternalBillingId != null)
        {
            var existingBilling = await _customerRepository.FindByExternalBillingIdAsync(dto.ExternalBillingId);
            if (existingBilling != null)
            {
                throw new ConflictException($"Customer with external billing ID '{dto.ExternalBillingId}' already exists");
            }
        }

        // Create domain entity (no ID - database will generate)
        var customer = new Customer(new CustomerProps(
            dto.Key,
            dto.DisplayName,
            dto.Email,
            dto.ExternalBillingId,
            CustomerStatus.Active,
            dto.Metadata,
            DateHelper.Now(),
            DateHelper.Now()
        ));

        // Save and get entity with generated ID
        var savedCustomer = await _customerRepository.SaveAsync(customer);
        return CustomerMapper.ToDto(savedCustomer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(string key, UpdateCustomerDto dto)
    {
        // Validate input
        var validationResult = await _updateCustomerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid update data", validationResult.Errors);
        }

        var customer = await _customerRepository.FindByKeyAsync(key);
        if (customer == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        // Key is immutable - no validation needed

        if (dto.ExternalBillingId != null && dto.ExternalBillingId != customer.Props.ExternalBillingId)
        {
            var existing = await _customerRepository.FindByExternalBillingIdAsync(dto.ExternalBillingId);
            if (existing != null && existing.Id != customer.Id)
            {
                throw new ConflictException($"Customer with external billing ID '{dto.ExternalBillingId}' already exists");
            }
        }

        // Update properties (key is immutable)
        if (dto.DisplayName != null)
        {
            customer.Props = customer.Props with { DisplayName = dto.DisplayName };
        }
        if (dto.Email != null)
        {
            customer.Props = customer.Props with { Email = dto.Email };
        }
        if (dto.ExternalBillingId != null)
        {
            customer.Props = customer.Props with { ExternalBillingId = dto.ExternalBillingId };
        }
        if (dto.Metadata != null)
        {
            customer.Props = customer.Props with { Metadata = dto.Metadata };
        }

        customer.Props = customer.Props with { UpdatedAt = DateHelper.Now() };
        var savedCustomer = await _customerRepository.SaveAsync(customer);
        return CustomerMapper.ToDto(savedCustomer);
    }

    public async Task<CustomerDto?> GetCustomerAsync(string key)
    {
        var customer = await _customerRepository.FindByKeyAsync(key);
        return customer != null ? CustomerMapper.ToDto(customer) : null;
    }

    public async Task<IReadOnlyList<CustomerDto>> ListCustomersAsync(CustomerFilterDto? filters = null)
    {
        filters ??= new CustomerFilterDto();
        var validationResult = await _customerFilterValidator.ValidateAsync(filters);
        if (!validationResult.IsValid)
        {
            throw new ValidationException("Invalid filter parameters", validationResult.Errors);
        }

        var customers = await _customerRepository.FindAllAsync(validationResult.Data);
        return customers.Select(CustomerMapper.ToDto).ToList();
    }

    public async Task ArchiveCustomerAsync(string key)
    {
        var customer = await _customerRepository.FindByKeyAsync(key);
        if (customer == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        customer.Archive();
        await _customerRepository.SaveAsync(customer);
    }

    public async Task UnarchiveCustomerAsync(string key)
    {
        var customer = await _customerRepository.FindByKeyAsync(key);
        if (customer == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        customer.Unarchive();
        await _customerRepository.SaveAsync(customer);
    }

    public async Task DeleteCustomerAsync(string key)
    {
        var customer = await _customerRepository.FindByKeyAsync(key);
        if (customer == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        if (!customer.CanDelete())
        {
            throw new ValidationException(
                $"Cannot delete customer with status '{customer.Status}'. " +
                "Customer must be archived before permanent deletion."
            );
        }

        // Customer from repository always has ID (BIGSERIAL PRIMARY KEY)
        if (!customer.Id.HasValue)
        {
            throw new DomainException($"Customer '{customer.Key}' does not have an ID and cannot be deleted.");
        }

        await _customerRepository.DeleteAsync(customer.Id.Value);
    }
}

