import { ICustomerRepository } from '../repositories/ICustomerRepository.js';
import { 
  CreateCustomerDto, 
  CreateCustomerDtoSchema, 
  UpdateCustomerDto, 
  UpdateCustomerDtoSchema,
  CustomerFilterDto,
  CustomerFilterDtoSchema,
  CustomerDto 
} from '../dtos/CustomerDto.js';
import { CustomerMapper } from '../mappers/CustomerMapper.js';
import { Customer } from '../../domain/entities/Customer.js';
import { CustomerStatus } from '../../domain/value-objects/CustomerStatus.js';
import { now } from '../../infrastructure/utils/date.js';
import { 
  ValidationError, 
  NotFoundError, 
  ConflictError 
} from '../errors/index.js';

export class CustomerManagementService {
  constructor(
    private readonly customerRepository: ICustomerRepository
  ) {}

  async createCustomer(dto: CreateCustomerDto): Promise<CustomerDto> {
    const validationResult = CreateCustomerDtoSchema.safeParse(dto);
    if (!validationResult.success) {
      throw new ValidationError(
        'Invalid customer data',
        validationResult.error.errors
      );
    }
    const validatedDto = validationResult.data;

    // Check if key already exists
    const existing = await this.customerRepository.findByKey(validatedDto.key);
    if (existing) {
      throw new ConflictError(`Customer with key '${validatedDto.key}' already exists`);
    }

    // Check if external billing ID already exists (if provided)
    if (validatedDto.externalBillingId) {
      const existingBilling = await this.customerRepository.findByExternalBillingId(validatedDto.externalBillingId);
      if (existingBilling) {
        throw new ConflictError(`Customer with external billing ID '${validatedDto.externalBillingId}' already exists`);
      }
    }

    // Create domain entity (no ID - database will generate)
    const customer = new Customer({
      key: validatedDto.key,
      displayName: validatedDto.displayName,
      email: validatedDto.email,
      externalBillingId: validatedDto.externalBillingId,
      status: CustomerStatus.Active,
      metadata: validatedDto.metadata,
      createdAt: now(),
      updatedAt: now()
    });

    // Save and get entity with generated ID
    const savedCustomer = await this.customerRepository.save(customer);
    return CustomerMapper.toDto(savedCustomer);
  }

  async updateCustomer(key: string, dto: UpdateCustomerDto): Promise<CustomerDto> {
    const validationResult = UpdateCustomerDtoSchema.safeParse(dto);
    if (!validationResult.success) {
      throw new ValidationError(
        'Invalid update data',
        validationResult.error.errors
      );
    }
    const validatedDto = validationResult.data;

    const customer = await this.customerRepository.findByKey(key);
    if (!customer) {
      throw new NotFoundError(`Customer with key '${key}' not found. Please check the customer key and try again.`);
    }

    // Key is immutable - no validation needed

    if (validatedDto.externalBillingId && validatedDto.externalBillingId !== customer.props.externalBillingId) {
      const existing = await this.customerRepository.findByExternalBillingId(validatedDto.externalBillingId);
      if (existing && existing.id !== customer.id) {
        throw new ConflictError(`Customer with external billing ID '${validatedDto.externalBillingId}' already exists`);
      }
    }

    // Update properties (key is immutable)
    if (validatedDto.displayName !== undefined) {
      customer.props.displayName = validatedDto.displayName;
    }
    if (validatedDto.email !== undefined) {
      customer.props.email = validatedDto.email;
    }
    if (validatedDto.externalBillingId !== undefined) {
      customer.props.externalBillingId = validatedDto.externalBillingId;
    }
    if (validatedDto.metadata !== undefined) {
      customer.props.metadata = validatedDto.metadata;
    }

    customer.props.updatedAt = now();
    const savedCustomer = await this.customerRepository.save(customer);
    return CustomerMapper.toDto(savedCustomer);
  }

  async getCustomer(key: string): Promise<CustomerDto | null> {
    const customer = await this.customerRepository.findByKey(key);
    return customer ? CustomerMapper.toDto(customer) : null;
  }

  async listCustomers(filters: CustomerFilterDto = { limit: 50, offset: 0 }): Promise<CustomerDto[]> {
    const validationResult = CustomerFilterDtoSchema.safeParse(filters);
    if (!validationResult.success) {
      throw new ValidationError(
        'Invalid filter parameters',
        validationResult.error.errors
      );
    }

    const customers = await this.customerRepository.findAll(validationResult.data);
    return customers.map(CustomerMapper.toDto);
  }

  async archiveCustomer(key: string): Promise<void> {
    const customer = await this.customerRepository.findByKey(key);
    if (!customer) {
      throw new NotFoundError(`Customer with key '${key}' not found. Please check the customer key and try again.`);
    }

    customer.archive();
    await this.customerRepository.save(customer);
  }

  async unarchiveCustomer(key: string): Promise<void> {
    const customer = await this.customerRepository.findByKey(key);
    if (!customer) {
      throw new NotFoundError(`Customer with key '${key}' not found. Please check the customer key and try again.`);
    }

    customer.unarchive();
    await this.customerRepository.save(customer);
  }

  async deleteCustomer(key: string): Promise<void> {
    const customer = await this.customerRepository.findByKey(key);
    if (!customer) {
      throw new NotFoundError(`Customer with key '${key}' not found. Please check the customer key and try again.`);
    }

    if (!customer.canDelete()) {
      throw new ValidationError(
        `Cannot delete customer with status '${customer.status}'. ` +
        'Customer must be archived before permanent deletion.'
      );
    }

    // Customer from repository always has ID (BIGSERIAL PRIMARY KEY)
    await this.customerRepository.delete(customer.id!);
  }
}
