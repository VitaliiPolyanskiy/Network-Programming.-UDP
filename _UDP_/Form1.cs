using System;
using System.IO;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace _UDP_
{
    public partial class Form1 : Form
    {
        [Serializable]
        public class Message
        {
            public string mes; // текст сообщения
            public string user; // имя пользователя
            public Message()
            {

            }
        }
        public SynchronizationContext uiContext;

        public Form1()
        {
            InitializeComponent();
            // Получим контекст синхронизации для текущего потока 
            uiContext = SynchronizationContext.Current;
            WaitClientQuery();
        }

        // прием сообщения
        private async void WaitClientQuery()
        {
            await Task.Run(async() =>
            {
                try
                {
                    // Инициализируем новый экземпляр класса UdpClient и связываем его с заданным номером локального порта.
                    UdpClient client = new UdpClient(49152 /* порт */); // принимаются все входящие соединения с локальной конечной точкой
                    while (true)
                    {
                        UdpReceiveResult result = await client.ReceiveAsync(); // получим UDP-датаграмму
                        IPEndPoint remote = result.RemoteEndPoint; // информация об удаленном хосте, который отправил датаграмму
                        byte[] arr = result.Buffer; // датаграмма
                        if (arr.Length > 0)
                        {
                            // Создадим поток, резервным хранилищем которого является память.
                            MemoryStream stream = new MemoryStream(arr);
                            // XmlSerializer сериализует и десериализует объект в XML-формате 
                            XmlSerializer serializer = new XmlSerializer(typeof(Message));
                            Message m = serializer.Deserialize(stream) as Message; // выполняем десериализацию
                            // полученную от удаленного узла информацию добавляем в список
                            uiContext.Send(d => listBox1.Items.Add(remote.Address.ToString()), null);
                            uiContext.Send(d => listBox1.Items.Add(m.user), null);
                            uiContext.Send(d => listBox1.Items.Add(m.mes), null);
                            stream.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Получатель: " + ex.Message);
                }
            });
        }

        // отправление сообщения
        private async void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(async() =>
            {
                try
                {
                    // Инициализируем новый экземпляр класса UdpClient и устанавливаем удаленный узел
                    UdpClient client = new UdpClient(
                        textBox1.Text.ToString() /* IP-адрес удаленного DNS-узла, к которому планируется подключение. */,
                        49152 /* Номер удаленного порта, к которому планируется выполнить подключение. */ );
                    // Создадим поток, резервным хранилищем которого является память.
                    MemoryStream stream = new MemoryStream();
                    // XmlSerializer сериализует и десериализует объект в XML-формате 
                    XmlSerializer serializer = new XmlSerializer(typeof(Message));
                    Message m = new Message();
                    m.mes = textBox2.Text; // текст сообщения
                    m.user = Environment.UserDomainName + @"\" + Environment.UserName; // имя пользователя
                    serializer.Serialize(stream, m); // выполняем сериализацию
                    byte[] arr = stream.ToArray(); // записываем содержимое потока в байтовый массив
                    stream.Close();
                    await client.SendAsync(arr, arr.Length); // передаем UDP-датаграмму на удаленный узел
                    client.Close(); // закрываем UDP-подключение и освобождаем все ресурсы, связанные с объектом UdpClient.
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Отправитель: " + ex.Message);
                }
            });
        }
    }
}
