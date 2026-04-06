namespace Controladores.Autenticacion;

public sealed record ErrorCampo(string Campo, string Mensaje);

public sealed class ResultadoValidacion
{
    public List<ErrorCampo> Errores { get; } = new();

    public bool EsValido => Errores.Count == 0;

    public void Agregar(string campo, string mensaje) => Errores.Add(new ErrorCampo(campo, mensaje));
}
