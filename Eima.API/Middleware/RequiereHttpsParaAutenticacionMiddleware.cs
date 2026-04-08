namespace Eima.API.Middleware;

/// <summary>Exige HTTPS para rutas bajo <c>/api/auth</c>, salvo desarrollo local si <c>Auth:PermitirAuthSinHttpsEnDesarrollo</c> es true.</summary>
public sealed class RequiereHttpsParaAutenticacionMiddleware
{
    private const string PrefijoAuth = "/api/auth";
    private readonly RequestDelegate _siguiente;

    public RequiereHttpsParaAutenticacionMiddleware(RequestDelegate siguiente)
    {
        _siguiente = siguiente;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IWebHostEnvironment entorno,
        IConfiguration configuration)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith(PrefijoAuth, StringComparison.OrdinalIgnoreCase))
        {
            await _siguiente(context);
            return;
        }

        if (context.Request.IsHttps)
        {
            await _siguiente(context);
            return;
        }

        var permitirEnDev = configuration.GetValue("Auth:PermitirAuthSinHttpsEnDesarrollo", true);
        if (entorno.IsDevelopment() && permitirEnDev)
        {
            await _siguiente(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            errores = new[]
            {
                new { campo = "Transporte", mensaje = "La autenticación solo está disponible mediante HTTPS." }
            }
        });
    }
}
