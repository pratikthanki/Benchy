using Microsoft.Extensions.Logging;

namespace Benchy.Helpers
{
    public interface IStageHandler
    {

    }

    public class StageHandler : IStageHandler
    {
        private readonly ILogger<StageHandler> _logger;
        
        public int Requests { get; set; }
        public int VirtualUsers { get; set; }

        public StageHandler(ILogger<StageHandler> logger)
        {
            _logger = logger;
        }
    }
}