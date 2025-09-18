namespace Server.Services;

using Server.ClassSession;
using System.Collections.Concurrent;
using System.Collections.Generic; 
using System.Linq; 

public class ClassService
{
    private static readonly Lazy<ClassService> _instance = new(() => new ClassService());
    public static ClassService Instance => _instance.Value;

    private readonly ConcurrentBag<ClassSession> _classes = new();
    private int _nextId = 1;
    private readonly object _lockNextId = new object();

    private ClassService() { }

    public ClassSession CreateClass(string name, string description, int maxSeats,
        DateTime startDateTime, int durationMinutes, string? imagePath)
    {
        int id;
        lock (_lockNextId)
        {
            id = _nextId++;
        }
        var newClass = new ClassSession
        {
            Id = id,
            Name = name,
            Description = description,
            MaxSeats = maxSeats,
            StartDateTime = startDateTime,
            DurationMinutes = durationMinutes,
            ImagePath = imagePath,
            Link = $"class-{Guid.NewGuid()}"
        };

        _classes.Add(newClass);
        return newClass;
    }
    
    public ClassSession CreateClassWithDetails(string name, string description, int maxSeats,
        DateTime startDateTime, int durationMinutes, string? imageBase64)
    {
        string? imagePath = null;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            byte[] imageBytes = Convert.FromBase64String(imageBase64);
            if (imageBytes.Length > 5 * 1024 * 1024)
            {
                throw new ArgumentException("Imagen demasiado grande (máximo 5MB)");
            }
            Directory.CreateDirectory("Images");
            imagePath = Path.Combine("Images", $"{Guid.NewGuid()}.png");
            File.WriteAllBytes(imagePath, imageBytes);
        }
        
        var createdClass = CreateClass(name, description, maxSeats, startDateTime, durationMinutes, imagePath);
        
        Console.WriteLine($"Clase creada: {createdClass.Id} ({createdClass.Name})");
        return createdClass;
    }

    public IEnumerable<ClassSession> GetAllClasses() => _classes.ToList();
}