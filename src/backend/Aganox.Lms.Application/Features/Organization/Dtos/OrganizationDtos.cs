namespace Aganox.Lms.Application.Features.Organization.Dtos;

public sealed record DepartmentDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive,
    int EmployeeCount);

public sealed record EmployeeSummaryDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    string DepartmentName,
    string Designation,
    DateOnly DateOfJoining,
    string Status,
    string Email,
    string? Phone,
    Guid? ManagerId,
    string? ManagerName,
    Guid? UserId);

public sealed record EmployeeProfileDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    string DepartmentName,
    string Designation,
    DateOnly DateOfJoining,
    string Status,
    string Email,
    string? Phone,
    Guid? ManagerId,
    string? ManagerName,
    Guid? UserId,
    string? UserEmail,
    IReadOnlyList<EmployeeSummaryDto> DirectReports);

public sealed record CreateDepartmentRequest(string Name, string Code, bool IsActive = true);
public sealed record UpdateDepartmentRequest(string Name, string Code, bool IsActive);

public sealed record CreateEmployeeRequest(
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    string Designation,
    DateOnly DateOfJoining,
    string Status,
    string Email,
    string? Phone,
    Guid? ManagerId,
    Guid? LeaveCalendarId,
    bool CreateUserAccount,
    string? UserPassword,
    string? UserRole);

public sealed record UpdateEmployeeRequest(
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    string Designation,
    DateOnly DateOfJoining,
    string Status,
    string Email,
    string? Phone,
    Guid? ManagerId,
    Guid? LeaveCalendarId);

public sealed record LinkEmployeeUserRequest(
    Guid? UserId,
    string? Email,
    string? Password,
    string? Role);
