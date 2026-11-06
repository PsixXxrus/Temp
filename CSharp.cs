
@page "/activemq-send"
@rendermode InteractiveServer

@using System.IO
@using System.Linq
@using System.Text
@using System.Xml
@using System.Xml.Linq
@using Apache.NMS
@using Apache.NMS.ActiveMQ
@using Microsoft.AspNetCore.Components.Forms

@inject IConfiguration Configuration

<div class="container py-4">
    <div class="card border-0 shadow-sm">

        <!-- Заголовок -->
        <div class="card-header bg-primary text-white p-4">
            <h4 class="mb-2">
                Отправка XML в ActiveMQ
            </h4>

            <p class="mb-3 opacity-75">
                Загрузите XML-документы для передачи
                в очередь сообщений ActiveMQ.
                Каждый файл отправляется отдельным
                текстовым сообщением.
            </p>

            <span class="badge bg-light text-primary me-2">
                Очередь: @QueueName
            </span>

            <span class="badge bg-light text-primary">
                Persistent
            </span>
        </div>

        <div class="card-body p-4">

            <!-- Описание -->
            <div class="alert alert-light border mb-4">
                <h6 class="fw-semibold">
                    Как работает отправка?
                </h6>

                <p class="small text-secondary mb-0">
                    Выберите один или несколько XML-файлов.
                    Система прочитает их содержимое и
                    проверит корректность XML.

                    После нажатия кнопки отправки
                    каждый документ будет передан
                    в очередь ActiveMQ.

                    По результатам отобразятся
                    статусы и идентификаторы сообщений.
                </p>
            </div>

            <!-- Загрузка -->
            <div class="mb-4">

                <label class="form-label fw-semibold">
                    Выберите XML-файлы
                </label>

                <InputFile OnChange="SelectFiles"
                           accept=".xml"
                           multiple
                           disabled="@isBusy"
                           class="form-control" />

                <div class="form-text">
                    До 5 файлов, размер каждого — не более 10 МБ.
                    Поддерживается одновременный выбор файлов.
                </div>

            </div>

            @if (isBusy)
            {
                <div class="alert alert-info">
                    <span class="spinner-border spinner-border-sm me-2">
                    </span>
                    Выполняется обработка...
                </div>
            }

            @if (!string.IsNullOrEmpty(error))
            {
                <div class="alert alert-danger">
                    @error
                </div>
            }

            <!-- Список файлов -->
            @if (files.Count > 0)
            {
                <div class="mb-4">

                    <h6 class="fw-semibold mb-3">
                        Выбранные файлы
                        <span class="badge bg-secondary">
                            @files.Count
                        </span>
                    </h6>

                    <div class="table-responsive">
                        <table class="table table-hover align-middle">

                            <thead class="table-light">
                                <tr>
                                    <th>Файл</th>
                                    <th>Размер</th>
                                    <th>Статус</th>
                                </tr>
                            </thead>

                            <tbody>
                                @foreach (var file in files)
                                {
                                    <tr>
                                        <td>
                                            <div class="fw-medium">
                                                @file.Name
                                            </div>

                                            @if (!string.IsNullOrEmpty(file.MessageId))
                                            {
                                                <div class="small text-muted text-break">
                                                    ID: @file.MessageId
                                                </div>
                                            }

                                            @if (!string.IsNullOrEmpty(file.Error))
                                            {
                                                <div class="small text-danger">
                                                    @file.Error
                                                </div>
                                            }
                                        </td>

                                        <td>
                                            @FormatSize(file.Size)
                                        </td>

                                        <td>
                                            <span class="badge @GetBadgeClass(file.Status)">
                                                @GetStatusText(file.Status)
                                            </span>
                                        </td>
                                    </tr>
                                }
                            </tbody>

                        </table>
                    </div>

                </div>
            }

            <!-- Отправка -->
            <div class="d-flex flex-wrap gap-2">

                <button type="button"
                        class="btn btn-primary px-4"
                        disabled="@(!CanSend)"
                        @onclick="SendFiles">

                    @if (isBusy)
                    {
                        <span class="spinner-border spinner-border-sm me-2">
                        </span>
                    }

                    @(files.Any(f => f.Status == FileStatus.Failed)
                        ? "Повторить отправку"
                        : "Отправить в ActiveMQ")
                </button>

            </div>

            <!-- Результат -->
            @if (sendAttempted)
            {
                <div class="alert @(FailedCount == 0 ? "alert-success" : "alert-warning") mt-4 mb-0">

                    <h6 class="fw-semibold">
                        Результат отправки
                    </h6>

                    <div>
                        Успешно отправлено:
                        <strong>@SentCount</strong>
                    </div>

                    <div>
                        Ошибок отправки:
                        <strong>@FailedCount</strong>
                    </div>

                    <div>
                        Очередь:
                        <strong>@QueueName</strong>
                    </div>

                </div>
            }

        </div>
    </div>
</div>

@code {

    private const long MaxFileSize = 10 * 1024 * 1024;
    private const int MaxFiles = 5;

    private readonly List<UploadedXml> files = new();

    private bool isBusy;
    private bool sendAttempted;
    private string error = "";

    private string QueueName =>
        Configuration["ActiveMq:QueueName"] ?? "correqts_out";

    private bool CanSend =>
        !isBusy &&
        files.Any(f => f.Status == FileStatus.Ready ||
                       f.Status == FileStatus.Failed);

    private int SentCount =>
        files.Count(f => f.Status == FileStatus.Sent);

    private int FailedCount =>
        files.Count(f => f.Status == FileStatus.Failed);

    // Модель загруженного файла
    private class UploadedXml
    {
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public string Content { get; set; } = "";
        public FileStatus Status { get; set; } = FileStatus.Ready;
        public string? MessageId { get; set; }
        public string? Error { get; set; }
    }

    private enum FileStatus
    {
        Ready,
        Invalid,
        Sent,
        Failed
    }

    private record SendResult(
        UploadedXml File,
        bool Success,
        string? MessageId,
        string? Error
    );

    // 1. ЗАГРУЗКА ФАЙЛОВ
    private async Task SelectFiles(InputFileChangeEventArgs e)
    {
        files.Clear();
        error = "";
        sendAttempted = false;
        isBusy = true;

        try
        {
            foreach (var file in e.GetMultipleFiles(MaxFiles))
            {
                var item = new UploadedXml
                {
                    Name = Path.GetFileName(file.Name),
                    Size = file.Size
                };

                files.Add(item);

                try
                {
                    if (!Path.GetExtension(file.Name)
                        .Equals(".xml", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception("Допускаются только XML-файлы.");
                    }

                    if (file.Size > MaxFileSize)
                    {
                        throw new Exception("Размер превышает 10 МБ.");
                    }

                    // Читаем содержимое файла
                    await using var stream =
                        file.OpenReadStream(MaxFileSize);

                    using var reader = new StreamReader(
                        stream,
                        new UTF8Encoding(false, true),
                        detectEncodingFromByteOrderMarks: true);

                    string xml = await reader.ReadToEndAsync();

                    // Проверяем корректность XML
                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null
                    };

                    using var xmlReader = XmlReader.Create(
                        new StringReader(xml),
                        settings);

                    XDocument.Load(xmlReader);

                    // Сохраняем оригинальный XML-текст
                    item.Content = xml;
                    item.Status = FileStatus.Ready;
                }
                catch (Exception ex)
                {
                    item.Status = FileStatus.Invalid;
                    item.Error = ex.Message;
                }
            }
        }
        catch (Exception ex)
        {
            error = "Ошибка выбора файлов: " + ex.Message;
        }
        finally
        {
            isBusy = false;
        }
    }

    // 2. ОТПРАВКА ФАЙЛОВ
    private async Task SendFiles()
    {
        if (!CanSend)
            return;

        isBusy = true;
        error = "";
        sendAttempted = true;

        var batch = files
            .Where(f => f.Status == FileStatus.Ready ||
                        f.Status == FileStatus.Failed)
            .ToList();

        try
        {
            // Синхронный API ActiveMQ запускаем
            // вне потока обработки интерфейса.
            var results = await Task.Run(() => SendBatch(batch));

            foreach (var result in results)
            {
                result.File.MessageId = result.MessageId;
                result.File.Error = result.Error;

                result.File.Status = result.Success
                    ? FileStatus.Sent
                    : FileStatus.Failed;
            }
        }
        catch (Exception ex)
        {
            error = "Ошибка подключения к ActiveMQ: " + ex.Message;

            foreach (var file in batch)
            {
                file.Status = FileStatus.Failed;
                file.Error = "Сообщение не отправлено.";
            }
        }
        finally
        {
            isBusy = false;
        }
    }

    // 3. ПОДКЛЮЧЕНИЕ К ACTIVEMQ
    private List<SendResult> SendBatch(List<UploadedXml> batch)
    {
        string brokerUrl =
            Configuration["ActiveMq:BrokerUrl"]
            ?? throw new Exception("Не указан BrokerUrl.");

        string username =
            Configuration["ActiveMq:Username"]
            ?? throw new Exception("Не указан Username.");

        string password =
            Configuration["ActiveMq:Password"]
            ?? throw new Exception("Не указан Password.");

        string queueName =
            Configuration["ActiveMq:QueueName"]
            ?? throw new Exception("Не указан QueueName.");

        var results = new List<SendResult>();

        var factory = new ConnectionFactory(brokerUrl);

        using IConnection connection =
            factory.CreateConnection(username, password);

        connection.Start();

        using ISession session =
            connection.CreateSession(
                AcknowledgementMode.AutoAcknowledge);

        IQueue queue = session.GetQueue(queueName);

        using IMessageProducer producer =
            session.CreateProducer(queue);

        producer.DeliveryMode = MsgDeliveryMode.Persistent;

        // Каждый XML отправляем отдельным сообщением
        foreach (var file in batch)
        {
            try
            {
                ITextMessage message =
                    session.CreateTextMessage(file.Content);

                producer.Send(message);

                results.Add(new SendResult(
                    file,
                    true,
                    message.NMSMessageId,
                    null));
            }
            catch (Exception ex)
            {
                results.Add(new SendResult(
                    file,
                    false,
                    null,
                    ex.Message));
            }
        }

        return results;
    }

    // 4. ОФОРМЛЕНИЕ
    private static string FormatSize(long bytes)
    {
        return $"{bytes / 1024.0:N1} КБ";
    }

    private static string GetBadgeClass(FileStatus status)
    {
        return status switch
        {
            FileStatus.Ready => "bg-secondary",
            FileStatus.Invalid => "bg-danger",
            FileStatus.Sent => "bg-success",
            FileStatus.Failed => "bg-danger",
            _ => "bg-secondary"
        };
    }

    private static string GetStatusText(FileStatus status)
    {
        return status switch
        {
            FileStatus.Ready => "Готов",
            FileStatus.Invalid => "Некорректный",
            FileStatus.Sent => "Отправлен",
            FileStatus.Failed => "Ошибка",
            _ => "Неизвестно"
        };
    }
}















<!-- Описание инструмента -->
<div class="mb-4">

    <div class="d-flex align-items-center gap-2 mb-3">
        <i class="bi bi-file-earmark-code fs-4 text-primary"></i>
        <h5 class="mb-0">
            Обработка банковской выписки XML
        </h5>
    </div>

    <p class="text-secondary mb-3">
        Инструмент предназначен для автоматической фильтрации
        операций банковской выписки по заданным параметрам.
        Позволяет быстро выделить необходимые платежи из
        большого количества транзакций без ручной обработки файла.
    </p>

    <!-- Условия фильтрации -->
    <div class="bg-body-tertiary rounded-3 p-3 mb-3">

        <h6 class="fw-semibold mb-2">
            Условия отбора операций
        </h6>

        <p class="small text-secondary mb-3">
            В итоговый XML-файл попадут только операции,
            соответствующие всем перечисленным условиям:
        </p>

        <div class="table-responsive">
            <table class="table table-sm table-borderless
                          align-middle mb-0">

                <tbody>
                    <tr>
                        <td class="text-secondary">
                            Тип операции
                        </td>
                        <td class="fw-medium">
                            <code>01</code>
                        </td>
                    </tr>

                    <tr>
                        <td class="text-secondary">
                            Счёт плательщика
                        </td>
                        <td class="fw-medium">
                            Начинается с <code>40602810</code>
                        </td>
                    </tr>

                    <tr>
                        <td class="text-secondary">
                            ИНН получателя
                        </td>
                        <td class="fw-medium">
                            Начинается с <code>860000</code>
                        </td>
                    </tr>

                    <tr>
                        <td class="text-secondary">
                            Счёт получателя
                        </td>
                        <td class="fw-medium">
                            Пустой или отсутствует
                        </td>
                    </tr>

                    <tr>
                        <td class="text-secondary">
                            Назначение платежа
                        </td>
                        <td class="fw-medium">
                            Содержит <code>ДОГОВОР 200</code>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </div>

    <!-- Информация о сохранении -->
    <div class="alert alert-success d-flex
                align-items-start gap-2 mb-0"
         role="alert">

        <div>
            <strong>Исходный файл не изменяется</strong>

            <div class="small mt-1">
                После обработки создаётся отдельный XML-файл
                с суффиксом <code>_new</code>.
                Все неподходящие операции удаляются,
                а структура выписки и остальные данные сохраняются.
            </div>
        </div>
    </div>

</div>





@page "/xml-filter"
@rendermode InteractiveServer

@using System.IO
@using System.Linq
@using System.Xml.Linq
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.JSInterop

@inject IJSRuntime JS

<div class="container py-4">
    <div class="card shadow-sm">
        <div class="card-body">

            <h5 class="card-title mb-4">
                Фильтрация XML-операций
            </h5>

            <!-- Загрузка XML -->
            <div class="mb-3">
                <label class="form-label fw-semibold">
                    Выберите XML-файл
                </label>

                <InputFile OnChange="UploadFile"
                           accept=".xml"
                           class="form-control" />
            </div>

            @if (isLoading)
            {
                <div class="alert alert-info">
                    Чтение и анализ XML-файла...
                </div>
            }

            @if (!string.IsNullOrEmpty(error))
            {
                <div class="alert alert-danger">
                    @error
                </div>
            }

            <!-- Статистика после загрузки -->
            @if (sourceDocument != null)
            {
                <div class="mb-3 text-muted small">
                    Файл: @fileName
                </div>

                <div class="row g-3 mb-4">

                    <div class="col-md-4">
                        <div class="border rounded p-3 text-center">
                            <div class="text-muted small">
                                Всего операций
                            </div>
                            <h3>@totalCount</h3>
                        </div>
                    </div>

                    <div class="col-md-4">
                        <div class="border rounded p-3 text-center">
                            <div class="text-muted small">
                                Подходят
                            </div>
                            <h3 class="text-success">
                                @validCount
                            </h3>
                        </div>
                    </div>

                    <div class="col-md-4">
                        <div class="border rounded p-3 text-center">
                            <div class="text-muted small">
                                Не подходят
                            </div>
                            <h3 class="text-danger">
                                @(totalCount - validCount)
                            </h3>
                        </div>
                    </div>

                </div>
            }

            <!-- Кнопки -->
            <div class="d-flex gap-2">

                <button class="btn btn-primary"
                        disabled="@(sourceDocument == null || isLoading)"
                        @onclick="ConvertXml">
                    Преобразовать
                </button>

                <button class="btn btn-success"
                        disabled="@(resultBytes == null || isLoading)"
                        @onclick="DownloadXml">
                    Скачать
                </button>

            </div>

            <!-- Результат преобразования -->
            @if (resultBytes != null)
            {
                <div class="alert alert-success mt-4 mb-0">
                    <strong>XML успешно преобразован!</strong>

                    <div class="mt-2">
                        Всего операций: @totalCount
                    </div>
                    <div>
                        Удалено: @removedCount
                    </div>
                    <div>
                        Оставлено: @validCount
                    </div>
                    <div class="mt-2">
                        Новый файл: <b>@outputFileName</b>
                    </div>
                </div>
            }

        </div>
    </div>
</div>

@code {

    // Максимальный размер файла: 20 МБ
    private const long MaxFileSize = 20 * 1024 * 1024;

    private XDocument? sourceDocument;
    private byte[]? resultBytes;

    private string fileName = "";
    private string outputFileName = "";
    private string error = "";

    private int totalCount = 0;
    private int validCount = 0;
    private int removedCount = 0;

    private bool isLoading = false;

    // 1. ЗАГРУЗКА И АНАЛИЗ XML
    private async Task UploadFile(InputFileChangeEventArgs e)
    {
        sourceDocument = null;
        resultBytes = null;

        totalCount = 0;
        validCount = 0;
        removedCount = 0;

        error = "";
        fileName = "";
        outputFileName = "";

        isLoading = true;

        try
        {
            var file = e.File;

            fileName = Path.GetFileName(file.Name);

            outputFileName =
                Path.GetFileNameWithoutExtension(fileName)
                + "_new"
                + Path.GetExtension(fileName);

            await using var stream =
                file.OpenReadStream(MaxFileSize);

            sourceDocument = await XDocument.LoadAsync(
                stream,
                LoadOptions.PreserveWhitespace,
                CancellationToken.None);

            bool hasOperations = sourceDocument
                .Descendants()
                .Any(x => x.Name.LocalName == "operations");

            if (!hasOperations)
                throw new Exception("Раздел operations не найден.");

            // Находим все операции
            var operations = GetOperations(sourceDocument);

            totalCount = operations.Count;

            // Считаем совпадения, ничего не удаляем
            validCount = operations.Count(IsValid);
        }
        catch (Exception ex)
        {
            sourceDocument = null;
            error = "Ошибка чтения XML: " + ex.Message;
        }
        finally
        {
            isLoading = false;
        }
    }

    // 2. ПОЛУЧЕНИЕ ВСЕХ OPERATION
    private static List<XElement> GetOperations(XDocument doc)
    {
        return doc.Descendants()
            .Where(x => x.Name.LocalName == "operations")
            .SelectMany(x => x.Elements()
                .Where(o => o.Name.LocalName == "Operation"))
            .ToList();
    }

    // 3. ПОЛУЧЕНИЕ ЗНАЧЕНИЯ ТЕГА
    private static string GetValue(XElement operation, string name)
    {
        return operation.Elements()
            .FirstOrDefault(x => x.Name.LocalName == name)?
            .Value.Trim() ?? "";
    }

    // 4. ПРОВЕРКА УСЛОВИЙ ФИЛЬТРАЦИИ
    private static bool IsValid(XElement operation)
    {
        string operationType =
            GetValue(operation, "operationType");

        string payerAccount =
            GetValue(operation, "payerAccount");

        string receiverINN =
            GetValue(operation, "receiverINN");

        string receiverAccount =
            GetValue(operation, "receiverAccount");

        return
            operationType == "01" &&
            payerAccount.StartsWith("40602810") &&
            receiverINN.StartsWith("860000") &&
            string.IsNullOrWhiteSpace(receiverAccount);
    }

    // 5. ПРЕОБРАЗОВАНИЕ XML
    private void ConvertXml()
    {
        if (sourceDocument == null)
            return;

        // Создаём копию оригинального XML
        var doc = new XDocument(sourceDocument);

        var operations = GetOperations(doc);

        removedCount = 0;

        foreach (var operation in operations)
        {
            if (!IsValid(operation))
            {
                operation.Remove();
                removedCount++;
            }
        }

        // Сохраняем результат в память
        using var memoryStream = new MemoryStream();

        doc.Save(
            memoryStream,
            SaveOptions.DisableFormatting);

        resultBytes = memoryStream.ToArray();
    }

    // 6. СКАЧИВАНИЕ НОВОГО XML
    private async Task DownloadXml()
    {
        if (resultBytes == null)
            return;

        using var stream = new MemoryStream(resultBytes);

        using var streamRef =
            new DotNetStreamReference(stream);

        var module = await JS.InvokeAsync<IJSObjectReference>(
            "import", "./js/xmlDownload.js");

        try
        {
            await module.InvokeVoidAsync(
                "downloadFileFromStream",
                outputFileName,
                streamRef);
        }
        finally
        {
            await module.DisposeAsync();
        }
    }
}



export async function downloadFileFromStream(fileName, streamReference) {
    const arrayBuffer = await streamReference.arrayBuffer();

    const blob = new Blob([arrayBuffer], {
        type: "application/xml"
    });

    const url = URL.createObjectURL(blob);

    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;

    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();

    URL.revokeObjectURL(url);
}



private static bool IsValid(XElement operation)
{
    string operationType =
        GetValue(operation, "operationType");

    string payerAccount =
        GetValue(operation, "payerAccount");

    string receiverINN =
        GetValue(operation, "receiverINN");

    string receiverAccount =
        GetValue(operation, "receiverAccount");

    string paymentPurpose =
        GetValue(operation, "paymentPurpose");

    return
        operationType == "01" &&
        payerAccount.StartsWith("40602810") &&
        receiverINN.StartsWith("860000") &&
        string.IsNullOrWhiteSpace(receiverAccount) &&
        paymentPurpose.Contains(
            "ДОГОВОР 200",
            StringComparison.OrdinalIgnoreCase);
}

































using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

Console.Write("Укажите путь к XML-файлу: ");
string inputPath = Console.ReadLine()?.Trim().Trim('"') ?? "";

if (!File.Exists(inputPath))
{
    Console.WriteLine("Файл не найден!");
    return;
}

// Загружаем XML
XDocument doc = XDocument.Load(inputPath, LoadOptions.PreserveWhitespace);

// Находим раздел operations
var operations = doc.Descendants()
    .FirstOrDefault(x => x.Name.LocalName == "operations");

if (operations == null)
{
    Console.WriteLine("Элемент operations не найден!");
    return;
}

// Получение значения элемента
string GetValue(XElement operation, string name)
{
    return operation.Elements()
        .FirstOrDefault(x => x.Name.LocalName == name)?
        .Value.Trim() ?? "";
}

// Получаем все операции
var allOperations = operations.Elements()
    .Where(x => x.Name.LocalName == "Operation")
    .ToList();

int removed = 0;

foreach (var operation in allOperations)
{
    string operationType = GetValue(operation, "operationType");
    string payerAccount = GetValue(operation, "payerAccount");
    string receiverINN = GetValue(operation, "receiverINN");
    string receiverAccount = GetValue(operation, "receiverAccount");

    // Проверяем условия
    bool isValid =
        operationType == "01" &&
        payerAccount.StartsWith("40602810") &&
        receiverINN.StartsWith("860000") &&
        string.IsNullOrWhiteSpace(receiverAccount);

    // Удаляем неподходящие операции
    if (!isValid)
    {
        operation.Remove();
        removed++;
    }
}

// Формируем имя нового файла
string directory = Path.GetDirectoryName(inputPath) ?? "";
string fileName = Path.GetFileNameWithoutExtension(inputPath);
string extension = Path.GetExtension(inputPath);

string outputPath = Path.Combine(
    directory,
    fileName + "_new" + extension
);

// Сохраняем новый XML
doc.Save(outputPath, SaveOptions.DisableFormatting);

Console.WriteLine($"Всего операций: {allOperations.Count}");
Console.WriteLine($"Удалено: {removed}");
Console.WriteLine($"Оставлено: {allOperations.Count - removed}");
Console.WriteLine($"Результат: {outputPath}");






using System;
using Apache.NMS.ActiveMQ.OpenWire;

var assembly = typeof(OpenWireFormat).Assembly;

Console.WriteLine($"DLL: {assembly.Location}");

foreach (int version in new[] { 1, 10 })
{
    string typeName =
        $"Apache.NMS.ActiveMQ.OpenWire.V{version}.MarshallerFactory";

    Type? type = assembly.GetType(typeName, false);

    Console.WriteLine(
        $"OpenWire V{version}: {(type != null ? "OK" : "NOT FOUND")}");
}

try
{
    var wireFormat = new OpenWireFormat();
    Console.WriteLine("OpenWireFormat создан успешно");
}
catch (Exception ex)
{
    Console.WriteLine("Ошибка OpenWireFormat:");
    Console.WriteLine(ex.ToString());
}



using Apache.NMS.ActiveMQ.Transport;
using Apache.NMS.ActiveMQ.Transport.Tcp;

// Проверяем реально загруженную DLL
var tcpType = typeof(TcpTransportFactory);

Console.WriteLine($"DLL: {tcpType.Assembly.Location}");
Console.WriteLine($"Assembly: {tcpType.Assembly.FullName}");

var ctor = tcpType.GetConstructor(Type.EmptyTypes);

Console.WriteLine(
    $"Публичный конструктор без параметров: {ctor != null}");

if (ctor == null)
{
    throw new InvalidOperationException(
        "В загруженной DLL отсутствует конструктор TcpTransportFactory()");
}

// Явный вызов сохраняет зависимость от конструктора
var tcpFactory = new TcpTransportFactory();

// Проверяем создание через reflection
var reflectedFactory = Activator.CreateInstance(tcpType);

Console.WriteLine("Reflection работает");

// Регистрируем TCP
new TransportFactory().RegisterTransportFactory(
    "tcp",
    tcpType);

// Подключаемся к существующему OpenWire
var factory = new Apache.NMS.ActiveMQ.ConnectionFactory(
    "tcp://127.0.0.1:6161");

using var connection = factory.CreateConnection(
    "admin",
    "admin");

connection.Start();

GC.KeepAlive(tcpFactory);

Console.WriteLine("Подключение успешно!");



using System;
using Apache.NMS.ActiveMQ;

Console.WriteLine("Apache.NMS: " +
    typeof(Apache.NMS.IConnection).Assembly.GetName().Version);

Console.WriteLine("Apache.NMS.ActiveMQ: " +
    typeof(ConnectionFactory).Assembly.GetName().Version);

try
{
    var factory = new ConnectionFactory(
        "tcp://127.0.0.1:6161");

    using var connection =
        factory.CreateConnection("admin", "admin");

    connection.Start();

    Console.WriteLine("Подключение успешно!");
}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}

using System;
using System.IO;
using System.Text;
using Apache.NMS;
using Apache.NMS.ActiveMQ;

class Program
{
    static void Main(string[] args)
    {
        // Настройки ActiveMQ
        const string brokerUrl = "tcp://localhost:61616";
        const string username = "admin";
        const string password = "admin";
        const string queueName = "correqts_out";

        if (args.Length == 0 || !File.Exists(args[0]))
        {
            Console.WriteLine("Укажите путь к XML-файлу.");
            return;
        }

        try
        {
            // Читаем XML целиком
            string xml = File.ReadAllText(args[0]);

            // Подключаемся к ActiveMQ
            var factory = new ConnectionFactory(brokerUrl);

            using IConnection connection =
                factory.CreateConnection(username, password);

            connection.Start();

            using ISession session =
                connection.CreateSession(
                    AcknowledgementMode.AutoAcknowledge);

            // Выбираем очередь
            IQueue queue = session.GetQueue(queueName);

            using IMessageProducer producer =
                session.CreateProducer(queue);

            // Сохранять сообщение при перезапуске брокера
            producer.DeliveryMode = MsgDeliveryMode.Persistent;

            // Создаем одно текстовое сообщение
            ITextMessage message = session.CreateTextMessage(xml);

            // Отправляем XML в очередь
            producer.Send(message);

            Console.WriteLine("XML успешно отправлен!");
            Console.WriteLine($"Очередь: {queueName}");
            Console.WriteLine($"Размер: {Encoding.UTF8.GetByteCount(xml)} байт");
            Console.WriteLine($"Message ID: {message.NMSMessageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex}");
            Environment.ExitCode = 1;
        }
    }
}
