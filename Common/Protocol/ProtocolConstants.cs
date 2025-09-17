namespace Common.Protocol;

/// <summary>
/// Constants for the communication protocol between client and server
/// </summary>
public static class ProtocolConstants
{
    // Network Configuration
    public const string DEFAULT_SERVER_IP = "127.0.0.1";
    public const int DEFAULT_SERVER_PORT = 20000;
    public const int MAX_BACKLOG_CONNECTIONS = 10;

    // Protocol Structure
    public const int HEADER_LENGTH = 3;   // HEADER field length
    public const int CMD_LENGTH = 2;      // CMD field length
    public const int LARGO_LENGTH = 4;    // LARGO field length

    // Header Values
    public const string HEADER_REQUEST = "REQ";
    public const string HEADER_RESPONSE = "RES";

    // ========================
    // Authentication Commands
    // ========================
    public const int CMD_LOGIN = 1;
    public const int CMD_LOGOUT = 2;
    public const int CMD_REGISTER = 3;

    // ========================
    // Class Management Commands
    // ========================
    public const int CMD_CREATE_CLASS = 10;     // Crear clase (CR2)
    public const int CMD_MODIFY_CLASS = 11;     // Modificar clase (CR4)
    public const int CMD_DELETE_CLASS = 12;     // Eliminar clase (CR5)
    public const int CMD_LIST_CLASSES = 13;     // Ver todas las clases (CR6)
    public const int CMD_SEARCH_CLASSES = 14;   // Buscar/filtrar clases (CR7)

    // ========================
    // Enrollment Commands
    // ========================
    public const int CMD_ENROLL_CLASS = 20;     // Inscripción en clase (CR3)
    public const int CMD_CANCEL_ENROLL = 21;    // Cancelar inscripción (CR8)
    public const int CMD_HISTORY = 22;          // Ver historial (CR9)

    // ========================
    // Error Handling
    // ========================
    public const int CMD_ERROR = 99;

    // ========================
    // Legacy / Utility
    // ========================
    public const string EXIT_COMMAND = "exit";  // Para salir del cliente
}
