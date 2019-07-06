﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using static mpvnet.libmpv;
using static mpvnet.Native;

namespace mpvnet
{
    public delegate void MpvBoolPropChangeHandler(string propName, bool value);

    public class mp
    {
        public static event Action VideoSizeChanged;
                                                              // Lua/JS event       libmpv event

                                                              //                    MPV_EVENT_NONE
        public static event Action Shutdown;                  // shutdown           MPV_EVENT_SHUTDOWN
        public static event Action LogMessage;                // log-message        MPV_EVENT_LOG_MESSAGE
        public static event Action GetPropertyReply;          // get-property-reply MPV_EVENT_GET_PROPERTY_REPLY
        public static event Action SetPropertyReply;          // set-property-reply MPV_EVENT_SET_PROPERTY_REPLY
        public static event Action CommandReply;              // command-reply      MPV_EVENT_COMMAND_REPLY
        public static event Action StartFile;                 // start-file         MPV_EVENT_START_FILE
        public static event Action<EndFileEventMode> EndFile; // end-file           MPV_EVENT_END_FILE
        public static event Action FileLoaded;                // file-loaded        MPV_EVENT_FILE_LOADED
        public static event Action TracksChanged;             //                    MPV_EVENT_TRACKS_CHANGED
        public static event Action TrackSwitched;             //                    MPV_EVENT_TRACK_SWITCHED
        public static event Action Idle;                      // idle               MPV_EVENT_IDLE
        public static event Action Pause;                     //                    MPV_EVENT_PAUSE
        public static event Action Unpause;                   //                    MPV_EVENT_UNPAUSE
        public static event Action Tick;                      // tick               MPV_EVENT_TICK
        public static event Action ScriptInputDispatch;       //                    MPV_EVENT_SCRIPT_INPUT_DISPATCH
        public static event Action<string[]> ClientMessage;   // client-message     MPV_EVENT_CLIENT_MESSAGE
        public static event Action VideoReconfig;             // video-reconfig     MPV_EVENT_VIDEO_RECONFIG
        public static event Action AudioReconfig;             // audio-reconfig     MPV_EVENT_AUDIO_RECONFIG
        public static event Action MetadataUpdate;            //                    MPV_EVENT_METADATA_UPDATE
        public static event Action Seek;                      // seek               MPV_EVENT_SEEK
        public static event Action PlaybackRestart;           // playback-restart   MPV_EVENT_PLAYBACK_RESTART
                                                              //                    MPV_EVENT_PROPERTY_CHANGE
        public static event Action ChapterChange;             //                    MPV_EVENT_CHAPTER_CHANGE
        public static event Action QueueOverflow;             //                    MPV_EVENT_QUEUE_OVERFLOW
        public static event Action Hook;                      //                    MPV_EVENT_HOOK

        public static IntPtr Handle { get; set; }
        public static IntPtr WindowHandle { get; set; }
        public static Addon Addon { get; set; }
        public static bool IsLogoVisible { set; get; }
        public static List<KeyValuePair<string, Action<bool>>> BoolPropChangeActions { get; set; } = new List<KeyValuePair<string, Action<bool>>>();
        public static List<KeyValuePair<string, Action<int>>> IntPropChangeActions { get; set; } = new List<KeyValuePair<string, Action<int>>>();
        public static List<KeyValuePair<string, Action<string>>> StringPropChangeActions { get; set; } = new List<KeyValuePair<string, Action<string>>>();
        public static Size VideoSize { get; set; } = new Size(1920, 1080);
        public static List<PythonScript> PythonScripts { get; set; } = new List<PythonScript>();
        public static AutoResetEvent AutoResetEvent { get; set; } = new AutoResetEvent(false);
        public static List<MediaTrack> MediaTracks { get; set; } = new List<MediaTrack>();
        public static List<KeyValuePair<string, double>> Chapters { get; set; } = new List<KeyValuePair<string, double>>();

        public static string InputConfPath { get; } = ConfFolder + "\\input.conf";
        public static string ConfPath      { get; } = ConfFolder + "\\mpv.conf";
        public static string Sid { get; set; } = "";
        public static string Aid { get; set; } = "";
        public static string Vid { get; set; } = "";

        public static bool Fullscreen { get; set; }
        public static bool Border { get; set; } = true;
        public static bool RememberHeight { get; set; } = true;

        public static int Screen { get; set; } = -1;
        public static int Edition { get; set; }

        public static float Autofit { get; set; } = 0.5f;

        public static void ProcessProperty(string name, string value)
        {
            switch (name)
            {
                case "autofit":
                    if (value.Length == 3 && value.EndsWith("%"))
                        if (int.TryParse(value.Substring(0, 2), out int result))
                            Autofit = result / 100f;
                    break;
                case "fs":
                case "fullscreen": Fullscreen = value == "yes"; break;
                case "border": Border = value == "yes"; break;
                case "screen": Screen = Convert.ToInt32(value); break;
                case "remember-height": RememberHeight = value == "yes"; break;
            }
        }

        static string _ConfFolder;

        public static string ConfFolder {
            get {
                if (_ConfFolder == null)
                {
                    string portableFolder = Application.StartupPath + "\\portable_config\\";
                    string appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\mpv\\";
                    string startupFolder = Application.StartupPath + "\\";

                    if (!Directory.Exists(appdataFolder) && !Directory.Exists(portableFolder) &&
                        Sys.IsDirectoryWritable(Application.StartupPath) &&
                        !File.Exists(startupFolder + "mpv.conf"))
                    {
                        using (TaskDialog<string> td = new TaskDialog<string>())
                        {
                            td.MainInstruction = "Choose a settings folder.";
                            td.Content = "[MPV documentation about files on Windows.](https://mpv.io/manual/master/#files-on-windows)";
                            td.AddCommandLink("appdata", appdataFolder, appdataFolder);
                            td.AddCommandLink("portable", portableFolder, portableFolder);
                            td.AddCommandLink("startup", startupFolder, startupFolder);
                            td.AllowCancel = false;
                            _ConfFolder = td.Show();
                        }
                    }
                    else if (Directory.Exists(portableFolder))
                        _ConfFolder = portableFolder;
                    else if (Directory.Exists(appdataFolder))
                        _ConfFolder = appdataFolder;
                    else if (File.Exists(Application.StartupPath + "\\mpv.conf"))
                        _ConfFolder = Application.StartupPath + "\\";

                    if (string.IsNullOrEmpty(_ConfFolder)) _ConfFolder = appdataFolder;
                    if (!Directory.Exists(_ConfFolder)) Directory.CreateDirectory(_ConfFolder);

                    if (!File.Exists(_ConfFolder + "\\input.conf"))
                        File.WriteAllText(_ConfFolder + "\\input.conf", Properties.Resources.inputConf);

                    if (!File.Exists(_ConfFolder + "\\mpv.conf"))
                        File.WriteAllText(_ConfFolder + "\\mpv.conf", Properties.Resources.mpvConf);
                }
                return _ConfFolder;
            }
        }

        static Dictionary<string, string> _Conf;

        public static Dictionary<string, string> Conf {
            get {
                if (_Conf == null)
                {
                    _Conf = new Dictionary<string, string>();

                    if (File.Exists(ConfPath))
                        foreach (var i in File.ReadAllLines(ConfPath))
                            if (i.Contains("=") && ! i.StartsWith("#"))
                                _Conf[i.Substring(0, i.IndexOf("=")).Trim()] = i.Substring(i.IndexOf("=") + 1).Trim();

                    foreach (var i in Conf)
                        ProcessProperty(i.Key, i.Value);
                }
                return _Conf;
            }
        }

        public static void Init()
        {
            string dummy = ConfFolder;
            LoadLibrary("mpv-1.dll");
            Handle = mpv_create();
            set_property_string("osc", "yes");
            set_property_string("config", "yes");
            set_property_string("wid", MainForm.Hwnd.ToString());
            set_property_string("force-window", "yes");
            set_property_string("input-media-keys", "yes");
            mpv_initialize(Handle);
            ShowLogo();
            ProcessCommandLine();
            Task.Run(() => { LoadScripts(); });
            Task.Run(() => { Addon = new Addon(); });
            Task.Run(() => { EventLoop(); });
        }

        public static void LoadScripts()
        {
            string[] startupScripts = Directory.GetFiles(Application.StartupPath + "\\Scripts");

            foreach (string scriptPath in startupScripts)
                if (scriptPath.EndsWith(".lua") || scriptPath.EndsWith(".js"))
                    commandv("load-script", $"{scriptPath}");

            foreach (string scriptPath in startupScripts)
                if (Path.GetExtension(scriptPath) == ".py")
                    PythonScripts.Add(new PythonScript(File.ReadAllText(scriptPath)));

            foreach (string scriptPath in startupScripts)
                if (Path.GetExtension(scriptPath) == ".ps1")
                    PowerShellScript.Init(scriptPath);

            if (Directory.Exists(ConfFolder + "Scripts"))
                foreach (string scriptPath in Directory.GetFiles(ConfFolder + "Scripts"))
                    if (Path.GetExtension(scriptPath) == ".py")
                        PythonScripts.Add(new PythonScript(File.ReadAllText(scriptPath)));
                    else if (Path.GetExtension(scriptPath) == ".ps1")
                        PowerShellScript.Init(scriptPath);
        }

        public static void EventLoop()
        {
            while (true)
            {
                IntPtr ptr = mpv_wait_event(Handle, -1);
                mpv_event evt = (mpv_event)Marshal.PtrToStructure(ptr, typeof(mpv_event));

                if (WindowHandle == IntPtr.Zero)
                    WindowHandle = FindWindowEx(MainForm.Hwnd, IntPtr.Zero, "mpv", null);

                //System.Diagnostics.Debug.WriteLine(evt.event_id.ToString());

                try
                {
                    switch (evt.event_id)
                    {
                        case mpv_event_id.MPV_EVENT_SHUTDOWN:
                            Shutdown?.Invoke();
                            WriteHistory(null);
                            AutoResetEvent.Set();
                            return;
                        case mpv_event_id.MPV_EVENT_LOG_MESSAGE:
                            LogMessage?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_GET_PROPERTY_REPLY:
                            GetPropertyReply?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_SET_PROPERTY_REPLY:
                            SetPropertyReply?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_COMMAND_REPLY:
                            CommandReply?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_START_FILE:
                            StartFile?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_END_FILE:
                            var end_fileData = (mpv_event_end_file)Marshal.PtrToStructure(evt.data, typeof(mpv_event_end_file));
                            EndFileEventMode reason = (EndFileEventMode)end_fileData.reason;
                            EndFile?.Invoke(reason);
                            break;
                        case mpv_event_id.MPV_EVENT_FILE_LOADED:
                            HideLogo();
                            FileLoaded?.Invoke();
                            WriteHistory(get_property_string("path"));
                            break;
                        case mpv_event_id.MPV_EVENT_TRACKS_CHANGED:
                            TracksChanged?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_TRACK_SWITCHED:
                            TrackSwitched?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_IDLE:
                            Idle?.Invoke();
                            if (get_property_int("playlist-count") == 0) ShowLogo();
                            break;
                        case mpv_event_id.MPV_EVENT_PAUSE:
                            Pause?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_UNPAUSE:
                            Unpause?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_TICK:
                            Tick?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_SCRIPT_INPUT_DISPATCH:
                            ScriptInputDispatch?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_CLIENT_MESSAGE:
                            var client_messageData = (mpv_event_client_message)Marshal.PtrToStructure(evt.data, typeof(mpv_event_client_message));
                            string[] args = NativeUtf8StrArray2ManagedStrArray(client_messageData.args, client_messageData.num_args);
                            if (args.Length > 1 && args[0] == "mpv.net")
                                Command.Execute(args[1], args.Skip(2).ToArray());
                            else if (args.Length > 0)
                                ClientMessage?.Invoke(args);
                            break;
                        case mpv_event_id.MPV_EVENT_VIDEO_RECONFIG:
                            VideoReconfig?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_AUDIO_RECONFIG:
                            AudioReconfig?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_METADATA_UPDATE:
                            MetadataUpdate?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_SEEK:
                            Seek?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_PROPERTY_CHANGE:
                            var propData = (mpv_event_property)Marshal.PtrToStructure(evt.data, typeof(mpv_event_property));

                            if (propData.format == mpv_format.MPV_FORMAT_FLAG)
                                foreach (var i in BoolPropChangeActions)
                                    if (i.Key== propData.name)
                                        i.Value.Invoke(Marshal.PtrToStructure<int>(propData.data) == 1);

                            if (propData.format == mpv_format.MPV_FORMAT_STRING)
                                foreach (var i in StringPropChangeActions)
                                    if (i.Key == propData.name)
                                        i.Value.Invoke(StringFromNativeUtf8(Marshal.PtrToStructure<IntPtr>(propData.data)));

                            if (propData.format == mpv_format.MPV_FORMAT_INT64)
                                foreach (var i in IntPropChangeActions)
                                    if (i.Key == propData.name)
                                        i.Value.Invoke(Marshal.PtrToStructure<int>(propData.data));
                            break;
                        case mpv_event_id.MPV_EVENT_PLAYBACK_RESTART:
                            PlaybackRestart?.Invoke();
                            Size vidSize = new Size(get_property_int("dwidth"), get_property_int("dheight"));

                            if (VideoSize != vidSize && vidSize != Size.Empty)
                            {
                                VideoSize = vidSize;
                                VideoSizeChanged?.Invoke();
                            }                    

                            Task.Run(new Action(() => ReadMetaData()));
                            break;
                        case mpv_event_id.MPV_EVENT_CHAPTER_CHANGE:
                            ChapterChange?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_QUEUE_OVERFLOW:
                            QueueOverflow?.Invoke();
                            break;
                        case mpv_event_id.MPV_EVENT_HOOK:
                            Hook?.Invoke();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Msg.ShowException(ex);
                }
            }
        }

        static void HideLogo()
        {
            if (IsLogoVisible)
            {
                commandv("overlay-remove", "0");
                IsLogoVisible = false;
            }
        }

        static List<PythonEventObject> PythonEventObjects = new List<PythonEventObject>();

        public static void register_event(string name, IronPython.Runtime.PythonFunction pyFunc)
        {
            foreach (var eventInfo in typeof(mp).GetEvents())
            {
                if (eventInfo.Name.ToLower() == name.Replace("-", ""))
                {
                    PythonEventObject eventObject = new PythonEventObject();
                    PythonEventObjects.Add(eventObject);
                    eventObject.PythonFunction = pyFunc;
                    MethodInfo mi;

                    if (eventInfo.EventHandlerType == typeof(Action))
                        mi = eventObject.GetType().GetMethod(nameof(PythonEventObject.Invoke));
                    else if (eventInfo.EventHandlerType == typeof(Action<EndFileEventMode>))
                        mi = eventObject.GetType().GetMethod(nameof(PythonEventObject.InvokeEndFileEventMode));
                    else if (eventInfo.EventHandlerType == typeof(Action<string[]>))
                        mi = eventObject.GetType().GetMethod(nameof(PythonEventObject.InvokeStrings));
                    else
                        throw new Exception();

                    eventObject.EventInfo = eventInfo;
                    Delegate handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, eventObject, mi);
                    eventObject.Delegate = handler;
                    eventInfo.AddEventHandler(eventObject, handler);
                    break;
                }
            }
        }

        public static void unregister_event(IronPython.Runtime.PythonFunction pyFunc)
        {
            foreach (var eventObjects in PythonEventObjects)
                if (eventObjects.PythonFunction == pyFunc)
                    eventObjects.EventInfo.RemoveEventHandler(eventObjects, eventObjects.Delegate);
        }

        public static void commandv(params string[] args)
        {
            if (Handle == IntPtr.Zero) return;
            IntPtr mainPtr = AllocateUtf8IntPtrArrayWithSentinel(args, out IntPtr[] byteArrayPointers);
            int err = mpv_command(Handle, mainPtr);
            if (err < 0) throw new Exception($"{(mpv_error)err}");
            foreach (var ptr in byteArrayPointers)
                Marshal.FreeHGlobal(ptr);
            Marshal.FreeHGlobal(mainPtr);
        }

        public static void command_string(string command, bool throwException = false)
        {
            if (Handle == IntPtr.Zero) return;
            int err = mpv_command_string(Handle, command);
            if (err < 0 && throwException)
                throw new Exception($"{(mpv_error)err}\r\n\r\n" + command);
        }

        public static void set_property_string(string name, string value, bool throwOnException = false)
        {
            byte[] bytes = GetUtf8Bytes(value);
            int err = mpv_set_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_STRING, ref bytes);
            if (err < 0 && throwOnException)
                throw new Exception($"{name}: {(mpv_error)err}");
        }

        public static string get_property_string(string name, bool throwOnException = false)
        {
            try
            {
                int err = mpv_get_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_STRING, out IntPtr lpBuffer);
                if (err < 0 && throwOnException) throw new Exception($"{name}: {(mpv_error)err}");
                string ret = StringFromNativeUtf8(lpBuffer);
                mpv_free(lpBuffer);
                return ret;
            }
            catch (Exception e)
            {
                if (throwOnException) throw e;
                return "";
            }
        }

        public static int get_property_int(string name, bool throwOnException = false)
        {
            int err = mpv_get_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_INT64, out IntPtr lpBuffer);

            if (err < 0 && throwOnException)
                throw new Exception($"{name}: {(mpv_error)err}");
            else
                return lpBuffer.ToInt32();
        }

        public static double get_property_number(string name, bool throwOnException = false)
        {
            double val = 0;
            int err = mpv_get_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_DOUBLE, ref val);

            if (err < 0 && throwOnException)
                throw new Exception($"{name}: {(mpv_error)err}");
            else
                return val;
        }

        public static bool get_property_bool(string name, bool throwOnException = false)
        {
            int err = mpv_get_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_FLAG, out IntPtr lpBuffer);

            if (err < 0 && throwOnException)
                throw new Exception($"{name}: {(mpv_error)err}");
            else
                return lpBuffer.ToInt32() == 1;
        }

        public static void set_property_int(string name, int value, bool throwOnException = false)
        {
            Int64 val = value;
            int err = mpv_set_property(Handle, GetUtf8Bytes(name), mpv_format.MPV_FORMAT_INT64, ref val);

            if (err < 0 && throwOnException)
                throw new Exception($"{name}: {(mpv_error)err}");
        }

        public static void observe_property_int(string name, Action<int> action)
        {
            int err = mpv_observe_property(Handle, (ulong)action.GetHashCode(), name, mpv_format.MPV_FORMAT_INT64);

            if (err < 0)
                throw new Exception($"{name}: {(mpv_error)err}");
            else
                IntPropChangeActions.Add(new KeyValuePair<string, Action<int>>(name, action));
        }

        public static void observe_property_bool(string name, Action<bool> action)
        {
            int err = mpv_observe_property(Handle, (ulong)action.GetHashCode(), name, mpv_format.MPV_FORMAT_FLAG);

            if (err < 0)
                throw new Exception($"{name}: {(mpv_error)err}");
            else
                BoolPropChangeActions.Add(new KeyValuePair<string, Action<bool>>(name, action));
        }

        public static void observe_property_string(string name, Action<string> action)
        {
            int err = mpv_observe_property(Handle, (ulong)action.GetHashCode(), name, mpv_format.MPV_FORMAT_STRING);

            if (err < 0)
                throw new Exception($"{name}: {(mpv_error)err}");
            else
                StringPropChangeActions.Add(new KeyValuePair<string, Action<string>>(name, action));
        }

        protected static void ProcessCommandLine()
        {
            var args = Environment.GetCommandLineArgs().Skip(1);
            List<string> files = new List<string>();

            foreach (string i in args)
            {
                if (!i.StartsWith("--") && (i == "-" || i.Contains("://") || File.Exists(i)))
                {
                    files.Add(i);
                    if (i.Contains("://")) RegHelp.SetObject(App.RegPath, "LastURL", i);
                }
            }

            Load(files.ToArray(), App.ProcessInstance != "queue", Control.ModifierKeys.HasFlag(Keys.Control));

            foreach (string i in args)
            {
                if (i.StartsWith("--"))
                {
                    try
                    {
                        if (i.Contains("="))
                        {
                            string left = i.Substring(2, i.IndexOf("=") - 2);
                            string right = i.Substring(left.Length + 3);
                            set_property_string(left, right, true);
                        }
                        else
                            set_property_string(i.Substring(2), "yes", true);
                    }
                    catch (Exception e)
                    {
                        Msg.ShowException(e);
                    }
                }
            }
        }

        static DateTime LastLoad;

        public static void Load(string[] files, bool loadFolder, bool append)
        {
            if (files is null || files.Length == 0) return;
            HideLogo();

            if ((DateTime.Now - LastLoad).TotalMilliseconds < 500)
                append = true;

            LastLoad = DateTime.Now;

            for (int i = 0; i < files.Length; i++)
                if (App.SubtitleTypes.Contains(Path.GetExtension(files[i]).TrimStart('.').ToLower()))
                    commandv("sub-add", files[i]);
                else
                    if (i == 0 && !append)
                        commandv("loadfile", files[i]);
                    else
                        commandv("loadfile", files[i], "append");

            if (string.IsNullOrEmpty(get_property_string("path")))
                set_property_int("playlist-pos", 0);
            if (loadFolder && !append) Task.Run(() => LoadFolder()); // user reported race condition
        }

        public static void LoadFolder()
        {
            Thread.Sleep(200); // user reported race condition
            string path = get_property_string("path");
            if (!File.Exists(path) || get_property_int("playlist-count") != 1) return;
            List<string> files = Directory.GetFiles(Path.GetDirectoryName(path)).ToList();
            files = files.Where((file) =>
                App.VideoTypes.Contains(Path.GetExtension(file).TrimStart('.').ToLower()) ||
                App.AudioTypes.Contains(Path.GetExtension(file).TrimStart('.').ToLower())).ToList();
            files.Sort(new StringLogicalComparer());
            int index = files.IndexOf(path);
            files.Remove(path);
            foreach (string i in files)
                commandv("loadfile", i, "append");
            if (index > 0) commandv("playlist-move", "0", (index + 1).ToString());
        }

        static IntPtr AllocateUtf8IntPtrArrayWithSentinel(string[] arr, out IntPtr[] byteArrayPointers)
        {
            int numberOfStrings = arr.Length + 1; // add extra element for extra null pointer last (sentinel)
            byteArrayPointers = new IntPtr[numberOfStrings];
            IntPtr rootPointer = Marshal.AllocCoTaskMem(IntPtr.Size * numberOfStrings);

            for (int index = 0; index < arr.Length; index++)
            {
                var bytes = GetUtf8Bytes(arr[index]);
                IntPtr unmanagedPointer = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, unmanagedPointer, bytes.Length);
                byteArrayPointers[index] = unmanagedPointer;
            }

            Marshal.Copy(byteArrayPointers, 0, rootPointer, numberOfStrings);
            return rootPointer;
        }

        static string[] NativeUtf8StrArray2ManagedStrArray(IntPtr unmanagedStringArray, int StringCount)
        {
            IntPtr[] intPtrArray = new IntPtr[StringCount];
            string[] stringArray = new string[StringCount];
            Marshal.Copy(unmanagedStringArray, intPtrArray, 0, StringCount);

            for (int i = 0; i < StringCount; i++)
                stringArray[i] = StringFromNativeUtf8(intPtrArray[i]);

            return stringArray;
        }

        static string StringFromNativeUtf8(IntPtr nativeUtf8)
        {
            int len = 0;
            while (Marshal.ReadByte(nativeUtf8, len) != 0) ++len;
            byte[] buffer = new byte[len];
            Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer);
        }

        static byte[] GetUtf8Bytes(string s) => Encoding.UTF8.GetBytes(s + "\0");

        static string LastHistoryPath;
        static DateTime LastHistoryStartDateTime;

        static void WriteHistory(string filePath)
        {
            int totalMinutes = Convert.ToInt32((DateTime.Now - LastHistoryStartDateTime).TotalMinutes);

            if (File.Exists(LastHistoryPath) && totalMinutes > 1)
            {
                string historyFilepath = ConfFolder + "history.txt";

                File.AppendAllText(historyFilepath, DateTime.Now.ToString().Substring(0, 16) +
                    " " + totalMinutes.ToString().PadLeft(3) + " " +
                    Path.GetFileNameWithoutExtension(LastHistoryPath) + "\r\n");
            }

            LastHistoryPath = filePath;
            LastHistoryStartDateTime = DateTime.Now;
        }

        public static void ShowLogo()
        {
            if (MainForm.Instance is null) return;
            Rectangle cr = MainForm.Instance.ClientRectangle;
            if (cr.Width == 0 || cr.Height == 0) return;
            int len = cr.Height / 5;

            using (Bitmap b = new Bitmap(len, len))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.Clear(Color.Black);
                    Rectangle rect = new Rectangle(0, 0, len, len);
                    g.DrawImage(Properties.Resources.mpvnet, rect);
                    BitmapData bd = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
                    int x = Convert.ToInt32((cr.Width - len) / 2.0);
                    int y = Convert.ToInt32(((cr.Height - len) / 2.0) * 0.9);
                    commandv("overlay-add", "0", $"{x}", $"{y}", "&" + bd.Scan0.ToInt64().ToString(), "0", "bgra", bd.Width.ToString(), bd.Height.ToString(), bd.Stride.ToString());
                    b.UnlockBits(bd);
                    IsLogoVisible = true;
                }
            }
        }

        static void ReadMetaData()
        {
            lock (MediaTracks)
            {
                MediaTracks.Clear();

                using (MediaInfo mi = new MediaInfo(get_property_string("path")))
                {
                    int count = mi.GetCount(MediaInfoStreamKind.Video);

                    for (int i = 0; i < count; i++)
                    {
                        MediaTrack track = new MediaTrack();
                        Add(track, mi.GetVideo(i, "Format"));
                        Add(track, mi.GetVideo(i, "Format_Profile"));
                        Add(track, mi.GetVideo(i, "Width") + "x" + mi.GetVideo(i, "Height"));
                        Add(track, mi.GetVideo(i, "FrameRate") + " FPS");
                        Add(track, mi.GetVideo(i, "Language/String"));
                        Add(track, mi.GetVideo(i, "Forced") == "Yes" ? "Forced" : "");
                        Add(track, mi.GetVideo(i, "Default") == "Yes" ? "Default" : "");
                        Add(track, mi.GetVideo(i, "Title"));
                        track.Text = "V: " + track.Text.Trim(' ', ',');
                        track.Type = "v";
                        track.ID = i + 1;
                        MediaTracks.Add(track);
                    }

                    count = mi.GetCount(MediaInfoStreamKind.Audio);

                    for (int i = 0; i < count; i++)
                    {
                        MediaTrack track = new MediaTrack();
                        Add(track, mi.GetAudio(i, "Language/String"));
                        Add(track, mi.GetAudio(i, "Format"));
                        Add(track, mi.GetAudio(i, "Format_Profile"));
                        Add(track, mi.GetAudio(i, "BitRate/String"));
                        Add(track, mi.GetAudio(i, "Channel(s)/String"));
                        Add(track, mi.GetAudio(i, "SamplingRate/String"));
                        Add(track, mi.GetAudio(i, "Forced") == "Yes" ? "Forced" : "");
                        Add(track, mi.GetAudio(i, "Default") == "Yes" ? "Default" : "");
                        Add(track, mi.GetAudio(i, "Title"));
                        track.Text = "A: " + track.Text.Trim(' ', ',');
                        track.Type = "a";
                        track.ID = i + 1;
                        MediaTracks.Add(track);
                    }

                    count = mi.GetCount(MediaInfoStreamKind.Text);

                    for (int i = 0; i < count; i++)
                    {
                        MediaTrack track = new MediaTrack();
                        Add(track, mi.GetText(i, "Language/String"));
                        Add(track, mi.GetText(i, "Format"));
                        Add(track, mi.GetText(i, "Format_Profile"));
                        Add(track, mi.GetText(i, "Forced") == "Yes" ? "Forced" : "");
                        Add(track, mi.GetText(i, "Default") == "Yes" ? "Default" : "");
                        Add(track, mi.GetText(i, "Title"));
                        track.Text = "S: " + track.Text.Trim(' ', ',');
                        track.Type = "s";
                        track.ID = i + 1;
                        MediaTracks.Add(track);
                    }

                    count = get_property_int("edition-list/count");

                    for (int i = 0; i < count; i++)
                    {
                        MediaTrack track = new MediaTrack();
                        track.Text = "E: " + get_property_string($"edition-list/{i}/title");
                        track.Type = "e";
                        track.ID = i;
                        MediaTracks.Add(track);
                    }

                    void Add(MediaTrack track, string val)
                    {
                        if (!string.IsNullOrEmpty(val) && !(track.Text != null && track.Text.Contains(val)))
                            track.Text += " " + val + ",";
                    }
                }
            }

            lock (Chapters)
            {
                Chapters.Clear();
                int count = get_property_int("chapter-list/count");

                for (int x = 0; x < count; x++)
                {
                    string text = get_property_string($"chapter-list/{x}/title");
                    double time = get_property_number($"chapter-list/{x}/time");
                    Chapters.Add(new KeyValuePair<string, double>(text, time));
                }
            }
        }
    }

    public enum EndFileEventMode
    {
        Eof,
        Stop,
        Quit,
        Error,
        Redirect,
        Unknown
    }
}