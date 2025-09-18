namespace Server.Services;

using Server.ClassSession;

public class ClassService
{
    private static readonly Lazy<ClassService> _instance = new(() => new ClassService());
    public static ClassService Instance => _instance.Value;

    private readonly List<ClassSession> _classes = new();
    private int _nextId = 1;

    private ClassService() { }

    public ClassSession CreateClass(string name, string description, int maxSeats,
        DateTime startDateTime, int durationMinutes, string? imagePath)
    {
        var newClass = new ClassSession
        {
            Id = _nextId++,
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

    public IEnumerable<ClassSession> GetAllClasses() => _classes;
}