namespace Gringotts.Contracts.DTO;

public record CreateTransactionDto(long RecipientId, decimal Amount, string Description);
