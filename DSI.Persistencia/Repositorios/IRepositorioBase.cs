using DSI.Dominio.Entidades;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Repositório base com operações CRUD comuns
/// </summary>
public interface IRepositorioBase<T> where T : class
{
    /// <summary>
    /// Obtém todos os registros
    /// </summary>
    Task<IEnumerable<T>> ObterTodosAsync();

    /// <summary>
    /// Obtém um registro por ID
    /// </summary>
    Task<T?> ObterPorIdAsync(Guid id);

    /// <summary>
    /// Adiciona um novo registro
    /// </summary>
    Task<T> AdicionarAsync(T entidade);

    /// <summary>
    /// Atualiza um registro existente
    /// </summary>
    Task AtualizarAsync(T entidade);

    /// <summary>
    /// Remove um registro
    /// </summary>
    Task RemoverAsync(Guid id);

    /// <summary>
    /// Salva as alterações no banco de dados
    /// </summary>
    Task<int> SalvarAsync();
}
