using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Database;
using Newtonsoft.Json;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werewolf_Control.Attributes;
using Werewolf_Control.Helpers;
using System.Threading;
using Telegram.Bot;

#pragma warning disable IDE0060 // Remove unused parameter

namespace Werewolf_Control
{
    public static partial class Commands
    {
        [Command(Trigger = "grouplist")]
        public static void GroupList(Update update, string[] args)
        {
//#if RELEASE
//            Send("Grouplist is temporarily disabled... Sorry for any inconvenience caused...", update.Message.Chat.Id);
//#else
            GroupList(update.Message.Chat.Id, update.Message.From.Id);
//#endif
        }

        public static void GroupList(long chatId, long fromId, int messageId = 0)
        {
            //new method, fun times....
            //now determine what languages are available in public groups.
            try
            {
                string[] disabledLangs = new string[] { /*"فارسی"*/ }; // Language bases of which no grouplist is accessible
                var langs = PublicGroups.GetBaseLanguages().Where(x => !disabledLangs.Contains(x)); // do not fetch disabled langs
                //create a menu out of this
                List<InlineKeyboardButton> buttons = langs.OrderBy(x => x).Select(x => InlineKeyboardButton.WithCallbackData(x, $"groups|{fromId}|{x}|null")).ToList();

                var baseMenu = new List<InlineKeyboardButton[]>();
                for (var i = 0; i < buttons.Count; i++)
                {
                    if (buttons.Count - 1 == i)
                    {
                        baseMenu.Add(new[] { buttons[i] });
                    }
                    else
                        baseMenu.Add(new[] { buttons[i], buttons[i + 1] });
                    i++;
                }

                var menu = new InlineKeyboardMarkup(baseMenu.ToArray());

                if (messageId != 0)
                {
                    var result = Bot.Api.EditMessageTextAsync(chatId: chatId, messageId: messageId,
                            text: GetLocaleString("WhatLangGroup", GetLanguage(fromId)),
                            replyMarkup: menu).Result;
                }
                else
                {
                    try
                    {
                        var result = Bot.Api.SendTextMessageAsync(chatId: fromId,
                            text: GetLocaleString("WhatLangGroup", GetLanguage(fromId)),
                            replyMarkup: menu).Result;
                        if (chatId != fromId)
                            Send(GetLocaleString("SentPrivate", GetLanguage(fromId)), chatId);
                    }
                    catch
                    {
                        RequestPM(chatId);
                    }
                }
            }
            catch (Exception e)
            {
                Send("Error in grouplist: " + e.Message + Environment.NewLine + Environment.NewLine + e.StackTrace, Settings.ErrorGroup);
            }
        }

        [Command(Trigger = "rolelist")]
        public static void RoleList(Update update, string[] args)
        {
            var lang = GetLanguage(update.Message.Chat.Id);
            // var reply =
            //    "/AboutVG - Villager\n/AboutSeer - Seer\n/AboutWw - Werewolf\n/AboutHarlot - Harlot\n/AboutDrunk - Drunk\n/AboutCursed - Cursed\n/AboutTraitor - Traitor\n/AboutGA - Guardian Angel\n/AboutDetective - Detective\n/AboutGunner - Gunner\n/AboutTanner - Tanner\n/AboutFool - Fool\n/AboutCult - Cultist\n/AboutCH - Cultist Hunter\n/AboutWC - Wild Child\n/AboutAppS - Apprentice seer\n/AboutBH - Beholder\n/AboutMason - Mason\n/AboutDG - Doppelgänger\n/AboutCupid - Cupid\n/AboutHunter - Hunter\n/AboutSK - Serial Killer";

            var reply = "";
            reply += "/aboutVG - " + GetLocaleString("Villager", lang) + "\n";
            reply += "/aboutWW - " + GetLocaleString("Wolf", lang) + "\n";
            reply += "/aboutDrunk - " + GetLocaleString("Drunk", lang) + "\n";
            reply += "/aboutSeer - " + GetLocaleString("Seer", lang) + "\n";
            reply += "/aboutCursed - " + GetLocaleString("Cursed", lang) + "\n";
            reply += "/aboutHarlot - " + GetLocaleString("Harlot", lang) + "\n";
            reply += "/aboutBH - " + GetLocaleString("Beholder", lang) + "\n";
            reply += "/aboutGunner - " + GetLocaleString("Gunner", lang) + "\n";
            reply += "/aboutTraitor - " + GetLocaleString("Traitor", lang) + "\n";
            reply += "/aboutGA - " + GetLocaleString("GuardianAngel", lang) + "\n";
            try
            {
                var result = Bot.Api.SendTextMessageAsync(chatId: update.Message.From.Id, text: reply).Result;
                if (update.Message.Chat.Type != ChatType.Private)
                    Send(GetLocaleString("SentPrivate", GetLanguage(update.Message.From.Id)), update.Message.Chat.Id);
            }
            catch
            {
                RequestPM(update.Message.Chat.Id);
                return;
            }
            Thread.Sleep(300);
            reply = "/aboutDetective - " + GetLocaleString("Detective", lang) + "\n";
            reply += "/aboutAppS - " + GetLocaleString("ApprenticeSeer", lang) + "\n";
            reply += "/aboutCult - " + GetLocaleString("Cultist", lang) + "\n";
            reply += "/aboutCH - " + GetLocaleString("CultistHunter", lang) + "\n";
            reply += "/aboutWC - " + GetLocaleString("WildChild", lang) + "\n";
            reply += "/aboutFool - " + GetLocaleString("Fool", lang) + "\n";
            reply += "/aboutMason - " + GetLocaleString("Mason", lang) + "\n";
            reply += "/aboutDG - " + GetLocaleString("Doppelgänger", lang) + "\n";
            reply += "/aboutCupid - " + GetLocaleString("Cupid", lang) + "\n";
            reply += "/aboutHunter - " + GetLocaleString("Hunter", lang) + "\n";
            Send(reply, update.Message.From.Id);
            Thread.Sleep(300);
            reply = "/aboutSK - " + GetLocaleString("SerialKiller", lang) + "\n";
            reply += "/aboutTanner - " + GetLocaleString("Tanner", lang) + "\n";
            reply += "/aboutMayor - " + GetLocaleString("Mayor", lang) + "\n";
            reply += "/aboutPrince - " + GetLocaleString("Prince", lang) + "\n";
            reply += "/aboutSorcerer - " + GetLocaleString("Sorcerer", lang) + "\n";
            reply += "/aboutClumsy - " + GetLocaleString("ClumsyGuy", lang) + "\n";
            reply += "/aboutBlacksmith - " + GetLocaleString("Blacksmith", lang) + "\n";
            reply += "/aboutAlphaWolf - " + GetLocaleString("AlphaWolf", lang) + "\n";
            reply += "/aboutWolfCub - " + GetLocaleString("WolfCub", lang) + "\n";
            Send(reply, update.Message.From.Id);
            Thread.Sleep(300);
            reply = "/aboutSandman - " + GetLocaleString("Sandman", lang) + "\n";
            reply += "/aboutOracle - " + GetLocaleString("Oracle", lang) + "\n";
            reply += "/aboutWolfMan - " + GetLocaleString("WolfMan", lang) + "\n";
            reply += "/aboutLycan - " + GetLocaleString("Lycan", lang) + "\n";
            reply += "/aboutPacifist - " + GetLocaleString("Pacifist", lang) + "\n";
            reply += "/aboutWiseElder - " + GetLocaleString("WiseElder", lang) + "\n";
            reply += "/aboutThief - " + GetLocaleString("Thief", lang) + "\n";
            reply += "/aboutTroublemaker - " + GetLocaleString("Troublemaker", lang) + "\n";
            reply += "/aboutChemist - " + GetLocaleString("Chemist", lang) + "\n";
            reply += "/aboutSnowWolf - " + GetLocaleString("SnowWolf", lang) + "\n";
            Send(reply, update.Message.From.Id);
            Thread.Sleep(300);
            reply = "/aboutGraveDigger - " + GetLocaleString("GraveDigger", lang) + "\n";
            reply += "/aboutArsonist - " + GetLocaleString("Arsonist", lang) + "\n";
            reply += "/aboutAugur - " + GetLocaleString("Augur", lang) + "\n";
            reply += "/aboutScapeGoat - " + GetLocaleString("ScapeGoat", lang) + "\n";
            Send(reply, update.Message.From.Id);
        }
    }
}
