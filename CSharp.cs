@page "/planner"

@using Quartz
@using Quartz.Impl.Matchers
@using Server.Core.Planner

@inject PlannerService PlannerService
@inject ISchedulerFactory SchedulerFactory
@inject IEnumerable<IPlannerAction> PlannerActions

<PageTitle>Планировщик</PageTitle>

<div class="planner-page">

    @* Header *@
    <div class="d-flex flex-column flex-lg-row
                justify-content-between align-items-lg-center
                gap-3 mb-4">

        <div>
            <div class="planner-label mb-1">SYSTEM</div>
            <h1 class="fw-bold mb-1">Планировщик</h1>
            <div class="text-body-secondary">
                Управление автоматическими заданиями и расписанием
            </div>
        </div>

        <button class="btn btn-primary px-4"
                @onclick="OpenCreateModal">
            <span class="me-2">＋</span>
            Новое задание
        </button>
    </div>


    @* Statistics *@
    <div class="row g-3 mb-4">

        <div class="col-6 col-xl-3">
            <div class="card planner-stat-card h-100">
                <div class="card-body">
                    <div class="text-body-secondary small mb-2">
                        Всего заданий
                    </div>

                    <div class="d-flex align-items-end justify-content-between">
                        <div class="planner-stat-value">
                            @Jobs.Count
                        </div>

                        <div class="planner-stat-icon">
                            ◫
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-6 col-xl-3">
            <div class="card planner-stat-card h-100">
                <div class="card-body">
                    <div class="text-body-secondary small mb-2">
                        Активных
                    </div>

                    <div class="d-flex align-items-end justify-content-between">
                        <div class="planner-stat-value">
                            @Jobs.Count(x => x.State == TriggerState.Normal)
                        </div>

                        <span class="status-dot bg-success"></span>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-6 col-xl-3">
            <div class="card planner-stat-card h-100">
                <div class="card-body">
                    <div class="text-body-secondary small mb-2">
                        Приостановлено
                    </div>

                    <div class="d-flex align-items-end justify-content-between">
                        <div class="planner-stat-value">
                            @Jobs.Count(x => x.State == TriggerState.Paused)
                        </div>

                        <span class="status-dot bg-warning"></span>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-6 col-xl-3">
            <div class="card planner-stat-card h-100">
                <div class="card-body">
                    <div class="text-body-secondary small mb-2">
                        Ошибки
                    </div>

                    <div class="d-flex align-items-end justify-content-between">
                        <div class="planner-stat-value">
                            @Jobs.Count(x => x.State == TriggerState.Error)
                        </div>

                        <span class="status-dot bg-danger"></span>
                    </div>
                </div>
            </div>
        </div>

    </div>


    @if (!string.IsNullOrWhiteSpace(Error))
    {
        <div class="alert alert-danger d-flex align-items-center"
             role="alert">
            <div>
                <strong>Ошибка.</strong>
                @Error
            </div>
        </div>
    }


    @* Main card *@
    <div class="card planner-main-card">

        @* Toolbar *@
        <div class="card-body border-bottom">
            <div class="row g-3">

                <div class="col-lg-7">
                    <div class="input-group">
                        <span class="input-group-text bg-body border-end-0">
                            ⌕
                        </span>

                        <input class="form-control border-start-0"
                               placeholder="Поиск по названию или действию..."
                               @bind="Search"
                               @bind:event="oninput" />
                    </div>
                </div>

                <div class="col-lg-3">
                    <select class="form-select"
                            @bind="StatusFilter">
                        <option value="">Все состояния</option>
                        <option value="Normal">Активные</option>
                        <option value="Paused">Приостановленные</option>
                        <option value="Error">Ошибки</option>
                        <option value="Complete">Завершённые</option>
                    </select>
                </div>

                <div class="col-lg-2">
                    <button class="btn btn-outline-secondary w-100"
                            disabled="@IsLoading"
                            @onclick="ReloadAsync">
                        @if (IsLoading)
                        {
                            <span class="spinner-border spinner-border-sm me-2"></span>
                        }

                        Обновить
                    </button>
                </div>

            </div>
        </div>


        @if (IsLoading && Jobs.Count == 0)
        {
            <div class="planner-empty">
                <div class="spinner-border text-primary mb-3"></div>
                <div>Загрузка заданий...</div>
            </div>
        }
        else if (!FilteredJobs.Any())
        {
            <div class="planner-empty">

                <div class="planner-empty-icon mb-3">
                    ◫
                </div>

                <h5 class="fw-semibold">
                    Заданий пока нет
                </h5>

                <p class="text-body-secondary mb-3">
                    Создайте первое задание и задайте для него расписание.
                </p>

                <button class="btn btn-primary"
                        @onclick="OpenCreateModal">
                    ＋ Создать задание
                </button>
            </div>
        }
        else
        {
            <div class="table-responsive">

                <table class="table planner-table align-middle mb-0">

                    <thead>
                    <tr>
                        <th>Задание</th>
                        <th>Действие</th>
                        <th>Расписание</th>
                        <th>Следующий запуск</th>
                        <th>Состояние</th>
                        <th class="text-end">Действия</th>
                    </tr>
                    </thead>

                    <tbody>

                    @foreach (var job in FilteredJobs)
                    {
                        <tr>

                            <td>
                                <div class="d-flex align-items-center gap-3">

                                    <div class="job-icon">
                                        ▶
                                    </div>

                                    <div>
                                        <div class="fw-semibold">
                                            @job.Name
                                        </div>

                                        <div class="small text-body-secondary font-monospace">
                                            @job.Id
                                        </div>
                                    </div>

                                </div>
                            </td>


                            <td>
                                <div class="fw-medium">
                                    @job.ActionName
                                </div>

                                <div class="small text-body-secondary font-monospace">
                                    @job.ActionKey
                                </div>
                            </td>


                            <td>
                                <span class="schedule-badge">
                                    @job.Schedule
                                </span>
                            </td>


                            <td>
                                @if (job.NextRun.HasValue)
                                {
                                    <div class="fw-medium">
                                        @job.NextRun.Value.ToString("dd.MM.yyyy")
                                    </div>

                                    <div class="small text-body-secondary">
                                        @job.NextRun.Value.ToString("HH:mm:ss")
                                    </div>
                                }
                                else
                                {
                                    <span class="text-body-secondary">
                                        —
                                    </span>
                                }
                            </td>


                            <td>
                                @StatusBadge(job.State)
                            </td>


                            <td>
                                <div class="d-flex justify-content-end gap-1">

                                    <button class="btn btn-sm btn-light action-button"
                                            title="Запустить сейчас"
                                            disabled="@IsBusy(job)"
                                            @onclick="() => RunNowAsync(job)">
                                        ▶
                                    </button>

                                    @if (job.State == TriggerState.Paused)
                                    {
                                        <button class="btn btn-sm btn-light action-button"
                                                title="Возобновить"
                                                disabled="@IsBusy(job)"
                                                @onclick="() => ResumeAsync(job)">
                                            ▷
                                        </button>
                                    }
                                    else
                                    {
                                        <button class="btn btn-sm btn-light action-button"
                                                title="Приостановить"
                                                disabled="@IsBusy(job)"
                                                @onclick="() => PauseAsync(job)">
                                            ‖
                                        </button>
                                    }

                                    <button class="btn btn-sm btn-light action-button text-danger"
                                            title="Удалить"
                                            disabled="@IsBusy(job)"
                                            @onclick="() => AskDelete(job)">
                                        ×
                                    </button>

                                </div>
                            </td>

                        </tr>
                    }

                    </tbody>

                </table>

            </div>
        }

    </div>
</div>


@* =========================================================
   CREATE MODAL
   ========================================================= *@

@if (ShowEditor)
{
    <div class="modal d-block planner-modal"
         tabindex="-1">

        <div class="modal-dialog modal-dialog-centered modal-lg">

            <div class="modal-content">

                <div class="modal-header border-bottom">
                    <div>
                        <h5 class="modal-title fw-bold mb-1">
                            Новое задание
                        </h5>

                        <div class="small text-body-secondary">
                            Настройте действие и расписание запуска
                        </div>
                    </div>

                    <button type="button"
                            class="btn-close"
                            @onclick="CloseEditor">
                    </button>
                </div>


                <EditForm Model="Editor"
                          OnValidSubmit="CreateAsync">

                    <DataAnnotationsValidator />

                    <div class="modal-body">

                        @if (!string.IsNullOrWhiteSpace(EditorError))
                        {
                            <div class="alert alert-danger">
                                @EditorError
                            </div>
                        }


                        <div class="mb-4">

                            <label class="form-label fw-semibold">
                                Название задания
                            </label>

                            <InputText class="form-control form-control-lg"
                                       placeholder="Например: Ночная очистка"
                                       @bind-Value="Editor.Name" />

                        </div>


                        <div class="mb-4">

                            <label class="form-label fw-semibold">
                                Действие
                            </label>

                            <InputSelect class="form-select"
                                         @bind-Value="Editor.ActionKey">

                                <option value="">
                                    Выберите действие...
                                </option>

                                @foreach (var action in PlannerActions.OrderBy(x => x.Name))
                                {
                                    <option value="@action.Key">
                                        @action.Name
                                    </option>
                                }

                            </InputSelect>

                            <div class="form-text">
                                Код, который будет выполнен планировщиком.
                            </div>

                        </div>


                        <div class="planner-section">
                            <div class="fw-semibold mb-3">
                                Расписание
                            </div>


                            <div class="row g-2 mb-4">

                                @foreach (var type in ScheduleTypes)
                                {
                                    <div class="col-6 col-lg-3">

                                        <button type="button"
                                                class="@GetScheduleButtonClass(type)"
                                                @onclick="() => Editor.ScheduleType = type">

                                            <span class="schedule-type-icon">
                                                @GetScheduleIcon(type)
                                            </span>

                                            <span>
                                                @GetScheduleName(type)
                                            </span>

                                        </button>

                                    </div>
                                }

                            </div>


                            @switch (Editor.ScheduleType)
                            {
                                case PlannerScheduleType.Once:

                                    <div class="row g-3">

                                        <div class="col-md-7">
                                            <label class="form-label">
                                                Дата
                                            </label>

                                            <InputDate class="form-control"
                                                       @bind-Value="Editor.RunDate" />
                                        </div>

                                        <div class="col-md-5">
                                            <label class="form-label">
                                                Время
                                            </label>

                                            <input type="time"
                                                   class="form-control"
                                                   @bind="Editor.RunTime" />
                                        </div>

                                    </div>

                                    break;


                                case PlannerScheduleType.Interval:

                                    <div class="row g-3">

                                        <div class="col-md-7">
                                            <label class="form-label">
                                                Интервал
                                            </label>

                                            <InputNumber class="form-control"
                                                         min="1"
                                                         @bind-Value="Editor.IntervalValue" />
                                        </div>

                                        <div class="col-md-5">
                                            <label class="form-label">
                                                Единица
                                            </label>

                                            <select class="form-select"
                                                    @bind="Editor.IntervalUnit">

                                                <option value="minutes">
                                                    Минут
                                                </option>

                                                <option value="hours">
                                                    Часов
                                                </option>

                                                <option value="days">
                                                    Дней
                                                </option>

                                            </select>
                                        </div>

                                    </div>

                                    break;


                                case PlannerScheduleType.Daily:

                                    <div>
                                        <label class="form-label">
                                            Время ежедневного запуска
                                        </label>

                                        <input type="time"
                                               class="form-control"
                                               style="max-width: 250px"
                                               @bind="Editor.DailyTime" />
                                    </div>

                                    break;


                                case PlannerScheduleType.Cron:

                                    <div>
                                        <label class="form-label">
                                            Cron expression
                                        </label>

                                        <InputText class="form-control font-monospace"
                                                   placeholder="0 0/5 * * * ?"
                                                   @bind-Value="Editor.Cron" />

                                        <div class="form-text">
                                            Например:
                                            <code>0 0/5 * * * ?</code>
                                            — каждые 5 минут
                                        </div>
                                    </div>

                                    break;
                            }

                        </div>


                        <div class="mt-4">

                            <label class="form-label fw-semibold">
                                Параметры
                            </label>

                            <InputTextArea class="form-control font-monospace"
                                           rows="4"
                                           placeholder="JSON, строка или другие параметры..."
                                           @bind-Value="Editor.Parameters" />

                            <div class="form-text">
                                Значение будет передано в
                                <code>IPlannerAction.ExecuteAsync()</code>.
                            </div>

                        </div>

                    </div>


                    <div class="modal-footer">

                        <button type="button"
                                class="btn btn-light"
                                @onclick="CloseEditor">
                            Отмена
                        </button>

                        <button type="submit"
                                class="btn btn-primary px-4"
                                disabled="@IsSaving">

                            @if (IsSaving)
                            {
                                <span class="spinner-border spinner-border-sm me-2"></span>
                            }

                            Создать задание
                        </button>

                    </div>

                </EditForm>

            </div>

        </div>

    </div>

    <div class="modal-backdrop fade show"></div>
}


@* =========================================================
   DELETE CONFIRMATION
   ========================================================= *@

@if (DeleteCandidate is not null)
{
    <div class="modal d-block planner-modal"
         tabindex="-1">

        <div class="modal-dialog modal-dialog-centered">

            <div class="modal-content">

                <div class="modal-body p-4">

                    <div class="delete-icon mb-3">
                        !
                    </div>

                    <h5 class="fw-bold">
                        Удалить задание?
                    </h5>

                    <p class="text-body-secondary">
                        Задание
                        <strong>@DeleteCandidate.Name</strong>
                        и связанное с ним расписание будут удалены.
                    </p>

                    <div class="d-flex justify-content-end gap-2 mt-4">

                        <button class="btn btn-light"
                                @onclick="CancelDelete">
                            Отмена
                        </button>

                        <button class="btn btn-danger"
                                @onclick="DeleteAsync">
                            Удалить
                        </button>

                    </div>

                </div>

            </div>

        </div>

    </div>

    <div class="modal-backdrop fade show"></div>
}


@code
{
    private const string PlannerGroup = "planner";

    private readonly List<PlannerJobViewModel> Jobs = [];

    private bool IsLoading;
    private bool IsSaving;

    private string? Error;
    private string? EditorError;
    private string? BusyJobId;

    private string Search = "";
    private string StatusFilter = "";

    private bool ShowEditor;

    private PlannerEditorModel Editor = new();

    private PlannerJobViewModel? DeleteCandidate;


    private static readonly PlannerScheduleType[] ScheduleTypes =
    [
        PlannerScheduleType.Once,
        PlannerScheduleType.Interval,
        PlannerScheduleType.Daily,
        PlannerScheduleType.Cron
    ];


    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }


    private IEnumerable<PlannerJobViewModel> FilteredJobs
    {
        get
        {
            IEnumerable<PlannerJobViewModel> result = Jobs;

            if (!string.IsNullOrWhiteSpace(Search))
            {
                result = result.Where(x =>
                    x.Name.Contains(
                        Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    x.ActionName.Contains(
                        Search,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    x.ActionKey.Contains(
                        Search,
                        StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                Enum.TryParse<TriggerState>(
                    StatusFilter,
                    out var state))
            {
                result = result.Where(x => x.State == state);
            }

            return result;
        }
    }


    private async Task ReloadAsync()
    {
        IsLoading = true;
        Error = null;

        try
        {
            var scheduler = await SchedulerFactory.GetScheduler();

            var keys = await scheduler.GetJobKeys(
                GroupMatcher<JobKey>.GroupEquals(PlannerGroup));

            Jobs.Clear();

            foreach (var key in keys)
            {
                var detail = await scheduler.GetJobDetail(key);

                if (detail is null)
                    continue;

                var triggers = await scheduler.GetTriggersOfJob(key);

                var trigger = triggers.FirstOrDefault();

                var state = trigger is null
                    ? TriggerState.None
                    : await scheduler.GetTriggerState(trigger.Key);

                var actionKey =
                    detail.JobDataMap.GetString("ActionKey") ?? "";

                var actionName =
                    PlannerActions.FirstOrDefault(
                        x => x.Key == actionKey)?.Name
                    ?? actionKey;

                Jobs.Add(new PlannerJobViewModel
                {
                    Id = key.Name,
                    Name = string.IsNullOrWhiteSpace(detail.Description)
                        ? key.Name
                        : detail.Description,

                    ActionKey = actionKey,
                    ActionName = actionName,

                    Schedule = trigger is null
                        ? "Без расписания"
                        : GetScheduleDescription(trigger),

                    NextRun = trigger?
                        .GetNextFireTimeUtc()?
                        .ToLocalTime(),

                    PreviousRun = trigger?
                        .GetPreviousFireTimeUtc()?
                        .ToLocalTime(),

                    State = state
                });
            }

            Jobs.Sort((a, b) =>
                Nullable.Compare(a.NextRun, b.NextRun));
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }


    private void OpenCreateModal()
    {
        EditorError = null;

        Editor = new PlannerEditorModel
        {
            RunDate = DateTime.Today,
            RunTime = DateTime.Now
                .AddMinutes(5)
                .ToString("HH:mm"),

            DailyTime = "03:00",

            IntervalValue = 5,
            IntervalUnit = "minutes",

            ScheduleType = PlannerScheduleType.Once
        };

        ShowEditor = true;
    }


    private void CloseEditor()
    {
        if (IsSaving)
            return;

        ShowEditor = false;
    }


    private async Task CreateAsync()
    {
        EditorError = null;

        if (string.IsNullOrWhiteSpace(Editor.Name))
        {
            EditorError = "Укажи название задания.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Editor.ActionKey))
        {
            EditorError = "Выбери действие.";
            return;
        }

        IsSaving = true;

        try
        {
            var request = new PlannerJobRequest
            {
                Name = Editor.Name.Trim(),
                ActionKey = Editor.ActionKey,
                ScheduleType = Editor.ScheduleType,
                Parameters = Editor.Parameters
            };


            switch (Editor.ScheduleType)
            {
                case PlannerScheduleType.Once:
                {
                    if (!TimeSpan.TryParse(
                            Editor.RunTime,
                            out var time))
                    {
                        EditorError = "Некорректное время.";
                        return;
                    }

                    var dateTime =
                        Editor.RunDate.Date + time;

                    dateTime = DateTime.SpecifyKind(
                        dateTime,
                        DateTimeKind.Local);

                    request.RunAt =
                        new DateTimeOffset(dateTime);

                    break;
                }


                case PlannerScheduleType.Interval:
                {
                    if (Editor.IntervalValue <= 0)
                    {
                        EditorError =
                            "Интервал должен быть больше нуля.";
                        return;
                    }

                    request.Interval =
                        Editor.IntervalUnit switch
                        {
                            "minutes" =>
                                TimeSpan.FromMinutes(
                                    Editor.IntervalValue),

                            "hours" =>
                                TimeSpan.FromHours(
                                    Editor.IntervalValue),

                            "days" =>
                                TimeSpan.FromDays(
                                    Editor.IntervalValue),

                            _ =>
                                throw new InvalidOperationException(
                                    "Unknown interval unit")
                        };

                    break;
                }


                case PlannerScheduleType.Daily:
                {
                    if (!TimeOnly.TryParse(
                            Editor.DailyTime,
                            out var dailyTime))
                    {
                        EditorError = "Некорректное время.";
                        return;
                    }

                    request.DailyTime = dailyTime;

                    break;
                }


                case PlannerScheduleType.Cron:
                {
                    if (string.IsNullOrWhiteSpace(Editor.Cron))
                    {
                        EditorError =
                            "Укажи Cron expression.";
                        return;
                    }

                    if (!CronExpression.IsValidExpression(
                            Editor.Cron))
                    {
                        EditorError =
                            "Некорректное Cron expression.";
                        return;
                    }

                    request.Cron = Editor.Cron.Trim();

                    break;
                }
            }


            await PlannerService.CreateAsync(request);

            ShowEditor = false;

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            EditorError = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }


    private async Task RunNowAsync(
        PlannerJobViewModel job)
    {
        await ExecuteJobCommand(
            job,
            () => PlannerService.RunNowAsync(
                Guid.Parse(job.Id)));
    }


    private async Task PauseAsync(
        PlannerJobViewModel job)
    {
        await ExecuteJobCommand(
            job,
            () => PlannerService.PauseAsync(
                Guid.Parse(job.Id)));
    }


    private async Task ResumeAsync(
        PlannerJobViewModel job)
    {
        await ExecuteJobCommand(
            job,
            () => PlannerService.ResumeAsync(
                Guid.Parse(job.Id)));
    }


    private async Task ExecuteJobCommand(
        PlannerJobViewModel job,
        Func<Task> command)
    {
        BusyJobId = job.Id;
        Error = null;

        try
        {
            await command();

            await ReloadAsync();
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
        finally
        {
            BusyJobId = null;
        }
    }


    private bool IsBusy(
        PlannerJobViewModel job)
    {
        return BusyJobId == job.Id;
    }


    private void AskDelete(
        PlannerJobViewModel job)
    {
        DeleteCandidate = job;
    }


    private void CancelDelete()
    {
        DeleteCandidate = null;
    }


    private async Task DeleteAsync()
    {
        if (DeleteCandidate is null)
            return;

        var job = DeleteCandidate;

        DeleteCandidate = null;

        await ExecuteJobCommand(
            job,
            () => PlannerService.DeleteAsync(
                Guid.Parse(job.Id)));
    }


    private static string GetScheduleDescription(
        ITrigger trigger)
    {
        if (trigger is ICronTrigger cron)
        {
            return $"Cron · {cron.CronExpressionString}";
        }

        if (trigger is ISimpleTrigger simple)
        {
            if (simple.RepeatCount == 0)
                return "Один раз";

            var interval = simple.RepeatInterval;

            if (interval.TotalDays >= 1 &&
                interval.TotalDays % 1 == 0)
            {
                return $"Каждые {interval.TotalDays:0} дн.";
            }

            if (interval.TotalHours >= 1 &&
                interval.TotalHours % 1 == 0)
            {
                return $"Каждые {interval.TotalHours:0} ч.";
            }

            if (interval.TotalMinutes >= 1)
            {
                return $"Каждые {interval.TotalMinutes:0} мин.";
            }

            return $"Каждые {interval.TotalSeconds:0} сек.";
        }

        return trigger.GetType().Name;
    }


    private static string GetScheduleName(
        PlannerScheduleType type)
    {
        return type switch
        {
            PlannerScheduleType.Once => "Один раз",
            PlannerScheduleType.Interval => "Интервал",
            PlannerScheduleType.Daily => "Ежедневно",
            PlannerScheduleType.Cron => "Cron",
            _ => type.ToString()
        };
    }


    private static string GetScheduleIcon(
        PlannerScheduleType type)
    {
        return type switch
        {
            PlannerScheduleType.Once => "◷",
            PlannerScheduleType.Interval => "↻",
            PlannerScheduleType.Daily => "☀",
            PlannerScheduleType.Cron => "</>",
            _ => ""
        };
    }


    private string GetScheduleButtonClass(
        PlannerScheduleType type)
    {
        return Editor.ScheduleType == type
            ? "schedule-type-button active"
            : "schedule-type-button";
    }


    private static RenderFragment StatusBadge(
        TriggerState state)
    {
        return builder =>
        {
            var (text, css) = state switch
            {
                TriggerState.Normal =>
                    ("Активно", "success"),

                TriggerState.Paused =>
                    ("Пауза", "warning"),

                TriggerState.Blocked =>
                    ("Выполняется", "primary"),

                TriggerState.Error =>
                    ("Ошибка", "danger"),

                TriggerState.Complete =>
                    ("Завершено", "secondary"),

                _ =>
                    (state.ToString(), "secondary")
            };

            builder.OpenElement(0, "span");

            builder.AddAttribute(
                1,
                "class",
                $"badge rounded-pill text-bg-{css} status-badge");

            builder.AddContent(2, text);

            builder.CloseElement();
        };
    }


    private sealed class PlannerEditorModel
    {
        public string Name { get; set; } = "";

        public string ActionKey { get; set; } = "";

        public PlannerScheduleType ScheduleType { get; set; }

        public DateTime RunDate { get; set; }

        public string RunTime { get; set; } = "12:00";

        public int IntervalValue { get; set; } = 5;

        public string IntervalUnit { get; set; } = "minutes";

        public string DailyTime { get; set; } = "03:00";

        public string? Cron { get; set; }

        public string? Parameters { get; set; }
    }


    private sealed class PlannerJobViewModel
    {
        public string Id { get; set; } = "";

        public string Name { get; set; } = "";

        public string ActionKey { get; set; } = "";

        public string ActionName { get; set; } = "";

        public string Schedule { get; set; } = "";

        public DateTimeOffset? NextRun { get; set; }

        public DateTimeOffset? PreviousRun { get; set; }

        public TriggerState State { get; set; }
    }
}

.planner-page {
    padding: 2rem 0 4rem;
}

.planner-label {
    font-size: .7rem;
    font-weight: 700;
    letter-spacing: .14em;
    color: var(--bs-primary);
}

.planner-stat-card,
.planner-main-card {
    border: 1px solid var(--bs-border-color);
    box-shadow: 0 .25rem 1.25rem rgba(0, 0, 0, .035);
}

.planner-stat-card {
    transition:
        transform .18s ease,
        box-shadow .18s ease;
}

.planner-stat-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 .5rem 1.5rem rgba(0, 0, 0, .07);
}

.planner-stat-value {
    font-size: 2rem;
    line-height: 1;
    font-weight: 700;
}

.planner-stat-icon {
    font-size: 1.4rem;
    opacity: .45;
}

.status-dot {
    display: block;
    width: .75rem;
    height: .75rem;
    border-radius: 50%;
    margin-bottom: .25rem;
}

.planner-main-card {
    overflow: hidden;
}

.planner-table thead th {
    padding: 1rem 1.25rem;
    font-size: .72rem;
    font-weight: 700;
    letter-spacing: .04em;
    text-transform: uppercase;
    color: var(--bs-secondary-color);
    background: var(--bs-tertiary-bg);
    border-bottom-width: 1px;
    white-space: nowrap;
}

.planner-table tbody td {
    padding: 1rem 1.25rem;
}

.planner-table tbody tr {
    transition: background-color .15s ease;
}

.planner-table tbody tr:hover {
    background: var(--bs-tertiary-bg);
}

.job-icon {
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;

    width: 2.6rem;
    height: 2.6rem;

    border-radius: .75rem;

    color: var(--bs-primary);
    background: rgba(var(--bs-primary-rgb), .1);

    font-size: .9rem;
}

.schedule-badge {
    display: inline-block;

    padding: .35rem .6rem;

    border-radius: .5rem;

    background: var(--bs-tertiary-bg);

    font-family: var(--bs-font-monospace);
    font-size: .78rem;

    white-space: nowrap;
}

.status-badge {
    padding: .45rem .7rem;
    font-weight: 600;
}

.action-button {
    display: inline-flex;
    align-items: center;
    justify-content: center;

    width: 2.1rem;
    height: 2.1rem;

    padding: 0;

    border: 1px solid var(--bs-border-color);

    font-size: .9rem;
}

.planner-empty {
    min-height: 340px;

    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;

    text-align: center;

    padding: 3rem;
}

.planner-empty-icon {
    display: flex;
    justify-content: center;
    align-items: center;

    width: 4rem;
    height: 4rem;

    border-radius: 1rem;

    background: var(--bs-tertiary-bg);

    font-size: 1.8rem;
    color: var(--bs-secondary-color);
}

.planner-modal {
    overflow-y: auto;
}

.modal-content {
    border: 0;
    border-radius: 1rem;

    box-shadow:
        0 1rem 3rem rgba(0, 0, 0, .2);
}

.modal-header,
.modal-footer {
    padding: 1.25rem 1.5rem;
}

.modal-body {
    padding: 1.5rem;
}

.planner-section {
    padding: 1.25rem;

    border: 1px solid var(--bs-border-color);
    border-radius: .8rem;

    background: var(--bs-tertiary-bg);
}

.schedule-type-button {
    width: 100%;
    min-height: 78px;

    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    gap: .4rem;

    padding: .65rem;

    border: 1px solid var(--bs-border-color);
    border-radius: .65rem;

    color: var(--bs-body-color);
    background: var(--bs-body-bg);

    font-size: .85rem;
    font-weight: 600;

    transition:
        border-color .15s ease,
        background-color .15s ease,
        transform .15s ease;
}

.schedule-type-button:hover {
    border-color: var(--bs-primary);
    transform: translateY(-1px);
}

.schedule-type-button.active {
    color: var(--bs-primary);
    border-color: var(--bs-primary);

    background:
        rgba(var(--bs-primary-rgb), .08);

    box-shadow:
        0 0 0 .15rem
        rgba(var(--bs-primary-rgb), .08);
}

.schedule-type-icon {
    font-size: 1.25rem;
}

.delete-icon {
    display: flex;
    align-items: center;
    justify-content: center;

    width: 3rem;
    height: 3rem;

    border-radius: .8rem;

    background: rgba(var(--bs-danger-rgb), .1);
    color: var(--bs-danger);

    font-size: 1.4rem;
    font-weight: 700;
}

@media (max-width: 767.98px) {

    .planner-page {
        padding-top: 1rem;
    }

    .planner-table tbody td {
        padding: .8rem;
    }

    .modal-dialog {
        margin: .75rem;
    }
}
