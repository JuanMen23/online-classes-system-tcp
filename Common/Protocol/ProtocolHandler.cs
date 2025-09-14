using System.Net.Sockets;
using System.Text;

namespace Common.Protocol;

/// <summary>
/// Handles protocol message sending and receiving with proper TCP message handling
/// </summary>
public class ProtocolHandler
{
    /// <summary>
    /// Sends a protocol message through the socket using 4 separate messages
    /// </summary>
    /// <param name="socket">The socket to send through</param>
    /// <param name="message">The protocol message to send</param>
    /// <exception cref="ArgumentNullException">Thrown when socket or message is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when message is invalid or socket error occurs</exception>
    public void SendMessage(Socket socket, ProtocolMessage message)
    {
        if (socket == null)
            throw new ArgumentNullException(nameof(socket));
        
        if (message == null)
            throw new ArgumentNullException(nameof(message));
        
        try
        {
            // Send HEADER (3 characters)
            SendComplete(socket, Encoding.UTF8.GetBytes(message.Header));
            
            // Send CMD (2 characters, right-aligned with zeros)
            string cmdStr = message.Command.ToString().PadLeft(ProtocolConstants.CMD_LENGTH, '0');
            SendComplete(socket, Encoding.UTF8.GetBytes(cmdStr));
            
            // Send LARGO (4 bytes, binary format)
            byte[] largoBytes = BitConverter.GetBytes(message.Length);
            SendComplete(socket, largoBytes);
            
            // Send DATOS (variable length)
            if (message.Length > 0)
            {
                SendComplete(socket, Encoding.UTF8.GetBytes(message.Data));
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send protocol message: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Receives a protocol message from the socket using 4 separate messages
    /// </summary>
    /// <param name="socket">The socket to receive from</param>
    /// <returns>The received protocol message</returns>
    /// <exception cref="ArgumentNullException">Thrown when socket is null</exception>
    /// <exception cref="InvalidOperationException">Thrown when socket error occurs or message is invalid</exception>
    public ProtocolMessage ReceiveMessage(Socket socket)
    {
        if (socket == null)
            throw new ArgumentNullException(nameof(socket));
        
        try
        {
            // Create buffers with expected sizes for fixed-length fields
            byte[] headerBytes = new byte[ProtocolConstants.HEADER_LENGTH];
            byte[] cmdBytes = new byte[ProtocolConstants.CMD_LENGTH];
            byte[] largoBytes = new byte[ProtocolConstants.LARGO_LENGTH];

            // Receive HEADER (3 characters)
            ReceiveComplete(socket, headerBytes);
            string header = Encoding.UTF8.GetString(headerBytes).TrimEnd();
            
            // Receive CMD (2 characters)
            ReceiveComplete(socket, cmdBytes);
            string cmdStr = Encoding.UTF8.GetString(cmdBytes).TrimStart('0');
            if (string.IsNullOrEmpty(cmdStr))
                cmdStr = "0";
            int command = int.Parse(cmdStr);
            
            // Receive LARGO (4 bytes, binary format)
            ReceiveComplete(socket, largoBytes);
            int length = BitConverter.ToInt32(largoBytes);
            
            // Receive DATOS (variable length - create buffer with the length we received)
            string data = "";
            if (length > 0)
            {
                byte[] dataBytes = new byte[length];
                ReceiveComplete(socket, dataBytes);
                data = Encoding.UTF8.GetString(dataBytes);
            }
            
            // Create and return the protocol message
            return new ProtocolMessage(header, command, data);
        }
        catch (Exception ex) when (!(ex is ArgumentNullException || ex is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to receive protocol message: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Sends complete data through socket using while loop with offset
    /// </summary>
    /// <param name="socket">The socket to send through</param>
    /// <param name="data">The data to send</param>
    private void SendComplete(Socket socket, byte[] data)
    {
        int offset = 0;
        int totalSize = data.Length;
        
        while (offset < totalSize)
        {
            int sent = socket.Send(data, offset, totalSize - offset, SocketFlags.None);
            offset += sent;
        }
    }
    
    /// <summary>
    /// Receives complete data from socket using while loop with offset into a pre-allocated buffer
    /// </summary>
    /// <param name="socket">The socket to receive from</param>
    /// <param name="buffer">The buffer to receive data into</param>
    private void ReceiveComplete(Socket socket, byte[] buffer)
    {
        int expectedSize = buffer.Length;
        int offset = 0;
        
        while (offset < expectedSize)
        {
            int received = socket.Receive(buffer, offset, expectedSize - offset, SocketFlags.None);
            
            // If received is 0, the connection was closed gracefully
            if (received == 0)
                throw new InvalidOperationException("Connection closed by remote host");
            
            offset += received;
        }
    }
}
