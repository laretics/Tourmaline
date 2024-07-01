using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Collections.Specialized;
using System.Net.Http.Headers;

namespace TourmalineNetSDK
{
    public class CCTV
    {
        private const int MAX_BUFFER = 2048;
        public delegate bool onClientAck();

        //                         ff 00 00 00 00 00 00 00 00 00         ..........
        //0040   00 00 00 00 e8 03 64 00 00 00 7b 20 22 45 6e 63   ......d...{"Enc
        //0050   72 79 70 74 54 79 70 65 22 20 3a 20 22 4d 44 35   ryptType" : "MD5
        //0060   22 2c 20 22 4c 6f 67 69 6e 54 79 70 65 22 20 3a   ", "LoginType" :
        //0070   20 22 44 56 52 49 50 2d 57 65 62 22 2c 20 22 50    "DVRIP-Web", "P
        //0080   61 73 73 57 6f 72 64 22 20 3a 20 22 74 6c 4a 77   assWord" : "tlJw
        //0090   70 62 6f 36 22 2c 20 22 55 73 65 72 4e 61 6d 65   pbo6", "UserName
        //00a0   22 20 3a 20 22 61 64 6d 69 6e 22 20 7d 0a         " : "admin" }·

        protected List<ConnectionCredentials> mcolCredentials = new List<ConnectionCredentials>();
        public CCTV()
        {
            dummyInit();
        }

        public void Init()
        {

        }
        public void Terminate()
        {

        }

        private void dummyInit()
        {
            mcolCredentials.Add(new ConnectionCredentials(new IPAddress(new byte[] { 192, 168, 4, 11 }), 34567, "admin", "tlJwpbo6"));

        }
        protected async Task<string> initSession(ConnectionCredentials credentials)
        {
            byte[] header = composeHeader(0xe8, 0x03, 0x64);                                                                               
            string message = string.Format("\"EncryptType\" : \"MD5\", \"LoginType\" : \"DVRIP-Web\", \"PassWord\" : \"{0}\", \"UserName\" : \"{1}\""
                ,credentials.Password
                ,credentials.UserId);

            return await sendBlock(composeBuffer(header, message),credentials);
        }

        protected byte[] composeHeader(byte addr0, byte addr1, byte addr2)
        {
            byte[] salida = new byte[20];
            salida[0] = 255;
            salida[14] = addr0;
            salida[15] = addr1;
            salida[16] = addr2;
            return salida;
        }

        protected byte[] composeBuffer(byte[] header, string message)
        {
            byte[] buffer = new byte[MAX_BUFFER];
            byte[] resto = Encoding.ASCII.GetBytes(message);
            int dimenxion = header.Length + resto.Length;
            for (int i = 0; i < header.Length; i++)
                buffer[i] = header[i];
            for (int i=0;i<resto.Length;i++)
                buffer[i+header.Length]= resto[i];
            return buffer;
        }

        protected async Task<string> sendBlock (byte[] message, ConnectionCredentials destination)
        {
            TcpClient cliente = new TcpClient(destination.address);
            NetworkStream channel = cliente.GetStream();  
            await channel.WriteAsync(message, 0, message.Length);
            await channel.FlushAsync();
            if(channel.DataAvailable)
            {
                byte[] buffer = new byte[MAX_BUFFER];
                await channel.ReadAsync(buffer, 0, MAX_BUFFER);
                return Encoding.ASCII.GetString(buffer);
            }
            return string.Empty; //Mal si entra por aquí.
        }

        public class ConnectionCredentials
        {
            public IPEndPoint address { get; set; }
            public string UserId { get; set; }
            public string Password { get; set; }
            public ConnectionCredentials() { } //Constructor por defecto
            public ConnectionCredentials(IPAddress add,int portId, string userId, string password)
            {
                address = new IPEndPoint(add, portId);
                this.UserId = userId;
                this.Password = password;
            }
        }
    }    
}
