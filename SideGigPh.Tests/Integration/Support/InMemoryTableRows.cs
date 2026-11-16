using System.Collections.Concurrent;
using Azure.Data.Tables;
using SideGigPh.Api.Telemetry;

namespace SideGigPh.Tests.Integration.Support;

/// <summary>
/// One Azure Table Storage table in memory, like the real one where it matters: rows read in row-key order within a
/// partition, and adding a row whose keys exist fails.
/// </summary>
public sealed class InMemoryTableRows : ITableRows
{
    private readonly ConcurrentDictionary<(string Partition, string Row), TableEntity> _rows = new();

    public IReadOnlyList<TableEntity> All => [.. _rows.Values];

    public Task AddAsync(TableEntity row, CancellationToken cancellationToken) =>
        _rows.TryAdd((row.PartitionKey, row.RowKey), row)
            ? Task.CompletedTask
            : throw new InvalidOperationException($"Row {row.PartitionKey}/{row.RowKey} exists.");

    public Task<IReadOnlyList<TableEntity>> ReadAsync(string partition, string? afterRowKey, int take, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<TableEntity>>([.. _rows.Values
            .Where(r => r.PartitionKey == partition && (afterRowKey is null || string.CompareOrdinal(r.RowKey, afterRowKey) > 0))
            .OrderBy(r => r.RowKey, StringComparer.Ordinal)
            .Take(take)]);

    public Task DeleteAsync(string partition, string rowKey, CancellationToken cancellationToken)
    {
        _rows.TryRemove((partition, rowKey), out _);
        return Task.CompletedTask;
    }
}
