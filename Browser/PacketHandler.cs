using System.Text.RegularExpressions;

namespace Browser
{
    public class PacketHandler
    {
        public enum opcodes : int
        {
            login           = 0,
            reload          = 1,
            mouse           = 2,
            keyboard        = 3,
            show            = 4,
            hide            = 5,
            blockinput      = 6
        }

        public enum mouse_event : int
        {
            mouse_move      = 1,
            mouse_down      = 2,
            mouse_up        = 3,
            mouse_click     = 4
        }

        public enum kboard_event : int
        {
            kboard_down     = 1,
            kboard_up       = 2,
            kboard_click    = 3
        }

        private int counter = 1;

        private readonly string[] data;

        public PacketHandler(string data)
        {
            this.data = data.Split('|');
        }

        public opcodes      Header          => (opcodes)int.Parse(data[0]);

        public mouse_event  NextMouse       => (mouse_event)int.Parse(data[counter++]);
        public kboard_event NextKey         => (kboard_event)int.Parse(data[counter++]);

        public string       Next            => data[counter++];
        public int          NextInt         => int.Parse(data[counter++]);
        public long         NextLong        => long.Parse(data[counter++]);
        public double       NextDouble      => double.Parse(data[counter++]);

        public bool     NextBool
        {
            get
            {
                var d = data[counter++];

                if (Regex.IsMatch(d, @"^\d+$"))
                {
                    return d == "1";
                }

                return bool.Parse(d);
            }
        }
    }
}
