using System.Text;

namespace Common.Protocol;

/// <summary>
/// Represents a protocol message with HEADER, CMD, LARGO, and DATOS fields
/// </summary>
public class ProtocolMessage
{
    /// <summary>
    /// Header field (REQ or RES)
    /// </summary>
    public string Header { get; set; } = "";
    
    /// <summary>
    /// Command field (0-99)
    /// </summary>
    public int Command { get; set; }
    
    /// <summary>
    /// Length field (length of data in bytes)
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    /// Data field (variable length)
    /// </summary>
    public string Data { get; set; } = "";
    
    /// <summary>
    /// Initializes a new instance of ProtocolMessage
    /// </summary>
    public ProtocolMessage()
    {
    }
    
    /// <summary>
    /// Initializes a new instance of ProtocolMessage with specified values
    /// </summary>
    /// <param name="header">Header value (REQ or RES)</param>
    /// <param name="command">Command number (0-99)</param>
    /// <param name="data">Data content</param>
    public ProtocolMessage(string header, int command, string data)
    {
        Header = header;
        Command = command;
        Data = data;
        Length = Encoding.UTF8.GetBytes(data).Length; // Length in bytes, not characters
    }
    
    /// <summary>
    /// Validates the protocol message
    /// </summary>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValid()
    {
        // Validate header
        if (Header != ProtocolConstants.HEADER_REQUEST && Header != ProtocolConstants.HEADER_RESPONSE)
            return false;
        
        // Validate command range (0-99 as per specification)
        if (Command < 0 || Command > 99)
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Returns a string representation of the protocol message
    /// </summary>
    /// <returns>String representation</returns>
    public override string ToString()
    {
        return $"{Header}|{Command:D2}|{Length:D4}|{Data}";
    }
}
