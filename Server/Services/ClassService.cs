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

    public IEnumerable<ClassSession> GetAllClasses() => _classes.ToList();
}