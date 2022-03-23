using Confluent.Kafka;
using MediatR;

namespace KedaWorker.Requests
{
    public class ProcessMessageRequest : IRequest
    {
        public ConsumeResult<string, string> Result { get; set; }

        public ProcessMessageRequest(ConsumeResult<string, string> result)
        {
            Result = result;
        }
    }
}
