﻿// TODOS
// Allow for echo channel -- DONE
// Single colour option for translated text -- DONE
// Fix not loading language selection -- DONE

// Append Translation (optional) -- TEST
// Replace with Translation (optional) -- TEST
// Fix colour 'bleeding' into next line - TEST

// Send all translations to USER DEFINED CHANNEL (optional)
// Option to only translate one language

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dalamud.Configuration;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using IvanAkcheurov.NTextCat.Lib;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Num = System.Numerics;
using System.Reflection;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace ChatTranslator
{
    public class ChatTranslator : UI, IDalamudPlugin
    {
        public UI ui;
        public Translater translater;
        public OnChat onChat;

        public string Name => "Chat Translator";
        public DalamudPluginInterface pluginInterface;
        public Config Configuration;
        public bool enable = true;
        public bool config = false;
        public string language = "en";
        public int languageInt = 16;
        public UIColorPick[] textColour;
        public UIColorPick chooser;
        public bool picker;
        public int tranMode = 0;
        public string[] tranModeOptions = { "Append", "Replace", "Additional" };


        public Lumina.Excel.ExcelSheet<UIColor> uiColours;

        public List<XivChatType> _channels = new List<XivChatType>();

        public List<XivChatType> order = new List<XivChatType>
        {
            XivChatType.None, XivChatType.Debug, XivChatType.Urgent, XivChatType.Notice, XivChatType.Say,
            XivChatType.Shout, XivChatType.TellOutgoing, XivChatType.TellIncoming, XivChatType.Party, XivChatType.Alliance,
            XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5,
            XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8, XivChatType.FreeCompany, XivChatType.NoviceNetwork,
            XivChatType.CustomEmote, XivChatType.StandardEmote, XivChatType.Yell, XivChatType.CrossParty, XivChatType.PvPTeam,
            XivChatType.CrossLinkShell1, XivChatType.Echo, XivChatType.SystemMessage, XivChatType.SystemError, XivChatType.GatheringSystemMessage,
            XivChatType.ErrorMessage, XivChatType.RetainerSale, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4,
            XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8
        };

        public bool[] yesno = {
            false, false, false, false, true,
            true, false, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, false, true, true, true,
            true, true, false, false, false,
            false, false, true, true, true,
            true, true, true, true
        };

        public string[] codes = {
            "af", "an", "ar", "az", "be_x_old",
            "bg", "bn", "br", "bs",
            "ca", "ceb", "cs", "cy", "da",
            "de", "el", "en", "eo", "es",
            "et", "eu", "fa", "fi", "fr",
            "gl", "he", "hi", "hr", "ht",
            "hu", "hy", "id", "is", "it",
            "ja", "jv", "ka", "kk", "ko",
            "la", "lb", "lt", "lv",
            "mg", "mk", "ml", "mr", "ms",
            "new", "nl", "nn", "no", "oc",
            "pl", "pt", "ro", "roa_rup",
            "ru", "sk", "sl",
            "sq", "sr", "sv", "sw", "ta",
            "te", "th", "tl", "tr", "uk",
            "ur", "vi", "vo", "war", "zh",
            "zh_classical", "zh_yue"
        };

        public string[] languages = {
            "Afrikaans", "Aragonese", "Arabic", "Azerbaijani", "Belarusian",
            "Bulgarian", "Bengali", "Breton", "Bosnian",
            "Catalan; Valencian", "Cebuano", "Czech", "Welsh", "Danish",
            "German", "Greek, Modern", "English", "Esperanto", "Spanish; Castilian",
            "Estonian", "Basque", "Persian", "Finnish", "French",
            "Galician", "Hebrew", "Hindi", "Croatian", "Haitian; Haitian Creole",
            "Hungarian", "Armenian", "Indonesian", "Icelandic", "Italian",
            "Japanese", "Javanese", "Georgian", "Kazakh", "Korean",
            "Latin", "Luxembourgish; Letzeburgesch", "Lithuanian", "Latvian",
            "Malagasy", "Macedonian", "Malayalam", "Marathi", "Malay",
            "Nepal Bhasa; Newari", "Dutch; Flemish", "Norwegian Nynorsk; Nynorsk, Norwegian", "Norwegian", "Occitan (post 1500)",
            "Polish", "Portuguese", "Romanian; Moldavian; Moldovan", "Romance languages",
            "Russian", "Slovak", "Slovenian",
            "Albanian", "Serbian", "Swedish", "Swahili", "Tamil",
            "Telugu", "Thai", "Tagalog", "Turkish", "Ukrainian",
            "Urdu", "Vietnamese", "Volapük", "Waray", "Chinese",
            "Chinese Classical", "Chinese yue"
        };




        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();
            this.pluginInterface.Framework.Gui.Chat.OnChatMessage += onChat.Chat_OnChatMessage;
            this.pluginInterface.UiBuilder.OnBuildUi += ui.TranslatorConfigUI;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += TranslatorConfig;
            this.pluginInterface.CommandManager.AddHandler("/trn", new CommandInfo(Command)
            {
                HelpMessage = "Opens the Chat Translator config menu"
            });
            uiColours = pluginInterface.Data.Excel.GetSheet<UIColor>();
            _channels = Configuration.Channels;
            textColour = Configuration.TextColour;
            languageInt = Configuration.Lang;
        }
        public class PluginConfiguration : IPluginConfiguration
        {
            public int Version { get; set; } = 0;
        }
        public void Dispose()
        {
            pluginInterface.Framework.Gui.Chat.OnChatMessage -= onChat.Chat_OnChatMessage;
            pluginInterface.UiBuilder.OnBuildUi -= TranslatorConfigUI;
            pluginInterface.UiBuilder.OnOpenConfigUi -= TranslatorConfig;
            pluginInterface.CommandManager.RemoveHandler("/trn");
        }
        private void Command(string command, string arguments) => config = true;

        // What to do when plugin install config button is pressed
        private void TranslatorConfig(object Sender, EventArgs args) => config = true;














    }

    public class UIColorPick
    {
        public uint Choice { get; set; }
        public uint Option { get; set; }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<XivChatType> Channels { get; set; } = new List<XivChatType>();
        public int Lang { get; set; } = 16;
        public UIColorPick[] TextColour { get; set; } =
        {
            new UIColorPick { Choice = 0, Option =0 }
        };
    }
}