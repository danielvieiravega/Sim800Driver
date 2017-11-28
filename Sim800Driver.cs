using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;


namespace TCC.Sim800Driver
{
    public class Sim800LDriver
    {
        private DataReader _reader;
        private DataWriter _writer;

        public async Task<bool> InitializeConnection()
        {
            var result = false;
            var filter = SerialDevice.GetDeviceSelector("UART0");
            var devices = await DeviceInformation.FindAllAsync(filter);
            if (devices.Any())
            {
                var deviceId = devices.First().Id;
                var serialDevice = await SerialDevice.FromIdAsync(deviceId);

                if (serialDevice != null)
                {
                    serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                    serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                    serialDevice.BaudRate = 9600;
                    serialDevice.StopBits = SerialStopBitCount.One;
                    serialDevice.DataBits = 8;
                    serialDevice.Parity = SerialParity.None;
                    serialDevice.Handshake = SerialHandshake.None;

                    _reader = new DataReader(serialDevice.InputStream)
                    {
                        InputStreamOptions = InputStreamOptions.Partial
                    };
                    _writer = new DataWriter(serialDevice.OutputStream);

                    result = true;
                }
            }

            return result;
        }

        public async Task<bool> SendSms(string message, string phoneNumber)
        {
            var response = string.Empty;

            await WriteAsync("AT\r");
            response = await ReadAsync();

            await WriteAsync("AT+CMGF=1\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();
            
            await WriteAsync("AT+CMGS=\"" + phoneNumber + "\"" + "\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync(message + char.ConvertFromUtf32(26) + "\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT\r");
            response = await ReadAsync();

            return response.Contains("OK");
        }

        public async Task<bool> ConnectToInternet()
        {
            var response = string.Empty;

            await WriteAsync("AT+CSQ\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();
            //AT+CIFSR
            await WriteAsync("AT+SAPBR=3,1,\"Contype\",\"GPRS\"\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+SAPBR=3,1,\"APN\",\"bandalarga.claro.com.br\"\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+SAPBR=3,1,\"USER\",\"claro\"\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+SAPBR=3,1,\"PWD\",\"claro\"\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+SAPBR=1,1\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+SAPBR=2,1\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+HTTPINIT\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+HTTPPARA=\"CID\",1\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+HTTPPARA=\"URL\",\"http://web25.redehost.com.br/info.php\"\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT+HTTPACTION=0\r");
            await Task.Delay(TimeSpan.FromSeconds(10));
            response = await ReadAsync();

            await WriteAsync("AT+HTTPREAD\r");
            await Task.Delay(TimeSpan.FromSeconds(1));
            response = await ReadAsync();

            await WriteAsync("AT\r");
            response = await ReadAsync();

            //await WriteAsync("AT\r");
            //response = await ReadAsync();

            //await WriteAsync("AT+CPIN?\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CREG?\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CGATT?\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CIPSHUT\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CIPSTATUS\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CIPMUX=0\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync(@"AT+CSTT=""claro.com.br"",""claro"",""claro""\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CIICR\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CIFSR\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync(@"AT+CIPSTART= ""TCP"", ""google.com"", ""80""\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT+CIPSEND\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            //await WriteAsync("AT\r");
            //await Task.Delay(TimeSpan.FromSeconds(1));
            //response = await ReadAsync();

            return false;
        }
        
        public async Task<List<ShortMessage>> ReadSms()
        {
            var result = string.Empty;

            await WriteAsync("AT\r");
            result = await ReadAsync();

            await WriteAsync("AT+CMGF=1\r");
            result = await ReadAsync();

            await WriteAsync("AT+CPMS=\"SM\"" + "\r");
            result = await ReadAsync();

            await WriteAsync("AT+CMGL=\"ALL\"" + "\r");
            result = await ReadAsync();

            /*
               "REC UNREAD" Received unread messages
               "REC READ" Received read messages
               "STO UNSENT" Stored unsent messages
               "STO SENT" Stored sent messages
               "ALL" All messages
             */

            var unReadMessages = new List<ShortMessage>();
            try
            {
                var messages = ParseMessages(result);
                unReadMessages = messages.Where(m => m.Status.Contains("UNREAD")).ToList();
            }
            catch (Exception)
            {
                //Ignored
            }
            
            return unReadMessages;
        }
        
        public List<ShortMessage> ParseMessages(string input)
        {
            var messages = new List<ShortMessage>();
            try
            {
                var r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
                var m = r.Match(input);
                while (m.Success)
                {
                    var msg = new ShortMessage
                    {
                        Index = m.Groups[1].Value,
                        Status = m.Groups[2].Value,
                        Sender = m.Groups[3].Value,
                        Alphabet = m.Groups[4].Value,
                        Sent = m.Groups[5].Value,
                        Message = m.Groups[6].Value
                    };

                    messages.Add(msg);

                    m = m.NextMatch();
                }
            }
            catch (Exception ex)
            {
                var x = ex;
            }
            
            return messages;
        }

        public async Task<string> ReadAsync()
        {
            var result = "nothing";
            try
            {
                var loadAsyncTask = _reader.LoadAsync(1024).AsTask();

                var bytesRead = await loadAsyncTask;

                if (bytesRead > 0)
                {
                    result = _reader.ReadString(bytesRead);
                }
            }
            catch (Exception e)
            {
                var x = e;
            }
           
            
            return result;
        }

        private async Task<bool> WriteAsync(string command)
        {
            var result = false;

            try
            {
                _writer.WriteString(command);

                var storeAsyncTask = _writer.StoreAsync().AsTask();

                var bytesWritten = await storeAsyncTask;

                if (bytesWritten > 0)
                {
                    result = true;
                }
            }
            catch (Exception e)
            {
                var x = e;
            }

            return result;
        }
    }
}
