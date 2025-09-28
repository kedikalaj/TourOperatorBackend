namespace TourOperator.Application.Interfaces
{
    public interface ICsvProcessingService
    {
        Task ProcessCsvAsync(Stream csvStream, Guid tourOperatorId, string connectionId, CancellationToken ct = default);

    }
}
