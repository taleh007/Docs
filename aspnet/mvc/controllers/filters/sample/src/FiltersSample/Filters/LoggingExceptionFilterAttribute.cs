using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace FiltersSample.Filters
{
    public class LoggingExceptionFilterAttribute : ExceptionFilterAttribute
    {
        private readonly ILogger _logger;
        public LoggingExceptionFilterAttribute(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<LoggingExceptionFilterAttribute>();
        }

        public override void OnException(ExceptionContext context)
        {
            _logger.LogError(eventId:0,state:null,error:context.Exception);
            context.Exception = null; // mark exception as handled
        }
    }
}