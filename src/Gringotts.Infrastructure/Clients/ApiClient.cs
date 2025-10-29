using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Gringotts.Contracts.Requests;
using Gringotts.Contracts.Responses;
using Gringotts.Contracts.Results;
using Gringotts.Domain.Entities;
using Gringotts.Shared.Enums;

namespace Gringotts.Infrastructure.Clients;

public class ApiClient
{
    private readonly IHttpClientFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiClient(IHttpClientFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    // Auth: POST /auth/check
    public async Task<Result> CheckAccessCodeAsync(string userName, int accessCode, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient();
        var payload = new { UserName = userName, AccessCode = accessCode };
        var resp = await client.PostAsJsonAsync("/auth/check", payload, cancellationToken).ConfigureAwait(false);

        // API returns BaseResponse (with ErrorCode and Errors). Map to Result.
        var result = new Result();

        try
        {
            var body = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (body != null)
            {
                result.ErrorCode = body.ErrorCode;
                if (body.Errors != null && body.Errors.Count > 0)
                    result.ErrorMessage.AddRange(body.Errors);
            }
        }
        catch
        {
            // ignore deserialization issues
        }

        if (resp.IsSuccessStatusCode)
        {
            result.Success = true;
            result.ErrorCode = ErrorCode.None;
        }
        else
        {
            result.Success = false;
            if (result.ErrorCode == 0) result.ErrorCode = ErrorCode.InternalError;
        }

        return result;
    }

    // Customers: GET /customers/{id}
    public async Task<CustomerResult> GetCustomerByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync($"/customers/{id}", cancellationToken).ConfigureAwait(false);

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

    // POST /customers
    public async Task<CustomerResult> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/customers", customer, cancellationToken).ConfigureAwait(false);

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

    // PATCH /customers/{id}/charactername
    public async Task<CustomerResult> UpdateCharacterNameAsync(long id, string characterName, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient();
        var payload = new { CharacterName = characterName };
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/customers/{id}/charactername")
        {
            Content = JsonContent.Create(payload, options: _jsonOptions)
        };

        var resp = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);

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

    // POST /transactions
    public async Task<TransactionResult> CreateTransactionAsync(TransactionRequest request, CancellationToken cancellationToken = default)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/transactions", request, cancellationToken).ConfigureAwait(false);

        if (resp.IsSuccessStatusCode)
        {
            // API returns an object { ErrorCode = None, Transaction = { ... } }
            try
            {
                using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

                var root = doc.RootElement;
                var txElem = root.GetProperty("Transaction");
                var tx = JsonSerializer.Deserialize<Transaction>(txElem.GetRawText(), _jsonOptions);

                return new TransactionResult
                {
                    Success = true,
                    ErrorCode = ErrorCode.None,
                    Transaction = tx!
                };
            }
            catch
            {
                return new TransactionResult { Success = true, ErrorCode = ErrorCode.None };
            }
        }

        // error
        var error = await resp.Content.ReadFromJsonAsync<BaseResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
        return new TransactionResult
        {
            Success = false,
            ErrorCode = error?.ErrorCode ?? ErrorCode.InternalError,
            ErrorMessage = error?.Errors ?? new List<string>()
        };
    }
}
