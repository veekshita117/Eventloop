namespace Eventloop.Core.DTOs.Events;

public class EventQueryParametersDto
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    private int _page = 1;

    public string? Search { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public bool? HasSpots { get; set; }
    public string? SortBy { get; set; } = "StartTime";
    public bool SortDescending { get; set; } = false;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }
}
