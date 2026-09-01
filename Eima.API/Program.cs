using System.Text;
using AccesoDatos;
using Controladores;
using Controladores.Admin;
using Controladores.Autenticacion;
using Controladores.Opciones;
using Eima.API.Middleware;
using Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

const string CorsPolicyFrontend = "Frontend";
var corsOrigens = builder.Configuration.GetSection("Cors:Origenes").Get<string[]>()
    ?? new[]
    {
        "http://localhost:5173",
        "https://localhost:5173",
        "http://127.0.0.1:5173",
        "https://127.0.0.1:5173"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyFrontend, policy =>
        policy
            .WithOrigins(corsOrigens)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddDbContext<EimaDbContext>(options =>
    options.UseNpgsql(ResolverCadenaConexion(builder.Configuration)));

builder.Services.Configure<JwtOpciones>(builder.Configuration.GetSection(JwtOpciones.Seccion));
builder.Services.Configure<RecuperacionContrasenaOpciones>(
    builder.Configuration.GetSection(RecuperacionContrasenaOpciones.Seccion));
builder.Services.AddSingleton<IPasswordHasher<CuentaUsuario>, PasswordHasher<CuentaUsuario>>();
builder.Services.AddScoped<ServicioAutenticacion>();
builder.Services.AddScoped<ServicioRecuperacionContrasena>();
builder.Services.AddScoped<ServicioCambioRolAdmin>();

var jwtConfig = builder.Configuration.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>() ?? new JwtOpciones();
if (string.IsNullOrWhiteSpace(jwtConfig.ClaveFirma) || jwtConfig.ClaveFirma.Length < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:ClaveFirma en appsettings (mínimo 32 caracteres) para firmar los tokens.");
}

var nombreCookieJwt = string.IsNullOrWhiteSpace(jwtConfig.NombreCookieAccessToken)
    ? "eima_access_token"
    : jwtConfig.NombreCookieAccessToken;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig.Emisor,
            ValidAudience = jwtConfig.Audiencia,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.ClaveFirma)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!string.IsNullOrEmpty(context.Token))
                    return Task.CompletedTask;
                if (context.Request.Cookies.TryGetValue(nombreCookieJwt, out var tokenCookie) &&
                    !string.IsNullOrWhiteSpace(tokenCookie))
                    context.Token = tokenCookie;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddApplicationPart(typeof(PersonasController).Assembly)
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Eima API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT en el encabezado Authorization. Ejemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

app.UseForwardedHeaders();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EimaDbContext>();
    await db.Database.MigrateAsync();
    await RolesCatalogoSemilla.AsegurarEnBdAsync(db);
    await MateriasCatalogoSemilla.AsegurarEnBdAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyFrontend);
app.UseMiddleware<RequiereHttpsParaAutenticacionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

app.Run();

static string ResolverCadenaConexion(IConfiguration configuration)
{
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
        return ConvertirDatabaseUrl(databaseUrl);

    var desdeConfig = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(desdeConfig))
    {
        if (EsCadenaSqlServer(desdeConfig))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection parece ser de SQL Server. " +
                "Eliminá esa variable en Render y usá DATABASE_URL (vinculando la Postgres) " +
                "o una cadena Npgsql con Host=...;Port=5432;...");
        }

        return desdeConfig;
    }

    throw new InvalidOperationException(
        "Configure DATABASE_URL (Render) o ConnectionStrings:DefaultConnection (PostgreSQL).");
}

static bool EsCadenaSqlServer(string connectionString)
{
    var lower = connectionString.ToLowerInvariant();
    return lower.Contains("trusted_connection")
        || lower.Contains("integrated security")
        || lower.Contains("initial catalog=")
        || lower.Contains("data source=");
}

static string ConvertirDatabaseUrl(string databaseUrl)
{
    if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        databaseUrl = "postgresql://" + databaseUrl["postgres://".Length..];

    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
        SslMode = SslMode.Require
    };
    return builder.ConnectionString;
}
