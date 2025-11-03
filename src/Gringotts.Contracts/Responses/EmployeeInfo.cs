using System;

namespace Gringotts.Contracts.Responses;

public class EmployeeInfo
{
 public Guid Id { get; set; }
 public string UserName { get; set; } = string.Empty;
}
