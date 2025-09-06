using Microsoft.JSInterop;
using Shared.Models.Security;

namespace Frontend.Services;

/// <summary>
/// Service for encryption/decryption operations in the frontend using JavaScript SubtleCrypto API
/// Compatible with backend UnifiedEncryption class
/// </summary>
public class CryptoService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isInitialized = false;

    public CryptoService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initializes the JavaScript crypto functions if not already done
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized)
            return;

        var cryptoScript = GetEmbeddedCryptoScript();
        await _jsRuntime.InvokeVoidAsync("eval", cryptoScript);
        _isInitialized = true;
    }

    /// <summary>
    /// Gets the embedded JavaScript crypto implementation
    /// </summary>
    private string GetEmbeddedCryptoScript()
    {
        return UnifiedEncryption.GetJavaScriptEncryptionCode() + @"
        
        // Add test function
        window.unifiedCrypto.test = async function(testText) {
            try {
                const text = testText || 'Hello World Test';
                const encrypted = await this.encrypt(text);
                const decrypted = await this.decrypt(encrypted);
                return decrypted === text;
            } catch (error) {
                console.error('Test failed:', error);
                return false;
            }
        };";
    }

    /// <summary>
    /// Encrypts plaintext using AES-GCM via JavaScript SubtleCrypto API
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <returns>Base64 encoded encrypted data compatible with backend</returns>
    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            await EnsureInitializedAsync();
            return await _jsRuntime.InvokeAsync<string>("unifiedCrypto.encrypt", plainText);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException($"Frontend encryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypts encrypted data using AES-GCM via JavaScript SubtleCrypto API
    /// </summary>
    /// <param name="encryptedData">Base64 encoded encrypted data</param>
    /// <returns>Decrypted plaintext</returns>
    public async Task<string> DecryptAsync(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return encryptedData;

        try
        {
            await EnsureInitializedAsync();
            return await _jsRuntime.InvokeAsync<string>("unifiedCrypto.decrypt", encryptedData);
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException($"Frontend decryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Tests the encryption/decryption functionality
    /// </summary>
    /// <param name="testText">Optional test text</param>
    /// <returns>True if test passed</returns>
    public async Task<bool> TestAsync(string testText = "Hello World Test")
    {
        try
        {
            await EnsureInitializedAsync();
            return await _jsRuntime.InvokeAsync<bool>("unifiedCrypto.test", testText);
        }
        catch (JSException ex)
        {
            return false;
        }
    }
}