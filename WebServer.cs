using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace DvRemoteRemote
{
    public static class WebServer
    {
        private static List<WebPage> _pages;

        public static void RegisterRequestHandler(Func<HttpListenerContext, bool> handler)
        {
            object instance = null;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetExportedTypes())
                    {
                        if (type.Name != "HttpServer") continue;
                        if (!type.GetEvents().Any(f => f.Name == "HandleRequest" && f.EventHandlerType == typeof(Func<HttpListenerContext, bool>))) continue;
                        var instanceField = type.GetFields().SingleOrDefault(f => f.IsStatic && f.Name == "Instance" && f.FieldType == type);
                        if (instanceField is null) continue;
                        instance = instanceField.GetValue(null);
                        if (instance is object) goto hasInstance;
                    }
                }
                catch { }
            }

#pragma warning disable 618
            instance = HttpServer.Instance = Activator.CreateInstance<HttpServer>();
#pragma warning restore 618

            hasInstance:
            
            var handlerEvent = instance.GetType().GetEvents().Single(f => f.Name == "HandleRequest" && f.EventHandlerType == typeof(Func<HttpListenerContext, bool>));
            if (handlerEvent is null)
            {
                Console.WriteLine("Unable to find HandleRequest event");
                return;
            }
            lock (handlerEvent) handlerEvent.AddEventHandler(instance, handler);
        }

        public static WebPage RegisterPage(string prefix, string name)
        {
            var page = new WebPage(prefix) {Name = name};

            if (_pages == null && Interlocked.CompareExchange(ref _pages, new List<WebPage>() {page}, null) is null) RegisterRequestHandler(PagesHandler);
            else lock(_pages) _pages.Add(page);
            RegisterRequestHandler(page.Handler);
            return page;
        }

        private static bool PagesHandler(HttpListenerContext context)
        {
            if (context.Request.Url.AbsolutePath == "/")
            {
                List<WebPage> pages;
                lock (_pages) pages = _pages.ToList();
                context.SetResponseTextAsync(string.Concat(pages.Select(p => $"<a href='{p.Prefix}/'>{p.Name ?? p.Prefix}</a>")), "text/html");
                return true;
            }
            return false;
        }
    }

    public class WebPage
    {
        public WebPage(string prefix)
        {
            Prefix = string.IsNullOrWhiteSpace(prefix) ? "" : "/" + prefix.Trim('/');
            _prefix = Prefix + "/";
        }

        private string _prefix;
        public string Prefix { get; }
        public string Name { get; set; }
        [method: MethodImpl(MethodImplOptions.Synchronized)]
        public event Func<HttpListenerContext, Uri, bool> HandleRequest;

        internal bool Handler(HttpListenerContext context)
        {
            if (!context.Request.Url.AbsolutePath.StartsWith(_prefix)) return false;

            var uri = new UriBuilder(context.Request.Url){Path = context.Request.Url.AbsolutePath.Substring(Prefix.Length)}.Uri;

            var actions = HandleRequest?.GetInvocationList().Cast<Func<HttpListenerContext, Uri, bool>>();
            if (actions is object)
                foreach (var action in actions)
                    if (action.Invoke(context, uri))
                        return true;
            return false;
        }

        public void Register(string path, Action<HttpListenerContext, Uri> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!string.Equals(uri.AbsolutePath, path, StringComparison.InvariantCultureIgnoreCase)) return false;
                handler(context, uri);
                return true;
            };
        }

        public void Register(string path, Action<HttpListenerContext> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!string.Equals(uri.AbsolutePath, path, StringComparison.InvariantCultureIgnoreCase)) return false;
                handler(context);
                return true;
            };
        }

        public void Register(string path, Func<HttpListenerContext, Uri, Task> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!string.Equals(uri.AbsolutePath, path, StringComparison.InvariantCultureIgnoreCase)) return false;
                handler(context, uri);
                return true;
            };
        }

        public void Register(string path, Func<HttpListenerContext, Task> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!string.Equals(uri.AbsolutePath, path, StringComparison.InvariantCultureIgnoreCase)) return false;
                handler(context);
                return true;
            };
        }

        public void Register(string path, Func<HttpListenerContext, Uri, bool> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!string.Equals(uri.AbsolutePath, path, StringComparison.InvariantCultureIgnoreCase)) return false;
                return handler(context, uri);
            };
        }
        
        public void Register(string path, Func<HttpListenerContext, bool> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!string.Equals(uri.AbsolutePath, path, StringComparison.InvariantCultureIgnoreCase)) return false;
                return handler(context);
            };
        }
        
        public void Register(Regex path, Action<HttpListenerContext, Uri> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!path.IsMatch(uri.AbsolutePath)) return false;
                handler(context, uri);
                return true;
            };
        }
        
        public void Register(Regex path, Func<HttpListenerContext, Uri, Task> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!path.IsMatch(uri.AbsolutePath)) return false;
                handler(context, uri);
                return true;
            };
        }
        
        public void Register(Regex path, Func<HttpListenerContext, Uri, bool> handler)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (handler is null) throw new ArgumentNullException(nameof(handler));
            HandleRequest += (context, uri) =>
            {
                if (!path.IsMatch(uri.AbsolutePath)) return false;
                return handler(context, uri);
            };
        }
    }

    [Obsolete("Do not use HttpServer, use the WebServer class instead")]
    public class HttpServer
    {
        [Obsolete("This field may or may not be initialized")]
        public static HttpServer Instance;

        private HttpListener _listener;

        [Obsolete("Do not instantiate HttpServer, use the WebServer class instead", true)]
        public HttpServer()
        {
            Listen();
        }
        
        public event Func<HttpListenerContext, bool> HandleRequest;
        
        private async void Listen()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://*:6886/");
            _listener.Start();
            while (true)
            {
                try
                {
                    Process(await _listener.GetContextAsync().ConfigureAwait(false));
                }
                catch (Exception ex) when (!(ex is InvalidOperationException))
                {

                }
            }
        }

        private async void Process(HttpListenerContext context)
        {
            try
            {
                var actions = HandleRequest?.GetInvocationList().Cast<Func<HttpListenerContext, bool>>();
                if (actions is object)
                    foreach (var action in actions)
                        if (action.Invoke(context))
                            return;

                await context.SetResponseTextAsync(null, null, 404).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                try
                {
                    Console.WriteLine(ex);
                    await context.SetResponseTextAsync(ex.ToString(), statusCode: 500).ConfigureAwait(false);
                }
                catch
                {
                }
            }
        }
    }

    public static class ResponseContextExtender
    {
        public static Task<bool> SetResponseTextAsync(this HttpListenerContext context, string content, string contentType = "text/plain", int statusCode = 200) =>
            context.Response.SetTextAsync(content, contentType, statusCode);

        public static async Task<bool> SetTextAsync(this HttpListenerResponse response, string content, string contentType = "text/plain", int statusCode = 200)
        {
            response.StatusCode = statusCode;
            if (contentType is object)
                response.AddHeader("content-type", contentType);
            if (content is object)
            {
                var data = Encoding.UTF8.GetBytes(content);
                response.ContentLength64 = data.Length;
                await response.OutputStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            }

            await response.OutputStream.FlushAsync().ConfigureAwait(false);
            response.OutputStream.Dispose();
            return true;
        }

        public static Task<bool> SetResponseStreamAsync(this HttpListenerContext context, Stream content, string contentType = "text/plain", int statusCode = 200) =>
            context.Response.SetStreamAsync(content, contentType, statusCode);
        public static async Task<bool> SetStreamAsync(this HttpListenerResponse response, Stream content, string contentType = "text/plain", int statusCode = 200)
        {
            response.StatusCode = statusCode;
            if (contentType is object)
                response.AddHeader("content-type", contentType);
            if (content is object)
            {
                try
                {
                    response.ContentLength64 = content.Length;
                }
                catch
                {
                    response.SendChunked = true;
                }

                await content.CopyToAsync(response.OutputStream).ConfigureAwait(false);
                content.Dispose();
            }

            await response.OutputStream.FlushAsync().ConfigureAwait(false);
            response.OutputStream.Dispose();
            return true;
        }

        public static Task<string> GetRequestTextAsync(this HttpListenerContext context) => context.Request.GetTextAsync();

        public static async Task<string> GetTextAsync(this HttpListenerRequest request)
        {
            var ms = new MemoryStream();
            await request.InputStream.CopyToAsync(ms).ConfigureAwait(false);
            return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int) ms.Length);
        }
    }
}