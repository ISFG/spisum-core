using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Hosting;
using Timer = System.Timers.Timer;

namespace ISFG.SpisUm.Jobs.Services
{
    public abstract class CronJobService : IHostedService, IDisposable
    {
        #region Fields

        private readonly CronExpression _expression;
        private readonly TimeZoneInfo _timeZoneInfo;
        private Timer _timer;

        #endregion

        #region Constructors

        protected CronJobService(string cronExpression, TimeZoneInfo timeZoneInfo)
        {
            _expression = CronExpression.Parse(cronExpression);
            _timeZoneInfo = timeZoneInfo;
        }

        #endregion

        #region Implementation of IDisposable

        public virtual void Dispose()
        {
            _timer?.Dispose();
        }

        #endregion

        #region Implementation of IHostedService

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            await ScheduleJob(cancellationToken);
        }

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            await Task.CompletedTask;
        }

        #endregion

        #region Public Methods

        public virtual async Task DoWork(CancellationToken cancellationToken)
        {
            await Task.Delay(5000, cancellationToken);
        }

        #endregion

        #region Protected Methods

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken)
        {
            var next = _expression.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);
            
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.Now;
                _timer = new Timer(delay.TotalMilliseconds);
                _timer.Elapsed += async (sender, args) =>
                {
                    _timer.Dispose();
                    _timer = null;

                    if (!cancellationToken.IsCancellationRequested) await DoWork(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested) await ScheduleJob(cancellationToken);
                };
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        #endregion
    }
}