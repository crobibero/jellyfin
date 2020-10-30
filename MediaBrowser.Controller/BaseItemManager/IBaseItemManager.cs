﻿using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.BaseItemManager
{
    /// <summary>
    /// The <c>BaseItem</c> manager.
    /// </summary>
    public interface IBaseItemManager
    {
        /// <summary>
        /// Is metadata fetcher enabled.
        /// </summary>
        /// <param name="baseItem">The base item.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <param name="name">The metadata fetcher name.</param>
        /// <returns><c>true</c> if metadata fetcher is enabled, else false.</returns>
        bool IsMetadataFetcherEnabled(BaseItem baseItem, LibraryOptions libraryOptions, string name);

        /// <summary>
        /// Is image fetcher enabled.
        /// </summary>
        /// <param name="baseItem">The base item.</param>
        /// <param name="libraryOptions">The library options.</param>
        /// <param name="name">The image fetcher name.</param>
        /// <returns><c>true</c> if image fetcher is enabled, else false.</returns>
        bool IsImageFetcherEnabled(BaseItem baseItem, LibraryOptions libraryOptions, string name);

        /// <summary>
        /// Adds a studio to the item.
        /// </summary>
        /// <param name="baseItem">The base item.</param>
        /// <param name="studioName">The studio name to add.</param>
        void AddStudio(BaseItem baseItem, string studioName);

        /// <summary>
        /// Sets the studio names to the provided enumerable.
        /// </summary>
        /// <param name="baseItem">The base item.</param>
        /// <param name="studioNames">The enumerable of studio names.</param>
        void SetStudios(BaseItem baseItem, IEnumerable<string> studioNames);

        /// <summary>
        /// Adds a genre to the item.
        /// </summary>
        /// <param name="baseItem">The base item.</param>
        /// <param name="genreName">The genre name to add.</param>
        void AddGenre(BaseItem baseItem, string genreName);

        /// <summary>
        /// Do whatever refreshing is necessary when the filesystem pertaining to this item has changed.
        /// </summary>
        /// <param name="baseItemId">The base item id.</param>
        void ChangedExternally(Guid baseItemId);
    }
}