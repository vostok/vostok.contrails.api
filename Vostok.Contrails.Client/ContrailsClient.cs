﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using Cassandra.Data.Linq;
using Newtonsoft.Json;
using Vostok.Logging;
using Vostok.Tracing;

namespace Vostok.Contrails.Client
{
    public class ContrailsClientSettings
    {
        public CassandraRetryExecutionStrategySettings CassandraRetryExecutionStrategySettings { get; set; }
        public IEnumerable<string> CassandraNodes { get; set; }
        public string Keyspace { get; set; }
    }

    public interface IContrailsClient
    {
        Task AddSpan(Span span);
        Task<IEnumerable<Span>> GetTracesById(Guid traceId, int limit = 1000, Tuple<DateTimeOffset, Guid> offset = null);
    }

    //public class TracesOffset

    public class ContrailsClient : IDisposable, IContrailsClient
    {
        private readonly ICassandraDataScheme dataScheme;
        private readonly ICassandraRetryExecutionStrategy retryExecutionStrategy;
        private readonly CassandraSessionKeeper cassandraSessionKeeper;
        private readonly JsonSerializer jsonSerializer;

        public ContrailsClient(ContrailsClientSettings settings, ILog log)
        {
            cassandraSessionKeeper = new CassandraSessionKeeper(settings.CassandraNodes, settings.Keyspace);
            retryExecutionStrategy = new CassandraRetryExecutionStrategy(settings.CassandraRetryExecutionStrategySettings, log, cassandraSessionKeeper.Session);
            dataScheme = new CassandraDataScheme(cassandraSessionKeeper.Session);
            dataScheme.CreateTableIfNotExists();
            jsonSerializer = new JsonSerializer();
        }

        public async Task AddSpan(Span span)
        {
            await retryExecutionStrategy.ExecuteAsync(dataScheme.GetInsertStatement(span));
        }

        public async Task<IEnumerable<Span>> GetTracesById(Guid traceId, int limit = 1000, Tuple<DateTimeOffset, Guid> offset = null)
        {
            var query = dataScheme.Table.Where(x => x.TraceId == traceId);
            if (offset != null)
                query = query.Where(x => x.BeginTimestamp > offset.Item1 && x.SpanId.CompareTo(offset.Item2) > 0);
            var spans = await query.OrderBy(x => x.BeginTimestamp).ThenBy(x => x.SpanId).Take(limit).ExecuteAsync();
            return spans.Select(x => new Span
            {
                SpanId = x.SpanId,
                TraceId = x.TraceId,
                BeginTimestamp = x.BeginTimestamp,
                EndTimestamp = x.EndTimestamp,
                ParentSpanId = x.ParentSpanId,
                Annotations = string.IsNullOrWhiteSpace(x.Annotations) ? null : jsonSerializer.Deserialize<Dictionary<string, string>>(new JsonTextReader(new StringReader(x.Annotations)))
            });
        }

        public void Dispose()
        {
            cassandraSessionKeeper.Dispose();
        }
    }
}