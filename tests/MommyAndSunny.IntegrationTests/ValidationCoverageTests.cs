using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MommyAndSunny.API.Controllers;
using MommyAndSunny.Application.Auth.DTOs;
using System.Reflection;

namespace MommyAndSunny.IntegrationTests;

public class ValidationCoverageTests
{
    [Fact]
    public void EveryApplicationRequestUsedByController_HasValidator()
    {
        var applicationAssembly = typeof(RegisterRequest).Assembly;
        var validatedTypes = applicationAssembly.GetTypes()
            .SelectMany(type => type.GetInterfaces())
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValidator<>))
            .Select(type => type.GetGenericArguments()[0])
            .ToHashSet();

        var requestTypes = typeof(PaymentsController).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            .SelectMany(method => method.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .Where(type => type.Assembly == applicationAssembly)
            .Where(type => type.Name.EndsWith("Request", StringComparison.Ordinal) ||
                           type.Name.EndsWith("FilterRequest", StringComparison.Ordinal) ||
                           type.Name == "CreatePersonalImportRequestDto")
            .Distinct()
            .ToArray();

        var missing = requestTypes.Where(type => !validatedTypes.Contains(type)).Select(type => type.FullName).ToArray();

        Assert.True(missing.Length == 0, $"Các request chưa có validator: {string.Join(", ", missing)}");
    }
}
