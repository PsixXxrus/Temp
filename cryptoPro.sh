#!/usr/bin/env bash

# Полная проверка:
#   CryptoPro CSP
#   сертификат пользователя
#   контейнер RuToken
#   TSP через stunnel
#   CAdES-X Long Type 1
#
# Запускать непосредственно от пользователя delta:
#
#   ./check-cryptopro.sh THUMBPRINT
#
# Либо:
#
#   ./check-cryptopro.sh THUMBPRINT TSP_URL
#
# Либо проверить подпись конкретного файла:
#
#   ./check-cryptopro.sh THUMBPRINT TSP_URL /путь/к/файлу
#
# Код возврата:
#   0 — все проверки успешны
#   1 — одна из проверок завершилась ошибкой
#   2 — ошибка параметров запуска

set -uo pipefail
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

# Максимальное время одной сетевой/криптографической операции
TIMEOUT_SEC="${TIMEOUT_SEC:-180}"

# ---------------------------------------------------------------------------
# Функции
# ---------------------------------------------------------------------------

usage() {
    cat <<EOF
Использование:

  $0 THUMBPRINT [TSP_URL] [ФАЙЛ]

Пример:

  $0 '0123456789abcdef0123456789abcdef01234567'

С явным адресом TSP:

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
EOF
}

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

print_command() {
    printf 'Команда:'

    printf ' %q' "$@"

    printf '\n'
}

run_capture() {
    local output_file="$1"
    shift

    {
        printf '\n================================================================\n'
        printf 'Время: %s\n' "$(date --iso-8601=seconds)"
        print_command "$@"
        printf '----------------------------------------------------------------\n'
    } >>"$LOG_FILE"

    if "$TIMEOUT_BIN" \
        --signal=TERM \
        --kill-after=10s \
        "${TIMEOUT_SEC}s" \
        "$@" \
        </dev/null >"$output_file" 2>&1
    then
        local rc=0
    else
        local rc=$?
    fi

    cat "$output_file" >>"$LOG_FILE"

    {
        printf '\nКод завершения: %d\n' "$rc"
        printf '================================================================\n'
    } >>"$LOG_FILE"

    return "$rc"
}

fail() {
    local reason="$1"

    printf '\n'
    printf '❌ ПРОВЕРКА НЕ ПРОЙДЕНА\n'
    printf 'Причина: %s\n' "$reason"
    printf 'Пользователь: %s\n' "$CURRENT_USER"
    printf 'Рабочий каталог: %s\n' "$RUN_DIR"
    printf 'Полный журнал: %s\n' "$LOG_FILE"

    if [[ -f "$LOG_FILE" ]]; then
        printf '\nПоследние 40 строк журнала:\n'
        printf '%s\n' '----------------------------------------------------------------'
        tail -n 40 "$LOG_FILE"
    fi

    exit 1
}

success() {
    local signature_size

    signature_size="$(stat -c '%s' "$SIGNATURE_FILE")"

    {
        printf '\nРезультат: SUCCESS\n'
        printf 'Время завершения: %s\n' "$(date --iso-8601=seconds)"
        printf 'Размер подписи: %s байт\n' "$signature_size"
    } >>"$LOG_FILE"

    printf '\n'
    printf '✅ ПРОВЕРКА ПРОЙДЕНА УСПЕШНО\n'
    printf '\n'
    printf 'Пользователь:       %s\n' "$CURRENT_USER"
    printf 'Сертификат:         %s\n' "$THUMBPRINT"
    printf 'Контейнер:          Aktiv Rutoken ECP\n'
    printf 'TSP URL:            %s\n' "$TSP_URL"
    printf 'Формат подписи:     CAdES-X Long Type 1\n'
    printf 'Размер подписи:     %s байт\n' "$signature_size"
    printf '\n'
    printf 'Тестовый файл:      %s\n' "$INPUT_FILE"
    printf 'TSP-штамп:          %s\n' "$TSP_STAMP_FILE"
    printf 'Файл подписи:       %s\n' "$SIGNATURE_FILE"
    printf 'Полный журнал:      %s\n' "$LOG_FILE"
    printf '\n'

    exit 0
}

# ---------------------------------------------------------------------------
# Проверка параметров запуска
# ---------------------------------------------------------------------------

THUMBPRINT="${1:-}"
TSP_URL="${2:-$DEFAULT_TSP_URL}"
SOURCE_FILE="${3:-}"

if [[ -z "$THUMBPRINT" ]]; then
    usage
    exit 2
fi

# Удаляем пробелы, двоеточия и переводы строк из отпечатка.
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

if [[ ! "$TIMEOUT_SEC" =~ ^[0-9]+$ ]] || (( TIMEOUT_SEC < 1 )); then
    printf 'Ошибка: TIMEOUT_SEC должен быть положительным числом.\n'
    exit 2
fi

CURRENT_USER="$(id -un)"

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

if [[ -z "$ACCOUNT_HOME" || ! -d "$ACCOUNT_HOME" ]]; then
    printf 'Ошибка: домашний каталог пользователя %s не найден.\n' \
        "$CURRENT_USER"
    exit 1
fi

export HOME="$ACCOUNT_HOME"
export USER="$CURRENT_USER"
export LOGNAME="$CURRENT_USER"

# Исключаем ошибочное пользовательское переопределение сокета PC/SC.
unset PCSCLITE_CSOCK_NAME

# ---------------------------------------------------------------------------
# Поиск утилит
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

TIMEOUT_BIN="$(command -v timeout 2>/dev/null || true)"

if [[ -z "$TIMEOUT_BIN" || ! -x "$TIMEOUT_BIN" ]]; then
    printf 'Ошибка: команда timeout не найдена.\n'
    printf 'Она необходима, чтобы проверка не зависла при недоступном TSP или PIN.\n'
    exit 1
fi

# ---------------------------------------------------------------------------
# Рабочий каталог
# ---------------------------------------------------------------------------

STATE_DIR="${XDG_STATE_HOME:-$HOME/.local/state}/cryptopro-check"
RUN_ID="$(date '+%Y%m%d-%H%M%S')-$$"
RUN_DIR="$STATE_DIR/$RUN_ID"

mkdir -p "$RUN_DIR" || {
    printf 'Ошибка: не удалось создать каталог %s\n' "$RUN_DIR"
    exit 1
}

chmod 700 "$STATE_DIR" "$RUN_DIR" 2>/dev/null || true

LOG_FILE="$RUN_DIR/check.log"
TSP_STAMP_FILE="$RUN_DIR/tsp-response.tsr"
SIGNATURE_FILE="$RUN_DIR/signature.p7s"

CERT_OUTPUT="$RUN_DIR/certificate-check.txt"
CONTAINER_OUTPUT="$RUN_DIR/container-check.txt"
CADES_HELP_OUTPUT="$RUN_DIR/cryptcp-help.txt"
TSP_OUTPUT="$RUN_DIR/tsp-request.txt"
TSP_INFO_OUTPUT="$RUN_DIR/tsp-info.txt"
SIGN_OUTPUT="$RUN_DIR/sign.txt"
SIGN_RETRY_OUTPUT="$RUN_DIR/sign-retry.txt"
VERIFY_OUTPUT="$RUN_DIR/verify.txt"

if [[ -n "$SOURCE_FILE" ]]; then
    if [[ ! -f "$SOURCE_FILE" ]]; then
        fail "Указанный файл не существует: $SOURCE_FILE"
    fi

    if [[ ! -r "$SOURCE_FILE" ]]; then
        fail "Нет прав на чтение файла: $SOURCE_FILE"
    fi

    INPUT_FILE="$RUN_DIR/input-data"

    cp -- "$SOURCE_FILE" "$INPUT_FILE" ||
        fail "Не удалось скопировать исходный файл"
else
    INPUT_FILE="$RUN_DIR/input-data.txt"

    {
        printf 'CryptoPro CAdES-X Long Type 1 test\n'
        printf 'User: %s\n' "$CURRENT_USER"
        printf 'Timestamp: %s\n' "$(date --iso-8601=seconds)"
        printf 'Random: %s-%s-%s\n' "$RANDOM" "$RANDOM" "$$"
    } >"$INPUT_FILE"
fi

{
    printf 'CryptoPro full check\n'
    printf 'Started: %s\n' "$(date --iso-8601=seconds)"
    printf 'User: %s\n' "$CURRENT_USER"
    printf 'UID: %s\n' "$(id -u)"
    printf 'HOME: %s\n' "$HOME"
    printf 'Thumbprint: %s\n' "$THUMBPRINT"
    printf 'TSP URL: %s\n' "$TSP_URL"
    printf 'TSP hash OID: %s\n' "$TSP_HASH_OID"
    printf 'Input file: %s\n' "$INPUT_FILE"
    printf 'cryptcp: %s\n' "$CRYPTCP"
    printf 'certmgr: %s\n' "$CERTMGR"
    printf 'csptest: %s\n' "$CSPTEST"
    printf 'tsputil: %s\n' "$TSPUTIL"
} >"$LOG_FILE"

# ---------------------------------------------------------------------------
# 1. Проверка сертификата
# ---------------------------------------------------------------------------

printf '[1/7] Проверка сертификата в хранилище uMy...\n'

if ! run_capture \
    "$CERT_OUTPUT" \
    "$CERTMGR" \
    -list \
    -store uMy \
    -thumbprint "$THUMBPRINT"
then
    rc=$?
    fail "certmgr завершился с ошибкой, код: $rc"
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
    fail "Сертификат с указанным отпечатком не найден в uMy"
fi

# ---------------------------------------------------------------------------
# 2. Проверка контейнера RuToken
# ---------------------------------------------------------------------------

printf '[2/7] Проверка контейнера RuToken...\n'

if ! run_capture \
    "$CONTAINER_OUTPUT" \
    "$CSPTEST" \
    -keyset \
    -enum_cont \
    -fqcn \
    -verifycontext
then
    rc=$?
    fail "CryptoPro не смог перечислить контейнеры, код: $rc"
fi

if ! grep -Fqi 'Aktiv Rutoken ECP' "$CONTAINER_OUTPUT"; then
    fail "Контейнер Aktiv Rutoken ECP не найден"
fi

# ---------------------------------------------------------------------------
# 3. Проверка поддержки CAdES-X Long Type 1
# ---------------------------------------------------------------------------

printf '[3/7] Проверка поддержки CAdES-X Long Type 1...\n'

"$CRYPTCP" -sign -help \
    </dev/null >"$CADES_HELP_OUTPUT" 2>&1 || true

cat "$CADES_HELP_OUTPUT" >>"$LOG_FILE"

if ! grep -qi 'xlongtype1' "$CADES_HELP_OUTPUT"; then
    fail "cryptcp не поддерживает -xlongtype1 либо не установлен CAdES Runtime"
fi

if ! grep -qi 'cadestsa' "$CADES_HELP_OUTPUT"; then
    fail "cryptcp не поддерживает параметр -cadesTSA"
fi

# ---------------------------------------------------------------------------
# 4. Получение и проверка TSP-штампа
# ---------------------------------------------------------------------------

printf '[4/7] Проверка TSP-службы через stunnel...\n'

if ! run_capture \
    "$TSP_OUTPUT" \
    "$TSPUTIL" \
    ms \
    --alg="$TSP_HASH_OID" \
    --url="$TSP_URL" \
    --cert-req \
    --nonce=yes \
    "$INPUT_FILE" \
    "$TSP_STAMP_FILE"
then
    rc=$?

    if [[ "$rc" -eq 124 || "$rc" -eq 137 ]]; then
        fail "Истекло время ожидания ответа TSP: ${TIMEOUT_SEC} секунд"
    fi

    fail "tsputil не смог получить или проверить TSP-штамп, код: $rc"
fi

if [[ ! -s "$TSP_STAMP_FILE" ]]; then
    fail "Файл TSP-штампа не создан или имеет нулевой размер"
fi

# ---------------------------------------------------------------------------
# 5. Чтение информации из TSP-штампа
# ---------------------------------------------------------------------------

printf '[5/7] Чтение информации из TSP-штампа...\n'

if ! run_capture \
    "$TSP_INFO_OUTPUT" \
    "$TSPUTIL" \
    si \
    "$TSP_STAMP_FILE"
then
    rc=$?
    fail "tsputil не смог прочитать полученный TSP-штамп, код: $rc"
fi

# ---------------------------------------------------------------------------
# 6. Создание CAdES-X Long Type 1
# ---------------------------------------------------------------------------

printf '[6/7] Создание CAdES-X Long Type 1...\n'

rm -f "$SIGNATURE_FILE"

run_capture \
    "$SIGN_OUTPUT" \
    "$CRYPTCP" \
    -sign \
    -uMy \
    -thumbprint "$THUMBPRINT" \
    -der \
    -detached \
    -xlongtype1 \
    -cadesTSA "$TSP_URL" \
    "$INPUT_FILE" \
    "$SIGNATURE_FILE"

SIGN_RC=$?

# Некоторые сборки cryptcp принимают URL только в объединённом виде:
#
#   -cadesTSAhttp://server/path
#
# Повторяем команду только при характерной ошибке парсинга URL.

if [[ "$SIGN_RC" -ne 0 ]] &&
    grep -Eqi \
        'URL of TSP service is not specified|URL службы TSP не указан|не указан.*URL.*TSP' \
        "$SIGN_OUTPUT"
then
    printf '      Повтор с объединённым параметром -cadesTSAURL...\n'

    rm -f "$SIGNATURE_FILE"

    run_capture \
        "$SIGN_RETRY_OUTPUT" \
        "$CRYPTCP" \
        -sign \
        -uMy \
        -thumbprint "$THUMBPRINT" \
        -der \
        -detached \
        -xlongtype1 \
        "-cadesTSA${TSP_URL}" \
        "$INPUT_FILE" \
        "$SIGNATURE_FILE"

    SIGN_RC=$?
fi

if [[ "$SIGN_RC" -eq 124 || "$SIGN_RC" -eq 137 ]]; then
    fail "Истекло время ожидания создания подписи: ${TIMEOUT_SEC} секунд"
fi

if [[ "$SIGN_RC" -ne 0 ]]; then
    fail "cryptcp не смог создать подпись, код: $SIGN_RC"
fi

if [[ ! -s "$SIGNATURE_FILE" ]]; then
    fail "cryptcp завершился без ошибки, но файл подписи отсутствует или пуст"
fi

# ---------------------------------------------------------------------------
# 7. Проверка CAdES-X Long Type 1
# ---------------------------------------------------------------------------

printf '[7/7] Проверка созданной CAdES-подписи...\n'

if ! run_capture \
    "$VERIFY_OUTPUT" \
    "$CRYPTCP" \
    -verify \
    -detached \
    -xlongtype1 \
    "$INPUT_FILE" \
    "$SIGNATURE_FILE"
then
    rc=$?

    if [[ "$rc" -eq 124 || "$rc" -eq 137 ]]; then
        fail "Истекло время ожидания проверки подписи"
    fi

    fail "Созданная подпись не прошла проверку, код: $rc"
fi

success
