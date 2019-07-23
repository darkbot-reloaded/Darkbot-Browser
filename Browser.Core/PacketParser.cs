namespace Browser.Core
{
    public class PacketParser
    {
        public enum KeyboardEvent
        {
            Down = 1,
            Up = 2,
            Click = 3
        }

        public enum MouseEvent
        {
            Move = 1,
            Down = 2,
            Up = 3,
            Click = 4
        }

        public enum PacketHeader
        {
            Login = 0,
            Reload = 1,
            Mouse = 2,
            Keyboard = 3,
            Show = 4,
            Hide = 5,
            BlockInput = 6
        }

        private readonly string[] _data;

        private int _counter = 1;

        public PacketParser(string data)
        {
            _data = data.Split('|');
        }

        public PacketHeader Header => (PacketHeader) int.Parse(_data[0]);

        public MouseEvent NextMouseEvent => (MouseEvent) int.Parse(_data[_counter++]);
        public KeyboardEvent NextKeyboardEvent => (KeyboardEvent) int.Parse(_data[_counter++]);

        public string Next => _data[_counter++];
        public int NextInt => int.Parse(_data[_counter++]);

        public bool NextBool => bool.Parse(_data[_counter++]);
    }
}