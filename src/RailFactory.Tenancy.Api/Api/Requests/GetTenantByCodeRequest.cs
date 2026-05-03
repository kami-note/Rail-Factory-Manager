using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace RailFactory.Tenancy.Api.Api;

public sealed record GetTenantByCodeRequest(
    [property: FromRoute(Name = "code"), Required, StringLength(32, MinimumLength = 2)]
    string Code);
