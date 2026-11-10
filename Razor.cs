public async Task UpdateAsync(
    Guid id,
    PlannerJobRequest request,
    CancellationToken cancellationToken = default)
{
    var entity =
        await _db.PlannerTasks
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

    if (entity is null)
    {
        throw new InvalidOperationException(
            $"Planner task {id} not found.");
    }


    entity.Name =
        request.Name;

    entity.ActionKey =
        request.ActionKey;

    entity.Parameters =
        request.Parameters;

    entity.ScheduleType =
        request.ScheduleType;


    // Сначала очищаем поля старого типа расписания.
    entity.RunAt = null;
    entity.IntervalTicks = null;
    entity.DailyTime = null;
    entity.Cron = null;


    switch (request.ScheduleType)
    {
        case PlannerScheduleType.Once:
        {
            entity.RunAt =
                request.RunAt
                ?? throw new InvalidOperationException(
                    "RunAt is required.");

            break;
        }


        case PlannerScheduleType.Interval:
        {
            entity.IntervalTicks =
                request.Interval?.Ticks
                ?? throw new InvalidOperationException(
                    "Interval is required.");

            break;
        }


        case PlannerScheduleType.Daily:
        {
            entity.DailyTime =
                request.DailyTime?.ToTimeSpan()
                ?? throw new InvalidOperationException(
                    "DailyTime is required.");

            break;
        }


        case PlannerScheduleType.Cron:
        {
            entity.Cron =
                request.Cron
                ?? throw new InvalidOperationException(
                    "Cron is required.");

            break;
        }


        default:
            throw new ArgumentOutOfRangeException();
    }


    entity.UpdatedAt =
        DateTimeOffset.Now;


    await _db.SaveChangesAsync(
        cancellationToken);


    var scheduler =
        await _schedulerFactory.GetScheduler(
            cancellationToken);


    var jobKey =
        new JobKey(
            entity.Id.ToString(),
            Group);


    if (entity.Enabled)
    {
        // ScheduleAsync должен удалить старый Quartz job
        // и создать его заново из entity.
        await ScheduleAsync(
            entity,
            cancellationToken);
    }
    else
    {
        // Если задача находится на паузе,
        // обновляем только БД.
        //
        // Старый Quartz job лучше удалить,
        // чтобы старое расписание случайно
        // не осталось активным.

        if (await scheduler.CheckExists(
                jobKey,
                cancellationToken))
        {
            await scheduler.DeleteJob(
                jobKey,
                cancellationToken);
        }
    }
}
