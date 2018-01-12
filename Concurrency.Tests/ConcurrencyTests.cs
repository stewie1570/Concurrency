using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Concurrency.Tests
{
    [TestClass]
    public class ConcurrencyTests
    {
        public class Log
        {
            public int Id { get; set; }
            public int NumTasksRunning { get; set; }
        }

        [TestMethod]
        public async Task ShouldRunAllTasks()
        {
            //Arrange
            var logs = new List<Log>();
            Func<Log, Task> addLog = log =>
            {
                logs.Add(log);
                return Task.FromResult(0);
            };

            var tasks = new List<Func<Task>>
            {
                async () => await addLog(new Log { Id = 1 }),
                async () => await addLog(new Log { Id = 2 }),
                async () => await addLog(new Log { Id = 3 }),
                async () => await addLog(new Log { Id = 4 }),
                async () => await addLog(new Log { Id = 5 })
            };

            //Act
            await Concurrency.WhenAll(tasks, maxConcurrency: 5);

            //Assert
            logs.ShouldBeEquivalentTo(new List<Log>
            {
                new Log { Id = 1 },
                new Log { Id = 2 },
                new Log { Id = 3 },
                new Log { Id = 4 },
                new Log { Id = 5 },
            });
        }

        [TestMethod]
        public async Task ShouldLimitConcurrency()
        {
            //Arrange
            var logs = new List<Log>();
            var numTasksRunning = 0;
            var taskCompletions = Enumerable
                .Range(start: 1, count: 10)
                .Select(i => new TaskCompletionSource<int>())
                .ToList();
            Func<int, Task> addLog = async taskId =>
            {
                numTasksRunning++;
                logs.Add(new Log { Id = taskId, NumTasksRunning = numTasksRunning });
                await taskCompletions[taskId - 1].Task;
                numTasksRunning--;
            };

            var tasks = new List<Func<Task>>
            {
                async () => await addLog(1),
                async () => await addLog(2),
                async () => await addLog(3),
                async () => await addLog(4),
                async () => await addLog(5),
                async () => await addLog(6),
                async () => await addLog(7),
                async () => await addLog(8),
                async () => await addLog(9),
                async () => await addLog(10)
            };

            //Act
            int maxConcurrency = 3;
            var whenAllComplete = Concurrency.WhenAll(tasks, maxConcurrency);
            taskCompletions.ForEach(taskCompletion => taskCompletion.SetResult(0));
            await whenAllComplete;

            //Assert
            logs
                .Max(log => log.NumTasksRunning)
                .Should()
                .Be(maxConcurrency, because: $"there should be up to {maxConcurrency} tasks in flight.");
        }

        [TestMethod]
        public async Task ShouldNotRepeatTasks()
        {
            //Arrange
            var logs = new List<Log>();
            var numTasksRunning = 0;
            var taskCompletions = Enumerable
                .Range(start: 1, count: 10)
                .Select(i => new TaskCompletionSource<int>())
                .ToList();
            Func<int, Task> addLog = async taskId =>
            {
                numTasksRunning++;
                logs.Add(new Log { Id = taskId, NumTasksRunning = numTasksRunning });
                await taskCompletions[taskId - 1].Task;
                numTasksRunning--;
            };

            var tasks = new List<Func<Task>>
            {
                async () => await addLog(1),
                async () => await addLog(2),
                async () => await addLog(3),
                async () => await addLog(4),
                async () => await addLog(5),
                async () => await addLog(6),
                async () => await addLog(7),
                async () => await addLog(8),
                async () => await addLog(9),
                async () => await addLog(10)
            };

            //Act
            int maxConcurrency = 3;
            var whenAllComplete = Concurrency.WhenAll(tasks, maxConcurrency);
            taskCompletions.ForEach(taskCompletion => taskCompletion.SetResult(0));
            await whenAllComplete;

            //Assert
            var repeats = logs
                .Select(log => new { Id = log.Id, Count = logs.Count(l => l.Id == log.Id) })
                .Where(result => result.Count > 1)
                .ToList();
            repeats.Count.Should().Be(0, because: $"there should be no repeats but: {string.Join(", ", repeats.Select(repeat => $"(Id: {repeat.Id}, Count: {repeat.Count})"))}.");
        }
    }
}
