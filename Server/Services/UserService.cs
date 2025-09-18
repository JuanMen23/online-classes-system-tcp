using System.Collections.Concurrent;
using Common.Protocol;

namespace Server.Services;

public class UserService
{
    private static readonly Lazy<UserService> _instance = new(() => new UserService());
    private readonly ConcurrentDictionary<string, User.User> _registeredUsers = new();
    private readonly ConcurrentDictionary<Guid, string> _activeSessions = new();
    
    public static UserService Instance => _instance.Value;

    public string RegisterUser(string username, string password)
    {
         var newUser = new User.User { Username = username, Password = password };
         if (_registeredUsers.TryAdd(username, newUser))
         {
              Console.WriteLine($"✅ Usuario registrado exitosamente: '{username}'");
              return ProtocolConstants.RESPONSE_OK;
         }
         return ProtocolConstants.RESPONSE_ERROR_USER_EXISTS;
    }

    public string LoginUser(Guid clientId, string username, string password)
    {
         if (_registeredUsers.TryGetValue(username, out User.User user) && user.Password == password)
         {
              // Avoid double login
              if (_activeSessions.Values.Contains(username))
              {
                   return ProtocolConstants.RESPONSE_ERROR_USER_ALREADY_LOGGED_IN;
              }

              _activeSessions.TryAdd(clientId, username);
              Console.WriteLine($"➡️ Usuario inició sesión: '{username}' (ID de conexión: {clientId})");
              return ProtocolConstants.RESPONSE_OK;
         }

         return ProtocolConstants.RESPONSE_ERROR_INVALID_CREDENTIALS;
    }

    public void LogoutUser(Guid clientId)
    {
         if (_activeSessions.TryRemove(clientId, out string username))
         {
              Console.WriteLine($"⬅️ Usuario cerró sesión: '{username}' (ID de conexión: {clientId})");
         }
    }

    public bool IsUserLoggedIn(Guid clientId)
    {
         return _activeSessions.ContainsKey(clientId);
    }
    
    public string? GetLoggedInUsername(Guid clientId)
    {
         return _activeSessions.TryGetValue(clientId, out string? username) ? username : null;
    }
}