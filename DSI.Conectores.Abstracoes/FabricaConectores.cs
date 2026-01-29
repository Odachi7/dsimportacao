using DSI.Conectores.Abstracoes.Interfaces;
using DSI.Dominio.Enums;

namespace DSI.Conectores.Abstracoes;

/// <summary>
/// Factory para criar instâncias de conectores de banco de dados
/// </summary>
public class FabricaConectores
{
    private readonly Dictionary<TipoBancoDados, Func<IConector>> _fabricas;

    public FabricaConectores()
    {
        _fabricas = new Dictionary<TipoBancoDados, Func<IConector>>();
    }

    /// <summary>
    /// Registra uma factory para um tipo de banco de dados
    /// </summary>
    /// <param name="tipo">Tipo de banco de dados</param>
    /// <param name="factory">Função factory que cria uma instância do conector</param>
    public void Registrar(TipoBancoDados tipo, Func<IConector> factory)
    {
        _fabricas[tipo] = factory;
    }

    /// <summary>
    /// Registra um conector usando instanciação direta do tipo
    /// </summary>
    /// <typeparam name="T">Tipo do conector</typeparam>
    /// <param name="tipo">Tipo de banco de dados</param>
    public void Registrar<T>(TipoBancoDados tipo) where T : IConector, new()
    {
        _fabricas[tipo] = () => new T();
    }

    /// <summary>
    /// Obtém uma instância de conector para o tipo de banco especificado
    /// </summary>
    /// <param name="tipo">Tipo de banco de dados</param>
    /// <returns>Instância do conector</returns>
    /// <exception cref="ArgumentException">Quando o tipo não está registrado</exception>
    public IConector ObterConector(TipoBancoDados tipo)
    {
        if (!_fabricas.ContainsKey(tipo))
        {
            throw new ArgumentException($"Nenhum conector registrado para o tipo de banco: {tipo}", nameof(tipo));
        }

        return _fabricas[tipo]();
    }

    /// <summary>
    /// Verifica se existe um conector registrado para o tipo especificado
    /// </summary>
    /// <param name="tipo">Tipo de banco de dados</param>
    /// <returns>True se existe conector registrado</returns>
    public bool TemConector(TipoBancoDados tipo)
    {
        return _fabricas.ContainsKey(tipo);
    }

    /// <summary>
    /// Obtém todos os tipos de banco de dados com conectores registrados
    /// </summary>
    /// <returns>Lista de tipos de banco suportados</returns>
    public IEnumerable<TipoBancoDados> ObterTiposSuportados()
    {
        return _fabricas.Keys;
    }

    /// <summary>
    /// Remove o registro de um tipo de banco de dados
    /// </summary>
    /// <param name="tipo">Tipo de banco de dados</param>
    /// <returns>True se foi removido, False se não existia</returns>
    public bool Remover(TipoBancoDados tipo)
    {
        return _fabricas.Remove(tipo);
    }

    /// <summary>
    /// Remove todos os registros de conectores
    /// </summary>
    public void LimparRegistros()
    {
        _fabricas.Clear();
    }

    /// <summary>
    /// Tenta obter um conector, retornando null se não estiver registrado
    /// </summary>
    /// <param name="tipo">Tipo de banco de dados</param>
    /// <returns>Instância do conector ou null</returns>
    public IConector? TentarObterConector(TipoBancoDados tipo)
    {
        if (_fabricas.TryGetValue(tipo, out var factory))
        {
            return factory();
        }

        return null;
    }
}
