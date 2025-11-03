using Gringotts.Contracts.Results;
using Gringotts.Domain.Entities;

namespace Gringotts.Infrastructure.Interfaces;

public interface ICustomersService
{
    Task<CustomerResult> GetCustomerById(long id);
    Task<CustomerResult> CreateCustomer(Customer customer);
    Task<CustomerResult> UpdateCustomer(long id, Customer customer);
    Task<CustomerResult> UpdateCharacterName(long id, string name);
    Task<CustomerResult> CreateOrUpdateCustomer(Customer customer);
    Task<CustomersListResult> SearchCustomers(string substring, int? pageNumber = null, int? pageSize = null);
    Task<CustomersListResult> GetAllCustomers(int? pageNumber = null, int? pageSize = null);
}
