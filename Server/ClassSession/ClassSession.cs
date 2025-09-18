namespace Server.ClassSession;

public class ClassSession
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int MaxSeats { get; set; }
    public DateTime StartDateTime { get; set; }
    public int DurationMinutes { get; set; }
    public string? ImagePath { get; set; }
    public string Link { get; set; } = string.Empty;
    public List<string> EnrolledUsers { get; set; } = new List<string>();
}