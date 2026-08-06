#!/usr/bin/env bash

# Проверка:
#   CryptoPro CSP
#   сертификат пользователя delta
#   контейнер RuToken
#   TSP через stunnel
#   CAdES-X Long Type 1
#
# Запуск:
#
#   ./check-cryptopro.sh THUMBPRINT
#
# Или:
#
#   ./check-cryptopro.sh THUMBPRINT TSP_URL
#
# Или с конкретным файлом:
#
#   ./check-cryptopro.sh THUMBPRINT TSP_URL INPUT_FILE
#
# Коды возврата:
#   0 — все проверки выполнены успешно
#   1 — одна из проверок завершилась ошибкой
#   2 — неверные параметры запуска

set -u
set -o pipefail
umask 077

# ---------------------------------------------------------------------------
# Настройки
# ---------------------------------------------------------------------------

EXPECTED_USER="${EXPECTED_USER:-delta}"

DEFAULT_TSP_URL="${
    DEFAULT_TSP_URL:-http://127.0.0.1:10001/tsp/tsp.srf
}"

# ГОСТ Р 34.11-2012, 256 бит
TSP_HASH_OID="${TSP_HASH_OID:-1.2.643.7.1.1.2.2}"

# Максимальное время одной операции
TIMEOUT_SEC="${TIMEOUT_SEC:-180}"

CURRENT_USER="$(id -un)"

RUN_DIR=""
LOG_FILE=""
INPUT_FILE=""
TSP_STAMP_FILE=""
SIGNATURE_FILE=""

# ---------------------------------------------------------------------------
# Функции вывода
# ---------------------------------------------------------------------------

separator() {
    printf '%s\n' \
        '----------------------------------------------------------------'
}

wide_separator() {
    printf '%s\n' \
        '================================================================'
}

now() {
    date '+%Y-%m-%dT%H:%M:%S%z'
}

usage() {
    cat <<EOF
Использование:

  $0 THUMBPRINT [TSP_URL] [INPUT_FILE]

Пример:

  $0 '0123456789abcdef0123456789abcdef01234567'

С явным TSP URL:

  $0 \\
    '0123456789abcdef0123456789abcdef01234567' \\
    'http://127.0.0.1:10001/tsp/tsp.srf'

Проверка конкретного файла:

  $0 \\
    '0123456789abcdef0123456789abcdef01234567' \\
    'http://127.0.0.1:10001/tsp/tsp.srf' \\
    '/home/delta/data.txt'

Переменные окружения:

  EXPECTED_USER=delta
  DEFAULT_TSP_URL=http://127.0.0.1:10001/tsp/tsp.srf
  TSP_HASH_OID=1.2.643.7.1.1.2.2
  TIMEOUT_SEC=180
  STATE_DIR=/home/delta/.local/state/cryptopro-check
EOF
}

# ---------------------------------------------------------------------------
# Поиск утилит
# ---------------------------------------------------------------------------

resolve_binary() {
    local name="$1"
    local candidate

    for candidate in \
        "/opt/cprocsp/bin/amd64/$name" \
        "/opt/cprocsp/bin/$name"
    do
        if [[ -x "$candidate" ]]; then
            printf '%s\n' "$candidate"
            return 0
        fi
    done

    candidate="$(command -v "$name" 2>/dev/null || true)"

    if [[ -n "$candidate" && -x "$candidate" ]]; then
        printf '%s\n' "$candidate"
        return 0
    fi

    return 1
}

# ---------------------------------------------------------------------------
# Журналирование
# ---------------------------------------------------------------------------

log_command() {
    local arg

    printf '%s' 'Команда:' >>"$LOG_FILE"

    for arg in "$@"; do
        printf ' %q' "$arg" >>"$LOG_FILE"
    done

    printf '\n' >>"$LOG_FILE"
}

run_capture() {
    local output_file="$1"
    shift

    local rc

    {
        printf '\n'
        wide_separator
        printf 'Время: %s\n' "$(now)"
    } >>"$LOG_FILE"

    log_command "$@"

    separator >>"$LOG_FILE"

    if "$TIMEOUT_BIN" \
        --signal=TERM \
        --kill-after=10s \
        "${TIMEOUT_SEC}s" \
        "$@" \
        </dev/null >"$output_file" 2>&1
    then
        rc=0
    else
        rc=$?
    fi

    cat "$output_file" >>"$LOG_FILE"

    {
        printf '\nКод завершения: %d\n' "$rc"
        wide_separator
    } >>"$LOG_FILE"

    return "$rc"
}

show_log_tail() {
    if [[ -n "$LOG_FILE" && -f "$LOG_FILE" ]]; then
        printf '\nПоследние 50 строк журнала:\n'
        separator
        tail -n 50 "$LOG_FILE"
    fi
}

fail() {
    local reason="$1"

    printf '\n[FAIL] ПРОВЕРКА НЕ ПРОЙДЕНА\n'
    printf 'Причина: %s\n' "$reason"
    printf 'Пользователь: %s\n' "$CURRENT_USER"

    if [[ -n "$RUN_DIR" ]]; then
        printf 'Каталог результатов: %s\n' "$RUN_DIR"
    fi

    if [[ -n "$LOG_FILE" ]]; then
        printf 'Полный журнал: %s\n' "$LOG_FILE"
    fi

    show_log_tail
    exit 1
}

success() {
    local signature_size
    local tsp_size

    signature_size="$(stat -c '%s' "$SIGNATURE_FILE")"
    tsp_size="$(stat -c '%s' "$TSP_STAMP_FILE")"

    {
        printf '\nРезультат: SUCCESS\n'
        printf 'Время завершения: %s\n' "$(now)"
        printf 'Размер TSP-штампа: %s байт\n' "$tsp_size"
        printf 'Размер подписи: %s байт\n' "$signature_size"
    } >>"$LOG_FILE"

    printf '\n[OK] ПРОВЕРКА ПРОЙДЕНА УСПЕШНО\n'
    printf 'Пользователь: %s\n' "$CURRENT_USER"
    printf 'Сертификат: %s\n' "$THUMBPRINT"
    printf 'Контейнер: Aktiv Rutoken ECP\n'
    printf 'TSP URL: %s\n' "$TSP_URL"
    printf 'Формат: CAdES-X Long Type 1\n'
    printf 'Размер TSP-штампа: %s байт\n' "$tsp_size"
    printf 'Размер подписи: %s байт\n' "$signature_size"
    printf 'Исходный файл: %s\n' "$INPUT_FILE"
    printf 'TSP-штамп: %s\n' "$TSP_STAMP_FILE"
    printf 'Подпись: %s\n' "$SIGNATURE_FILE"
    printf 'Журнал: %s\n' "$LOG_FILE"

    exit 0
}

is_timeout_rc() {
    local rc="$1"

    [[ "$rc" -eq 124 ||
       "$rc" -eq 137 ||
       "$rc" -eq 143 ]]
}

is_tsp_url_parse_error() {
    local file="$1"

    [[ -f "$file" ]] || return 1

    grep -Eqi \
        'URL of TSP service is not specified|URL службы TSP не указан|не указан.*URL.*TSP' \
        "$file"
}

is_verify_arguments_error() {
    local file="$1"

    [[ -f "$file" ]] || return 1

    grep -Eqi \
        'Useless positional arguments|лишн.*позицион|too many arguments|неверн.*параметр' \
        "$file"
}

# ---------------------------------------------------------------------------
# Параметры запуска
# ---------------------------------------------------------------------------

THUMBPRINT="${1:-}"
TSP_URL="${2:-$DEFAULT_TSP_URL}"
SOURCE_FILE="${3:-}"

if [[ -z "$THUMBPRINT" ]]; then
    usage
    exit 2
fi

if (( $# > 3 )); then
    printf 'Ошибка: передано слишком много аргументов.\n'
    usage
    exit 2
fi

# Удаляем пробелы и двоеточия из отпечатка
THUMBPRINT="$(
    printf '%s' "$THUMBPRINT" |
        tr -d '[:space:]:'
)"

if [[ ! "$THUMBPRINT" =~ ^[[:xdigit:]]+$ ]]; then
    printf 'Ошибка: отпечаток содержит недопустимые символы.\n'
    exit 2
fi

if [[ ! "$TSP_URL" =~ ^https?:// ]]; then
    printf 'Ошибка: TSP URL должен начинаться с http:// или https://\n'
    exit 2
fi

if [[ ! "$TIMEOUT_SEC" =~ ^[0-9]+$ ]] ||
   (( TIMEOUT_SEC < 1 ))
then
    printf 'Ошибка: TIMEOUT_SEC должен быть положительным целым числом.\n'
    exit 2
fi

# ---------------------------------------------------------------------------
# Проверка пользователя
# ---------------------------------------------------------------------------

if [[ "$CURRENT_USER" != "$EXPECTED_USER" ]]; then
    printf 'Ошибка: скрипт запущен от пользователя "%s".\n' \
        "$CURRENT_USER"

    printf 'Запустите его непосредственно от пользователя "%s".\n' \
        "$EXPECTED_USER"

    exit 1
fi

ACCOUNT_HOME="$(
    getent passwd "$CURRENT_USER" |
        cut -d: -f6
)"

if [[ -z "$ACCOUNT_HOME" ||
      ! -d "$ACCOUNT_HOME" ]]
then
    printf 'Ошибка: домашний каталог пользователя %s не найден.\n' \
        "$CURRENT_USER"

    exit 1
fi

export HOME="$ACCOUNT_HOME"
export USER="$CURRENT_USER"
export LOGNAME="$CURRENT_USER"

# Не используем случайно переопределённый сокет PC/SC
unset PCSCLITE_CSOCK_NAME

# ---------------------------------------------------------------------------
# Поиск программ
# ---------------------------------------------------------------------------

CRYPTCP="$(resolve_binary cryptcp)" || {
    printf 'Ошибка: cryptcp не найден.\n'
    exit 1
}

CERTMGR="$(resolve_binary certmgr)" || {
    printf 'Ошибка: certmgr не найден.\n'
    exit 1
}

CSPTEST="$(resolve_binary csptest)" || {
    printf 'Ошибка: csptest не найден.\n'
    exit 1
}

TSPUTIL="$(resolve_binary tsputil)" || {
    printf 'Ошибка: tsputil не найден.\n'
    exit 1
}

TIMEOUT_BIN="$(
    command -v timeout 2>/dev/null ||
        true
)"

if [[ -z "$TIMEOUT_BIN" ||
      ! -x "$TIMEOUT_BIN" ]]
then
    printf 'Ошибка: команда timeout не найдена.\n'
    exit 1
fi

# ---------------------------------------------------------------------------
# Каталог результатов
# ---------------------------------------------------------------------------

STATE_DIR="${
    STATE_DIR:-$HOME/.local/state/cryptopro-check
}"

RUN_ID="$(date '+%Y%m%d-%H%M%S')-$$"
RUN_DIR="$STATE_DIR/$RUN_ID"

if ! mkdir -p "$RUN_DIR"; then
    printf 'Ошибка: не удалось создать каталог %s\n' "$RUN_DIR"
    exit 1
fi

chmod 700 "$STATE_DIR" "$RUN_DIR" 2>/dev/null || true

LOG_FILE="$RUN_DIR/check.log"

TSP_STAMP_FILE="$RUN_DIR/tsp-response.tsr"
SIGNATURE_FILE="$RUN_DIR/signature.p7s"

CERT_OUTPUT="$RUN_DIR/certificate-check.txt"
CONTAINER_OUTPUT="$RUN_DIR/container-check.txt"
CADES_HELP_OUTPUT="$RUN_DIR/cryptcp-sign-help.txt"
TSP_OUTPUT="$RUN_DIR/tsp-request.txt"
TSP_INFO_OUTPUT="$RUN_DIR/tsp-info.txt"

SIGN_OUTPUT_1="$RUN_DIR/sign-attempt-1.txt"
SIGN_OUTPUT_2="$RUN_DIR/sign-attempt-2.txt"
SIGN_OUTPUT_3="$RUN_DIR/sign-attempt-3.txt"

VERIFY_OUTPUT="$RUN_DIR/verify.txt"
VERIFY_FALLBACK_OUTPUT="$RUN_DIR/verify-fallback.txt"

# ---------------------------------------------------------------------------
# Исходный файл
# ---------------------------------------------------------------------------

if [[ -n "$SOURCE_FILE" ]]; then
    if [[ ! -f "$SOURCE_FILE" ]]; then
        fail "Указанный исходный файл не существует: $SOURCE_FILE"
    fi

    if [[ ! -r "$SOURCE_FILE" ]]; then
        fail "Нет прав на чтение исходного файла: $SOURCE_FILE"
    fi

    INPUT_FILE="$RUN_DIR/input-data"

    if ! cp -- "$SOURCE_FILE" "$INPUT_FILE"; then
        fail 'Не удалось скопировать исходный файл в рабочий каталог'
    fi
else
    INPUT_FILE="$RUN_DIR/input-data.txt"

    {
        printf 'CryptoPro CAdES-X Long Type 1 test\n'
        printf 'User: %s\n' "$CURRENT_USER"
        printf 'Timestamp: %s\n' "$(now)"
        printf 'Random: %s-%s-%s\n' \
            "$RANDOM" \
            "$RANDOM" \
            "$$"
    } >"$INPUT_FILE"
fi

# ---------------------------------------------------------------------------
# Заголовок журнала
# ---------------------------------------------------------------------------

{
    printf 'CryptoPro full check\n'
    printf 'Начало: %s\n' "$(now)"
    printf 'Пользователь: %s\n' "$CURRENT_USER"
    printf 'UID: %s\n' "$(id -u)"
    printf 'HOME: %s\n' "$HOME"
    printf 'Отпечаток: %s\n' "$THUMBPRINT"
    printf 'TSP URL: %s\n' "$TSP_URL"
    printf 'OID хэша TSP: %s\n' "$TSP_HASH_OID"
    printf 'Исходный файл: %s\n' "$INPUT_FILE"
    printf 'cryptcp: %s\n' "$CRYPTCP"
    printf 'certmgr: %s\n' "$CERTMGR"
    printf 'csptest: %s\n' "$CSPTEST"
    printf 'tsputil: %s\n' "$TSPUTIL"
} >"$LOG_FILE"

# ---------------------------------------------------------------------------
# 1. Проверка сертификата
# ---------------------------------------------------------------------------

printf '[1/7] Проверка сертификата в uMy...\n'

run_capture \
    "$CERT_OUTPUT" \
    "$CERTMGR" \
    -list \
    -store uMy \
    -thumbprint "$THUMBPRINT"

rc=$?

if (( rc != 0 )); then
    fail "certmgr завершился с кодом $rc"
fi

THUMBPRINT_LOWER="$(
    printf '%s' "$THUMBPRINT" |
        tr '[:upper:]' '[:lower:]'
)"

CERT_OUTPUT_NORMALIZED="$(
    tr -d '[:space:]:' <"$CERT_OUTPUT" |
        tr '[:upper:]' '[:lower:]'
)"

if [[ "$CERT_OUTPUT_NORMALIZED" != *"$THUMBPRINT_LOWER"* ]]; then
    fail 'Сертификат с указанным отпечатком не найден в uMy'
fi

# ---------------------------------------------------------------------------
# 2. Проверка контейнера RuToken
# ---------------------------------------------------------------------------

printf '[2/7] Проверка контейнера RuToken...\n'

run_capture \
    "$CONTAINER_OUTPUT" \
    "$CSPTEST" \
    -keyset \
    -enum_cont \
    -fqcn \
    -verifycontext

rc=$?

if (( rc != 0 )); then
    fail "CryptoPro не смог перечислить контейнеры, код $rc"
fi

if ! grep -Fqi \
    'Aktiv Rutoken ECP' \
    "$CONTAINER_OUTPUT"
then
    fail 'Контейнер Aktiv Rutoken ECP не найден'
fi

# ---------------------------------------------------------------------------
# 3. Проверка поддержки CAdES
# ---------------------------------------------------------------------------

printf '[3/7] Проверка поддержки CAdES-X Long Type 1...\n'

if "$CRYPTCP" \
    -sign \
    -help \
    </dev/null >"$CADES_HELP_OUTPUT" 2>&1
then
    rc=0
else
    rc=$?
fi

{
    printf '\n'
    wide_separator
    printf 'Время: %s\n' "$(now)"
    printf 'Команда: %q -sign -help\n' "$CRYPTCP"
    separator
    cat "$CADES_HELP_OUTPUT"
    printf '\nКод завершения: %d\n' "$rc"
    wide_separator
} >>"$LOG_FILE"

if ! grep -qi \
    'xlongtype1' \
    "$CADES_HELP_OUTPUT"
then
    fail 'cryptcp не сообщает поддержку параметра -xlongtype1'
fi

if ! grep -qi \
    'cadestsa' \
    "$CADES_HELP_OUTPUT"
then
    fail 'cryptcp не сообщает поддержку параметра -cadesTSA'
fi

# ---------------------------------------------------------------------------
# 4. Проверка TSP через stunnel
# ---------------------------------------------------------------------------

printf '[4/7] Получение TSP-штампа через stunnel...\n'

run_capture \
    "$TSP_OUTPUT" \
    "$TSPUTIL" \
    ms \
    "--alg=$TSP_HASH_OID" \
    "--url=$TSP_URL" \
    --cert-req \
    "$INPUT_FILE" \
    "$TSP_STAMP_FILE"

rc=$?

if is_timeout_rc "$rc"; then
    fail "Истекло время ожидания TSP: ${TIMEOUT_SEC} секунд"
fi

if (( rc != 0 )); then
    fail "tsputil не смог получить или проверить TSP-штамп, код $rc"
fi

if [[ ! -s "$TSP_STAMP_FILE" ]]; then
    fail 'Файл TSP-штампа не создан или имеет нулевой размер'
fi

# ---------------------------------------------------------------------------
# 5. Чтение информации из TSP-штампа
# ---------------------------------------------------------------------------

printf '[5/7] Чтение информации из TSP-штампа...\n'

run_capture \
    "$TSP_INFO_OUTPUT" \
    "$TSPUTIL" \
    si \
    "$TSP_STAMP_FILE"

rc=$?

if is_timeout_rc "$rc"; then
    fail 'Истекло время ожидания чтения TSP-штампа'
fi

if (( rc != 0 )); then
    fail "tsputil не смог прочитать TSP-штамп, код $rc"
fi

# ---------------------------------------------------------------------------
# 6. Создание CAdES-X Long Type 1
# ---------------------------------------------------------------------------

printf '[6/7] Создание CAdES-X Long Type 1...\n'

rm -f "$SIGNATURE_FILE"

# Основной вариант синтаксиса
run_capture \
    "$SIGN_OUTPUT_1" \
    "$CRYPTCP" \
    -sign \
    -thumbprint "$THUMBPRINT" \
    -cert \
    -detached \
    -der \
    -xlongtype1 \
    -cadesTSA "$TSP_URL" \
    "$INPUT_FILE" \
    "$SIGNATURE_FILE"

SIGN_RC=$?

# Некоторые сборки принимают параметр только как -cadesTSA=URL
if (( SIGN_RC != 0 )) &&
   is_tsp_url_parse_error "$SIGN_OUTPUT_1"
then
    printf '      Повтор с параметром -cadesTSA=URL...\n'

    rm -f "$SIGNATURE_FILE"

    run_capture \
        "$SIGN_OUTPUT_2" \
        "$CRYPTCP" \
        -sign \
        -thumbprint "$THUMBPRINT" \
        -cert \
        -detached \
        -der \
        -xlongtype1 \
        "-cadesTSA=$TSP_URL" \
        "$INPUT_FILE" \
        "$SIGNATURE_FILE"

    SIGN_RC=$?
fi

# Резервный вариант для отдельных сборок
if (( SIGN_RC != 0 )) &&
   is_tsp_url_parse_error "$SIGN_OUTPUT_2"
then
    printf '      Повтор с объединённым параметром -cadesTSAURL...\n'

    rm -f "$SIGNATURE_FILE"

    run_capture \
        "$SIGN_OUTPUT_3" \
        "$CRYPTCP" \
        -sign \
        -thumbprint "$THUMBPRINT" \
        -cert \
        -detached \
        -der \
        -xlongtype1 \
        "-cadesTSA${TSP_URL}" \
        "$INPUT_FILE" \
        "$SIGNATURE_FILE"

    SIGN_RC=$?
fi

if is_timeout_rc "$SIGN_RC"; then
    fail "Истекло время ожидания создания подписи: ${TIMEOUT_SEC} секунд"
fi

if (( SIGN_RC != 0 )); then
    fail "cryptcp не смог создать CAdES-подпись, код $SIGN_RC"
fi

if [[ ! -s "$SIGNATURE_FILE" ]]; then
    fail 'cryptcp завершился без ошибки, но файл подписи отсутствует или пуст'
fi

# ---------------------------------------------------------------------------
# 7. Проверка созданной подписи
# ---------------------------------------------------------------------------

printf '[7/7] Проверка созданной CAdES-подписи...\n'

run_capture \
    "$VERIFY_OUTPUT" \
    "$CRYPTCP" \
    -verify \
    -detached \
    -verall \
    -xlongtype1 \
    "$INPUT_FILE" \
    "$SIGNATURE_FILE"

VERIFY_RC=$?

# Старые сборки могут искать подпись как input-data.sgn
if (( VERIFY_RC != 0 )) &&
   is_verify_arguments_error "$VERIFY_OUTPUT"
then
    printf '      Повтор проверки через стандартный файл .sgn...\n'

    if ! cp -- \
        "$SIGNATURE_FILE" \
        "${INPUT_FILE}.sgn"
    then
        fail 'Не удалось подготовить подпись для резервного способа проверки'
    fi

    run_capture \
        "$VERIFY_FALLBACK_OUTPUT" \
        "$CRYPTCP" \
        -verify \
        -detached \
        -verall \
        -xlongtype1 \
        "$INPUT_FILE"

    VERIFY_RC=$?
fi

if is_timeout_rc "$VERIFY_RC"; then
    fail 'Истекло время ожидания проверки CAdES-подписи'
fi

if (( VERIFY_RC != 0 )); then
    fail "Созданная CAdES-подпись не прошла проверку, код $VERIFY_RC"
fi

success
