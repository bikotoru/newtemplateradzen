using System.Security.Cryptography;
using System.Text;

namespace Shared.Models.Security;

/// <summary>
/// Unified encryption service that provides AES-CBC+HMAC encryption compatible with both
/// browser SubtleCrypto API and .NET System.Security.Cryptography
/// </summary>
public static class UnifiedEncryption
{
    /// <summary>
    /// Shared encryption key (32 bytes for AES-256) - MUST match backend Variables.Auth.EncryptionKey
    /// </summary>
    public const string SharedKey = "SZG4!CdV$6@FJnwKpS6^P5Vnu7@J8#JUJKtL9EQDn!QuMWw@ir^kCmXxuBLuxa4%pLg8VcVVi9uiDYKLBVpZn@F5&@mKwzsHgjN3XGzuwqhStv&H9YSSQuSD";
    
    /// <summary>
    /// Key size in bytes for AES-256
    /// </summary>
    public const int KeySize = 32;
    
    /// <summary>
    /// IV size in bytes for AES-CBC
    /// </summary>
    public const int IvSize = 16;
    
    /// <summary>
    /// HMAC size in bytes for SHA-256
    /// </summary>
    public const int HmacSize = 32;

    /// <summary>
    /// Encrypts data using AES-CBC + HMAC-SHA256 (backend implementation)
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <returns>Encrypted data in format: IV + HMAC + Ciphertext (Base64 encoded)</returns>
    public static string EncryptAesCbc(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            var keyBytes = GetKeyBytes();
            var encryptionKey = new byte[32];
            var hmacKey = new byte[32];
            
            // Derive encryption and HMAC keys from main key
            Array.Copy(keyBytes, 0, encryptionKey, 0, 32);
            using var sha = SHA256.Create();
            hmacKey = sha.ComputeHash(keyBytes);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var iv = new byte[IvSize];
            RandomNumberGenerator.Fill(iv);

            byte[] cipherBytes;
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;

                using var encryptor = aes.CreateEncryptor();
                cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            // Calculate HMAC over IV + Ciphertext
            byte[] hmac;
            using (var hmacSha = new HMACSHA256(hmacKey))
            {
                var dataToAuth = new byte[iv.Length + cipherBytes.Length];
                Buffer.BlockCopy(iv, 0, dataToAuth, 0, iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, dataToAuth, iv.Length, cipherBytes.Length);
                hmac = hmacSha.ComputeHash(dataToAuth);
            }

            // Combine IV + HMAC + Ciphertext
            var result = new byte[IvSize + HmacSize + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, IvSize);
            Buffer.BlockCopy(hmac, 0, result, IvSize, HmacSize);
            Buffer.BlockCopy(cipherBytes, 0, result, IvSize + HmacSize, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypts data using AES-CBC + HMAC-SHA256 (backend implementation)
    /// </summary>
    /// <param name="encryptedData">Encrypted data in format: IV + HMAC + Ciphertext (Base64 encoded)</param>
    /// <returns>Decrypted plain text</returns>
    public static string DecryptAesCbc(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return encryptedData;

        try
        {
            var keyBytes = GetKeyBytes();
            var encryptionKey = new byte[32];
            var hmacKey = new byte[32];
            
            // Derive encryption and HMAC keys from main key
            Array.Copy(keyBytes, 0, encryptionKey, 0, 32);
            using var sha = SHA256.Create();
            hmacKey = sha.ComputeHash(keyBytes);

            var data = Convert.FromBase64String(encryptedData);

            if (data.Length < IvSize + HmacSize)
                throw new ArgumentException("Invalid encrypted data format");

            // Extract components
            var iv = new byte[IvSize];
            var receivedHmac = new byte[HmacSize];
            var cipherBytes = new byte[data.Length - IvSize - HmacSize];

            Buffer.BlockCopy(data, 0, iv, 0, IvSize);
            Buffer.BlockCopy(data, IvSize, receivedHmac, 0, HmacSize);
            Buffer.BlockCopy(data, IvSize + HmacSize, cipherBytes, 0, cipherBytes.Length);

            // Verify HMAC
            byte[] computedHmac;
            using (var hmacSha = new HMACSHA256(hmacKey))
            {
                var dataToAuth = new byte[iv.Length + cipherBytes.Length];
                Buffer.BlockCopy(iv, 0, dataToAuth, 0, iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, dataToAuth, iv.Length, cipherBytes.Length);
                computedHmac = hmacSha.ComputeHash(dataToAuth);
            }

            // Constant-time comparison to prevent timing attacks
            if (!CryptographicOperations.FixedTimeEquals(receivedHmac, computedHmac))
                throw new CryptographicException("HMAC verification failed");

            // Decrypt
            byte[] plainBytes;
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Encrypts data using AES-CBC + HMAC-SHA256 with custom key
    /// </summary>
    /// <param name="plainText">Text to encrypt</param>
    /// <param name="customKey">Custom encryption key (will be used instead of SharedKey)</param>
    /// <returns>Encrypted data in format: IV + HMAC + Ciphertext (Base64 encoded)</returns>
    public static string EncryptAesCbcWithKey(string plainText, string customKey)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            var keyBytes = GetKeyBytes(customKey);
            var encryptionKey = new byte[32];
            var hmacKey = new byte[32];
            
            // Derive encryption and HMAC keys from custom key
            Array.Copy(keyBytes, 0, encryptionKey, 0, 32);
            using var sha = SHA256.Create();
            hmacKey = sha.ComputeHash(keyBytes);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var iv = new byte[IvSize];
            RandomNumberGenerator.Fill(iv);

            byte[] cipherBytes;
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;

                using var encryptor = aes.CreateEncryptor();
                cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
            }

            // Calculate HMAC over IV + Ciphertext
            byte[] hmac;
            using (var hmacSha = new HMACSHA256(hmacKey))
            {
                var dataToAuth = new byte[iv.Length + cipherBytes.Length];
                Buffer.BlockCopy(iv, 0, dataToAuth, 0, iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, dataToAuth, iv.Length, cipherBytes.Length);
                hmac = hmacSha.ComputeHash(dataToAuth);
            }

            // Combine IV + HMAC + Ciphertext
            var result = new byte[IvSize + HmacSize + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, IvSize);
            Buffer.BlockCopy(hmac, 0, result, IvSize, HmacSize);
            Buffer.BlockCopy(cipherBytes, 0, result, IvSize + HmacSize, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Encryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypts data using AES-CBC + HMAC-SHA256 with custom key
    /// </summary>
    /// <param name="encryptedData">Encrypted data in format: IV + HMAC + Ciphertext (Base64 encoded)</param>
    /// <param name="customKey">Custom encryption key (must match the one used for encryption)</param>
    /// <returns>Decrypted plain text</returns>
    public static string DecryptAesCbcWithKey(string encryptedData, string customKey)
    {
        if (string.IsNullOrEmpty(encryptedData))
            return encryptedData;

        try
        {
            var keyBytes = GetKeyBytes(customKey);
            var encryptionKey = new byte[32];
            var hmacKey = new byte[32];
            
            // Derive encryption and HMAC keys from custom key
            Array.Copy(keyBytes, 0, encryptionKey, 0, 32);
            using var sha = SHA256.Create();
            hmacKey = sha.ComputeHash(keyBytes);

            var data = Convert.FromBase64String(encryptedData);

            if (data.Length < IvSize + HmacSize)
                throw new ArgumentException("Invalid encrypted data format");

            // Extract components
            var iv = new byte[IvSize];
            var receivedHmac = new byte[HmacSize];
            var cipherBytes = new byte[data.Length - IvSize - HmacSize];

            Buffer.BlockCopy(data, 0, iv, 0, IvSize);
            Buffer.BlockCopy(data, IvSize, receivedHmac, 0, HmacSize);
            Buffer.BlockCopy(data, IvSize + HmacSize, cipherBytes, 0, cipherBytes.Length);

            // Verify HMAC
            byte[] computedHmac;
            using (var hmacSha = new HMACSHA256(hmacKey))
            {
                var dataToAuth = new byte[iv.Length + cipherBytes.Length];
                Buffer.BlockCopy(iv, 0, dataToAuth, 0, iv.Length);
                Buffer.BlockCopy(cipherBytes, 0, dataToAuth, iv.Length, cipherBytes.Length);
                computedHmac = hmacSha.ComputeHash(dataToAuth);
            }

            // Constant-time comparison to prevent timing attacks
            if (!CryptographicOperations.FixedTimeEquals(receivedHmac, computedHmac))
                throw new CryptographicException("HMAC verification failed");

            // Decrypt
            byte[] plainBytes;
            using (var aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = encryptionKey;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Decryption failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the encryption key as byte array (32 bytes for AES-256)
    /// </summary>
    private static byte[] GetKeyBytes()
    {
        return GetKeyBytes(SharedKey);
    }

    /// <summary>
    /// Gets the encryption key as byte array from custom key (32 bytes for AES-256)
    /// </summary>
    private static byte[] GetKeyBytes(string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        
        // Ensure exactly 32 bytes for AES-256
        if (keyBytes.Length > KeySize)
        {
            Array.Resize(ref keyBytes, KeySize);
        }
        else if (keyBytes.Length < KeySize)
        {
            var paddedKey = new byte[KeySize];
            Buffer.BlockCopy(keyBytes, 0, paddedKey, 0, keyBytes.Length);
            // Fill remaining bytes with the key repeated
            for (int i = keyBytes.Length; i < KeySize; i++)
            {
                paddedKey[i] = keyBytes[i % keyBytes.Length];
            }
            keyBytes = paddedKey;
        }

        return keyBytes;
    }

    /// <summary>
    /// JavaScript code for SubtleCrypto encryption (frontend)
    /// This returns the JavaScript function that can be used in Blazor
    /// </summary>
    public static string GetJavaScriptEncryptionCode()
    {
        return @"
window.unifiedCrypto = {
    sharedKey: '" + SharedKey + @"',
    
    async getKeys() {
        const keyBytes = new TextEncoder().encode(this.sharedKey);
        const key = new Uint8Array(32);
        for (let i = 0; i < 32; i++) {
            key[i] = keyBytes[i % keyBytes.length];
        }
        
        // Derive encryption key (same as .NET)
        const encryptionKey = await crypto.subtle.importKey(
            'raw',
            key,
            { name: 'AES-CBC' },
            false,
            ['encrypt', 'decrypt']
        );
        
        // Derive HMAC key (SHA-256 hash of main key, same as .NET)
        const hmacKeyData = await crypto.subtle.digest('SHA-256', key);
        const hmacKey = await crypto.subtle.importKey(
            'raw',
            hmacKeyData,
            { name: 'HMAC', hash: 'SHA-256' },
            false,
            ['sign', 'verify']
        );
        
        return { encryptionKey, hmacKey };
    },
    
    async encrypt(plainText) {
        try {
            if (!plainText) return plainText;
            
            const { encryptionKey, hmacKey } = await this.getKeys();
            const iv = crypto.getRandomValues(new Uint8Array(16)); // 16 bytes for AES-CBC
            const plainBytes = new TextEncoder().encode(plainText);
            
            // Encrypt with AES-CBC
            const encrypted = await crypto.subtle.encrypt(
                { name: 'AES-CBC', iv: iv },
                encryptionKey,
                plainBytes
            );
            
            const cipherBytes = new Uint8Array(encrypted);
            
            // Calculate HMAC over IV + Ciphertext (same order as .NET)
            const dataToAuth = new Uint8Array(iv.length + cipherBytes.length);
            dataToAuth.set(iv, 0);
            dataToAuth.set(cipherBytes, iv.length);
            
            const hmacSignature = await crypto.subtle.sign('HMAC', hmacKey, dataToAuth);
            const hmacBytes = new Uint8Array(hmacSignature);
            
            // Combine IV + HMAC + Ciphertext (same format as .NET)
            const result = new Uint8Array(iv.length + hmacBytes.length + cipherBytes.length);
            result.set(iv, 0);
            result.set(hmacBytes, iv.length);
            result.set(cipherBytes, iv.length + hmacBytes.length);
            
            return btoa(String.fromCharCode(...result));
        } catch (error) {
            throw new Error('Encryption failed: ' + error.message);
        }
    },
    
    async decrypt(encryptedData) {
        try {
            if (!encryptedData) return encryptedData;
            
            const { encryptionKey, hmacKey } = await this.getKeys();
            const data = new Uint8Array(atob(encryptedData).split('').map(c => c.charCodeAt(0)));
            
            if (data.length < 48) { // 16 IV + 32 HMAC minimum
                throw new Error('Invalid encrypted data format');
            }
            
            // Extract components: IV + HMAC + Ciphertext
            const iv = data.slice(0, 16);
            const receivedHmac = data.slice(16, 48);
            const cipherBytes = data.slice(48);
            
            // Verify HMAC
            const dataToAuth = new Uint8Array(iv.length + cipherBytes.length);
            dataToAuth.set(iv, 0);
            dataToAuth.set(cipherBytes, iv.length);
            
            const isValid = await crypto.subtle.verify('HMAC', hmacKey, receivedHmac, dataToAuth);
            if (!isValid) {
                throw new Error('HMAC verification failed');
            }
            
            // Decrypt
            const decrypted = await crypto.subtle.decrypt(
                { name: 'AES-CBC', iv: iv },
                encryptionKey,
                cipherBytes
            );
            
            return new TextDecoder().decode(decrypted);
        } catch (error) {
            throw new Error('Decryption failed: ' + error.message);
        }
    }
};";
    }
}