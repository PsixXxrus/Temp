
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
