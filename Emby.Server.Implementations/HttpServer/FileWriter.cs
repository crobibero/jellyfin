using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.IO;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.HttpServer
{
    public class FileWriter : IHttpResult
    {
        private ILogger Logger { get; set; }
        public IFileSystem FileSystem { get; }

        private string RangeHeader { get; set; }
        private bool IsHeadRequest { get; set; }

        private long RangeStart { get; set; }
        private long RangeEnd { get; set; }
        private long RangeLength { get; set; }
        public long TotalContentLength { get; set; }

        public Action OnComplete { get; set; }
        public Action OnError { get; set; }
        private static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        public List<Cookie> Cookies { get; private set; }

        public FileShareMode FileShare { get; set; }

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();
        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Headers => _options;

        public string Path { get; set; }

        public FileWriter(string path, string contentType, string rangeHeader, ILogger logger, IFileSystem fileSystem)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            Path = path;
            Logger = logger;
            FileSystem = fileSystem;
            RangeHeader = rangeHeader;

            Headers[HeaderNames.ContentType] = contentType;

            TotalContentLength = fileSystem.GetFileInfo(path).Length;
            Headers[HeaderNames.AcceptRanges] = "bytes";

            if (string.IsNullOrWhiteSpace(rangeHeader))
            {
                StatusCode = HttpStatusCode.OK;
            }
            else
            {
                StatusCode = HttpStatusCode.PartialContent;
                SetRangeValues();
            }

            FileShare = FileShareMode.Read;
            Cookies = new List<Cookie>();
        }

        /// <summary>
        /// Sets the range values.
        /// </summary>
        private void SetRangeValues()
        {
            var requestedRange = RequestedRanges[0];

            // If the requested range is "0-", we can optimize by just doing a stream copy
            if (!requestedRange.Value.HasValue)
            {
                RangeEnd = TotalContentLength - 1;
            }
            else
            {
                RangeEnd = requestedRange.Value.Value;
            }

            RangeStart = requestedRange.Key;
            RangeLength = 1 + RangeEnd - RangeStart;

            // Content-Length is the length of what we're serving, not the original content
            var lengthString = RangeLength.ToString(UsCulture);
            var rangeString = $"bytes {RangeStart}-{RangeEnd}/{TotalContentLength}";
            Headers[HeaderNames.ContentRange] = rangeString;

            Logger.LogInformation("Setting range response values for {0}. RangeRequest: {1} Content-Length: {2}, Content-Range: {3}", Path, RangeHeader, lengthString, rangeString);
        }

        /// <summary>
        /// The _requested ranges
        /// </summary>
        private List<KeyValuePair<long, long?>> _requestedRanges;
        /// <summary>
        /// Gets the requested ranges.
        /// </summary>
        /// <value>The requested ranges.</value>
        protected List<KeyValuePair<long, long?>> RequestedRanges
        {
            get
            {
                if (_requestedRanges == null)
                {
                    _requestedRanges = new List<KeyValuePair<long, long?>>();

                    // Example: bytes=0-,32-63
                    var ranges = RangeHeader.Split('=')[1].Split(',');

                    foreach (var range in ranges)
                    {
                        var vals = range.Split('-');

                        long start = 0;
                        long? end = null;

                        if (!string.IsNullOrEmpty(vals[0]))
                        {
                            start = long.Parse(vals[0], UsCulture);
                        }
                        if (!string.IsNullOrEmpty(vals[1]))
                        {
                            end = long.Parse(vals[1], UsCulture);
                        }

                        _requestedRanges.Add(new KeyValuePair<long, long?>(start, end));
                    }
                }

                return _requestedRanges;
            }
        }

        private string[] SkipLogExtensions = new string[]
        {
            ".js",
            ".html",
            ".css"
        };

        public async Task WriteToAsync(IResponse response, CancellationToken cancellationToken)
        {
            try
            {
                // Headers only
                if (IsHeadRequest)
                {
                    return;
                }

                var path = Path;

                if (string.IsNullOrWhiteSpace(RangeHeader) || (RangeStart <= 0 && RangeEnd >= TotalContentLength - 1))
                {
                    var extension = System.IO.Path.GetExtension(path);

                    if (extension == null || !SkipLogExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        Logger.LogDebug("Transmit file {0}", path);
                    }

                    //var count = FileShare == FileShareMode.ReadWrite ? TotalContentLength : 0;
                    // TODO not DI friendly lol
                    await response.TransmitFile(path, 0, 0, FileShare, FileSystem, new StreamHelper(), cancellationToken).ConfigureAwait(false);
                    return;
                }
                // TODO not DI friendly lol
                await response.TransmitFile(path, RangeStart, RangeLength, FileShare, FileSystem, new StreamHelper(), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                if (OnComplete != null)
                {
                    OnComplete();
                }
            }
        }

        public string ContentType { get; set; }

        public IRequest RequestContext { get; set; }

        public object Response { get; set; }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get => (HttpStatusCode)Status;
            set => Status = (int)value;
        }

        public string StatusDescription { get; set; }

    }
}
