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
            public string mes; // текст повідомлення
            public string user; // ім'я користувача
            public Message()
            {

            }
        }
        public SynchronizationContext uiContext;

        public Form1()
        {
            InitializeComponent();
            // Отримаємо контекст синхронізації для поточного потоку 
            uiContext = SynchronizationContext.Current;
            WaitClientQuery();
        }

        // прийом повідомлення
        private async void WaitClientQuery()
        {
            await Task.Run(async () =>
            {
                try
                {
                    // Ініціалізуємо новий екземпляр класу UdpClient і зв'язуємо його з заданим номером локального порту.
                    UdpClient client = new UdpClient(49152 /* порт */); // приймаються всі вхідні з'єднання з локальною кінцевою точкою
                    while (true)
                    {
                        UdpReceiveResult result = await client.ReceiveAsync(); // отримаємо UDP-датаграму
                        IPEndPoint remote = result.RemoteEndPoint; // інформація про віддалений хост, який відправив датаграму
                        byte[] arr = result.Buffer; // датаграма
                        if (arr.Length > 0)
                        {
                            // Створимо потік, резервним сховищем якого є пам'ять.
                            MemoryStream stream = new MemoryStream(arr);
                            // XmlSerializer серіалізує та десеріалізує об'єкт у XML-форматі 
                            XmlSerializer serializer = new XmlSerializer(typeof(Message));
                            Message m = serializer.Deserialize(stream) as Message; // виконуємо десеріалізацію
                            // отриману від віддаленого вузла інформацію додаємо до списку
                            uiContext.Send(d => listBox1.Items.Add(remote.Address.ToString()), null);
                            uiContext.Send(d => listBox1.Items.Add(m.user), null);
                            uiContext.Send(d => listBox1.Items.Add(m.mes), null);
                            stream.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Отримувач: " + ex.Message);
                }
            });
        }

        // відправлення повідомлення
        private async void button1_Click(object sender, EventArgs e)
        {
            await Task.Run(async () =>
            {
                try
                {
                    // Ініціалізуємо новий екземпляр класу UdpClient і встановлюємо віддалений вузол
                    UdpClient client = new UdpClient(
                        textBox1.Text.ToString() /* IP-адреса віддаленого DNS-вузла, до якого планується підключення. */,
                        49152 /* Номер віддаленого порту, до якого планується виконати підключення. */ );
                    // Створимо потік, резервним сховищем якого є пам'ять.
                    MemoryStream stream = new MemoryStream();
                    // XmlSerializer серіалізує та десеріалізує об'єкт у XML-форматі 
                    XmlSerializer serializer = new XmlSerializer(typeof(Message));
                    Message m = new Message();
                    m.mes = textBox2.Text; // text повідомлення
                    m.user = Environment.UserDomainName + @"\" + Environment.UserName; // ім'я користувача
                    serializer.Serialize(stream, m); // виконуємо серіалізацію
                    byte[] arr = stream.ToArray(); // записуємо вміст потоку в байтовий масив
                    stream.Close();
                    await client.SendAsync(arr, arr.Length); // передаємо UDP-датаграму на віддалений вузол
                    client.Close(); // закриваємо UDP-підключення та звільняємо всі ресурси, пов'язані з об'єктом UdpClient.
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Відправник: " + ex.Message);
                }
            });
        }
    }
}