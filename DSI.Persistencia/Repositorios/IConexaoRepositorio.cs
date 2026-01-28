using DSI.Dominio.Entidades;

namespace DSI.Persistencia.Repositorios;

/// <summary>
/// Repositório específico para Conexões
/// </summary>
public interface IConexaoRepositorio : IRepositorioBase<Conexao>
{
    /// <summary>
    /// Obtém conexões por tipo de banco
    /// </summary>
    Task<IEnumerable<Conexao>> ObterPorTipoBancoAsync(DSI.Dominio.Enums.TipoBancoDados tipoBanco);

    /// <summary>
    /// Obtém conexão por nome
    /// </summary>
    Task<Conexao?> ObterPorNomeAsync(string nome);

    /// <summary>
    /// Verifica se existe uma conexão com o nome especificado
    /// </summary>
    Task<bool> ExistePorNomeAsync(string nome);
}
