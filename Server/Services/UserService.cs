using System.Collections.Concurrent;
using System.Collections.Generic;
using Common.Protocol;

namespace Server.Services;

public class UserService
{
    private static readonly Lazy<UserService> _instance = new(() => new UserService());
    private readonly ConcurrentDictionary<string, User.User> _registeredUsers = new();
    private readonly ConcurrentDictionary<Guid, string> _activeSessions = new();
    private readonly object _sessionLock = new object();
    
    public static UserService Instance => _instance.Value;

    private void PublishUserLog(string evento, string? usuario, string mensaje, string nivel = "INFO", Guid? clientId = null, Dictionary<string, string>? extra = null)
    {
        var metadata = extra != null ? new Dictionary<string, string>(extra) : new Dictionary<string, string>();
        if (clientId.HasValue)
        {
            metadata["client_id"] = clientId.Value.ToString();
        }

        LoggingService.Instance.PublishLog(evento, usuario, mensaje, nivel, null, metadata);
    }

    public string RegisterUser(string username, string password, Guid? clientId = null, bool emitLog = true)
    {
         var newUser = new User.User { Username = username, Password = password };
         if (_registeredUsers.TryAdd(username, newUser))
         {
              Console.WriteLine($"✅ Usuario registrado exitosamente: '{username}'");
              if (emitLog)
              {
                  PublishUserLog(
                      "register_success",
                      username,
                      $"Usuario '{username}' registrado correctamente",
                      "INFO",
                      clientId);
              }
              return ProtocolConstants.RESPONSE_OK;
         }
         if (emitLog)
         {
             PublishUserLog(
                 "register_exists",
                 username,
                 $"Intento de registrar '{username}' pero ya existe",
                 "WARN",
                 clientId);
         }
         return ProtocolConstants.RESPONSE_ERROR_USER_EXISTS;
    }

    public string LoginUser(Guid clientId, string username, string password)
    {
         if (_registeredUsers.TryGetValue(username, out User.User user) && user.Password == password)
         {
              // Double-checked locking para evitar login duplicado
              lock (_sessionLock)
              {
                   // Re-verificar dentro del lock
                   if (_activeSessions.Values.Contains(username))
                   {
                        return ProtocolConstants.RESPONSE_ERROR_USER_ALREADY_LOGGED_IN;
                   }

                   _activeSessions.TryAdd(clientId, username);
                   Console.WriteLine($"➡️ Usuario inició sesión: '{username}' (ID de conexión: {clientId})");
                   PublishUserLog(
                       "login",
                       username,
                       $"Usuario '{username}' inició sesión",
                       "INFO",
                       clientId);
                   return ProtocolConstants.RESPONSE_OK;
              }
         }

                   if (_activeSessions.Values.Contains(username))
                   {
                        PublishUserLog(
                            "login_already_logged",
                            username,
                            $"Intentó iniciar sesión pero ya tenía una sesión activa",
                            "WARN",
                            clientId);
                        return ProtocolConstants.RESPONSE_ERROR_USER_ALREADY_LOGGED_IN;
                   }
         PublishUserLog(
             "login_invalid_credentials",
             username,
             $"Intento de login fallido para '{username}'",
             "WARN",
             clientId);

         return ProtocolConstants.RESPONSE_ERROR_INVALID_CREDENTIALS;
    }

    public void LogoutUser(Guid clientId, string reason = "user_request")
    {
         if (_activeSessions.TryRemove(clientId, out string username))
         {
              Console.WriteLine($"⬅️ Usuario cerró sesión: '{username}' (ID de conexión: {clientId})");
              PublishUserLog(
                  "logout",
                  username,
                  $"Usuario '{username}' cerró sesión",
                  "INFO",
                  clientId,
                  new Dictionary<string, string> { ["reason"] = reason });
         }
         else
         {
              PublishUserLog(
                  "logout_unknown_session",
                  null,
                  $"Se intentó cerrar sesión para un cliente desconocido ({clientId})",
                  "WARN",
                  clientId,
                  new Dictionary<string, string> { ["reason"] = reason });
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

    public bool ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        return _registeredUsers.TryGetValue(username, out var user) &&
               user.Password == password;
    }
}