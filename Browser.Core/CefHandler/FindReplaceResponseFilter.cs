using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CefSharp;

namespace Browser.Core.CefHandler
{
    //https://stackoverflow.com/questions/38096349/modifying-remote-javascripts-as-they-load-with-cefsharp
    public class FindReplaceResponseFilter : IResponseFilter
    {
        private readonly List<byte> _overflow = new List<byte>();

        private readonly Dictionary<string, string> _dictionary;
        private int _findMatchOffset;

        private int _replaceCount;

        public FindReplaceResponseFilter(Dictionary<string, string> dictionary)
        {
            _dictionary = dictionary;
        }

        bool IResponseFilter.InitFilter()
        {
            return true;
        }

        FilterStatus IResponseFilter.Filter(Stream dataIn, out long dataInRead, Stream dataOut, out long dataOutWritten)
        {
            dataInRead = dataIn?.Length ?? 0;
            dataOutWritten = 0;

            if (_overflow.Count > 0) WriteOverflow(dataOut, ref dataOutWritten);

            for (var i = 0; i < dataInRead; ++i)
            {
                if (dataIn == null) continue;

                var readByte = (byte) dataIn.ReadByte();
                var charForComparison = Convert.ToChar(readByte);

                if (_replaceCount < _dictionary.Count)
                {
                    var replace = _dictionary.ElementAt(_replaceCount);
                    if (charForComparison == replace.Key[_findMatchOffset])
                    {
                        _findMatchOffset++;

                        if (_findMatchOffset == replace.Key.Length)
                        {
                            WriteString(replace.Value, replace.Value.Length, dataOut, ref dataOutWritten);


                            _findMatchOffset = 0;
                            _replaceCount++;
                        }

                        continue;
                    }

                    if (_findMatchOffset > 0)
                    {
                        WriteString(replace.Key, _findMatchOffset, dataOut, ref dataOutWritten);

                        _findMatchOffset = 0;
                    }
                }

                WriteSingleByte(readByte, dataOut, ref dataOutWritten);
            }

            if (_overflow.Count > 0) return FilterStatus.NeedMoreData;
            return _findMatchOffset > 0 ? FilterStatus.NeedMoreData : FilterStatus.Done;
        }

        public void Dispose()
        {
        }

        private void WriteOverflow(Stream dataOut, ref long dataOutWritten)
        {
            var remainingSpace = dataOut.Length - dataOutWritten;
            var maxWrite = Math.Min(_overflow.Count, remainingSpace);

            if (maxWrite > 0)
            {
                dataOut.Write(_overflow.ToArray(), 0, (int) maxWrite);
                dataOutWritten += maxWrite;
            }

            if (maxWrite < _overflow.Count)
                _overflow.RemoveRange(0, (int) (maxWrite - 1));
            else
                _overflow.Clear();
        }

        private void WriteString(string str, int stringSize, Stream dataOut, ref long dataOutWritten)
        {
            var remainingSpace = dataOut.Length - dataOutWritten;
            var maxWrite = Math.Min(stringSize, remainingSpace);
            if (maxWrite > 0)
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                dataOut.Write(bytes, 0, (int) maxWrite);
                dataOutWritten += maxWrite;
            }

            if (maxWrite < stringSize)
                _overflow.AddRange(
                    Encoding.UTF8.GetBytes(str.Substring((int) maxWrite, (int) (stringSize - maxWrite))));
        }

        private void WriteSingleByte(byte data, Stream dataOut, ref long dataOutWritten)
        {
            var remainingSpace = dataOut.Length - dataOutWritten;

            if (remainingSpace > 0)
            {
                dataOut.WriteByte(data);
                dataOutWritten += 1;
            }
            else
            {
                _overflow.Add(data);
            }
        }
    }
}