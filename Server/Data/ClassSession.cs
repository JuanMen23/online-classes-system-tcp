using System.Collections.Concurrent;

namespace Server.Data;

public class ClassSession
{
    private readonly ConcurrentBag<string> _enrolledUsers = new();
    private readonly ConcurrentBag<Enrollment> _enrollments = new();

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
        if (!_enrolledUsers.Contains(username))
        {
            _enrolledUsers.Add(username);
            _enrollments.Add(new Enrollment
            {
                Username = username,
                IsCancelled = false,
                EnrolledAt = DateTime.Now
            });
        }
    }

    public bool RemoveUser(string username)
    {
        // ConcurrentBag no tiene Remove directo, necesitamos recrear la colección
        var currentUsers = _enrolledUsers.ToList();
        if (currentUsers.Remove(username))
        {
            while (_enrolledUsers.TryTake(out _)) { }
            foreach (var user in currentUsers)
            {
                _enrolledUsers.Add(user);
            }

            // Actualizamos el historial
            var enrollment = _enrollments
                .FirstOrDefault(e => e.Username == username && !e.IsCancelled);
            if (enrollment != null)
            {
                enrollment.IsCancelled = true;
            }

            return true;
        }
        return false;
    }

    public IEnumerable<string> GetEnrolledUsers()
    {
        return _enrolledUsers.ToList();
    }

    // Propiedad pública para acceder al historial de inscripciones
    public IEnumerable<Enrollment> Enrollments => _enrollments.ToList();

    public class Enrollment
    {
        public string Username { get; set; } = "";
        public bool IsCancelled { get; set; } = false;
        public DateTime EnrolledAt { get; set; } = DateTime.Now;
    }
}
