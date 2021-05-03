#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.Tmdb.Movies;

namespace MediaBrowser.Providers.Plugins.Tmdb.Music
{
    public class TmdbMusicVideoProvider : IRemoteMetadataProvider<MusicVideo, MusicVideoInfo>
    {
        public Task<MetadataResult<MusicVideo>> GetMetadata(MusicVideoInfo info, CancellationToken cancellationToken)
        {
            return TmdbMovieProvider.Current.GetItemMetadata<MusicVideo>(info, cancellationToken);
        }

        public Task<IEnumerable<RemoteSearchResult>> GetSearchResults(MusicVideoInfo searchInfo, CancellationToken cancellationToken)
        {
            return Task.FromResult((IEnumerable<RemoteSearchResult>)new List<RemoteSearchResult>());
        }

        public string Name => TmdbMovieProvider.Current.Name;

        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}