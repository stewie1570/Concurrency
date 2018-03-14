# Concurrency

[![Build](http://stewiebuild.cloudapp.net:8080/app/rest/builds/buildType:Concurrency_Build/statusIcon.svg)](http://stewiebuild.cloudapp.net:8080/project.html?projectId=Concurrency&tab=projectOverview)
[![NuGet version](https://badge.fury.io/nu/Concurrency.svg)](https://badge.fury.io/nu/Concurrency)

Similar to Task.WhenAll but allows you to limit the number of tasks in flight (max concurrency).

**NOTE**: This has been written to run concurrently (not parallel). It uses a single enumerator concurrently to enumerate and run the tasks. As such, if you want to run in parallel and limit the parallelism I recommend:

```csharp
Parallel.ForEach(
    someList,
    new ParallelOptions { MaxDegreeOfParallelism = 4 },
    item => { /*...*/ }
);
```

Here is some example usage from some unit tests:

```csharp
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
    await ConcurrentTask.WhenAll(tasks, maxConcurrency: 5);

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
    var whenAllComplete = ConcurrentTask.WhenAll(tasks, maxConcurrency);
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
    var whenAllComplete = ConcurrentTask.WhenAll(tasks, maxConcurrency);
    taskCompletions.ForEach(taskCompletion => taskCompletion.SetResult(0));
    await whenAllComplete;

    //Assert
    var repeats = logs
        .Select(log => new { Id = log.Id, Count = logs.Count(l => l.Id == log.Id) })
        .Where(result => result.Count > 1)
        .ToList();
    repeats.Count.Should().Be(0, because: $"there should be no repeats but: {string.Join(", ", repeats.Select(repeat => $"(Id: {repeat.Id}, Count: {repeat.Count})"))}.");
}
```
