
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
