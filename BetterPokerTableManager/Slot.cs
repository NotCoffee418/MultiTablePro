using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BetterPokerTableManager
{
    public class Slot
    {
        public Slot(ActivityUses activityUse, int x, int y, int width, int height)
        {
            _lastUsedId++;
            Id = _lastUsedId;
            ActivityUse = activityUse;
            X = x;
            Y = y;
            Width = Width;
            Height = height;
        }

        public enum ActivityUses
        {
            Inactive = 0,
            Active = 1,
            Aside = 2,
        }
        public enum Statuses
        {
            Undefined = 0,
            Free = 0,
            UsedByInactive = 1,
            UsedByActive = 2,
            UsedByPriority = 3,
        }
        static string[] activityUseNames = Enum.GetNames(typeof(ActivityUses));
        static string[] statusNames = Enum.GetNames(typeof(Statuses));
        private int _lastUsedId;

        public int Id { get; set; } // lower Id is used first
        public ActivityUses ActivityUse { get; set; }
        public Statuses Status { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
