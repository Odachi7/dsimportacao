using DSI.Logging.Enums;
using DSI.Logging.Interfaces;
using System.Collections.Concurrent;

namespace DSI.Logging.Implementacoes;

/// <summary>
/// Implementação do log amigável com buffer em memória para UI
/// </summary>
public class LogAmigavel : ILogAmigavel
{
    private readonly ConcurrentQueue<MensagemLog> _buffer = new();
    private readonly int _tamanhoMaximoBuffer = 1000;

    public void Informar(string mensagem)
    {
        AdicionarAoBuffer(new MensagemLog
        {
            DataHora = DateTime.Now,
            Nivel = NivelLog.Informacao,
            Mensagem = mensagem
        });
    }

    public void Avisar(string mensagem)
    {
      AdicionarAoBuffer(new MensagemLog
        {
            DataHora = DateTime.Now,
            Nivel = NivelLog.Aviso,
            Mensagem = $"⚠ {mensagem}"
        });
    }

    public void Erro(string mensagem)
    {
        AdicionarAoBuffer(new MensagemLog
        {
            DataHora = DateTime.Now,
            Nivel = NivelLog.Erro,
            Mensagem = $"✗ {mensagem}"
        });
    }

    public IEnumerable<MensagemLog> ObterUltimasMensagens(int quantidade = 100)
    {
        return _buffer.TakeLast(quantidade).ToList();
    }

    public void LimparBuffer()
    {
        _buffer.Clear();
    }

    private void AdicionarAoBuffer(MensagemLog mensagem)
    {
        _buffer.Enqueue(mensagem);

        // Limita o tamanho do buffer
        while (_buffer.Count > _tamanhoMaximoBuffer)
        {
            _buffer.TryDequeue(out _);
        }
    }
}
