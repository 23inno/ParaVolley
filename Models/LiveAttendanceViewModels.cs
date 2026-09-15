namespace SportsManagementMVC.Models;

public sealed class LiveAttendanceViewModel
{
    public List<Event> Events { get; set; } = new();

    public Event? SelectedEvent { get; set; }

    public int? SelectedEventId => SelectedEvent?.Id;
}
