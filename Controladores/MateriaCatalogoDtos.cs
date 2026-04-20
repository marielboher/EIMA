namespace Controladores;

public sealed record MateriaCatalogoItemDto(int Id, string Nombre);

public sealed record MateriaCatalogoAreaDto(string Area, IReadOnlyList<MateriaCatalogoItemDto> Materias);
