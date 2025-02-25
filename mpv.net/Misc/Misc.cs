﻿
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Windows.Forms;

using Microsoft.Win32;

using static mpvnet.Core;

namespace mpvnet
{
    public static class NewLine
    {
        public static string BR = Environment.NewLine;
        public static string BR2 = Environment.NewLine + Environment.NewLine;
    }

    public class Sys
    {
        public static bool IsDarkTheme {
            get {
                object value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1);
            
                if (value is null)
                    value = 1;

                return (int)value == 0;
            }
        }
    }

    public class StringLogicalComparer : IComparer, IComparer<string>
    {
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
        public static extern int StrCmpLogical(string x, string y);

        int IComparer_Compare(object x, object y) => StrCmpLogical(x.ToString(), y.ToString());
        int IComparer.Compare(object x, object y) => IComparer_Compare(x, y);
        int IComparerOfString_Compare(string x, string y) => StrCmpLogical(x, y);
        int IComparer<string>.Compare(string x, string y) => IComparerOfString_Compare(x, y);
    }

    public class FileAssociation
    {
        static string ExePath = Application.ExecutablePath;
        static string ExeFilename = Path.GetFileName(Application.ExecutablePath);
        static string ExeFilenameNoExt = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
        static string[] Types;

        public static void Register(string[] types)
        {
            Types = types;

            RegistryHelp.SetValue(@"HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\" + ExeFilename, null, ExePath);
            RegistryHelp.SetValue(@"HKCR\Applications\" + ExeFilename, "FriendlyAppName", "mpv.net media player");
            RegistryHelp.SetValue($@"HKCR\Applications\{ExeFilename}\shell\open\command", null, $"\"{ExePath}\" \"%1\"");
            RegistryHelp.SetValue(@"HKLM\SOFTWARE\Clients\Media\mpv.net\Capabilities", "ApplicationDescription", "mpv.net media player");
            RegistryHelp.SetValue(@"HKLM\SOFTWARE\Clients\Media\mpv.net\Capabilities", "ApplicationName", "mpv.net");
            RegistryHelp.SetValue(@"HKCR\SystemFileAssociations\video\OpenWithList\" + ExeFilename, null, "");
            RegistryHelp.SetValue(@"HKCR\SystemFileAssociations\audio\OpenWithList\" + ExeFilename, null, "");
            RegistryHelp.SetValue(@"HKLM\SOFTWARE\RegisteredApplications", "mpv.net", @"SOFTWARE\Clients\Media\mpv.net\Capabilities");

            foreach (string ext in Types)
            {
                RegistryHelp.SetValue($@"HKCR\Applications\{ExeFilename}\SupportedTypes", "." + ext, "");
                RegistryHelp.SetValue($@"HKCR\" + "." + ext, null, ExeFilenameNoExt + "." + ext);
                RegistryHelp.SetValue($@"HKCR\" + "." + ext + @"\OpenWithProgIDs", ExeFilenameNoExt + "." + ext, "");

                if (Core.VideoTypes.Contains(ext))
                    RegistryHelp.SetValue(@"HKCR\" + "." + ext, "PerceivedType", "video");

                if (Core.AudioTypes.Contains(ext))
                    RegistryHelp.SetValue(@"HKCR\" + "." + ext, "PerceivedType", "audio");

                if (Core.ImageTypes.Contains(ext))
                    RegistryHelp.SetValue(@"HKCR\" + "." + ext, "PerceivedType", "image");

                RegistryHelp.SetValue($@"HKCR\" + ExeFilenameNoExt + "." + ext + @"\shell\open\command", null, $"\"{ExePath}\" \"%1\"");
                RegistryHelp.SetValue(@"HKLM\SOFTWARE\Clients\Media\mpv.net\Capabilities\FileAssociations", "." + ext, ExeFilenameNoExt + "." + ext);
            }
        }
    }

    public class MediaTrack
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public int    ID   { get; set; }
    }

    public class CommandItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Path { get; set; } = "";
        public string Command { get; set; } = "";
        public string Display { get { return string.IsNullOrEmpty(Path) ? Command : Path; } }

        public CommandItem() { }

        public CommandItem(SerializationInfo info, StreamingContext context) { }

        void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        string _Input = "";

        public string Input {
            get => _Input;
            set {
                _Input = value;
                NotifyPropertyChanged();
            }
        }

        public static ObservableCollection<CommandItem> GetItems(string content)
        {
            var items = new ObservableCollection<CommandItem>();

            if (!string.IsNullOrEmpty(content))
            {
                foreach (string line in content.Split('\r', '\n'))
                {
                    string val = line.Trim();

                    if (val.StartsWith("#"))
                        continue;

                    if (!val.Contains(" "))
                        continue;

                    CommandItem item = new CommandItem();
                    item.Input = val.Substring(0, val.IndexOf(" "));

                    if (item.Input == "_")
                        item.Input = "";

                    val = val.Substring(val.IndexOf(" ") + 1);

                    if (val.Contains("#menu:"))
                    {
                        item.Path = val.Substring(val.IndexOf("#menu:") + 6).Trim();
                        val = val.Substring(0, val.IndexOf("#menu:"));

                        if (item.Path.Contains(";"))
                            item.Path = item.Path.Substring(item.Path.IndexOf(";") + 1).Trim();
                    }

                    item.Command = val.Trim();

                    if (item.Command == "")
                        continue;

                    if (item.Command.ToLower() == "ignore")
                        item.Command = "";

                    items.Add(item);
                }
            }
            return items;
        }

        static ObservableCollection<CommandItem> _Items;

        public static ObservableCollection<CommandItem> Items {
            get {
                if (_Items is null)
                    _Items = GetItems(File.ReadAllText(core.InputConfPath));

                return _Items;
            }
        }
    }

    public class Folder
    {
        public static string Startup { get; } = Application.StartupPath + @"\";
    }
}
