using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DarkBotBrowser.Communication.In
{
    public class IncomingPacket
    {
        private int _counter = 1;
        private readonly string[] _data;
        public IncomingPacket(string data)
        {
            _data = data.Split('|');
        }

        public string Header => _data[0];

        public string Next => _data[_counter++];
        public int NextInt => int.Parse(_data[_counter++]);
        public long NextLong => long.Parse(_data[_counter++]);
        public double NextDouble => int.Parse(_data[_counter++]);
        public bool NextBool
        {
            get
            {
                var d = _data[_counter++];

                if (Regex.IsMatch(d, @"^\d+$"))
                {
                    return d == "1";
                }

                return bool.Parse(d);
            }
        }
    }
}
