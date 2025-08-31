using Xunit;
using Shared.Models.Security;

namespace Backend.Tests.Security;

public class UnifiedEncryptionTests
{
    // Datos de prueba fijos - deben coincidir con los del frontend
    private const string FixedTestText = "Hola desde el Frontend!";
    private const string FixedEncryptedValue = "4PlIK33ouFrs8MXw6ZB0XZa776cJ0o6ylYAKSoonpPe7V21d7HDGNMnj0saKS9bPFJjDOqhBJrL/zP/NzeBcmbiWiQmgSADW2kkPIIhLR48=";

    [Fact]
    public void DecryptAesCbc_WithFixedValue_ShouldReturnOriginalText()
    {
        // Act
        var decrypted = UnifiedEncryption.DecryptAesCbc(FixedEncryptedValue);

        // Assert
        Assert.Equal(FixedTestText, decrypted);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ShouldWorkCorrectly()
    {
        // Arrange
        const string originalText = "Test de encriptación round-trip";

        // Act
        var encrypted = UnifiedEncryption.EncryptAesCbc(originalText);
        var decrypted = UnifiedEncryption.DecryptAesCbc(encrypted);

        // Assert
        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void EncryptAesCbc_SameText_ShouldProduceDifferentResults()
    {
        // Arrange
        const string text = "Mismo texto";

        // Act
        var encrypted1 = UnifiedEncryption.EncryptAesCbc(text);
        var encrypted2 = UnifiedEncryption.EncryptAesCbc(text);

        // Assert - Los IVs aleatorios deben hacer que los resultados sean diferentes
        Assert.NotEqual(encrypted1, encrypted2);
        
        // Pero ambos deben desencriptar al mismo texto
        Assert.Equal(text, UnifiedEncryption.DecryptAesCbc(encrypted1));
        Assert.Equal(text, UnifiedEncryption.DecryptAesCbc(encrypted2));
    }

    [Fact]
    public void DecryptAesCbc_WithNullOrEmpty_ShouldReturnSameValue()
    {
        // Act & Assert
        Assert.Null(UnifiedEncryption.DecryptAesCbc(null));
        Assert.Equal("", UnifiedEncryption.DecryptAesCbc(""));
    }

    [Fact]
    public void EncryptAesCbc_WithNullOrEmpty_ShouldReturnSameValue()
    {
        // Act & Assert
        Assert.Null(UnifiedEncryption.EncryptAesCbc(null));
        Assert.Equal("", UnifiedEncryption.EncryptAesCbc(""));
    }

    [Fact]
    public void DecryptAesCbc_WithInvalidData_ShouldThrowException()
    {
        // Arrange
        const string invalidEncryptedData = "InvalidBase64Data";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            UnifiedEncryption.DecryptAesCbc(invalidEncryptedData));
    }

    [Theory]
    [InlineData("Texto simple")]
    [InlineData("Texto con símbolos !@#$%^&*()")]
    [InlineData("Texto con acentos: ñáéíóú")]
    [InlineData("Texto muy largo que contiene múltiples palabras y caracteres especiales para probar que la encriptación funciona correctamente con diferentes tipos de contenido")]
    [InlineData("123456789")]
    [InlineData("")]
    public void EncryptDecrypt_VariousTexts_ShouldWorkCorrectly(string originalText)
    {
        // Act
        var encrypted = UnifiedEncryption.EncryptAesCbc(originalText);
        var decrypted = UnifiedEncryption.DecryptAesCbc(encrypted);

        // Assert
        Assert.Equal(originalText, decrypted);
    }
}