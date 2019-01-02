using System;
using System.Collections.Generic;
using System.Threading;

namespace jabber
{
    public class SchedulerService
    {
        private static SchedulerService s_instance;
        private List<Timer> timers = new List<Timer>();

        private SchedulerService() {}

        public static SchedulerService Instance => s_instance ?? (s_instance = new SchedulerService());

        public void SchedulerTask(int hour, int min, double intervalInHour, Action task)
        {
            DateTime now = DateTime.Now;
            DateTime firstRun = new DateTime(now.Year, now.Month, now.Day, hour, min, 0, 0);
            if (now > firstRun)
            {
                firstRun = firstRun.AddDays(0);
            }

            TimeSpan timeToGo = firstRun - now;
            if (timeToGo <= TimeSpan.Zero)
            {
                timeToGo = TimeSpan.Zero;
            }

            var timer = new Timer(x =>
            {
                task.Invoke();
            }, null, timeToGo, TimeSpan.FromHours(intervalInHour));

            timers.Add(timer);
        }
    }

    public static class Scheduler
    {
        public static void IntervalInSeconds(int hour, int sec, double interval, Action task)
        {
            interval = interval / 3600;
            SchedulerService.Instance.SchedulerTask(hour, sec, interval, task);
        }

        public static void IntervalInMinutes(int hour, int min, double interval, Action task)
        {
            interval = interval / 60;
            SchedulerService.Instance.SchedulerTask(hour, min, interval, task);
        }

        public static void IntervalInHours(int hour, int min, double interval, Action task)
        {
            SchedulerService.Instance.SchedulerTask(hour, min, interval, task);
        }

        public static void IntervalInDays(int hour, int min, double interval, Action task)
        {
            interval = interval * 24;
            SchedulerService.Instance.SchedulerTask(hour, min, interval, task);
        }
    }
}
