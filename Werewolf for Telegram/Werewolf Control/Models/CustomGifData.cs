using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Models
{
    public class CustomGifData
    {
        public bool HasPurchased { get; set; }
        public bool? Approved { get; set; } = null;
        public string DenyReason { get; set; }
        public bool NSFW { get; set; }
        public int ApprovedBy { get; set; }
        public string VillagerDieImage { get; set; }
        public string WolfWin { get; set; }
        public string WolvesWin { get; set; }
        public string VillagersWin { get; set; }
        public string NoWinner { get; set; }
        public string StartGame { get; set; }
        public string StartChaosGame { get; set; }
        public string TannerWin { get; set; }
        public string CultWins { get; set; }
        public string SerialKillerWins { get; set; }
        public string LoversWin { get; set; }
        public bool ShowBadge { get; set; } = true;
        public bool Submitted { get; set; }
    }
}
