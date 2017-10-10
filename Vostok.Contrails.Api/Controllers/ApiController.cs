﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Vostok.Contrails.Client;
using Vostok.Tracing;

namespace Vostok.Contrails.Api.Controllers
{
    public class TracesByIdResponce
    {
        public Guid TraceId { get; set; }
        public IEnumerable<Span> Spans { get; set; }
    }

    public class ApiController : Controller
    {
        private readonly IContrailsClient contrailsClient;

        public ApiController(IContrailsClient contrailsClient)
        {
            this.contrailsClient = contrailsClient;
        }

        [HttpGet]
        [Route("api/findTrace")]
        public async Task<TracesByIdResponce> TracesById(Guid traceId, [Bind(Prefix = "fromTs")] DateTimeOffset? fromTimestamp, Guid? fromSpan, [Bind(Prefix = "toTs")]DateTimeOffset? toTimestamp, Guid? toSpan, int limit = 1000, bool ascending = true)
        {
            if (traceId == Guid.Empty)
                return new TracesByIdResponce {TraceId = traceId, Spans = new Span[] {}};
            var spans = await contrailsClient.GetTracesById(traceId, fromTimestamp, fromSpan, toTimestamp, toSpan, ascending, limit);
            return new TracesByIdResponce { TraceId = traceId, Spans = spans };
        }
    }
}
