using Azure;
using Azure.Data.Tables;

namespace SideGigPh.Api.Telemetry;

/// <summary>
/// One Azure Table Storage table, the few calls the write-mostly stores need (<see cref="ErrorReportTable"/>,
/// <see cref="OtherJobNoteTable"/>). Table Storage costs pennies and never wakes the SQL database, which bills for every
/// minute it is awake. Tests use an in-memory one.
/// </summary>
public interface ITableRows
{
    Task AddAsync(TableEntity row, CancellationToken cancellationToken);

    /// <summary>Adds rows of one partition all together or not at all (at most 100, Table Storage's limit).</summary>
    Task AddAllAsync(IReadOnlyList<TableEntity> rows, CancellationToken cancellationToken);

    /// <summary>Up to <paramref name="take"/> rows of a partition in row-key order, starting after <paramref name="afterRowKey"/>.</summary>
    Task<IReadOnlyList<TableEntity>> ReadAsync(string partition, string? afterRowKey, int take, CancellationToken cancellationToken);

    Task DeleteAsync(string partition, string rowKey, CancellationToken cancellationToken);
}

/// <summary><see cref="ITableRows"/> on a real table, created on first use (in the photos' storage account).</summary>
public sealed class AzureTableRows(TableClient table) : ITableRows
{
    private volatile bool _created;

    public async Task AddAsync(TableEntity row, CancellationToken cancellationToken)
    {
        await EnsureAsync(cancellationToken);
        await table.AddEntityAsync(row, cancellationToken);
    }

    public async Task AddAllAsync(IReadOnlyList<TableEntity> rows, CancellationToken cancellationToken)
    {
        await EnsureAsync(cancellationToken);
        await table.SubmitTransactionAsync(rows.Select(row => new TableTransactionAction(TableTransactionActionType.Add, row)), cancellationToken);
    }

    public async Task<IReadOnlyList<TableEntity>> ReadAsync(string partition, string? afterRowKey, int take, CancellationToken cancellationToken)
    {
        await EnsureAsync(cancellationToken);
        var filter = afterRowKey is null
            ? TableClient.CreateQueryFilter($"PartitionKey eq {partition}")
            : TableClient.CreateQueryFilter($"PartitionKey eq {partition} and RowKey gt {afterRowKey}");
        var rows = new List<TableEntity>(take);
        await foreach (var row in table.QueryAsync<TableEntity>(filter, maxPerPage: Math.Min(take, 1000), cancellationToken: cancellationToken))
        {
            rows.Add(row);
            if (rows.Count == take)
            {
                break;
            }
        }

        return rows;
    }

    public async Task DeleteAsync(string partition, string rowKey, CancellationToken cancellationToken)
    {
        try
        {
            await table.DeleteEntityAsync(partition, rowKey, ETag.All, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Already gone (another instance cleaned it).
        }
    }

    private async Task EnsureAsync(CancellationToken cancellationToken)
    {
        if (!_created)
        {
            await table.CreateIfNotExistsAsync(cancellationToken);
            _created = true;
        }
    }
}

/// <summary>The telemetry tables, by name.</summary>
public sealed record TelemetryTables(ITableRows ErrorReports, ITableRows OtherJobNotes)
{
    public const string ErrorReportsName = "ErrorReports";
    public const string OtherJobNotesName = "OtherJobNotes";

    /// <summary>The real tables in the storage account of <paramref name="connectionString"/>.</summary>
    public static TelemetryTables In(string connectionString)
    {
        var service = new TableServiceClient(connectionString);
        return new TelemetryTables(
            new AzureTableRows(service.GetTableClient(ErrorReportsName)),
            new AzureTableRows(service.GetTableClient(OtherJobNotesName)));
    }
}

/// <summary>Row keys that sort newest first, so a read of the first rows is a read of the newest.</summary>
public static class NewestFirst
{
    /// <summary>A key for <paramref name="at"/>: 19 digits counting down, then a random part so two rows never clash.</summary>
    public static string Key(DateTimeOffset at) => $"{Ticks(at)}-{Guid.NewGuid():N}";

    /// <summary>The key part of <paramref name="at"/>: every row older than it has a larger key.</summary>
    public static string Ticks(DateTimeOffset at) => (DateTimeOffset.MaxValue.UtcTicks - at.UtcTicks).ToString("D19");
}
