namespace Client.Services;

/// <summary>
/// Manages user authentication state and operations
/// </summary>
public class AuthManager
{
    private bool _isLoggedIn = false;
    private string? _currentUser = null;

    /// <summary>
    /// Gets whether a user is currently logged in
    /// </summary>
    public bool IsLoggedIn => _isLoggedIn;

    /// <summary>
    /// Gets the current logged in username
    /// </summary>
    public string? CurrentUser => _currentUser;

    /// <summary>
    /// Sets the user as logged in
    /// </summary>
    /// <param name="username">Username of the logged in user</param>
    public void SetLoggedIn(string username)
    {
        _isLoggedIn = true;
        _currentUser = username;
    }

    /// <summary>
    /// Sets the user as logged out
    /// </summary>
    public void SetLoggedOut()
    {
        _isLoggedIn = false;
        _currentUser = null;
    }

    /// <summary>
    /// Attempts to login with the given username
    /// This method sets the current user but doesn't validate credentials
    /// </summary>
    /// <param name="username">Username to attempt login with</param>
    public void AttemptLogin(string username)
    {
        _currentUser = username;
        // Note: Actual login validation happens on server response
    }

    /// <summary>
    /// Clears the current user without logging out
    /// Used when login attempt fails
    /// </summary>
    public void ClearCurrentUser()
    {
        _currentUser = null;
    }

    /// <summary>
    /// Checks if a user is currently logged in
    /// </summary>
    /// <returns>True if logged in, false otherwise</returns>
    public bool IsUserLoggedIn()
    {
        return _isLoggedIn && !string.IsNullOrEmpty(_currentUser);
    }

    /// <summary>
    /// Gets a display-friendly status message
    /// </summary>
    /// <returns>Status message string</returns>
    public string GetStatusMessage()
    {
        if (IsUserLoggedIn())
        {
            return $"Conectado como: {_currentUser}";
        }
        return "No hay usuario conectado";
    }
}
