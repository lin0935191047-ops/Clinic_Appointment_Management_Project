namespace ClinicAppointment.Application.Common.Models;

public class PaginatedList<T>
{
    public IReadOnlyCollection<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalItems { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PaginatedList(IReadOnlyCollection<T> items, int count, int page, int pageSize)
    {
        Page = page <= 0 ? 1 : page;
        PageSize = pageSize <= 0 ? 20 : pageSize;
        TotalItems = count;
        TotalPages = (int)Math.Ceiling(count / (double)PageSize);
        Items = items;
    }

    public static PaginatedList<T> Create(IEnumerable<T> source, int count, int page, int pageSize)
    {
        return new PaginatedList<T>(source.ToList(), count, page, pageSize);
    }
}
