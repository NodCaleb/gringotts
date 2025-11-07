using System.Text.Json;
using Gringotts.Contracts.Interfaces;
using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Responses;
using Gringotts.Contracts.Results;
using Gringotts.Domain.Entities;
using Gringotts.Shared.Enums;

namespace Gringotts.Web.Internals;

internal sealed class BffClient : IBffClient
{
    private readonly IHttpClientFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BffClient(IHttpClientFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public async Task<AuthResult> LoginAsync(AuthRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var client = _factory.CreateClient("GringottsBffClient");
        var resp = await client.PostAsJsonAsync("/auth/login", request, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode)
        {
            var authResp = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            return new AuthResult
            {
                Success = true,
                ErrorCode = authResp?.ErrorCode ?? ErrorCode.None,
                ErrorMessage = authResp?.Errors ?? new List<string>(),
                EmployeeId = authResp?.EmployeeId,
                AccessToken = authResp?.AccessToken
            };
        }

        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new AuthResult
        {
            Success = false,
            ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError,
            ErrorMessage = error?.Errors ?? new List<string>()
        };
    }

    public async Task<AuthResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("GringottsBffClient");
        var resp = await client.PostAsync("/auth/refresh", null, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode)
        {
            var authResp = await resp.Content.ReadFromJsonAsync<AuthResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            return new AuthResult
            {
                Success = true,
                ErrorCode = authResp?.ErrorCode ?? ErrorCode.None,
                ErrorMessage = authResp?.Errors ?? new List<string>(),
                EmployeeId = authResp?.EmployeeId,
                AccessToken = authResp?.AccessToken
            };
        }

        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new AuthResult
        {
            Success = false,
            ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError,
            ErrorMessage = error?.Errors ?? new List<string>()
        };
    }

    public async Task<bool> LogoutAsync(CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("GringottsBffClient");
        var resp = await client.PostAsync("/auth/logout", null, cancellationToken).ConfigureAwait(false);
        return resp.IsSuccessStatusCode;
    }

    public async Task<CustomerResult> GetCustomerByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("GringottsBffClient");
        var resp = await client.GetAsync($"/bff/customers/{id}", cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode)
        {
            var customerResp = await resp.Content.ReadFromJsonAsync<CustomerResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            return new CustomerResult
            {
                Success = true,
                ErrorCode = ErrorCode.None,
                Customer = customerResp?.Customer
            };
        }

        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new CustomerResult
        {
            Success = false,
            ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError,
            ErrorMessage = error?.Errors ?? new List<string>()
        };
    }
        
    public async Task<TransactionResult> CreateTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("GringottsBffClient");
        var resp = await client.PostAsJsonAsync("/bff/transactions", request, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode)
        {
            try
            {
                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
                var root = doc.RootElement;
                var txElem = root.GetProperty("Transaction");
                var tx = JsonSerializer.Deserialize<Transaction>(txElem.GetRawText(), _jsonOptions);
                return new TransactionResult { Success = true, ErrorCode = ErrorCode.None, Transaction = tx! };
            }
            catch
            {
                return new TransactionResult { Success = true, ErrorCode = ErrorCode.None };
            }
        }

        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new TransactionResult
        {
            Success = false,
            ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError,
            ErrorMessage = error?.Errors ?? new List<string>()
        };
    }

    public async Task<CustomersListResult> SearchCustomersAsync(string? search, int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("GringottsBffClient");
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (pageNumber.HasValue)
            queryParams.Add($"pageNumber={pageNumber.Value}");
        if (pageSize.HasValue)
            queryParams.Add($"pageSize={pageSize.Value}");

        var url = "/bff/customers" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);
        var resp = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode)
        {
            var listResp = await resp.Content.ReadFromJsonAsync<CustomersListResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            return new CustomersListResult
            {
                Success = true,
                ErrorCode = ErrorCode.None,
                Customers = listResp?.Customers ?? new List<Customer>()
            };
        }

        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new CustomersListResult
        {
            Success = false,
            ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError,
            ErrorMessage = error?.Errors ?? new List<string>()
        };
    }

    public async Task<TransactionsListResult> GetTransactionsAsync(int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("GringottsBffClient");
        var queryParams = new List<string>();
        if (pageNumber.HasValue) queryParams.Add($"pageNumber={pageNumber.Value}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        var url = "/bff/transactions" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);

        var resp = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode)
        {
            var listResp = await resp.Content.ReadFromJsonAsync<TransactionsListResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            return new TransactionsListResult { Success = true, ErrorCode = ErrorCode.None, Transactions = listResp?.Transactions ?? new List<Contracts.DTO.TransactionInfo>() };
        }

        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new TransactionsListResult { Success = false, ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError, ErrorMessage = error?.Errors ?? new List<string>() };
    }

    public async Task<TransactionsListResult> GetTransactionsByCustomerAsync(long customerId, int? pageNumber = null, int? pageSize = null, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient("GringottsBffClient");
        var queryParams = new List<string>();
        if (pageNumber.HasValue) queryParams.Add($"pageNumber={pageNumber.Value}");
        if (pageSize.HasValue) queryParams.Add($"pageSize={pageSize.Value}");
        var url = $"/bff/customers/{customerId}/transactions" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty);

        var resp = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (resp.IsSuccessStatusCode)
        {
            var listResp = await resp.Content.ReadFromJsonAsync<TransactionsListResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            return new TransactionsListResult { Success = true, ErrorCode = ErrorCode.None, Transactions = listResp?.Transactions ?? new List<Contracts.DTO.TransactionInfo>() };
        }

        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new TransactionsListResult { Success = false, ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError, ErrorMessage = error?.Errors ?? new List<string>() };
    }
}
