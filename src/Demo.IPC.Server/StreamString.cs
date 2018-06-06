using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Demo.IPC.Server
{
    public class StreamString
    {
        private Stream ioStream;
        private UnicodeEncoding streamEncoding;

        public StreamString(Stream ioStream)
        {
            this.ioStream = ioStream;
            streamEncoding = new UnicodeEncoding();
        }

        public string ReadString()
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            ioStream.Read(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public async Task<string> ReadStringAsync()
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            await ioStream.ReadAsync(inBuffer, 0, len);

            return streamEncoding.GetString(inBuffer);
        }

        public async Task<string> ReadStringAsync(CancellationToken cancellationToken)
        {
            int len = 0;

            len = ioStream.ReadByte() * 256;
            len += ioStream.ReadByte();
            byte[] inBuffer = new byte[len];
            await ioStream.ReadAsync(inBuffer, 0, len, cancellationToken);

            return streamEncoding.GetString(inBuffer);
        }

        public int WriteString(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            ioStream.Write(outBuffer, 0, len);
            ioStream.Flush();

            return outBuffer.Length + 2;
        }

        public async Task<int> WriteStringAsync(string outString)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            Log.Logger.Verbose("Write size byte to stream");
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            Log.Logger.Verbose("Write content to stream");
            await ioStream.WriteAsync(outBuffer, 0, len);
            Log.Logger.Verbose("Flush stream");
            await ioStream.FlushAsync();

            return outBuffer.Length + 2;
        }

        public async Task<int> WriteStringAsync(string outString, CancellationToken cancellationToken)
        {
            byte[] outBuffer = streamEncoding.GetBytes(outString);
            int len = outBuffer.Length;
            if (len > UInt16.MaxValue)
            {
                len = (int)UInt16.MaxValue;
            }
            Log.Logger.Verbose("Write size byte to stream");
            ioStream.WriteByte((byte)(len / 256));
            ioStream.WriteByte((byte)(len & 255));
            Log.Logger.Verbose("Write content to stream");
            await ioStream.WriteAsync(outBuffer, 0, len, cancellationToken);
            Log.Logger.Verbose("Flush stream");
            await ioStream.FlushAsync(cancellationToken);

            return outBuffer.Length + 2;
        }
    }
}
