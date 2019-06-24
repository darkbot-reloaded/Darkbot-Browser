using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarkBotBrowser.Communication.Out
{
    public class PacketComposer
    {
        public static string Compose(params object[] args)
        {
            return string.Join("|", args);
        }
    }
}
