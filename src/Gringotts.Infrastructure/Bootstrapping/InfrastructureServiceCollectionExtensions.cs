using Gringotts.Infrastructure.UnitOfWork;
using Gringotts.Infrastructure.Repositories;
using Gringotts.Infrastructure.Services;
using Gringotts.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Gringotts.Infrastructure.Bootstrapping
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Unit of work
            services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();

            // Repositories
            services.AddScoped<ICustomersRepository, PostgreCustomersRepository>();
            services.AddScoped<ITransactionsRepository, PostgreTransactionsRepository>();
            services.AddScoped<IEmployeesRepository, PostgreEmployeesRepository>();

            // Services
            services.AddScoped<ICustomersService, CustomersService>();
            services.AddScoped<ITransactionsService, TransactionsService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
