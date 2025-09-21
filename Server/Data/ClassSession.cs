using System.Collections.Concurrent;

namespace Server.Data;

public class ClassSession
{
    private readonly ConcurrentBag<string> _enrolledUsers = new();
    
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxSeats { get; set; }
    public DateTime StartDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? ImagePath { get; set; }
    public string Link { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    
    // Propiedades thread-safe para acceso a usuarios inscritos
    public int EnrolledCount => _enrolledUsers.Count;
    
    public bool IsEnrolled(string username)
    {
        return _enrolledUsers.Contains(username);
    }
    
    public void EnrollUser(string username)
    {
        _enrolledUsers.Add(username);
    }
    
    public bool RemoveUser(string username)
    {
        // ConcurrentBag no tiene Remove directo, necesitamos recrear la colección
        var currentUsers = _enrolledUsers.ToList();
        if (currentUsers.Remove(username))
        {
            // Limpiar y repoblar
            while (_enrolledUsers.TryTake(out _)) { }
            foreach (var user in currentUsers)
            {
                _enrolledUsers.Add(user);
            }
            return true;
        }
        return false;
    }
    
    public IEnumerable<string> GetEnrolledUsers()
    {
        return _enrolledUsers.ToList();
    }
}