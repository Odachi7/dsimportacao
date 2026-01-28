using System.Security.Cryptography;
using System.Text;

namespace DSI.Seguranca.Criptografia;

/// <summary>
/// Serviço de criptografia usando DPAPI (Data Protection API) do Windows
/// </summary>
public class ServicoCriptografia
{
    /// <summary>
    /// Criptografa uma string usando DPAPI vinculado ao usuário atual do Windows
    /// </summary>
    public string Criptografar(string textoPlano)
    {
        if (string.IsNullOrEmpty(textoPlano))
            return string.Empty;

        try
        {
            var bytesTexto = Encoding.UTF8.GetBytes(textoPlano);
            var bytesCriptografados = ProtectedData.Protect(
                bytesTexto,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser
            );

            return Convert.ToBase64String(bytesCriptografados);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Erro ao criptografar dados", ex);
        }
    }

    /// <summary>
    /// Descriptografa uma string previamente criptografada com DPAPI
    /// </summary>
    public string Descriptografar(string textoCriptografado)
    {
        if (string.IsNullOrEmpty(textoCriptografado))
            return string.Empty;

        try
        {
            var bytesCriptografados = Convert.FromBase64String(textoCriptografado);
            var bytesDescriptografados = ProtectedData.Unprotect(
                bytesCriptografados,
                optionalEntropy: null,
                scope: DataProtectionScope.CurrentUser
            );

            return Encoding.UTF8.GetString(bytesDescriptografados);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Erro ao descriptografar dados. Os dados podem ter sido criptografados por outro usuário Windows.", ex);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Formato de dados criptografados inválido", ex);
        }
    }

    /// <summary>
    /// Verifica se uma string está cifrada (formato Base64 válido)
    /// </summary>
    public bool EstaCriptografado(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return false;

        try
        {
            Convert.FromBase64String(texto);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
