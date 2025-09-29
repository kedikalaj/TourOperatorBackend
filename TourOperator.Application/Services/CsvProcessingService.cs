using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Data;
using System.Globalization;
using TourOperator.Application.Hubs;
using TourOperator.Application.Interfaces;
using TourOperator.Domain.Entities;
using TourOperator.Infrastructure.Data;

namespace TourOperator.Application.Services
{
    public class CsvProcessingService : ICsvProcessingService
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<UploadProgressHub> _hub;
        private readonly IConfiguration _config;

        public CsvProcessingService(AppDbContext db, IHubContext<UploadProgressHub> hub, IConfiguration cfg)
        {
            _db = db;
            _hub = hub;
            _config = cfg;
        }

        public async Task ProcessCsvAsync(Stream csvStream, Guid tourOperatorId, string? connectionId, CancellationToken ct = default)
        {
            await _hub.Clients.Client(connectionId).SendAsync("Progress", "Validation started", ct);
            Log.Information("CSV processing started for tourOperatorId={TourOperatorId}", tourOperatorId);

            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null, BadDataFound = null, HeaderValidated = null };
            int total = 0;
            using (var readerCount = new StreamReader(csvStream, leaveOpen: true))
            using (var csvCount = new CsvReader(readerCount, cfg))
            {
                await csvCount.ReadAsync();
                csvCount.ReadHeader();
                while (await csvCount.ReadAsync())
                    total++;
            }
            csvStream.Position = 0;

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, cfg);
            csv.Context.RegisterClassMap<PricingCsvMap>();
            await csv.ReadAsync();
            csv.ReadHeader();

            int processed = 0;
            int nextThreshold = 10;
            var dt = CreateDataTableSkeleton();

            while (await csv.ReadAsync())
            {
                try
                {
                    var rec = csv.GetRecord<PricingCsvRow>();
                    if (!DateTime.TryParse(rec.Date, out var dtDate))
                    {
                        await _hub.Clients.Client(connectionId).SendAsync("Progress", $"Row {processed + 1}: Invalid date '{rec.Date}' - skipped");
                        Log.Warning("Invalid date at row {Row}: {Value}", processed + 1, rec.Date);
                        continue;
                    }
                    if (!decimal.TryParse(rec.EconomyPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var econPrice))
                    {
                        await _hub.Clients.Client(connectionId).SendAsync("Progress", $"Row {processed + 1}: Invalid economy price - skipped");
                        Log.Warning("Invalid economy price at row {Row}", processed + 1);
                        continue;
                    }
                    if (!decimal.TryParse(rec.BusinessPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var busPrice))
                    {
                        await _hub.Clients.Client(connectionId).SendAsync("Progress", $"Row {processed + 1}: Invalid business price - skipped");
                        Log.Warning("Invalid business price at row {Row}", processed + 1);
                        continue;
                    }

                    var economySeats = int.TryParse(rec.EconomySeats, out var es) ? es : 0;
                    var businessSeats = int.TryParse(rec.BusinessSeats, out var bs) ? bs : 0;

                    var pr = new PricingRecord
                    {
                        TourOperatorId = tourOperatorId,
                        RouteCode = rec.RouteCode?.Trim() ?? "",
                        SeasonCode = rec.SeasonCode?.Trim() ?? "",
                        Date = dtDate,
                        EconomySeats = economySeats,
                        BusinessSeats = businessSeats,
                        EconomyPrice = econPrice,
                        BusinessPrice = busPrice
                    };

                    var row = dt.NewRow();
                    row["Id"] = pr.Id;
                    row["TourOperatorId"] = pr.TourOperatorId;
                    row["RouteCode"] = pr.RouteCode;
                    row["SeasonCode"] = pr.SeasonCode;
                    row["Date"] = pr.Date;
                    row["EconomySeats"] = pr.EconomySeats;
                    row["BusinessSeats"] = pr.BusinessSeats;
                    row["EconomyPrice"] = pr.EconomyPrice;
                    row["BusinessPrice"] = pr.BusinessPrice;
                    row["CreatedAt"] = pr.CreatedAt;
                    dt.Rows.Add(row);

                    processed++;

                    if (dt.Rows.Count >= 5000)
                    {
                        await BulkInsertAsync(dt, ct);
                        dt.Clear();
                    }

                    int percent = (processed * 100) / total;
                    if (percent >= nextThreshold)
                    {
                        await _hub.Clients.Client(connectionId).SendAsync("Progress", $"{percent}% completed", ct);
                        nextThreshold += 10;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception while parsing csv row {Row}", processed + 1);
                }
            }

            if (dt.Rows.Count > 0)
            {
                await BulkInsertAsync(dt, ct);
            }

            await _hub.Clients.Client(connectionId).SendAsync("Progress", "Bulk insert completed", ct);
            Log.Information("CSV processing completed for tourOperatorId={TourOperatorId}. Processed={Processed}", tourOperatorId, processed);
        }


        private DataTable CreateDataTableSkeleton()
        {
            var dt = new DataTable();
            dt.Columns.Add("Id", typeof(Guid));
            dt.Columns.Add("TourOperatorId", typeof(Guid));
            dt.Columns.Add("RouteCode", typeof(string));
            dt.Columns.Add("SeasonCode", typeof(string));
            dt.Columns.Add("Date", typeof(DateTime));
            dt.Columns.Add("EconomySeats", typeof(int));
            dt.Columns.Add("BusinessSeats", typeof(int));
            dt.Columns.Add("EconomyPrice", typeof(decimal));
            dt.Columns.Add("BusinessPrice", typeof(decimal));
            dt.Columns.Add("CreatedAt", typeof(DateTime));
            return dt;
        }

        private async Task BulkInsertAsync(DataTable dt, CancellationToken ct)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            using var sqlConn = new SqlConnection(connStr);
            await sqlConn.OpenAsync(ct);
            using var bulk = new SqlBulkCopy(sqlConn, SqlBulkCopyOptions.KeepIdentity, null)
            {
                DestinationTableName = "PricingRecords"
            };
            bulk.ColumnMappings.Add("Id", "Id");
            bulk.ColumnMappings.Add("TourOperatorId", "TourOperatorId");
            bulk.ColumnMappings.Add("RouteCode", "RouteCode");
            bulk.ColumnMappings.Add("SeasonCode", "SeasonCode");
            bulk.ColumnMappings.Add("Date", "Date");
            bulk.ColumnMappings.Add("EconomySeats", "EconomySeats");
            bulk.ColumnMappings.Add("BusinessSeats", "BusinessSeats");
            bulk.ColumnMappings.Add("EconomyPrice", "EconomyPrice");
            bulk.ColumnMappings.Add("BusinessPrice", "BusinessPrice");
            bulk.ColumnMappings.Add("CreatedAt", "CreatedAt");

            await bulk.WriteToServerAsync(dt, ct);
        }
    }

    // CSV row map
    public class PricingCsvRow
    {
        public string? RouteCode { get; set; }
        public string? SeasonCode { get; set; }
        public string? EconomySeats { get; set; }
        public string? BusinessSeats { get; set; }
        public string Date { get; set; } = null!;
        public string EconomyPrice { get; set; } = null!;
        public string BusinessPrice { get; set; } = null!;
    }

    public sealed class PricingCsvMap : ClassMap<PricingCsvRow>
    {
        public PricingCsvMap()
        {
            Map(m => m.RouteCode).Name("RouteCode");
            Map(m => m.SeasonCode).Name("SeasonCode");
            Map(m => m.EconomySeats).Name("EconomySeats");
            Map(m => m.BusinessSeats).Name("BusinessSeats");
            Map(m => m.Date).Name("Date");
            Map(m => m.EconomyPrice).Name("EconomyPrice");
            Map(m => m.BusinessPrice).Name("BusinessPrice");
        }
    }
}
