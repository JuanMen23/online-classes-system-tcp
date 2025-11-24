using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace ChatServer.Models;

public class ChatRoom
{
    private readonly ConcurrentDictionary<string, WebSocket> _participants = new(StringComparer.OrdinalIgnoreCase);

    public ChatRoom(string link, int classId, string className)
    {
        Link = link;
        ClassId = classId;
        ClassName = className;
    }

    public string Link { get; }
    public int ClassId { get; }
    public string ClassName { get; }

    public IEnumerable<KeyValuePair<string, WebSocket>> Participants => _participants;
    public int ParticipantCount => _participants.Count;

    public bool ContainsUser(string username) => _participants.ContainsKey(username);

    public bool TryAddParticipant(string username, WebSocket socket)
        => _participants.TryAdd(username, socket);

    public bool TryRemoveParticipant(string username, out WebSocket? socket)
        => _participants.TryRemove(username, out socket);
}

