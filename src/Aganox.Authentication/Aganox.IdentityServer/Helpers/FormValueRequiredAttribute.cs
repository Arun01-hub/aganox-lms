using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Aganox.IdentityServer.Helpers;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class FormValueRequiredAttribute : Attribute, IActionConstraint
{
    private readonly string _formKey;

    public FormValueRequiredAttribute(string formKey) => _formKey = formKey;

    public int Order => 0;

    public bool Accept(ActionConstraintContext context)
    {
        if (!context.RouteContext.HttpContext.Request.HasFormContentType)
        {
            return false;
        }

        return context.RouteContext.HttpContext.Request.Form.ContainsKey(_formKey);
    }
}
