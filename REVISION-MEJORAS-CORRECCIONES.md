# Revisión EIMA — Mejoras, correcciones y optimizaciones

> **Fecha:** 26 de agosto de 2026  
> **Alcance:** EIMA-Backend (.NET 8) + EIMAFront (React 19 + Vite)  
> **Tipo:** Informe informativo para backlog / sprint review

---

## Resumen ejecutivo

| Proyecto | Stack | Estado general |
|----------|-------|----------------|
| **EIMA-Backend** | .NET 8, EF Core, JWT, SQL Server | Funcional, con **riesgos de seguridad altos** (endpoints abiertos, contraseña = DNI) |
| **EIMAFront** | React 19 + Vite + Axios | Buena base (validaciones, guards), con **deuda en seguridad cliente** y **módulo de materias desconectado** |

**Priorización sugerida:** seguridad → integridad de datos → robustez UX → limpieza/refactor.

---

## Top 5 hallazgos principales

### 1. Endpoints críticos sin autenticación (Backend)
La mayoría de la API está abierta: crear, editar y dar de baja personas no exige login ni rol. Cualquiera podría manipular usuarios y datos sensibles.

**Acción:** proteger todos los endpoints con `[Authorize]` y roles (`super_admin`, `administrativo`, etc.).

### 2. Contraseña por defecto = DNI (Backend)
Al crear o editar una persona, la contraseña inicial es el DNI (`PersonasController.cs` L339, L615). Como el DNI también se expone en respuestas GET, el riesgo es alto.

**Acción:** generar contraseña temporal aleatoria y forzar cambio en el primer login.

### 3. Escalada de privilegios a `super_admin` (Backend)
En `PersonasController`, el mapeo de roles acepta cualquier valor en el `default` del `switch`. Se puede crear un superadministrador sin restricción.

**Acción:** bloquear explícitamente el rol `super_admin` en creación/edición pública.

### 4. Módulo de materias desconectado del backend (Frontend)
`MateriasDashboard` guarda especialidades en `localStorage` (`eima_specialties`); el resto de la app usa `/api/Materias`. Los catálogos no coinciden.

**Acción:** conectar el ABM de materias a la API y eliminar el mock local.

### 5. Seguridad de sesión débil en el frontend (Frontend)
El JWT y el rol viven en `localStorage`, la UI confía en ese rol para mostrar pantallas admin, y no hay interceptor global para 401 (token expirado).

**Acción:** validar permisos contra el backend, usar cookies HttpOnly o token en memoria, y redirigir a login ante 401.

---

## Endpoints — Autorización

### Ya protegidos (no tocar)

| Método | Endpoint | Protección actual |
|--------|----------|-------------------|
| `GET` | `/api/Personas/mi-perfil` | `[Authorize]` — cualquier usuario logueado |
| `POST` | `/api/Auth/logout` | `[Authorize]` |
| `PATCH` | `/api/Admin/usuarios/rol` | `[Authorize(Roles = "super_admin")]` |

### Deben quedar públicos (sin auth)

| Método | Endpoint | Motivo |
|--------|----------|--------|
| `POST` | `/api/Auth/registro` | Registro público |
| `POST` | `/api/Auth/login` | Login |
| `POST` | `/api/Auth/recuperar-contrasena` | Recuperación |
| `POST` | `/api/Auth/restablecer-contrasena` | Restablecer clave |
| `POST` | `/api/Auth/fortaleza-contrasena` | Indicador en formularios |
| `POST` | `/api/Consultas/publico` | Formulario de contacto (`[AllowAnonymous]`) |
| `GET` | `/api/Materias/catalogo-por-area` | Catálogo para contacto/registro (`[AllowAnonymous]`) |

### Endpoints que hay que autorizar (19 en total)

#### PersonasController — usados por el frontend admin

| Método | Endpoint | Rol sugerido | Riesgo si queda abierto |
|--------|----------|--------------|-------------------------|
| `GET` | `/api/Personas` | `super_admin`, `administrativo` | Listado con DNI, correo, salario, etc. |
| `GET` | `/api/Personas/{id}` | `super_admin`, `administrativo` | Detalle completo de cualquier persona |
| `POST` | `/api/Personas` | `super_admin`, `administrativo` | Crear usuarios con cualquier rol |
| `PUT` | `/api/Personas/{id}` | `super_admin`, `administrativo` | Modificar datos y roles |
| `PATCH` | `/api/Personas/{id}/cambiar-estado` | `super_admin`, `administrativo` | Dar de baja/alta a cualquiera |

#### MateriasController

| Método | Endpoint | Rol sugerido |
|--------|----------|--------------|
| `GET` | `/api/Materias` | `super_admin`, `administrativo` |
| `GET` | `/api/Materias/{id}` | `super_admin`, `administrativo` |

#### RolesController

| Método | Endpoint | Rol sugerido |
|--------|----------|--------------|
| `GET` | `/api/Roles` | `super_admin`, `administrativo` |

#### Listados por rol (PII masiva)

| Método | Endpoint | Rol sugerido |
|--------|----------|--------------|
| `GET` | `/api/Alumnos` | `super_admin`, `administrativo` |
| `GET` | `/api/Alumnos/{id}` | `super_admin`, `administrativo` |
| `GET` | `/api/Profesores` | `super_admin`, `administrativo` |
| `GET` | `/api/Profesores/{id}` | `super_admin`, `administrativo` |
| `GET` | `/api/Empleados` | `super_admin`, `administrativo` |
| `GET` | `/api/Empleados/{id}` | `super_admin`, `administrativo` |

#### Datos académicos sensibles

| Método | Endpoint | Rol sugerido |
|--------|----------|--------------|
| `GET` | `/api/Clases` | `super_admin`, `administrativo`, `profesor`* |
| `GET` | `/api/Clases/{id}` | `super_admin`, `administrativo`, `profesor`* |
| `GET` | `/api/InscripcionesMateria` | `super_admin`, `administrativo` |
| `GET` | `/api/InscripcionesMateria/{id}` | `super_admin`, `administrativo` |

\*Si más adelante un docente solo ve sus clases, filtrar por `personaId` del token, no solo autorizar por rol.

### Implementación sugerida

```csharp
// A nivel de controller (ejemplo PersonasController)
[Authorize(Roles = $"{RolesSistema.SuperAdmin},{RolesSistema.Administrativo}")]
public class PersonasController : ControllerBase
{
    [Authorize] // cualquier rol autenticado
    [HttpGet("mi-perfil")]
    public async Task<IActionResult> MiPerfil(...) { ... }
}
```

**Patrón recomendado:**
1. `[Authorize(Roles = "...")]` en el **controller** para todo el ABM.
2. `[Authorize]` solo en `MiPerfil` (cualquier rol autenticado).
3. `[AllowAnonymous]` solo en auth, contacto y catálogo público de materias.

**Roles del sistema:** `super_admin` | `administrativo` | `profesor` | `alumno`

### Demostración: acceso anónimo a GET /api/Personas

Hoy ese endpoint no exige autenticación. Cualquiera puede acceder así:

- **Navegador:** `https://localhost:7145/api/Personas`
- **Swagger (Development):** `GET /api/Personas` → Execute sin Bearer token
- **PowerShell:** `Invoke-RestMethod -Uri "https://localhost:7145/api/Personas" -Method Get`
- **curl:** `curl -k "https://localhost:7145/api/Personas"`

Respuesta: JSON con DNI, teléfono, dirección, rol, cuenta de usuario, materias del docente, etc.

---

## Cuadro de revisión del backlog

### Seguridad y autenticación

| ID | Descripción | Estado anterior | Estado actual | Acción / observaciones |
|----|-------------|-----------------|---------------|------------------------|
| SEC-01 | Endpoints CRUD de personas sin `[Authorize]` | Con errores | Pendiente | Solo 3 puntos protegidos. Aplicar roles por endpoint. |
| SEC-02 | Escalada a `super_admin` vía API | Con errores | Pendiente | Bloquear `super_admin` en creación/edición en `PersonasController`. |
| SEC-03 | Contraseña por defecto = DNI | Con errores | Pendiente | Contraseña temporal + cambio obligatorio en primer login. |
| SEC-04 | Login no valida baja lógica (`Activo`) | Con errores | Pendiente | Verificar `persona.Activo` en `ServicioAutenticacion.cs` antes de emitir JWT. |
| SEC-05 | Exposición masiva de PII sin auth | Con errores | Pendiente | DTOs por rol + autenticación en GET de personas/alumnos/profesores. |
| SEC-06 | Enumeración de usuarios en recuperación de contraseña | Con errores | Pendiente | Respuesta genérica siempre 200 (backend y frontend). |
| SEC-07 | Token JWT en `localStorage` (frontend) | Con errores | Pendiente | Preferir cookies HttpOnly o token en memoria. |
| SEC-08 | Autorización basada en rol de `localStorage` | Con errores | Pendiente | Validar rol contra `/api/Personas/mi-perfil` o claims del JWT. |
| SEC-09 | Sin rate limiting en login/registro/consultas | Pendiente | Pendiente | Agregar `AddRateLimiter` en ASP.NET Core 8. |

### Bugs funcionales

| ID | Descripción | Estado anterior | Estado actual | Acción / observaciones |
|----|-------------|-----------------|---------------|------------------------|
| BUG-01 | `Crear` persona sin validar `Direccion` | Con errores | Pendiente | `dto.Direccion.Trim()` sin null check → posible 500. Alinear con `Editar`. |
| BUG-02 | Validación de teléfono inconsistente (Crear vs Editar) | Con errores | Pendiente | Unificar reglas entre operaciones. |
| BUG-03 | `colSpan` incorrecto en tabla vacía (frontend) | Con errores | Pendiente | `PersonasDashboard.jsx` L195: `colSpan="6"` pero hay 7 columnas. |
| BUG-04 | Paginación desincronizada al filtrar | Con errores | Pendiente | Resetear `paginaActual` a 1 al cambiar filtros. |
| BUG-05 | Race condition en búsqueda de personas | Con errores | Pendiente | Agregar `AbortController` como en `RoleManagementPage`. |
| BUG-06 | Cambio de rol accidental sin confirmación | Con errores | Pendiente | Confirmación o botón Guardar en `RoleManagementPage`. |
| BUG-07 | Favicon 404 | Con errores | Pendiente | `index.html` referencia `/favicon.svg` — verificar que exista en `public/`. |
| BUG-08 | Materias admin desconectadas del backend | Con errores | Pendiente | Conectar `MateriasDashboard` a `/api/Materias`. |
| BUG-09 | Errores silenciosos al cargar personas | Con errores | Pendiente | Mostrar `toastError` en lugar de solo `console.error`. |
| BUG-10 | Sin interceptor 401 en HTTP (frontend) | Con errores | Pendiente | Interceptor en `http.js`: clear session + redirect a `/login`. |

### Optimización y arquitectura

| ID | Descripción | Estado anterior | Estado actual | Acción / observaciones |
|----|-------------|-----------------|---------------|------------------------|
| OPT-01 | `PersonasController` monolítico (~670 líneas) | Implementado | Pendiente | Extraer `ServicioPersonas` + validador compartido. |
| OPT-02 | Validación de email duplicada (3 lugares) | Implementado | Pendiente | Centralizar en `ValidadorAutenticacion`. |
| OPT-03 | Consultas EF sin `AsNoTracking()` | Implementado | Pendiente | Añadir en lecturas de Alumnos, Profesores, Clases, Materias, Inscripciones. |
| OPT-04 | Includes excesivos en GET de materias | Implementado | Pendiente | Endpoints separados o proyecciones `Select`. |
| OPT-05 | Paginación: default 5 vs comentario "20" | Implementado | Pendiente | `Math.Clamp(limite, 1, 100)` y default coherente. |
| OPT-06 | Carga masiva client-side (limite 1000) | Implementado | Pendiente | Paginación server-side en roles y listados por rol. |
| OPT-07 | Código duplicado en frontend | Implementado | Pendiente | Extraer `format.js`, `AuthBrand.jsx`, `formHelpers.js`. |
| OPT-08 | Sin lazy loading de rutas | Implementado | Pendiente | `React.lazy` + `Suspense` para admin/auth. |
| OPT-09 | Campos deprecados en `Persona.cs` | Implementado | Pendiente | Limpiar `ValorClasePorHora`, etc. (marcados `//sacar`). |
| OPT-10 | Sin tests automatizados (ambos proyectos) | Pendiente | Pendiente | Vitest + Testing Library (FE); tests integración auth (BE). |

### Mejoras de UX y completitud

| ID | Descripción | Estado anterior | Estado actual | Acción / observaciones |
|----|-------------|-----------------|---------------|------------------------|
| UX-01 | Recuperación de contraseña sin envío real de email | Implementado | Pendiente | Integrar servicio de email; no exponer enlace en prod. |
| UX-02 | `PersonaForm` sin protección contra doble envío | Con errores | Pendiente | Estado `submitting` + deshabilitar botón. |
| UX-03 | Modal de detalle sin accesibilidad | Implementado | Pendiente | `role="dialog"`, Escape, trap de foco. |
| UX-04 | Confirmaciones inconsistentes | Implementado | Pendiente | Unificar con `confirmDialog` (SweetAlert2). |
| UX-05 | `PersonasDashboard` embebido dos veces para super_admin | Implementado | Pendiente | Índice con resumen/links; ABM solo en `/dashboard/personas`. |
| UX-06 | Sin manejo global de excepciones (backend) | Implementado | Pendiente | `UseExceptionHandler` + respuestas RFC 7807. |
| UX-07 | Código muerto en frontend | Implementado | Pendiente | Eliminar `DashboardPlaceholder`, `DashboardPage`, etc. |
| UX-08 | `index.html` con `lang="en"` | Implementado | Pendiente | Cambiar a `lang="es"`. |

---

## Detalle por proyecto

### EIMA-Backend — Hallazgos adicionales

#### Alta severidad
- **Registro público de profesores sin aprobación:** cuenta activa inmediatamente en `ServicioAutenticacion.cs`.
- **DNI almacenado en texto claro:** documentado como requisito funcional; evaluar cifrado en reposo.
- **JWT 24 h sin refresh token:** ventana larga si el token se filtra.
- **Recuperación de contraseña:** token generado pero email no enviado; enlace opcional en respuesta JSON.

#### Media severidad
- **Sin manejo global de excepciones** en `Program.cs`.
- **Inconsistencia `secretaria` vs `administrativo`** en mensajes y migraciones.
- **`EmpleadosController`** filtra por `TipoColaboradorId != null`, no por rol.
- **Materias inválidas silenciadas** al asignar a docente (`if (!materiaExiste) continue`).

#### Baja severidad / deuda técnica
- Seed de materias en cada arranque (`Program.cs`).
- Consultas públicas sin CAPTCHA ni rate limit.
- Posible `NullReference` en login si `Persona.Rol` es null.
- No hay `appsettings.example.json` ni README documentado.

#### Archivos clave
- `Controladores/PersonasController.cs`
- `Controladores/Autenticacion/ServicioAutenticacion.cs`
- `Controladores/Autenticacion/ServicioRecuperacionContrasena.cs`
- `Controladores/MateriasController.cs`
- `Entidades/Persona.cs`
- `Eima.API/Program.cs`

---

### EIMAFront — Hallazgos adicionales

#### Alta severidad
- Ver SEC-07, SEC-08, BUG-08, BUG-10 (arriba).

#### Media severidad
- **`PersonaForm`:** errores silenciosos al cargar materias (`.catch(() => {})`).
- **Modal `PersonaDetailModal`:** sin accesibilidad completa.
- **`.env` no está en `.gitignore`:** riesgo de commitear secretos.
- **Sin Error Boundary:** error no capturado puede dejar pantalla en blanco.
- **Ruta `/dashboard/materias` activa pero oculta en nav** y usa mock local.
- **`RecoverPasswordPage`:** en dev redirige con token en URL (historial del navegador).

#### Baja severidad
- Validaciones de teléfono incompletas en `RegisterPage` y `ContactoForm`.
- Estado `alert` muerto en `LoginPage`.
- Mezcla de estilo con/sin punto y coma.
- Sin proxy Vite al backend para DX.
- Welcome del dashboard solo muestra `nombre`, no apellido.

#### Archivos clave
- `src/lib/http.js`
- `src/lib/authStorage.js`
- `src/routes/RequireRole.jsx`
- `src/screens/admin/personas/PersonasDashboard.jsx`
- `src/screens/admin/personas/PersonaForm.jsx`
- `src/screens/admin/materias/MateriasDashboard.jsx`
- `src/screens/admin/RoleManagementPage.jsx`

---

## Matriz de priorización

```
INMEDIATO (Sprint actual)
├── SEC-01, SEC-02, SEC-03, SEC-04
├── BUG-01, BUG-08
└── Autorizar los 19 endpoints listados

CORTO PLAZO (próximo sprint)
├── SEC-05, SEC-06, SEC-07, SEC-08
├── BUG-03 a BUG-10
└── UX-01

MEDIO PLAZO
├── OPT-01 a OPT-05
├── UX-02 a UX-06
└── SEC-09

LARGO PLAZO
├── OPT-06 a OPT-10
└── UX-07, UX-08
```

---

## Referencia rápida — Roles del sistema

```csharp
// Entidades/RolesSistema.cs
super_admin    // Super administrador
administrativo // Colaborador administrativo (ex secretaria)
profesor       // Docente
alumno         // Alumno
```

---

*Documento generado a partir de revisión de código informativa. No implica cambios aplicados en el repositorio.*
