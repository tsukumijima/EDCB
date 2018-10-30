using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Linq;

namespace EpgTimer
{
    static class CmdHistorys
    {
        public const int MaxCount = 16;
        public static readonly List<CmdHistoryItem> Historys = new List<CmdHistoryItem>(MaxCount + 1);
        public static int Count { get { return Historys.Count; } }
        public static void Clear() { Historys.Clear(); }
        public static void Add(ICommand cmd, IEnumerable<IRecWorkMainData> items)
        {
            var hist = new CmdHistoryItem(cmd, items);
            if (hist.Items.Count > 0) Historys.Insert(0, hist);
            if (Count > MaxCount) Historys.RemoveAt(MaxCount);
        }
    }

    public class CmdHistoryItem
    {
        public readonly ICommand Command;
        public readonly List<IRecWorkMainData> Items;
        public CmdHistoryItem(ICommand cmd, IEnumerable<IRecWorkMainData> items)
        {
            Command = cmd;
            Items = (items ?? new List<IRecWorkMainData>()).OfType<IRecWorkMainData>().ToList();
        }
    }
}
