using System.Text;
using AccesoDatos;
using Controladores;
using Controladores.Autenticacion;
using Controladores.Opciones;
using Entidades;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<EimaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<JwtOpciones>(builder.Configuration.GetSection(JwtOpciones.Seccion));
builder.Services.AddSingleton<IPasswordHasher<CuentaUsuario>, PasswordHasher<CuentaUsuario>>();
builder.Services.AddScoped<ServicioAutenticacion>();

var jwtConfig = builder.Configuration.GetSection(JwtOpciones.Seccion).Get<JwtOpciones>() ?? new JwtOpciones();
if (string.IsNullOrWhiteSpace(jwtConfig.ClaveFirma) || jwtConfig.ClaveFirma.Length < 32)
{
    throw new InvalidOperationException(
        "Configure Jwt:ClaveFirma en appsettings (mínimo 32 caracteres) para firmar los tokens.");
}

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
    });

builder.Services.AddControllers()
    .AddApplicationPart(typeof(PersonasController).Assembly)
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
