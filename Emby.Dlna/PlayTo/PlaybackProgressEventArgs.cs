#pragma warning disable CS1591

using System;

namespace Emby.Dlna.PlayTo
{
    public class PlaybackProgressEventArgs : EventArgs
    {
        public UBaseObject MediaInfo { get; set; }
    }
}
