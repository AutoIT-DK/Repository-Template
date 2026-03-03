using System.Net.Http.Headers;
using System.Security.Claims;
using Duende.Bff;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using SecurityModel;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin();
    });
});

builder.Services.AddReverseProxiedAuthentication(builder.Configuration);
builder.Services.AddBff();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("cookie", options =>
    {
        options.Cookie.Name = "bff";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.Path = "/";
    })
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["Security:IdentityServerUrl"] ?? "https://devidentityserver.autoit.dk/";
        options.ClientId = builder.Configuration["Security:ClientId"] ?? "50100";
        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SaveTokens = true;
        options.MapInboundClaims = false;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add(builder.Configuration["Security:Scope"] ?? "Autodesktop");
        options.Scope.Add("offline_access");

        options.NonceCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.NonceCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.NonceCookie.HttpOnly = true;
        options.CorrelationCookie.HttpOnly = true;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseRouting();
app.UseBff();
app.UseAuthorization();

app.MapBffManagementEndpoints();

app.MapGet("/api/user", async (HttpContext httpContext, IHttpClientFactory httpClientFactory, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var userId = ResolveUserId(httpContext.User);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var policyBaseUrl = configuration["Security:PolicyProviderBaseUrl"];
    if (string.IsNullOrWhiteSpace(policyBaseUrl))
    {
        return Results.Problem("Missing Security:PolicyProviderBaseUrl configuration.", statusCode: StatusCodes.Status500InternalServerError);
    }

    var accessToken = await httpContext.GetTokenAsync("access_token");
    var client = httpClientFactory.CreateClient();

    using var request = new HttpRequestMessage(HttpMethod.Get, $"{policyBaseUrl.TrimEnd('/')}/v1/UserManagement/User/{Uri.EscapeDataString(userId)}");
    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    var response = await client.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(
            $"Policy provider user lookup failed with status code {(int)response.StatusCode}.",
            statusCode: (int)response.StatusCode);
    }

    var body = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(body, "application/json");
})
.RequireAuthorization()
.AsBffApiEndpoint();

app.MapGet("/api/user/access", async (HttpContext httpContext, IHttpClientFactory httpClientFactory, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var userId = ResolveUserId(httpContext.User);
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Unauthorized();
    }

    var policyBaseUrl = configuration["Security:PolicyProviderBaseUrl"];
    if (string.IsNullOrWhiteSpace(policyBaseUrl))
    {
        return Results.Problem("Missing Security:PolicyProviderBaseUrl configuration.", statusCode: StatusCodes.Status500InternalServerError);
    }

    var accessToken = await httpContext.GetTokenAsync("access_token");
    var client = httpClientFactory.CreateClient();

    using var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"{policyBaseUrl.TrimEnd('/')}/v1/SecurityPolicyProvider/User/{Uri.EscapeDataString(userId)}/PolicyType/1?includePermissions=true&includeCapabilities=true");

    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    var response = await client.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem(
            $"Policy provider access lookup failed with status code {(int)response.StatusCode}.",
            statusCode: (int)response.StatusCode);
    }

    var body = await response.Content.ReadAsStringAsync(cancellationToken);
    return Results.Content(body, "application/json");
})
.RequireAuthorization()
.AsBffApiEndpoint();

app.Run();

static string? ResolveUserId(ClaimsPrincipal user)
{
    return user.FindFirstValue("sub")
        ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("userId");
}
