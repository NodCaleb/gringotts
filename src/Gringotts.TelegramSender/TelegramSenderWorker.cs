using Microsoft.Extensions.Hosting;

namespace Gringotts.TelegramSender;

internal class TelegramSenderWorker : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}
