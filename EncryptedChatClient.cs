using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

class EncryptedChatClient
{
    static readonly byte[] key = Encoding.UTF8.GetBytes("1234567812345678");
    static readonly byte[] iv = Encoding.UTF8.GetBytes("8765432187654321");

    static async Task Main()
    {
        using TcpClient client = new TcpClient();
        await client.ConnectAsync("127.0.0.1", 9000);
        Console.Write("Podaj swój nick: ");
        string nick = Console.ReadLine()?.Trim() ?? "Anon";

        NetworkStream stream = client.GetStream();

        // Wyślij nick na początek
        byte[] nickBytes = Encrypt(nick);
        await stream.WriteAsync(nickBytes, 0, nickBytes.Length);

        _ = Task.Run(() => ListenAsync(stream));

        while (true)
        {
            string msg = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(msg)) continue;

            byte[] encrypted = Encrypt(msg);
            await stream.WriteAsync(encrypted, 0, encrypted.Length);
        }
    }

    static async Task ListenAsync(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string msg = Decrypt(buffer, bytesRead);
                if (msg == "PING")
                {
                    byte[] pong = Encrypt("PONG");
                    await stream.WriteAsync(pong, 0, pong.Length);
                }
                else
                {
                    Console.WriteLine(msg);
                }
            }
        }
        catch
        {
            Console.WriteLine("Rozłączono z serwerem.");
        }
    }

    static byte[] Encrypt(string plainText)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using MemoryStream ms = new();
        using CryptoStream cs = new(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        using StreamWriter sw = new(cs);
        sw.Write(plainText);
        sw.Close();
        return ms.ToArray();
    }

    static string Decrypt(byte[] cipherData, int count)
    {
        using Aes aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        using MemoryStream ms = new(cipherData, 0, count);
        using CryptoStream cs = new(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using StreamReader sr = new(cs);
        return sr.ReadToEnd();
    }
}
