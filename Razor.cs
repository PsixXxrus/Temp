@page "/planner"

@using Quartz
@using Server.Core.Planner
@using Server.Core.Planner.Data

@inject PlannerService PlannerService
@inject ISchedulerFactory SchedulerFactory
@inject IEnumerable<IPlannerAction> PlannerActions

<PageTitle>Планировщик</PageTitle>

<div class="planner-page">

    @* =====================================================
       HEADER
       ===================================================== *@

    <div class="planner-header">
        <div>
            <div class="planner-overline">
                AUTOMATION
            </div>

            <h1 class="planner-title">
                Планировщик
            </h1>

            <div class="planner-subtitle">
                Управление автоматическими заданиями и расписанием
            </div>
        </div>

        <div class="d-flex gap-2">
            <button type="button"
                    class="btn btn-outline-secondary"
                    disabled="@IsLoading"
                    @onclick="ReloadAsync">

                @if (IsLoading)
                {
                    <span class="spinner-border spinner-border-sm me-2"></span>
                }
                else
                {
                    <span class="me-1">↻</span>
                }

                Обновить
            </button>

            <button type="button"
                    class="btn btn-primary px-4"
                    @onclick="OpenCreateModal">
                <span class="me-1">＋</span>
                Новое задание
            </button>
        </div>
    </div>


    @* =====================================================
       STATISTICS
       ===================================================== *@

    <div class="row g-3 mb-4">

        <div class="col-6 col-xl-3">
            <div class="planner-stat-card">
                <div>
                    <div class="planner-stat-label">
                        Всего
                    </div>

                    <div class="planner-stat-value">
                        @Jobs.Count
                    </div>
                </div>

                <div class="planner-stat-icon">
                    ◫
                </div>
            </div>
        </div>

        <div class="col-6 col-xl-3">
            <div class="planner-stat-card">
                <div>
                    <div class="planner-stat-label">
                        Активных
                    </div>

                    <div class="planner-stat-value">
                        @Jobs.Count(x =>
                            GetUiStatus(x) == PlannerUiStatus.Active ||
                            GetUiStatus(x) == PlannerUiStatus.Running)
                    </div>
                </div>

                <div class="planner-stat-state success"></div>
            </div>
        </div>

        <div class="col-6 col-xl-3">
            <div class="planner-stat-card">
                <div>
                    <div class="planner-stat-label">
                        На паузе
                    </div>

                    <div class="planner-stat-value">
                        @Jobs.Count(x =>
                            GetUiStatus(x) == PlannerUiStatus.Paused)
                    </div>
                </div>

                <div class="planner-stat-state warning"></div>
            </div>
        </div>

        <div class="col-6 col-xl-3">
            <div class="planner-stat-card">
                <div>
                    <div class="planner-stat-label">
                        С ошибкой
                    </div>

                    <div class="planner-stat-value">
                        @Jobs.Count(x =>
                            GetUiStatus(x) == PlannerUiStatus.Error)
                    </div>
                </div>

                <div class="planner-stat-state danger"></div>
            </div>
        </div>

    </div>


    @if (!string.IsNullOrWhiteSpace(Error))
    {
        <div class="alert alert-danger planner-alert">
            <div class="fw-semibold mb-1">
                Произошла ошибка
            </div>

            <div>
                @Error
            </div>
        </div>
    }


    @* =====================================================
       MAIN CARD
       ===================================================== *@

    <div class="planner-card">

        <div class="planner-toolbar">

            <div class="planner-search">
                <span class="planner-search-icon">
                    ⌕
                </span>

                <input class="form-control planner-search-input"
                       placeholder="Поиск по названию, действию или Cron..."
                       @bind="Search"
                       @bind:event="oninput" />
            </div>

            <select class="form-select planner-filter"
                    @bind="StatusFilter">

                <option value="">
                    Все состояния
                </option>

                <option value="Active">
                    Активные
                </option>

                <option value="Paused">
                    На паузе
                </option>

                <option value="Error">
                    Ошибки
                </option>

                <option value="Completed">
                    Завершённые
                </option>

                <option value="Missing">
                    Не загружены
                </option>

            </select>

        </div>


        @if (IsLoading && Jobs.Count == 0)
        {
            <div class="planner-empty">

                <div class="spinner-border text-primary mb-3"></div>

                <div class="fw-semibold">
                    Загрузка планировщика
                </div>

            </div>
        }
        else if (!FilteredJobs.Any())
        {
            <div class="planner-empty">

                <div class="planner-empty-icon">
                    ◫
                </div>

                <h5 class="fw-bold mt-3">
                    Заданий не найдено
                </h5>

                <div class="text-body-secondary mb-4">
                    Создайте новое задание или измените параметры поиска.
                </div>

                @if (Jobs.Count == 0)
                {
                    <button type="button"
                            class="btn btn-primary"
                            @onclick="OpenCreateModal">
                        ＋ Создать первое задание
                    </button>
                }

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
                            <th>Последний запуск</th>
                            <th>Следующий запуск</th>
                            <th>Состояние</th>
                            <th class="text-end">
                                Действия
                            </th>
                        </tr>
                    </thead>

                    <tbody>

                        @foreach (var job in FilteredJobs)
                        {
                            var status = GetUiStatus(job);

                            <tr @key="job.Id">

                                @* NAME *@
                                <td>
                                    <div class="d-flex align-items-center gap-3">

                                        <div class="planner-job-icon">
                                            @GetJobIcon(job.ScheduleType)
                                        </div>

                                        <div>
                                            <div class="planner-job-name">
                                                @job.Name
                                            </div>

                                            <div class="planner-job-id">
                                                @job.Id
                                            </div>
                                        </div>

                                    </div>
                                </td>


                                @* ACTION *@
                                <td>
                                    <div class="fw-semibold">
                                        @job.ActionName
                                    </div>

                                    <div class="planner-secondary-text font-monospace">
                                        @job.ActionKey
                                    </div>
                                </td>


                                @* SCHEDULE *@
                                <td>
                                    <div class="planner-schedule">
                                        @GetScheduleDescription(job)
                                    </div>

                                    <div class="planner-secondary-text mt-1">
                                        @GetScheduleTypeName(job.ScheduleType)
                                    </div>
                                </td>


                                @* LAST RUN *@
                                <td>
                                    @if (job.LastRunAt.HasValue)
                                    {
                                        <div class="fw-medium">
                                            @job.LastRunAt.Value
                                                .ToLocalTime()
                                                .ToString("dd.MM.yyyy")
                                        </div>

                                        <div class="planner-secondary-text">
                                            @job.LastRunAt.Value
                                                .ToLocalTime()
                                                .ToString("HH:mm:ss")
                                        </div>
                                    }
                                    else
                                    {
                                        <span class="planner-muted">
                                            Никогда
                                        </span>
                                    }
                                </td>


                                @* NEXT RUN *@
                                <td>
                                    @if (job.NextRun.HasValue)
                                    {
                                        <div class="fw-medium">
                                            @job.NextRun.Value
                                                .ToLocalTime()
                                                .ToString("dd.MM.yyyy")
                                        </div>

                                        <div class="planner-secondary-text">
                                            @job.NextRun.Value
                                                .ToLocalTime()
                                                .ToString("HH:mm:ss")
                                        </div>
                                    }
                                    else
                                    {
                                        <span class="planner-muted">
                                            —
                                        </span>
                                    }
                                </td>


                                @* STATUS *@
                                <td>
                                    <span class="@GetStatusClass(status)">
                                        <span class="planner-status-dot"></span>
                                        @GetStatusText(status)
                                    </span>

                                    @if (status == PlannerUiStatus.Error &&
                                         !string.IsNullOrWhiteSpace(job.LastError))
                                    {
                                        <div class="planner-error-preview"
                                             title="@job.LastError">
                                            @GetShortError(job.LastError)
                                        </div>
                                    }
                                </td>


                                @* ACTION BUTTONS *@
                                <td>

                                    <div class="planner-actions">

                                        <button type="button"
                                                class="planner-action-button"
                                                title="Запустить сейчас"
                                                disabled="@IsBusy(job)"
                                                @onclick="() => RunNowAsync(job)">
                                            ▶
                                        </button>


                                        @if (job.Enabled)
                                        {
                                            <button type="button"
                                                    class="planner-action-button"
                                                    title="Приостановить"
                                                    disabled="@IsBusy(job)"
                                                    @onclick="() => PauseAsync(job)">
                                                ‖
                                            </button>
                                        }
                                        else
                                        {
                                            <button type="button"
                                                    class="planner-action-button resume"
                                                    title="Возобновить"
                                                    disabled="@IsBusy(job)"
                                                    @onclick="() => ResumeAsync(job)">
                                                ▷
                                            </button>
                                        }


                                        <button type="button"
                                                class="planner-action-button"
                                                title="Редактировать"
                                                disabled="@IsBusy(job)"
                                                @onclick="() => OpenEditModal(job)">
                                            ✎
                                        </button>


                                        <button type="button"
                                                class="planner-action-button danger"
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
   CREATE / EDIT MODAL
   ========================================================= *@

@if (ShowEditor)
{
    <div class="modal d-block planner-modal"
         tabindex="-1">

        <div class="modal-dialog modal-dialog-centered modal-lg modal-dialog-scrollable">

            <div class="modal-content planner-modal-content">

                <div class="modal-header planner-modal-header">

                    <div>
                        <div class="planner-modal-overline">
                            @(IsEditing ? "EDIT TASK" : "NEW TASK")
                        </div>

                        <h5 class="modal-title fw-bold">
                            @(IsEditing
                                ? "Редактирование задания"
                                : "Новое задание")
                        </h5>
                    </div>

                    <button type="button"
                            class="btn-close"
                            disabled="@IsSaving"
                            @onclick="CloseEditor">
                    </button>

                </div>


                <EditForm Model="@Editor"
                          OnValidSubmit="SaveAsync">

                    <DataAnnotationsValidator />


                    <div class="modal-body planner-modal-body">

                        @if (!string.IsNullOrWhiteSpace(EditorError))
                        {
                            <div class="alert alert-danger">
                                @EditorError
                            </div>
                        }


                        @if (IsEditing)
                        {
                            <div class="planner-edit-info mb-4">

                                <div>
                                    <span class="planner-edit-info-label">
                                        ID
                                    </span>

                                    <span class="font-monospace">
                                        @Editor.Id
                                    </span>
                                </div>

                                @if (Editor.CreatedAt.HasValue)
                                {
                                    <div>
                                        <span class="planner-edit-info-label">
                                            Создано
                                        </span>

                                        @Editor.CreatedAt.Value
                                            .ToLocalTime()
                                            .ToString("dd.MM.yyyy HH:mm")
                                    </div>
                                }

                            </div>
                        }


                        @* NAME *@

                        <div class="mb-4">

                            <label class="form-label planner-form-label">
                                Название задания
                            </label>

                            <InputText class="form-control form-control-lg"
                                       placeholder="Например: Ночная очистка"
                                       @bind-Value="Editor.Name" />

                        </div>


                        @* ACTION *@

                        <div class="mb-4">

                            <label class="form-label planner-form-label">
                                Действие
                            </label>

                            <InputSelect class="form-select"
                                         @bind-Value="Editor.ActionKey">

                                <option value="">
                                    Выберите действие...
                                </option>

                                @if (!string.IsNullOrWhiteSpace(Editor.ActionKey) &&
                                     !PlannerActions.Any(x =>
                                         x.Key == Editor.ActionKey))
                                {
                                    <option value="@Editor.ActionKey">
                                        @Editor.ActionKey (не зарегистрировано)
                                    </option>
                                }

                                @foreach (var action in
                                    PlannerActions.OrderBy(x => x.Name))
                                {
                                    <option value="@action.Key">
                                        @action.Name
                                    </option>
                                }

                            </InputSelect>

                            <div class="form-text">
                                Зарегистрированный
                                <code>IPlannerAction</code>,
                                который будет выполнен.
                            </div>

                        </div>


                        @* SCHEDULE *@

                        <div class="planner-form-section">

                            <div class="planner-section-title">
                                Расписание
                            </div>

                            <div class="planner-schedule-types">

                                @foreach (var type in ScheduleTypes)
                                {
                                    <button type="button"
                                            class="@GetScheduleButtonClass(type)"
                                            @onclick="() => SelectScheduleType(type)">

                                        <span class="planner-schedule-type-icon">
                                            @GetJobIcon(type)
                                        </span>

                                        <span>
                                            @GetScheduleTypeName(type)
                                        </span>

                                    </button>
                                }

                            </div>


                            <div class="planner-schedule-editor">

                                @switch (Editor.ScheduleType)
                                {
                                    case PlannerScheduleType.Once:
                                    {
                                        <div class="row g-3">

                                            <div class="col-md-7">

                                                <label class="form-label">
                                                    Дата запуска
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
                                    }


                                    case PlannerScheduleType.Interval:
                                    {
                                        <div class="row g-3">

                                            <div class="col-md-7">

                                                <label class="form-label">
                                                    Запускать каждые
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

                                                    <option value="seconds">
                                                        Секунд
                                                    </option>

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
                                    }


                                    case PlannerScheduleType.Daily:
                                    {
                                        <div>

                                            <label class="form-label">
                                                Время ежедневного запуска
                                            </label>

                                            <input type="time"
                                                   class="form-control planner-time-input"
                                                   @bind="Editor.DailyTime" />

                                        </div>

                                        break;
                                    }


                                    case PlannerScheduleType.Cron:
                                    {
                                        <div>

                                            <label class="form-label">
                                                Cron expression
                                            </label>

                                            <InputText class="form-control font-monospace"
                                                       placeholder="0 0/5 * * * ?"
                                                       @bind-Value="Editor.Cron" />

                                            <div class="form-text mt-2">
                                                Например
                                                <code>0 0/5 * * * ?</code>
                                                — каждые 5 минут.
                                            </div>

                                        </div>

                                        break;
                                    }
                                }

                            </div>

                        </div>


                        @* PARAMETERS *@

                        <div class="mt-4">

                            <label class="form-label planner-form-label">
                                Параметры
                            </label>

                            <InputTextArea class="form-control font-monospace"
                                           rows="5"
                                           placeholder="JSON, строка или другие параметры..."
                                           @bind-Value="Editor.Parameters" />

                            <div class="form-text">
                                Передаются в
                                <code>IPlannerAction.ExecuteAsync()</code>.
                            </div>

                        </div>


                        @if (IsEditing &&
                             !string.IsNullOrWhiteSpace(Editor.LastError))
                        {
                            <div class="planner-last-error mt-4">

                                <div class="planner-last-error-title">
                                    Последняя ошибка
                                </div>

                                <pre>@Editor.LastError</pre>

                            </div>
                        }

                    </div>


                    <div class="modal-footer planner-modal-footer">

                        <button type="button"
                                class="btn btn-light px-4"
                                disabled="@IsSaving"
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

                            @(IsEditing
                                ? "Сохранить изменения"
                                : "Создать задание")

                        </button>

                    </div>

                </EditForm>

            </div>

        </div>

    </div>

    <div class="modal-backdrop fade show"></div>
}


@* =========================================================
   DELETE MODAL
   ========================================================= *@

@if (DeleteCandidate is not null)
{
    <div class="modal d-block planner-modal"
         tabindex="-1">

        <div class="modal-dialog modal-dialog-centered">

            <div class="modal-content planner-modal-content">

                <div class="modal-body p-4">

                    <div class="planner-delete-icon">
                        !
                    </div>

                    <h5 class="fw-bold mt-3">
                        Удалить задание?
                    </h5>

                    <p class="text-body-secondary">
                        Задание
                        <strong>@DeleteCandidate.Name</strong>
                        будет удалено из базы данных и Quartz.
                    </p>

                    <div class="d-flex justify-content-end gap-2 mt-4">

                        <button type="button"
                                class="btn btn-light"
                                @onclick="CancelDelete">
                            Отмена
                        </button>

                        <button type="button"
                                class="btn btn-danger px-4"
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

    private string Search = string.Empty;
    private string StatusFilter = string.Empty;

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


    private bool IsEditing =>
        Editor.Id.HasValue;


    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }


    // =====================================================
    // FILTER
    // =====================================================

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
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    (x.Cron?.Contains(
                        Search,
                        StringComparison.OrdinalIgnoreCase) ?? false));
            }


            if (!string.IsNullOrWhiteSpace(StatusFilter) &&
                Enum.TryParse<PlannerUiStatus>(
                    StatusFilter,
                    out var filterStatus))
            {
                result = result.Where(x =>
                    GetUiStatus(x) == filterStatus);
            }

            return result;
        }
    }


    // =====================================================
    // LOAD FROM DATABASE
    // =====================================================

    private async Task ReloadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        Error = null;

        try
        {
            var entities =
                await PlannerService.GetAllAsync();

            var scheduler =
                await SchedulerFactory.GetScheduler();

            Jobs.Clear();


            foreach (var entity in entities)
            {
                var jobKey = new JobKey(
                    entity.Id.ToString(),
                    PlannerGroup);

                ITrigger? trigger = null;

                TriggerState? triggerState = null;


                try
                {
                    if (await scheduler.CheckExists(jobKey))
                    {
                        var triggers =
                            await scheduler.GetTriggersOfJob(jobKey);

                        trigger =
                            triggers.FirstOrDefault();

                        if (trigger is not null)
                        {
                            triggerState =
                                await scheduler.GetTriggerState(
                                    trigger.Key);
                        }
                    }
                }
                catch
                {
                    // БД остаётся источником истины.
                    // Ошибка чтения runtime Quartz не должна
                    // ломать отображение всей страницы.
                }


                var actionName =
                    PlannerActions.FirstOrDefault(
                        x => x.Key == entity.ActionKey)?.Name
                    ?? entity.ActionKey;


                Jobs.Add(new PlannerJobViewModel
                {
                    Id = entity.Id,

                    Name = entity.Name,

                    ActionKey = entity.ActionKey,

                    ActionName = actionName,

                    Parameters = entity.Parameters,

                    ScheduleType = entity.ScheduleType,

                    RunAt = entity.RunAt,

                    IntervalTicks = entity.IntervalTicks,

                    DailyTime = entity.DailyTime,

                    Cron = entity.Cron,

                    Enabled = entity.Enabled,

                    CreatedAt = entity.CreatedAt,

                    UpdatedAt = entity.UpdatedAt,

                    LastRunAt = entity.LastRunAt,

                    LastSuccessAt = entity.LastSuccessAt,

                    LastError = entity.LastError,

                    RuntimeState = triggerState,

                    NextRun =
                        trigger?
                            .GetNextFireTimeUtc()?
                            .ToLocalTime()
                });
            }


            Jobs.Sort((a, b) =>
            {
                if (a.NextRun is null &&
                    b.NextRun is null)
                {
                    return string.Compare(
                        a.Name,
                        b.Name,
                        StringComparison.OrdinalIgnoreCase);
                }

                if (a.NextRun is null)
                    return 1;

                if (b.NextRun is null)
                    return -1;

                return Nullable.Compare(
                    a.NextRun,
                    b.NextRun);
            });
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


    // =====================================================
    // CREATE
    // =====================================================

    private void OpenCreateModal()
    {
        EditorError = null;

        var now =
            DateTime.Now.AddMinutes(5);

        Editor = new PlannerEditorModel
        {
            RunDate = now.Date,

            RunTime =
                now.ToString("HH:mm"),

            DailyTime = "03:00",

            IntervalValue = 5,

            IntervalUnit = "minutes",

            ScheduleType =
                PlannerScheduleType.Once
        };

        ShowEditor = true;
    }


    // =====================================================
    // EDIT
    // =====================================================

    private void OpenEditModal(
        PlannerJobViewModel job)
    {
        EditorError = null;

        var runAt =
            job.RunAt?.ToLocalTime();

        Editor = new PlannerEditorModel
        {
            Id = job.Id,

            Name = job.Name,

            ActionKey = job.ActionKey,

            Parameters = job.Parameters,

            ScheduleType = job.ScheduleType,

            RunDate =
                runAt?.Date ??
                DateTime.Today,

            RunTime =
                runAt?.ToString("HH:mm")
                ?? "12:00",

            DailyTime =
                job.DailyTime?
                    .ToString(@"hh\:mm")
                ?? "03:00",

            Cron = job.Cron,

            CreatedAt = job.CreatedAt,

            LastError = job.LastError
        };


        SetEditorInterval(
            job.IntervalTicks);


        ShowEditor = true;
    }


    private void SetEditorInterval(
        long? ticks)
    {
        if (!ticks.HasValue ||
            ticks.Value <= 0)
        {
            Editor.IntervalValue = 5;
            Editor.IntervalUnit = "minutes";
            return;
        }


        var interval =
            TimeSpan.FromTicks(ticks.Value);


        if (interval.Ticks %
            TimeSpan.TicksPerDay == 0)
        {
            Editor.IntervalValue =
                (int)interval.TotalDays;

            Editor.IntervalUnit = "days";

            return;
        }


        if (interval.Ticks %
            TimeSpan.TicksPerHour == 0)
        {
            Editor.IntervalValue =
                (int)interval.TotalHours;

            Editor.IntervalUnit = "hours";

            return;
        }


        if (interval.Ticks %
            TimeSpan.TicksPerMinute == 0)
        {
            Editor.IntervalValue =
                (int)interval.TotalMinutes;

            Editor.IntervalUnit = "minutes";

            return;
        }


        Editor.IntervalValue =
            Math.Max(
                1,
                (int)interval.TotalSeconds);

        Editor.IntervalUnit = "seconds";
    }


    private void CloseEditor()
    {
        if (IsSaving)
            return;

        ShowEditor = false;
    }


    private void SelectScheduleType(
        PlannerScheduleType type)
    {
        Editor.ScheduleType = type;
    }


    // =====================================================
    // SAVE
    // =====================================================

    private async Task SaveAsync()
    {
        if (IsSaving)
            return;

        EditorError = null;


        if (string.IsNullOrWhiteSpace(Editor.Name))
        {
            EditorError =
                "Укажите название задания.";

            return;
        }


        if (string.IsNullOrWhiteSpace(
                Editor.ActionKey))
        {
            EditorError =
                "Выберите действие.";

            return;
        }


        var request =
            new PlannerJobRequest
            {
                Name =
                    Editor.Name.Trim(),

                ActionKey =
                    Editor.ActionKey,

                Parameters =
                    Editor.Parameters,

                ScheduleType =
                    Editor.ScheduleType
            };


        switch (Editor.ScheduleType)
        {
            case PlannerScheduleType.Once:
            {
                if (!TimeOnly.TryParse(
                        Editor.RunTime,
                        out var time))
                {
                    EditorError =
                        "Некорректное время запуска.";

                    return;
                }


                var localDateTime =
                    Editor.RunDate.Date
                        .Add(time.ToTimeSpan());


                localDateTime =
                    DateTime.SpecifyKind(
                        localDateTime,
                        DateTimeKind.Local);


                request.RunAt =
                    new DateTimeOffset(
                        localDateTime);

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
                        "seconds" =>
                            TimeSpan.FromSeconds(
                                Editor.IntervalValue),

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
                                "Unknown interval unit.")
                    };

                break;
            }


            case PlannerScheduleType.Daily:
            {
                if (!TimeOnly.TryParse(
                        Editor.DailyTime,
                        out var dailyTime))
                {
                    EditorError =
                        "Некорректное время.";

                    return;
                }


                request.DailyTime =
                    dailyTime;

                break;
            }


            case PlannerScheduleType.Cron:
            {
                if (string.IsNullOrWhiteSpace(
                        Editor.Cron))
                {
                    EditorError =
                        "Укажите Cron expression.";

                    return;
                }


                if (!CronExpression.IsValidExpression(
                        Editor.Cron))
                {
                    EditorError =
                        "Некорректное Cron expression.";

                    return;
                }


                request.Cron =
                    Editor.Cron.Trim();

                break;
            }
        }


        IsSaving = true;

        try
        {
            if (Editor.Id.HasValue)
            {
                await PlannerService.UpdateAsync(
                    Editor.Id.Value,
                    request);
            }
            else
            {
                await PlannerService.CreateAsync(
                    request);
            }


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


    // =====================================================
    // COMMANDS
    // =====================================================

    private async Task RunNowAsync(
        PlannerJobViewModel job)
    {
        await ExecuteJobCommand(
            job,
            () =>
                PlannerService.RunNowAsync(
                    job.Id));
    }


    private async Task PauseAsync(
        PlannerJobViewModel job)
    {
        await ExecuteJobCommand(
            job,
            () =>
                PlannerService.PauseAsync(
                    job.Id));
    }


    private async Task ResumeAsync(
        PlannerJobViewModel job)
    {
        await ExecuteJobCommand(
            job,
            () =>
                PlannerService.ResumeAsync(
                    job.Id));
    }


    private async Task ExecuteJobCommand(
        PlannerJobViewModel job,
        Func<Task> command)
    {
        if (BusyJobId is not null)
            return;

        BusyJobId =
            job.Id.ToString();

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
        return BusyJobId ==
               job.Id.ToString();
    }


    // =====================================================
    // DELETE
    // =====================================================

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


        var job =
            DeleteCandidate;

        DeleteCandidate = null;


        await ExecuteJobCommand(
            job,
            () =>
                PlannerService.DeleteAsync(
                    job.Id));
    }


    // =====================================================
    // STATUS
    // =====================================================

    private PlannerUiStatus GetUiStatus(
        PlannerJobViewModel job)
    {
        if (!string.IsNullOrWhiteSpace(
                job.LastError))
        {
            return PlannerUiStatus.Error;
        }


        if (!job.Enabled)
        {
            return PlannerUiStatus.Paused;
        }


        if (job.RuntimeState ==
            TriggerState.Error)
        {
            return PlannerUiStatus.Error;
        }


        if (job.RuntimeState ==
            TriggerState.Blocked)
        {
            return PlannerUiStatus.Running;
        }


        if (job.RuntimeState ==
            TriggerState.Complete)
        {
            return PlannerUiStatus.Completed;
        }


        if (job.ScheduleType ==
                PlannerScheduleType.Once &&
            job.RunAt.HasValue &&
            job.RunAt.Value <=
                DateTimeOffset.Now &&
            !job.NextRun.HasValue)
        {
            return PlannerUiStatus.Completed;
        }


        if (!job.RuntimeState.HasValue ||
            job.RuntimeState ==
                TriggerState.None)
        {
            return PlannerUiStatus.Missing;
        }


        return PlannerUiStatus.Active;
    }


    private static string GetStatusText(
        PlannerUiStatus status)
    {
        return status switch
        {
            PlannerUiStatus.Active =>
                "Активно",

            PlannerUiStatus.Running =>
                "Выполняется",

            PlannerUiStatus.Paused =>
                "Пауза",

            PlannerUiStatus.Error =>
                "Ошибка",

            PlannerUiStatus.Completed =>
                "Завершено",

            PlannerUiStatus.Missing =>
                "Не загружено",

            _ =>
                status.ToString()
        };
    }


    private static string GetStatusClass(
        PlannerUiStatus status)
    {
        return status switch
        {
            PlannerUiStatus.Active =>
                "planner-status active",

            PlannerUiStatus.Running =>
                "planner-status running",

            PlannerUiStatus.Paused =>
                "planner-status paused",

            PlannerUiStatus.Error =>
                "planner-status error",

            PlannerUiStatus.Completed =>
                "planner-status completed",

            _ =>
                "planner-status missing"
        };
    }


    // =====================================================
    // DISPLAY HELPERS
    // =====================================================

    private static string GetScheduleDescription(
        PlannerJobViewModel job)
    {
        return job.ScheduleType switch
        {
            PlannerScheduleType.Once =>
                job.RunAt.HasValue
                    ? job.RunAt.Value
                        .ToLocalTime()
                        .ToString(
                            "dd.MM.yyyy · HH:mm")
                    : "Дата не задана",

            PlannerScheduleType.Interval =>
                GetIntervalDescription(
                    job.IntervalTicks),

            PlannerScheduleType.Daily =>
                job.DailyTime.HasValue
                    ? $"Каждый день · " +
                      job.DailyTime.Value
                          .ToString(@"hh\:mm")
                    : "Время не задано",

            PlannerScheduleType.Cron =>
                string.IsNullOrWhiteSpace(
                    job.Cron)
                    ? "Cron не задан"
                    : job.Cron,

            _ =>
                "—"
        };
    }


    private static string GetIntervalDescription(
        long? ticks)
    {
        if (!ticks.HasValue)
            return "Интервал не задан";


        var interval =
            TimeSpan.FromTicks(
                ticks.Value);


        if (interval.Ticks %
            TimeSpan.TicksPerDay == 0)
        {
            return $"Каждые " +
                   $"{interval.TotalDays:0} дн.";
        }


        if (interval.Ticks %
            TimeSpan.TicksPerHour == 0)
        {
            return $"Каждые " +
                   $"{interval.TotalHours:0} ч.";
        }


        if (interval.Ticks %
            TimeSpan.TicksPerMinute == 0)
        {
            return $"Каждые " +
                   $"{interval.TotalMinutes:0} мин.";
        }


        return $"Каждые " +
               $"{interval.TotalSeconds:0} сек.";
    }


    private static string GetScheduleTypeName(
        PlannerScheduleType type)
    {
        return type switch
        {
            PlannerScheduleType.Once =>
                "Один раз",

            PlannerScheduleType.Interval =>
                "Интервал",

            PlannerScheduleType.Daily =>
                "Ежедневно",

            PlannerScheduleType.Cron =>
                "Cron",

            _ =>
                type.ToString()
        };
    }


    private static string GetJobIcon(
        PlannerScheduleType type)
    {
        return type switch
        {
            PlannerScheduleType.Once => "◷",
            PlannerScheduleType.Interval => "↻",
            PlannerScheduleType.Daily => "◉",
            PlannerScheduleType.Cron => "⌘",
            _ => "◫"
        };
    }


    private string GetScheduleButtonClass(
        PlannerScheduleType type)
    {
        return Editor.ScheduleType == type
            ? "planner-schedule-type active"
            : "planner-schedule-type";
    }


    private static string GetShortError(
        string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
            return string.Empty;


        var line =
            error.Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()
                ?.Trim()
            ?? error;


        return line.Length <= 60
            ? line
            : line[..57] + "...";
    }


    // =====================================================
    // MODELS
    // =====================================================

    private enum PlannerUiStatus
    {
        Active,
        Running,
        Paused,
        Error,
        Completed,
        Missing
    }


    private sealed class PlannerEditorModel
    {
        public Guid? Id { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string ActionKey { get; set; } =
            string.Empty;

        public PlannerScheduleType ScheduleType
        {
            get;
            set;
        }

        public DateTime RunDate { get; set; } =
            DateTime.Today;

        public string RunTime { get; set; } =
            "12:00";

        public int IntervalValue { get; set; } =
            5;

        public string IntervalUnit { get; set; } =
            "minutes";

        public string DailyTime { get; set; } =
            "03:00";

        public string? Cron { get; set; }

        public string? Parameters { get; set; }

        public DateTimeOffset? CreatedAt
        {
            get;
            set;
        }

        public string? LastError { get; set; }
    }


    private sealed class PlannerJobViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } =
            string.Empty;

        public string ActionKey { get; set; } =
            string.Empty;

        public string ActionName { get; set; } =
            string.Empty;

        public string? Parameters { get; set; }

        public PlannerScheduleType ScheduleType
        {
            get;
            set;
        }

        public DateTimeOffset? RunAt
        {
            get;
            set;
        }

        public long? IntervalTicks
        {
            get;
            set;
        }

        public TimeSpan? DailyTime
        {
            get;
            set;
        }

        public string? Cron { get; set; }

        public bool Enabled { get; set; }

        public DateTimeOffset CreatedAt
        {
            get;
            set;
        }

        public DateTimeOffset? UpdatedAt
        {
            get;
            set;
        }

        public DateTimeOffset? LastRunAt
        {
            get;
            set;
        }

        public DateTimeOffset? LastSuccessAt
        {
            get;
            set;
        }

        public string? LastError { get; set; }

        public TriggerState? RuntimeState
        {
            get;
            set;
        }

        public DateTimeOffset? NextRun
        {
            get;
            set;
        }
    }
}
