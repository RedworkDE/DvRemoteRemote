using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

#if BepInEx
using BepInEx;
#endif

#if UMM
using UnityModManagerNet;
#endif

namespace DvRemoteRemote
{
#if BepInEx
    [BepInPlugin("RedworkDE.RemoteControl", "Remote Control", "0.1")]
#endif
    public class RemoteControl
#if BepInEx
        : BaseUnityPlugin
#endif
    {
#if UMM
        public static bool Load(UnityModManager.ModEntry mod)
        {
            var instance = new RemoteControl();

            instance.Start();
            mod.OnUpdate = (entry, f) => instance.Update();

            return true;
        }
#endif

        private static readonly Task CompletedTask = Task.Delay(0);
#if DEBUG
        private static readonly string _sourcePath = GetSourcePath();
        private static readonly ConcurrentDictionary<string, DateTimeOffset> _tsCompile = new ConcurrentDictionary<string, DateTimeOffset>(Directory
            .EnumerateFiles(GetSourcePath(), "*.ts", SearchOption.AllDirectories)
            .Select(path => new KeyValuePair<string, DateTimeOffset>(
                path.Substring(GetSourcePath().Length + 1), 
                new FileInfo(path).LastWriteTimeUtc)));
#endif

        private readonly List<Client> _clients = new List<Client>();
        private readonly ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        private void QueueAction(Action action)
        {
            _actions.Enqueue(action);
        }

        private Task<T> QueueFunction<T>(Func<T> func)
        {
            var tsc = new TaskCompletionSource<T>();
            _actions.Enqueue(()=>tsc.SetResult(func()));
            return tsc.Task;
        }

        private static string GetSourcePath([CallerFilePath] string file = null)
        {
            return Path.GetDirectoryName(file);
        }

        private static Stream GetFile(string name)
        {
#if DEBUG
            var path = Path.Combine(_sourcePath, name);
            if (File.Exists(path)) return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
#endif
            return Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(RemoteControl), name.Replace('/','.'));
        }

        private static Stream GetTsFile(string name)
        {
#if DEBUG
            try
            {
                //if (!NativeMethods.CreateProcess(null, "tsc --lib esnext,dom --noImplicitAny " + name, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, _sourcePath, new NativeMethods.StartupInfo(), out var info))
                //    throw new Win32Exception(Marshal.GetLastWin32Error());

                //var proc = Process.GetProcessById(info.dwProcessId);
                //proc.WaitForExit();

                var current = (DateTimeOffset) new FileInfo(Path.Combine(_sourcePath, name)).LastWriteTimeUtc;
                if (!_tsCompile.TryGetValue(name, out var old) || old != current)
                {
                    Process.Start(new ProcessStartInfo("tsc", "-t es2018 -m commonjs --lib esnext,dom --strict " + name) {WorkingDirectory = _sourcePath}).WaitForExit();
                    _tsCompile[name] = current;
                }

                var path = Path.Combine(_sourcePath, Path.ChangeExtension(name, "js"));
                if (File.Exists(path)) return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
#endif
            return GetFile(Path.ChangeExtension(name, "js"));
        }

        private static string GetContentType(string ext)
        {
            switch (ext.ToLowerInvariant())
            {
                case ".html":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
            }

            return null;
        }

        public void Start()
        {
            var server = WebServer.RegisterPage("remoteremote", "Remote Remote Control");

            server.Register("/poll", PollClientAsync);
            server.Register("/send", SendClientAsync);
            server.Register(new Regex("^/static/.*\\.ts$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                (context, uri) => context.SetResponseStreamAsync(GetTsFile(uri.AbsolutePath.Substring(1)), GetContentType(".js")));
            server.Register(new Regex("^/static/", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                (context, uri) => context.SetResponseStreamAsync(GetFile(uri.AbsolutePath.Substring(1)), GetContentType(Path.GetExtension(context.Request.Url.AbsolutePath))));
            server.Register("/", context => context.SetResponseStreamAsync(GetFile("static/RemoteControlPage.html"), "text/html"));
            server.Register("/register", AcceptClientAsync);
            server.Register("/unregister" ,UnregisterClientAsync);
        }

        public void Update()
        {
            for (var i = 0; i < _clients.Count; i++) _clients[i]?.Update?.Invoke();
            while (_actions.TryDequeue(out var action)) action();
        }
        
        private async Task SendClientAsync(HttpListenerContext context)
        {
            if (int.TryParse(context.Request.Url.Query.Substring(1), out var i) && i >= 0 && i < _clients.Count) _clients[i]?.Rcv(JToken.Parse(await context.GetRequestTextAsync().ConfigureAwait(false)).ToObject<Msg>());
            await context.SetResponseTextAsync(null).ConfigureAwait(false);
        }
        
        private async Task PollClientAsync(HttpListenerContext context)
        {
            if (int.TryParse(context.Request.Url.Query.Substring(1), out var i) && i >= 0 && i < _clients.Count)
            {
                var client = _clients[i];
                if (client is null) return;
                var msg = await client.Snd(() => client.Waiting = true);
                await context.SetResponseTextAsync(JToken.FromObject(msg).ToString());
            }
            else await context.SetResponseTextAsync(null, statusCode: 403).ConfigureAwait(false);
        }

        private async Task UnregisterClientAsync(HttpListenerContext context)
        {
            if (int.TryParse(context.Request.Url.Query.Substring(1), out var i) && i >= 0 && i < _clients.Count)
            {
                var client = _clients[1];
                _clients[i] = null;
                if (client is object)
                {
                    client.Connected = false;
                    client.Snd(new Msg() {Name = "quit"});
                    client.Rcv(new Msg() {Name = "quit"});
                }
            }
            else await context.SetResponseTextAsync(null, statusCode: 403).ConfigureAwait(false);
        }

        private async Task AcceptClientAsync(HttpListenerContext context)
        {
            var num = _clients.Count;
            var client = new Client();
            lock (_clients) _clients.Add(client);
            await context.SetResponseTextAsync(num.ToString()).ConfigureAwait(false);


            Func<object, object, JObject> propDiff = null;
            Dictionary<string, Tuple<Type, Action<object>>> actionsDelegates = null;
            ILocoWrapperBase loco = null;
            object stateCurrent = null;
            object stateOther = null;

            client.Update = UpdateState;

            while (client.Connected) HandleCommand(await client.Rcv());
            
            async void HandleCommand(Msg msg)
            {
                try
                {
                    await HandleCommandInner(msg);
                }
                catch (Exception ex)
                {
#if BepInEx
                    Logger.LogError(ex);
#else
                    Console.WriteLine(ex);
#endif
                }
            }

            Task HandleCommandInner(Msg msg)
            {
                switch (msg.Name)
                {
                    case "pair":
                        return PickLoco();
                    case "unpair":
                        return Unpair();
                    case "action":
                        try
                        {
                            var actionData = actionsDelegates[(string) msg.Json["name"]];
                            var data = msg.Json["data"]?.ToObject(actionData.Item1);
                            QueueAction(() => actionData.Item2(data));
                        }
                        catch (Exception ex)
                        {
#if BepInEx
                            Logger.LogError(ex);
#else
                            Console.WriteLine(ex);
#endif
                        }

                        return CompletedTask;
                }

                return CompletedTask;
            }

            Task Send(string cmd, object data = null)
            {
                return SendJson(cmd, data is null ? null : JToken.FromObject(data));
            }

            Task SendJson(string cmd, JToken data)
            {
                client.Snd(new Msg {Name = cmd, Json = data});
                return CompletedTask;
            }

            Task Unpair()
            {
                loco = null;
                stateCurrent = null;
                stateOther = null;
                actionsDelegates = null;
                propDiff = null;
                return CompletedTask;
            }

            Task PickLoco()
            {
                var current = PlayerManager.Car;

                if (!current) return Send("error", "not in a car");
                var controller = current.GetComponent<LocoControllerBase>();
                if (!controller) return Send("error", "not in a loco");
                switch (controller)
                {
                    case LocoControllerShunter locoControllerShunter:
                        SetTargetLoco(new LocoShunter(locoControllerShunter));
                        break;
                    default:
                        SetTargetLoco(new LocoBase(controller));
                        break;
                }

                return Send("paired");
            }

            void SetTargetLoco<TState, TActions>(Loco<TState, TActions> locoWrapper) where TState : BaseLocoState, new() where TActions : BaseLocoActions, new()
            {
                TActions actions;
                if (loco != null)
                {
                    actions = new TActions();
                    locoWrapper.GetActions(actions);
                    var dele = MakeActionDelegates(actions);
                    foreach (var pair in dele)
                        if (actionsDelegates.TryGetValue(pair.Key, out var tuple) && tuple.Item1 == pair.Value.Item1)
                            actionsDelegates[pair.Key] = Tuple.Create(tuple.Item1, tuple.Item2 + pair.Value.Item2);

                    return;
                }

                loco = locoWrapper;
                actions = new TActions();
                var state = new TState();
                stateCurrent = state;
                stateOther = new TState();
                locoWrapper.GetState(state);
                locoWrapper.GetActions(actions);

                actionsDelegates = MakeActionDelegates(actions);

                {
                    var newObj = Expression.Parameter(typeof(object), "newObj");
                    var oldObj = Expression.Parameter(typeof(object), "oldObj");
                    var newState = Expression.Parameter(typeof(TState), "newState");
                    var oldState = Expression.Parameter(typeof(TState), "oldState");
                    var result = Expression.Parameter(typeof(JObject), "result");
                    var expression = Expression.Lambda<Func<object, object, JObject>>(Expression.Block(new[] {newState, oldState, result},
                            new Expression[]
                                {
                                    Expression.Assign(newState,
                                        Expression.Convert(newObj, typeof(TState))),
                                    Expression.Assign(oldState,
                                        Expression.Convert(oldObj, typeof(TState))),
                                    Expression.Assign(result,
                                        Expression.New(typeof(JObject).GetConstructor(new Type[0])))
                                }.Concat(typeof(TState).GetProperties()
                                    .Select(prop =>
                                        Expression.IfThen(
                                            Expression.Not(
                                                Expression.Call(
                                                    Expression.Property(null, typeof(EqualityComparer<>).MakeGenericType(prop.PropertyType).GetProperty("Default")),
                                                    typeof(EqualityComparer<>).MakeGenericType(prop.PropertyType).GetMethod("Equals", new[] {prop.PropertyType, prop.PropertyType}),
                                                    Expression.Property(newState, prop),
                                                    Expression.Property(oldState, prop)
                                                )),
                                            Expression.Call(result,
                                                typeof(JObject).GetProperty("Item", typeof(JToken), new[] {typeof(string)}).SetMethod,
                                                Expression.Constant(prop.Name),
                                                Expression.Call(
                                                    typeof(JToken).GetMethod("FromObject", new[] {typeof(object)}),
                                                    Expression.Convert(Expression.Property(newState, prop), typeof(object)))))))
                                .Concat(new Expression[]
                                {
                                    result
                                })),
                        newObj, oldObj);
                    propDiff = expression.Compile();
                }
                Send("init-state", stateCurrent);
                Send("action-list", actionsDelegates.Select(a => new {name = a.Key, type = a.Value.Item1.ToString()}));
            }

            Dictionary<string, Tuple<Type, Action<object>>> MakeActionDelegates<TActions>(TActions actions)
            {
                var actionsList = typeof(TActions).GetProperties()
                    .Where(prop => prop.PropertyType == typeof(Action) || prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Action<>))
                    .Select(prop =>
                    {
                        var val = (Delegate)prop.GetValue(actions);
                        if (val is null) return default;
                        if (prop.PropertyType == typeof(Action))
                            return new { prop.Name, type = typeof(void), action = new Action<object>(obj => ((Action)val)()) };
                        var param = Expression.Parameter(typeof(object));
                        return new
                        {
                            prop.Name,
                            type = prop.PropertyType.GenericTypeArguments[0],
                            action = Expression.Lambda<Action<object>>(
                                Expression.Call(
                                    Expression.Constant(val, prop.PropertyType),
                                    prop.PropertyType.GetMethod("Invoke"),
                                    Expression.Convert(param, prop.PropertyType.GenericTypeArguments[0])),
                                param).Compile()
                        };
                    })
                    .ToList();
                return actionsList.ToDictionary(a => a.Name, a => Tuple.Create(a.type, a.action));
            }

            void UpdateState()
            {
                if (loco is null || propDiff is null) return;
                if (!client.Waiting) return;

                var tmp = stateOther;
                stateOther = stateCurrent;
                stateCurrent = tmp;
                loco.GetState(stateCurrent);
                var diff = propDiff(stateCurrent, stateOther);
                if (diff is object && diff.Count != 0)
                {
                    SendJson("update-state", diff);
                    client.Waiting = false;
                }
            }
        }

        private class Client
        {
            public Action Update;
            public bool Connected = true;
            public bool Waiting;
            private readonly Queue<Msg> rcv = new Queue<Msg>();
            private readonly SemaphoreSlim semRcv = new SemaphoreSlim(0);
            private readonly SemaphoreSlim semSnd = new SemaphoreSlim(0);
            private readonly Queue<Msg> snd = new Queue<Msg>();

            public async Task<Msg> Rcv()
            {
                await semRcv.WaitAsync();
                return rcv.Dequeue();
            }

            public void Rcv(Msg msg)
            {
                rcv.Enqueue(msg);
                semRcv.Release();
            }

            public async Task<Msg> Snd(Action empty)
            {
                if (semSnd.CurrentCount == 0) empty?.Invoke();
                await semSnd.WaitAsync();
                return snd.Dequeue();
            }

            public void Snd(Msg msg)
            {
                snd.Enqueue(msg);
                semSnd.Release();
            }
        }

        private class Msg
        {
            public string Name { get; set; }
            public JToken Json { get; set; }
        }

        internal interface ILocoWrapperBase
        {
            void GetState(object state);
            void GetActions(object actions);
        }
    }

    public partial class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr GetEnvironmentStrings();

        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CreateProcess
        (
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            CreateProcessFlags dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] StartupInfo lpStartupInfo,
            out ProcessInformation lpProcessInformation
        );

        [Flags]
        public enum LogonFlags
        {
            LOGON_WITH_PROFILE = 0x00000001,
            LOGON_NETCREDENTIALS_ONLY = 0x00000002
        }

        [StructLayout(LayoutKind.Sequential)]
        public class StartupInfo
        {
            public int cb = 0;
            public IntPtr lpReserved = IntPtr.Zero;
            public IntPtr lpDesktop = IntPtr.Zero; // MUST be Zero
            public IntPtr lpTitle = IntPtr.Zero;
            public int dwX = 0;
            public int dwY = 0;
            public int dwXSize = 0;
            public int dwYSize = 0;
            public int dwXCountChars = 0;
            public int dwYCountChars = 0;
            public int dwFillAttribute = 0;
            public int dwFlags = 0;
            public short wShowWindow = 0;
            public short cbReserved2 = 0;
            public IntPtr lpReserved2 = IntPtr.Zero;
            public IntPtr hStdInput = IntPtr.Zero;
            public IntPtr hStdOutput = IntPtr.Zero;
            public IntPtr hStdError = IntPtr.Zero;

            public StartupInfo()
            {
                this.cb = Marshal.SizeOf(this);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ProcessInformation
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [Flags]
        public enum CreateProcessFlags : uint
        {
            DEBUG_PROCESS = 0x00000001,
            DEBUG_ONLY_THIS_PROCESS = 0x00000002,
            CREATE_SUSPENDED = 0x00000004,
            DETACHED_PROCESS = 0x00000008,
            CREATE_NEW_CONSOLE = 0x00000010,
            NORMAL_PRIORITY_CLASS = 0x00000020,
            IDLE_PRIORITY_CLASS = 0x00000040,
            HIGH_PRIORITY_CLASS = 0x00000080,
            REALTIME_PRIORITY_CLASS = 0x00000100,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SHARED_WOW_VDM = 0x00001000,
            CREATE_FORCEDOS = 0x00002000,
            BELOW_NORMAL_PRIORITY_CLASS = 0x00004000,
            ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000,
            INHERIT_PARENT_AFFINITY = 0x00010000,
            INHERIT_CALLER_PRIORITY = 0x00020000,
            CREATE_PROTECTED_PROCESS = 0x00040000,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
            PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000,
            PROCESS_MODE_BACKGROUND_END = 0x00200000,
            CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
            CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NO_WINDOW = 0x08000000,
            PROFILE_USER = 0x10000000,
            PROFILE_KERNEL = 0x20000000,
            PROFILE_SERVER = 0x40000000,
            CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000,
        }

        [Flags]
        public enum DuplicateOptions : uint
        {
            DUPLICATE_CLOSE_SOURCE = 0x00000001,
            DUPLICATE_SAME_ACCESS = 0x00000002
        }
    }
}