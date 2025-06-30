# Encrypted Chat: Client/Server Chat with encrypted communication

## Project overview

This is a simple TCP-based client/server chat system where all communication is encrypted using the AES symmetric encryption algorithm. The application supports multiple clients connected to a single server and enables asynchronous message exchange. Each client sets a nickname, and all messages are broadcast to all connected users. The server periodically checks client availability and disconnects unresponsive clients.

## Structure

The project consists of two independent console applications:

- `EncryptedChatServer` – the server application
- `EncryptedChatClient` – the client application

## Requirements

- .NET 7.0 or later
- Visual Studio / Visual Studio Code / .NET CLI

## How to run

### 1. Build both projects:

```bash
dotnet build EncryptedChatServer
dotnet build EncryptedChatClient
```

### 2. Run the server:

```bash
cd EncryptedChatServer
dotnet run
```

The server listens on port `9000`.

### 3. Run one or more clients:

```bash
cd EncryptedChatClient
dotnet run
```

Each client is prompted to enter a nickname after connecting.

## Testing the Chat

1. Start the server in one terminal window.
2. Start at least two client instances in separate terminal windows.
3. Enter different nicknames for each client.
4. When Client A sends a message, Client B sees it, and vice versa.
5. If a client is closed or unresponsive, the server automatically disconnects it after 10 seconds.

## Encryption details

- **Algorithm**: AES (via `Aes.Create()`, default CBC mode)
- **Key and IV**: 128-bit, hardcoded on both client and server side (16 UTF-8 bytes each)
- **Encrypted content**: all messages including nicknames and chat messages
- **Keepalive mechanism**: server sends encrypted `PING`, expects `PONG` response

## Limitations

- The AES key and IV are hardcoded and shared manually.
- No key exchange protocol (e.g., RSA handshake) is implemented.
- No authentication or message integrity (e.g., HMAC or AES-GCM).
- No TLS. Encryption is implemented manually using AES.

