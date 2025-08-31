using Xunit;
using Microsoft.JSInterop;
using Frontend.Services;

namespace Frontend.Tests.Security;

public class CryptoServiceTests
{
    // Datos de prueba fijos - deben coincidir con los del backend
    private const string FixedTestText = "Hola desde el Frontend!";
    private const string FixedEncryptedValue = "4PlIK33ouFrs8MXw6ZB0XZa776cJ0o6ylYAKSoonpPe7V21d7HDGNMnj0saKS9bPFJjDOqhBJrL/zP/NzeBcmbiWiQmgSADW2kkPIIhLR48=";

    [Fact]
    public async Task DecryptAsync_WithFixedValue_ShouldReturnOriginalText()
    {
        // Arrange
        var mockJSRuntime = new MockJSRuntime();
        var cryptoService = new CryptoService(mockJSRuntime);

        // Act
        var decrypted = await cryptoService.DecryptAsync(FixedEncryptedValue);

        // Assert
        Assert.Equal(FixedTestText, decrypted);
    }

    [Fact]
    public async Task DecryptAsync_WithNullOrEmpty_ShouldReturnSameValue()
    {
        // Arrange
        var mockJSRuntime = new MockJSRuntime();
        var cryptoService = new CryptoService(mockJSRuntime);

        // Act & Assert
        Assert.Null(await cryptoService.DecryptAsync(null));
        Assert.Equal("", await cryptoService.DecryptAsync(""));
    }

    [Fact]
    public async Task EncryptAsync_WithNullOrEmpty_ShouldReturnSameValue()
    {
        // Arrange
        var mockJSRuntime = new MockJSRuntime();
        var cryptoService = new CryptoService(mockJSRuntime);

        // Act & Assert
        Assert.Null(await cryptoService.EncryptAsync(null));
        Assert.Equal("", await cryptoService.EncryptAsync(""));
    }

    [Fact]
    public async Task TestAsync_ShouldReturnTrue()
    {
        // Arrange
        var mockJSRuntime = new MockJSRuntime();
        var cryptoService = new CryptoService(mockJSRuntime);

        // Act
        var result = await cryptoService.TestAsync("Test text");

        // Assert
        Assert.True(result);
    }
}

// Mock del JSRuntime para testing
public class MockJSRuntime : IJSRuntime
{
    // Datos de prueba fijos - deben coincidir con los del backend
    private const string FixedTestText = "Hola desde el Frontend!";
    private const string FixedEncryptedValue = "4PlIK33ouFrs8MXw6ZB0XZa776cJ0o6ylYAKSoonpPe7V21d7HDGNMnj0saKS9bPFJjDOqhBJrL/zP/NzeBcmbiWiQmgSADW2kkPIIhLR48=";

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        // Simulaciones para nuestros tests de crypto
        switch (identifier)
        {
            case "eval":
                // Simular inicialización del script crypto
                return new ValueTask<TValue>(default(TValue));
                
            case "unifiedCrypto.decrypt":
                if (args != null && args.Length > 0)
                {
                    if (args[0]?.ToString() == FixedEncryptedValue)
                    {
                        return new ValueTask<TValue>((TValue)(object)FixedTestText);
                    }
                    if (args[0]?.ToString()?.StartsWith("encrypted-") == true)
                    {
                        var original = args[0].ToString().Replace("encrypted-", "");
                        return new ValueTask<TValue>((TValue)(object)original);
                    }
                }
                break;

            case "unifiedCrypto.encrypt":
                if (args != null && args.Length > 0)
                {
                    var text = args[0]?.ToString();
                    return new ValueTask<TValue>((TValue)(object)$"encrypted-{text}");
                }
                break;

            case "unifiedCrypto.test":
                return new ValueTask<TValue>((TValue)(object)true);
        }

        return new ValueTask<TValue>(default(TValue));
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        return InvokeAsync<TValue>(identifier, args);
    }
}