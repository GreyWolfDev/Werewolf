using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Werewolf_Control.Models
{
    public class CustomGifData : ICustomGifData
    {
        public CustomGifDataBeta Beta { get; set; } = new CustomGifDataBeta();
        public bool HasPurchased { get; set; }
        public bool? Approved { get => Beta.Approved; set => Beta.Approved = value; }
        public string DenyReason { get; set; }
        public bool NSFW { get => Beta.NSFW; set => Beta.NSFW = value; }
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
        public string SKKilled { get; set; }
        public string ArsonistWins { get; set; }
        public string BurnToDeath { get; set; }
        public bool ShowBadge { get => Beta.ShowBadge; set => Beta.ShowBadge = value; }
        public bool Submitted { get; set; }
    }

    public class CustomGifDataBeta : ICustomGifData
    {
        public bool? Approved { get; set; }
        public bool NSFW { get; set; }
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
        public string SKKilled { get; set; }
        public string ArsonistWins { get; set; }
        public string BurnToDeath { get; set; }
        public bool ShowBadge { get; set; } = true;
    }

    public interface ICustomGifData
    {
        bool? Approved { get; set; }
        bool NSFW { get; set; }
        string VillagerDieImage { get; set; }
        string WolfWin { get; set; }
        string WolvesWin { get; set; }
        string VillagersWin { get; set; }
        string NoWinner { get; set; }
        string StartGame { get; set; }
        string StartChaosGame { get; set; }
        string TannerWin { get; set; }
        string CultWins { get; set; }
        string SerialKillerWins { get; set; }
        string LoversWin { get; set; }
        string SKKilled { get; set; }
        string ArsonistWins { get; set; }
        string BurnToDeath { get; set; }
        bool ShowBadge { get; set; }
    }
}
