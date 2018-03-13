using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Concurrency.Tests
{
    [TestClass]
    public class GenericConcurrencyTests
    {
        public class Log
        {
            public int Id { get; set; }
            public int NumTasksRunning { get; set; }
        }

        [TestMethod]
        public async Task ShouldLimitConcurrency()
        {
            //Arrange
            var numTasksRunning = 0;
            var taskCompletions = Enumerable
                .Range(start: 1, count: 10)
                .Select(i => new TaskCompletionSource<int>())
                .ToList();
            Func<int, Task<int>> resultFromTaskId = async taskId =>
            {
                numTasksRunning++;
                var taskCount = numTasksRunning;
                await taskCompletions[taskId - 1].Task;
                numTasksRunning--;

                return taskCount;
            };

            var tasks = new List<Func<Task<int>>>
            {
                async () => await resultFromTaskId(1),
                async () => await resultFromTaskId(4),
                async () => await resultFromTaskId(6),
                async () => await resultFromTaskId(3),
                async () => await resultFromTaskId(5),
                async () => await resultFromTaskId(7),
                async () => await resultFromTaskId(2),
                async () => await resultFromTaskId(9),
                async () => await resultFromTaskId(10),
                async () => await resultFromTaskId(8)
            };

            //Act
            int maxConcurrency = 3;
            var getTaskCounts = ConcurrentTask.WhenAll(tasks, maxConcurrency);
            taskCompletions.ForEach(taskCompletion => taskCompletion.SetResult(0));
            var taskCounts = await getTaskCounts;

            //Assert
            taskCounts.Max().Should().Be(maxConcurrency, because: $"there should be up to {maxConcurrency} tasks in flight.");
        }

        [TestMethod]
        public async Task ShouldReturnTasksInOrderDespiteWhenTheyResolve()
        {
            //Arrange
            var taskCompletions = Enumerable
                .Range(start: 1, count: 10)
                .Select(i => new TaskCompletionSource<int>())
                .ToList();
            Func<int, Task<int>> getResultFromTaskId = async taskId =>
            {
                await taskCompletions[taskId - 1].Task;
                return taskId;
            };

            var tasks = Enumerable
                .Range(start: 1, count: 10)
                .Select(index => ((Func<Task<int>>)(async () => await getResultFromTaskId(index))));

            //Act
            int maxConcurrency = 3;
            var getResults = ConcurrentTask.WhenAll(tasks, maxConcurrency);
            taskCompletions[4].SetResult(0);
            taskCompletions[1].SetResult(0);
            taskCompletions[0].SetResult(0);
            taskCompletions[9].SetResult(0);
            taskCompletions[2].SetResult(0);
            taskCompletions[6].SetResult(0);
            taskCompletions[8].SetResult(0);
            taskCompletions[3].SetResult(0);
            taskCompletions[7].SetResult(0);
            taskCompletions[5].SetResult(0);
            var results = await getResults;

            //Assert
            results.ShouldBeEquivalentTo(new int[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            }, ops => ops.WithStrictOrdering());
        }
    }
}
