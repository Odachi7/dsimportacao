using DSI.Dominio.Enums;
using DSI.Motor.Regras.Implementacoes;
using Xunit;

namespace DSI.Testes.Unitarios;

public class RegrasTests
{
    [Fact]
    public async Task RegraUpper_DeveConverterParaMaiusculo()
    {
        // Arrange
        var regra = new RegraUpper();
        var valor = "teste";

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal("TESTE", resultado.ValorTransformado);
    }

    [Fact]
    public async Task RegraLower_DeveConverterParaMinusculo()
    {
        // Arrange
        var regra = new RegraLower();
        var valor = "TESTE";

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal("teste", resultado.ValorTransformado);
    }

    [Fact]
    public async Task RegraTrim_DeveRemoverEspacos()
    {
        // Arrange
        var regra = new RegraTrim();
        var valor = "  teste  ";

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal("teste", resultado.ValorTransformado);
    }

    [Fact]
    public async Task RegraDefaultSeNulo_DeveUsarPadraoSeNulo()
    {
        // Arrange
        var regra = new RegraDefaultSeNulo();
        string? valor = null;
        var padrao = "PADRAO";

        // Act
        var resultado = await regra.AplicarAsync(valor, padrao, null!);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal("PADRAO", resultado.ValorTransformado);
    }

    [Fact]
    public async Task RegraDefaultSeNulo_NaoDeveAlterarSeNaoNulo()
    {
        // Arrange
        var regra = new RegraDefaultSeNulo();
        var valor = "VALOR";
        var padrao = "PADRAO";

        // Act
        var resultado = await regra.AplicarAsync(valor, padrao, null!);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal("VALOR", resultado.ValorTransformado);
    }

    [Fact]
    public async Task RegraToInt_DeveConverterStringNumerica()
    {
        // Arrange
        var regra = new RegraToInt();
        var valor = "123";

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal(123, resultado.ValorTransformado);
    }

    [Fact]
    public async Task RegraToInt_DeveFalharComStringInvalida()
    {
        // Arrange
        var regra = new RegraToInt();
        var valor = "abc";

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Contains("Não foi possível converter", resultado.MensagemErro);
    }

    [Fact]
    public async Task RegraObrigatorio_DeveFalharComNulo()
    {
        // Arrange
        var regra = new RegraObrigatorio();
        string? valor = null;

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public async Task RegraParseData_DeveConverterFormatoPadrao()
    {
        // Arrange
        var regra = new RegraParseData();
        var valor = "25/12/2023";

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.IsType<DateTime>(resultado.ValorTransformado);
        Assert.Equal(new DateTime(2023, 12, 25), resultado.ValorTransformado);
    }

    [Fact]
    public async Task RegraParseData_DeveFalharComFormatoInvalido()
    {
        // Arrange
        var regra = new RegraParseData();
        var valor = "2023.12.25"; // Formato não padrão (assumindo que não está na lista default)

        // Act
        var resultado = await regra.AplicarAsync(valor, null, null!);

        // Assert
        Assert.False(resultado.Sucesso);
    }
}
