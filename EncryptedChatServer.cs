using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

class EncryptedChatServer
{
    static readonly byte[] key = Encoding.UTF8.GetBytes("1234567812345678");
    static readonly byte[] iv = Encoding.UTF8.GetBytes("8765432187654321");

    class ClientInfo
    {
        public TcpClient Client { get; set; }
        public string Nick { get; set; }
    }

    static List<ClientInfo> clients = new();

    static async Task Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 9000);
        listener.Start();
        Console.WriteLine("Serwer nasłuchuje na porcie 9000...");

        while (true)
        {
            TcpClient tcpClient = await listener.AcceptTcpClientAsync();
            _ = HandleClientAsync(tcpClient);
        }
    }

    static async Task HandleClientAsync(TcpClient tcpClient)
    {
        var stream = tcpClient.GetStream();
        string nick = "";

        try
        {
            byte[] buffer = new byte[1024];
            int nameBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            nick = Decrypt(buffer, nameBytes).Trim();
            if (string.IsNullOrWhiteSpace(nick)) nick = "Anon";

            var client = new ClientInfo { Client = tcpClient, Nick = nick };
            clients.Add(client);
            Console.WriteLine($"[{nick}] połączył się.");

            await BroadcastAsync($"** {nick} dołączył do czatu **", client);

            DateTime lastSeen = DateTime.UtcNow;
            System.Timers.Timer pingTimer = new(5000);
            pingTimer.Elapsed += async (_, _) =>
            {
                if ((DateTime.UtcNow - lastSeen).TotalSeconds > 10)
                {
                    Console.WriteLine($"[{nick}] nie odpowiada. Rozłączenie.");
                    pingTimer.Stop();
                    tcpClient.Close();
                }
                else
                {
                    try
                    {
                        byte[] ping = Encrypt("PING");
                        await stream.WriteAsync(ping, 0, ping.Length);
                    }
                    catch
                    {
                        pingTimer.Stop();
                        tcpClient.Close();
                    }
                }
            };
            pingTimer.Start();

            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                string msg = Decrypt(buffer, bytesRead).Trim();
                if (msg == "PONG")
                {
                    lastSeen = DateTime.UtcNow;
                    continue;
                }

                Console.WriteLine($"[{nick}]: {msg}");
                await BroadcastAsync($"[{nick}]: {msg}", client);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd [{nick}]: {ex.Message}");
        }
        finally
        {
            clients.RemoveAll(c => c.Client == tcpClient);
            tcpClient.Close();
            Console.WriteLine($"[{nick}] rozłączony.");
            await BroadcastAsync($"** {nick} opuścił czat **", null);
        }
    }

    static async Task BroadcastAsync(string message, ClientInfo sender)
    {
        byte[] data = Encrypt(message);
        foreach (var client in clients)
        {
            if (client != sender)
            {
                try
                {
                    await client.Client.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch { }
            }
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
