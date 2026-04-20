using System.Text;
using AccesoDatos;
using Controladores;
using Controladores.Admin;
using Controladores.Autenticacion;
using Controladores.Opciones;
using Eima.API.Middleware;
using Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// CORS: permitir al frontend (Vite) consumir la API en desarrollo.
const string CorsPolicyFrontend = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyFrontend, policy =>
        policy
            .WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            // Necesario si se usan cookies HttpOnly (JWT en cookie) o credenciales.
            .AllowCredentials());
});

builder.Services.AddDbContext<EimaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EimaDbContext>();
    await MateriasCatalogoSemilla.AsegurarEnBdAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(CorsPolicyFrontend);
app.UseMiddleware<RequiereHttpsParaAutenticacionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
