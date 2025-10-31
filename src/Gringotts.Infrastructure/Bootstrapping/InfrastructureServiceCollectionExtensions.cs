using Gringotts.Infrastructure.UnitOfWork;
using Gringotts.Infrastructure.Repositories;
using Gringotts.Infrastructure.Services;
using Gringotts.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Gringotts.Contracts.Interfaces;
using Gringotts.Infrastructure.Caching;

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
        public static IServiceCollection AddCache(
            this IServiceCollection services,
            string mode,                          // "Memory" or "Redis"
            string? redisConnectionString = null)
        {
            if (string.Equals(mode, "Redis", StringComparison.OrdinalIgnoreCase))
            {
                // Packages:
                // - Microsoft.Extensions.Caching.StackExchangeRedis
                // - StackExchange.Redis
                services.AddStackExchangeRedisCache(o =>
                {
                    o.Configuration = redisConnectionString
                        ?? throw new ArgumentNullException(nameof(redisConnectionString));
                    // Optional: o.InstanceName = "myapp:";
                });
                services.AddSingleton<ICache, RedisCache>();
            }
            else
            {
                // Package: Microsoft.Extensions.Caching.Memory
                services.AddMemoryCache();
                services.AddSingleton<ICache, MemoryCache>();
            }

            return services;
        }
    }
}
