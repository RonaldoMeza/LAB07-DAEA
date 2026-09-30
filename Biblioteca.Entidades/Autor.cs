namespace Biblioteca.Entidades;

// Capa de Entidades: solo propiedades. No sabe nada de SQL, de reglas ni de pantallas.
public class Autor
{
    public int AutorId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Nacionalidad { get; set; }
    public bool Activo { get; set; } = true;
}
