using FluentValidation;
using Subscrio.Core.Application.DTOs;
using Subscrio.Core.Application.Errors;
using Subscrio.Core.Application.Mappers;
using Subscrio.Core.Application.Repositories;
using Subscrio.Core.Application.Validators;
using Subscrio.Core.Domain.Entities;
using Subscrio.Core.Domain.ValueObjects;
using Subscrio.Core.Infrastructure.Database;
using Subscrio.Core.Infrastructure.Utils;
using ValidationException = Subscrio.Core.Application.Errors.ValidationException;

namespace Subscrio.Core.Application.Services;

public class CustomerManagementService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly CreateCustomerDtoValidator _createValidator;
    private readonly UpdateCustomerDtoValidator _updateValidator;
    private readonly CustomerFilterDtoValidator _filterValidator;

    public CustomerManagementService(
        ICustomerRepository customerRepository,
        CreateCustomerDtoValidator createValidator,
        UpdateCustomerDtoValidator updateValidator,
        CustomerFilterDtoValidator filterValidator)
    {
        _customerRepository = customerRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerDto dto)
    {
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Invalid customer data",
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

        // Create record from DTO
        var record = new CustomerRecord
        {
            Id = 0, // Will be set by EF Core
            Key = dto.Key,
            DisplayName = dto.DisplayName,
            Email = dto.Email,
            ExternalBillingId = dto.ExternalBillingId,
            Status = CustomerStatus.Active.ToString().ToLowerInvariant(),
            Metadata = dto.Metadata,
            CreatedAt = DateHelper.Now(),
            UpdatedAt = DateHelper.Now()
        };

        // Save record
        var savedRecord = await _customerRepository.SaveAsync(record);
        var customer = CustomerMapper.ToDomain(savedRecord);
        return CustomerMapper.ToDto(customer);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(string key, UpdateCustomerDto dto)
    {
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Invalid update data",
                validationResult.Errors
            );
        }

        var record = await _customerRepository.FindByKeyAsync(key);
        if (record == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        // Convert to domain entity to check existing external billing ID
        var customer = CustomerMapper.ToDomain(record);
        if (dto.ExternalBillingId != null && dto.ExternalBillingId != customer.Props.ExternalBillingId)
        {
            var existing = await _customerRepository.FindByExternalBillingIdAsync(dto.ExternalBillingId);
            if (existing != null && existing.Id != record.Id)
            {
                throw new ConflictException($"Customer with external billing ID '{dto.ExternalBillingId}' already exists");
            }
        }

        // Update properties directly on record
        if (dto.DisplayName != null)
        {
            record.DisplayName = dto.DisplayName;
        }
        if (dto.Email != null)
        {
            record.Email = dto.Email;
        }
        if (dto.ExternalBillingId != null)
        {
            record.ExternalBillingId = dto.ExternalBillingId;
        }
        if (dto.Metadata != null)
        {
            record.Metadata = dto.Metadata;
        }

        record.UpdatedAt = DateHelper.Now();
        var savedRecord = await _customerRepository.SaveAsync(record);
        var savedCustomer = CustomerMapper.ToDomain(savedRecord);
        return CustomerMapper.ToDto(savedCustomer);
    }

    public async Task<CustomerDto?> GetCustomerAsync(string key)
    {
        var record = await _customerRepository.FindByKeyAsync(key);
        if (record == null) return null;
        
        var customer = CustomerMapper.ToDomain(record);
        return CustomerMapper.ToDto(customer);
    }

    public async Task<List<CustomerDto>> ListCustomersAsync(CustomerFilterDto? filters = null)
    {
        var filterDto = filters ?? new CustomerFilterDto();
        var validationResult = await _filterValidator.ValidateAsync(filterDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(
                "Invalid filter parameters",
                validationResult.Errors
            );
        }

        var records = await _customerRepository.FindAllAsync(filterDto);
        return records.Select(r => CustomerMapper.ToDto(CustomerMapper.ToDomain(r))).ToList();
    }

    public async Task ArchiveCustomerAsync(string key)
    {
        var record = await _customerRepository.FindByKeyAsync(key);
        if (record == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        // Simple property update - modify record directly
        record.Status = CustomerStatus.Archived.ToString().ToLowerInvariant();
        record.UpdatedAt = DateHelper.Now();
        await _customerRepository.SaveAsync(record);
    }

    public async Task UnarchiveCustomerAsync(string key)
    {
        var record = await _customerRepository.FindByKeyAsync(key);
        if (record == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        // Simple property update - modify record directly
        record.Status = CustomerStatus.Active.ToString().ToLowerInvariant();
        record.UpdatedAt = DateHelper.Now();
        await _customerRepository.SaveAsync(record);
    }

    public async Task DeleteCustomerAsync(string key)
    {
        var record = await _customerRepository.FindByKeyAsync(key);
        if (record == null)
        {
            throw new NotFoundException($"Customer with key '{key}' not found. Please check the customer key and try again.");
        }

        // Convert to domain entity for business rule validation
        var customer = CustomerMapper.ToDomain(record);
        if (!customer.CanDelete())
        {
            throw new ValidationException(
                $"Cannot delete customer with status '{customer.Status}'. " +
                "Customer must be archived before permanent deletion."
            );
        }

        await _customerRepository.DeleteAsync(record.Id);
    }
}
