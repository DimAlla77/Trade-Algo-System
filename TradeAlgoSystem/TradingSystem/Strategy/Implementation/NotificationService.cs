namespace TradingSystem.Strategy.Implementation
{
    using TradingSystem.Core;
    using TradingSystem.Core.Models;

    public class NotificationService : INotificationService
    {
        private readonly Queue<ImbalanceSignal> _validationQueue = new();
        private readonly SemaphoreSlim _queueSemaphore = new(1, 1);

        public async Task SendAlertAsync(ImbalanceSignal signal)
        {
            // Implement your notification logic here
            // Could be: Email, Telegram, Discord, etc.

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"🚨 CRITICAL ALERT: {signal.Type}");
            Console.WriteLine($"   Description: {signal.Description}");
            Console.WriteLine($"   Confidence: {signal.ConfidenceLevel:P}");
            Console.WriteLine($"   Timestamp: {signal.Timestamp:yyyy-MM-dd HH:mm:ss}");

            foreach (var metric in signal.Metrics)
            {
                Console.WriteLine($"   {metric.Key}: {metric.Value:F2}");
            }

            Console.ResetColor();

            await Task.CompletedTask;
        }

        public async Task QueueForValidationAsync(ImbalanceSignal signal)
        {
            await _queueSemaphore.WaitAsync();
            try
            {
                _validationQueue.Enqueue(signal);

                // Process validation queue if it gets too large
                if (_validationQueue.Count > 10)
                {
                    await ProcessValidationQueue();
                }
            }
            finally
            {
                _queueSemaphore.Release();
            }
        }

        private async Task ProcessValidationQueue()
        {
            var signalsToValidate = new List<ImbalanceSignal>();

            while (_validationQueue.Count > 0)
            {
                signalsToValidate.Add(_validationQueue.Dequeue());
            }

            // Group similar signals
            var groupedSignals = signalsToValidate
                .GroupBy(s => s.Type)
                .Where(g => g.Count() >= 2); // Need at least 2 similar signals

            foreach (var group in groupedSignals)
            {
                var strongestSignal = group.OrderByDescending(s => s.ConfidenceLevel).First();

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚡ VALIDATED SIGNAL: {strongestSignal.Type}");
                Console.WriteLine($"   Occurrences: {group.Count()}");
                Console.WriteLine($"   Max Confidence: {strongestSignal.ConfidenceLevel:P}");
                Console.ResetColor();

                await Task.CompletedTask;
            }
        }
    }
}
