.planner-page {
    padding: 2rem 0 4rem;
}


/* =========================================================
   HEADER
   ========================================================= */

.planner-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1.5rem;

    margin-bottom: 1.75rem;
}

.planner-overline,
.planner-modal-overline {
    margin-bottom: .25rem;

    color: var(--bs-primary);

    font-size: .68rem;
    font-weight: 800;
    letter-spacing: .14em;
}

.planner-title {
    margin: 0;

    font-size: 2rem;
    font-weight: 750;
    letter-spacing: -.035em;
}

.planner-subtitle {
    margin-top: .3rem;

    color: var(--bs-secondary-color);
    font-size: .94rem;
}


/* =========================================================
   STATISTICS
   ========================================================= */

.planner-stat-card {
    min-height: 112px;

    display: flex;
    align-items: center;
    justify-content: space-between;

    padding: 1.25rem 1.35rem;

    background: var(--bs-body-bg);

    border:
        1px solid
        var(--bs-border-color);

    border-radius: .9rem;

    box-shadow:
        0 .15rem .9rem
        rgba(0, 0, 0, .025);

    transition:
        transform .16s ease,
        box-shadow .16s ease,
        border-color .16s ease;
}

.planner-stat-card:hover {
    transform: translateY(-2px);

    border-color:
        rgba(
            var(--bs-primary-rgb),
            .25
        );

    box-shadow:
        0 .5rem 1.4rem
        rgba(0, 0, 0, .055);
}

.planner-stat-label {
    margin-bottom: .4rem;

    color: var(--bs-secondary-color);

    font-size: .76rem;
    font-weight: 600;
}

.planner-stat-value {
    font-size: 2rem;
    font-weight: 750;
    line-height: 1;
}

.planner-stat-icon {
    opacity: .3;

    font-size: 1.8rem;
}

.planner-stat-state {
    width: .75rem;
    height: .75rem;

    border-radius: 50%;

    box-shadow:
        0 0 0 .3rem
        rgba(108, 117, 125, .08);
}

.planner-stat-state.success {
    background: var(--bs-success);
}

.planner-stat-state.warning {
    background: var(--bs-warning);
}

.planner-stat-state.danger {
    background: var(--bs-danger);
}


/* =========================================================
   CARD / TOOLBAR
   ========================================================= */

.planner-card {
    overflow: hidden;

    background: var(--bs-body-bg);

    border:
        1px solid
        var(--bs-border-color);

    border-radius: .95rem;

    box-shadow:
        0 .25rem 1.3rem
        rgba(0, 0, 0, .035);
}

.planner-toolbar {
    display: grid;
    grid-template-columns: minmax(250px, 1fr) 220px;
    gap: .75rem;

    padding: 1rem 1.15rem;

    border-bottom:
        1px solid
        var(--bs-border-color);
}

.planner-search {
    position: relative;
}

.planner-search-icon {
    position: absolute;

    left: .9rem;
    top: 50%;

    z-index: 2;

    transform: translateY(-50%);

    color: var(--bs-secondary-color);
    font-size: 1.15rem;

    pointer-events: none;
}

.planner-search-input {
    padding-left: 2.7rem;
}

.planner-filter {
    min-width: 180px;
}


/* =========================================================
   TABLE
   ========================================================= */

.planner-table {
    --bs-table-bg: transparent;
}

.planner-table thead th {
    padding: .9rem 1.1rem;

    border-bottom-width: 1px;

    background:
        var(--bs-tertiary-bg);

    color:
        var(--bs-secondary-color);

    font-size: .68rem;
    font-weight: 750;

    letter-spacing: .045em;

    text-transform: uppercase;

    white-space: nowrap;
}

.planner-table tbody td {
    padding: 1rem 1.1rem;

    vertical-align: middle;

    border-color:
        var(--bs-border-color-translucent);
}

.planner-table tbody tr {
    transition:
        background-color .13s ease;
}

.planner-table tbody tr:hover {
    background:
        var(--bs-tertiary-bg);
}

.planner-job-icon {
    width: 2.65rem;
    height: 2.65rem;

    flex-shrink: 0;

    display: flex;
    align-items: center;
    justify-content: center;

    border-radius: .75rem;

    color:
        var(--bs-primary);

    background:
        rgba(
            var(--bs-primary-rgb),
            .09
        );

    font-size: 1rem;
}

.planner-job-name {
    max-width: 230px;

    overflow: hidden;

    font-weight: 650;

    text-overflow: ellipsis;
    white-space: nowrap;
}

.planner-job-id {
    max-width: 170px;

    margin-top: .15rem;

    overflow: hidden;

    color:
        var(--bs-secondary-color);

    font-family:
        var(--bs-font-monospace);

    font-size: .67rem;

    text-overflow: ellipsis;
    white-space: nowrap;
}

.planner-secondary-text {
    color:
        var(--bs-secondary-color);

    font-size: .75rem;
}

.planner-muted {
    color:
        var(--bs-secondary-color);

    font-size: .82rem;
}

.planner-schedule {
    display: inline-flex;
    align-items: center;

    max-width: 220px;

    padding: .34rem .58rem;

    overflow: hidden;

    border-radius: .45rem;

    background:
        var(--bs-tertiary-bg);

    font-family:
        var(--bs-font-monospace);

    font-size: .74rem;

    text-overflow: ellipsis;
    white-space: nowrap;
}


/* =========================================================
   STATUS
   ========================================================= */

.planner-status {
    display: inline-flex;
    align-items: center;
    gap: .4rem;

    padding: .38rem .62rem;

    border-radius: 100px;

    font-size: .72rem;
    font-weight: 650;

    white-space: nowrap;
}

.planner-status-dot {
    width: .4rem;
    height: .4rem;

    border-radius: 50%;

    background: currentColor;
}

.planner-status.active {
    color: var(--bs-success);

    background:
        rgba(
            var(--bs-success-rgb),
            .1
        );
}

.planner-status.running {
    color: var(--bs-primary);

    background:
        rgba(
            var(--bs-primary-rgb),
            .1
        );
}

.planner-status.paused {
    color:
        var(--bs-warning-text-emphasis);

    background:
        rgba(
            var(--bs-warning-rgb),
            .14
        );
}

.planner-status.error {
    color: var(--bs-danger);

    background:
        rgba(
            var(--bs-danger-rgb),
            .1
        );
}

.planner-status.completed {
    color:
        var(--bs-secondary-color);

    background:
        var(--bs-tertiary-bg);
}

.planner-status.missing {
    color:
        var(--bs-secondary-color);

    background:
        var(--bs-tertiary-bg);
}

.planner-error-preview {
    max-width: 180px;

    margin-top: .35rem;

    overflow: hidden;

    color: var(--bs-danger);

    font-size: .68rem;

    text-overflow: ellipsis;
    white-space: nowrap;
}


/* =========================================================
   ACTIONS
   ========================================================= */

.planner-actions {
    display: flex;
    justify-content: flex-end;
    gap: .3rem;

    min-width: 160px;
}

.planner-action-button {
    width: 2.15rem;
    height: 2.15rem;

    display: inline-flex;
    align-items: center;
    justify-content: center;

    padding: 0;

    color:
        var(--bs-body-color);

    background:
        var(--bs-tertiary-bg);

    border:
        1px solid
        transparent;

    border-radius: .5rem;

    font-size: .9rem;

    transition:
        background-color .12s ease,
        border-color .12s ease,
        color .12s ease,
        transform .12s ease;
}

.planner-action-button:hover:not(:disabled) {
    transform: translateY(-1px);

    border-color:
        var(--bs-border-color);

    background:
        var(--bs-secondary-bg);
}

.planner-action-button.resume {
    color: var(--bs-success);
}

.planner-action-button.danger {
    color: var(--bs-danger);
}

.planner-action-button.danger:hover:not(:disabled) {
    background:
        rgba(
            var(--bs-danger-rgb),
            .09
        );

    border-color:
        rgba(
            var(--bs-danger-rgb),
            .2
        );
}

.planner-action-button:disabled {
    opacity: .4;
}


/* =========================================================
   EMPTY
   ========================================================= */

.planner-empty {
    min-height: 330px;

    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;

    padding: 3rem;

    text-align: center;
}

.planner-empty-icon {
    width: 4rem;
    height: 4rem;

    display: flex;
    align-items: center;
    justify-content: center;

    border-radius: 1rem;

    color:
        var(--bs-secondary-color);

    background:
        var(--bs-tertiary-bg);

    font-size: 1.7rem;
}


/* =========================================================
   MODAL
   ========================================================= */

.planner-modal {
    overflow-y: auto;
}

.planner-modal-content {
    overflow: hidden;

    border: 0;
    border-radius: 1rem;

    box-shadow:
        0 1.5rem 4rem
        rgba(0, 0, 0, .22);
}

.planner-modal-header {
    padding: 1.2rem 1.5rem;
}

.planner-modal-body {
    padding: 1.5rem;
}

.planner-modal-footer {
    padding: 1rem 1.5rem;
}

.planner-edit-info {
    display: flex;
    flex-wrap: wrap;
    gap: .5rem 1.5rem;

    padding: .8rem 1rem;

    border:
        1px solid
        var(--bs-border-color);

    border-radius: .65rem;

    background:
        var(--bs-tertiary-bg);

    color:
        var(--bs-secondary-color);

    font-size: .75rem;
}

.planner-edit-info-label {
    margin-right: .4rem;

    font-weight: 700;
}


/* =========================================================
   FORM
   ========================================================= */

.planner-form-label {
    font-size: .84rem;
    font-weight: 650;
}

.planner-form-section {
    padding: 1.15rem;

    border:
        1px solid
        var(--bs-border-color);

    border-radius: .85rem;

    background:
        var(--bs-tertiary-bg);
}

.planner-section-title {
    margin-bottom: .9rem;

    font-size: .84rem;
    font-weight: 700;
}

.planner-schedule-types {
    display: grid;
    grid-template-columns:
        repeat(4, minmax(0, 1fr));

    gap: .55rem;
}

.planner-schedule-type {
    min-height: 78px;

    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;

    gap: .35rem;

    padding: .7rem .5rem;

    color:
        var(--bs-body-color);

    background:
        var(--bs-body-bg);

    border:
        1px solid
        var(--bs-border-color);

    border-radius: .65rem;

    font-size: .78rem;
    font-weight: 650;

    transition:
        transform .12s ease,
        border-color .12s ease,
        color .12s ease,
        background-color .12s ease;
}

.planner-schedule-type:hover {
    transform: translateY(-1px);

    border-color:
        rgba(
            var(--bs-primary-rgb),
            .5
        );
}

.planner-schedule-type.active {
    color: var(--bs-primary);

    border-color:
        var(--bs-primary);

    background:
        rgba(
            var(--bs-primary-rgb),
            .08
        );

    box-shadow:
        0 0 0 .15rem
        rgba(
            var(--bs-primary-rgb),
            .06
        );
}

.planner-schedule-type-icon {
    font-size: 1.25rem;
}

.planner-schedule-editor {
    margin-top: 1rem;
    padding-top: 1rem;

    border-top:
        1px solid
        var(--bs-border-color);
}

.planner-time-input {
    max-width: 260px;
}


/* =========================================================
   ERROR
   ========================================================= */

.planner-last-error {
    padding: 1rem;

    border:
        1px solid
        rgba(
            var(--bs-danger-rgb),
            .2
        );

    border-radius: .7rem;

    background:
        rgba(
            var(--bs-danger-rgb),
            .05
        );
}

.planner-last-error-title {
    margin-bottom: .6rem;

    color: var(--bs-danger);

    font-size: .8rem;
    font-weight: 700;
}

.planner-last-error pre {
    max-height: 180px;

    margin: 0;

    overflow: auto;

    color:
        var(--bs-body-color);

    font-size: .72rem;

    white-space: pre-wrap;
}

.planner-delete-icon {
    width: 3rem;
    height: 3rem;

    display: flex;
    align-items: center;
    justify-content: center;

    border-radius: .75rem;

    color: var(--bs-danger);

    background:
        rgba(
            var(--bs-danger-rgb),
            .1
        );

    font-size: 1.3rem;
    font-weight: 750;
}


/* =========================================================
   RESPONSIVE
   ========================================================= */

@media (max-width: 991.98px) {

    .planner-header {
        align-items: flex-start;
        flex-direction: column;
    }

    .planner-toolbar {
        grid-template-columns: 1fr;
    }

    .planner-filter {
        width: 100%;
    }
}


@media (max-width: 767.98px) {

    .planner-page {
        padding-top: 1rem;
    }

    .planner-title {
        font-size: 1.7rem;
    }

    .planner-header > div:last-child {
        width: 100%;
    }

    .planner-header > div:last-child > button {
        flex: 1;
    }

    .planner-schedule-types {
        grid-template-columns:
            repeat(2, minmax(0, 1fr));
    }

    .planner-modal .modal-dialog {
        margin: .6rem;
    }

    .planner-table tbody td,
    .planner-table thead th {
        padding-left: .8rem;
        padding-right: .8rem;
    }
}
