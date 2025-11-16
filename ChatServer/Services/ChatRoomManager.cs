using System.Collections.Concurrent;
using ChatServer.Models;

namespace ChatServer.Services;

public class ChatRoomManager
{
    private readonly ConcurrentDictionary<string, ChatRoom> _rooms = new(StringComparer.OrdinalIgnoreCase);

    public ChatRoom GetOrCreateRoom(string link, int classId, string className)
    {
        return _rooms.GetOrAdd(link, _ => new ChatRoom(link, classId, className));
    }

    public void RemoveRoomIfEmpty(string link)
    {
        if (_rooms.TryGetValue(link, out var room) && room.ParticipantCount == 0)
        {
            _rooms.TryRemove(link, out _);
        }
    }
}

