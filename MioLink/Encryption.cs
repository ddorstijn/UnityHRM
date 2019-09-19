using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Encrypt
{
    /// <summary>
    /// Enable simple encryption on a string.
    /// </summary>
    public static class StringCipher
    {
        
        private const int Keysize = 256;                    ///< Determine the keysize of the encryption algorithm in bits.
                                                            ///< We divide this by 8 to get the equivalent number of bytes.
                                                            
        private const int DerivationIterations = 1000;      ///< Determines the number of iterations for the password bytes generation function.

        private const string passPhrase = "12ghjGS3@3jf";   ///< Password for string. Hardcoded for simplicity.
                                                            ///< Also means it doesn't have to be synced so the password is not send over network.

        /// <summary>
        /// Encrypt the input string.
        /// </summary>
        /// <param name="plainText">String in plain text to be encrypted.</param>
        /// <returns></returns>
        public static byte[] Encrypt(string plainText)
        {
            // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
            // so that the same Salt and IV values can be used when decrypting.  
            var saltStringBytes = Generate256BitsOfRandomEntropy();
            var ivStringBytes = Generate256BitsOfRandomEntropy();
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
            {
                var keyBytes = password.GetBytes(Keysize / 8);
                using (var symmetricKey = new RijndaelManaged())
                {
                    symmetricKey.BlockSize = 256;
                    symmetricKey.Mode = CipherMode.CBC;
                    symmetricKey.Padding = PaddingMode.PKCS7;
                    using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                            {
                                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                cryptoStream.FlushFinalBlock();
                                // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                var cipherTextBytes = saltStringBytes;
                                cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                memoryStream.Close();
                                cryptoStream.Close();
                                return cipherTextBytes;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate random bytes.
        /// </summary>
        /// <returns>Random byte sequence.</returns>
        private static byte[] Generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                // Fill the array with cryptographically secure random bytes.
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }
    }
}
