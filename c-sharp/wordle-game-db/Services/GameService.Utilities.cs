using System;
using System.Text;
using System.Security.Cryptography;

namespace WordleGame.Services
{
    public partial class GameService
    {
        private string GenerateHash(int length, string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                string hexHash = Convert.ToHexString(hashBytes);
                return hexHash.Length > length ? hexHash.Substring(0, length) : hexHash;
            }
        }
    }
}
