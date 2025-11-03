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
    Task<SearchCustomerResult> SearchCustomer(string substring);
}
