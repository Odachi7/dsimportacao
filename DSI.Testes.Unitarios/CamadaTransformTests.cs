using DSI.Dominio.Entidades;
using DSI.Dominio.Enums;
using DSI.Motor;
using DSI.Motor.ETL;
using DSI.Motor.Modelos;
using Moq; // Vamos precisar do Moq ou criar fakes manuais se não tiver
using System.Data;
using Xunit;

namespace DSI.Testes.Unitarios;

public class CamadaTransformTests
{
    [Fact]
    public async Task TransformarLote_DeveMapearColunasCorretamente()
    {
        // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var transformador = new CamadaTransform(serviceProviderMock.Object);

        // Configurar TabelaJob com Mapeamentos
        var tabelaJob = new TabelaJob
        {
            TabelaOrigem = "ClientesOrigem",
            TabelaDestino = "ClientesDestino",
            Mapeamentos = new List<Mapeamento>
            {
                new Mapeamento { ColunaOrigem = "Nome", ColunaDestino = "NomeCompleto" },
                new Mapeamento { ColunaOrigem = "Idade", ColunaDestino = "IdadeAnos" }
            }
        };

        // Dados de entrada
        var linha1 = new Dictionary<string, object?> { { "Nome", "João" }, { "Idade", 30 } };
        var loteEntrada = new LoteDados
        {
            NumeroLote = 1,
            Linhas = new List<Dictionary<string, object?>> { linha1 }
        };

        // Contexto fake (mínimo necessário)
        // Precisamos mockar dependências do contexto se ele validar algo no construtor
        // O construtor do ContextoExecucao é cheio de dependencias. 
        // Talvez seja melhor criar um Mock do Contexto se possivel, ou criar um contexto real com mocks.
        
        // Simplificação: Como ContextoExecucao é concreto e complexo, vamos instanciar com nulls onde der (cuidado com NullRef) ou Mocks.
        var execucao = new Execucao();
        var job = new Job();
        var mockConn = new Mock<IDbConnection>();
        var mockConector = new Mock<DSI.Conectores.Abstracoes.Interfaces.IConector>();
        
        var contexto = new ContextoExecucao(execucao, job, mockConn.Object, mockConn.Object, mockConector.Object, mockConector.Object);

        // Act
        var resultado = await transformador.TransformarLoteAsync(contexto, loteEntrada, tabelaJob);

        // Assert
        Assert.Single(resultado.LinhasSucesso);
        var linhaSaida = resultado.LinhasSucesso[0];
        
        Assert.True(linhaSaida.ContainsKey("NomeCompleto"));
        Assert.Equal("João", linhaSaida["NomeCompleto"]);
        
        Assert.True(linhaSaida.ContainsKey("IdadeAnos"));
        Assert.Equal(30, linhaSaida["IdadeAnos"]);
        
        // Verifica se colunas não mapeadas foram ignoradas (comportamento padrão esperado se não houver "*" mapeado)
        Assert.False(linhaSaida.ContainsKey("Nome")); 
    }

    [Fact]
    public async Task TransformarLote_DeveIgnorarLinhaSeRegraPular()
    {
         // Arrange
        var serviceProviderMock = new Mock<IServiceProvider>();
        var transformador = new CamadaTransform(serviceProviderMock.Object);

        // Tabela com regra de Pular se Nulo
        var tabelaJob = new TabelaJob
        {
            TabelaOrigem = "Origem",
            TabelaDestino = "Destino",
            Mapeamentos = new List<Mapeamento>
            {
                new Mapeamento 
                { 
                    ColunaOrigem = "Valor", 
                    ColunaDestino = "Valor",
                    Regras = new List<Regra>
                    {
                        new Regra 
                        { 
                            TipoRegra = TipoRegra.Obrigatorio,
                            AcaoQuandoFalhar = AcaoFalhaRegra.PularLinha
                        }
                    }
                }
            }
        };

        var linhaValida = new Dictionary<string, object?> { { "Valor", "Ok" } };
        var linhaInvalida = new Dictionary<string, object?> { { "Valor", null } };

        var loteEntrada = new LoteDados
        {
            NumeroLote = 1,
            Linhas = new List<Dictionary<string, object?>> { linhaValida, linhaInvalida }
        };

        var execucao = new Execucao();
        var job = new Job();
        var mockConn = new Mock<IDbConnection>();
        var mockConector = new Mock<DSI.Conectores.Abstracoes.Interfaces.IConector>();
        var contexto = new ContextoExecucao(execucao, job, mockConn.Object, mockConn.Object, mockConector.Object, mockConector.Object);

        // Act
        var resultado = await transformador.TransformarLoteAsync(contexto, loteEntrada, tabelaJob);

        // Assert
        Assert.Single(resultado.LinhasSucesso);
        Assert.Equal("Ok", resultado.LinhasSucesso[0]["Valor"]);
        // Linha com erro/pulo não vai para sucesso nem erro (se for PularLinha, ela apenas some ou vai pra log?)
        // Revisando CamadaTransform: "if (pularLinha) return null;" -> Se retorna null, não add em LinhasSucesso nem LinhasErro.
        Assert.Empty(resultado.LinhasErro);
    }
}
