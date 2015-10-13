﻿using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie
{
  internal class GetMoviesBasic : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(MovieAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Movies found");

      var output = new List<WebMovieBasic>();

      foreach (var item in items)
      {
        SingleMediaItemAspect mediaAspect = MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata);
        SingleMediaItemAspect movieAspect = MediaItemAspect.GetAspect(item.Aspects, MovieAspect.Metadata);
        SingleMediaItemAspect videoAspect = MediaItemAspect.GetAspect(item.Aspects, VideoAspect.Metadata);
        SingleMediaItemAspect importerAspect = MediaItemAspect.GetAspect(item.Aspects, ImporterAspect.Metadata);

        WebMovieBasic webMovieBasic = new WebMovieBasic();
        webMovieBasic.ExternalId = new List<WebExternalId>();
        string TMDBId;
        MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_TMDB, ExternalIdentifierAspect.TYPE_MOVIE, out TMDBId);
        if (TMDBId != null)
        {
          webMovieBasic.ExternalId.Add(new WebExternalId
          {
            Site = "TMDB",
            Id = TMDBId
          });
        }
        string ImdbId;
        MediaItemAspect.TryGetExternalAttribute(item.Aspects, ExternalIdentifierAspect.SOURCE_IMDB, ExternalIdentifierAspect.TYPE_MOVIE, out ImdbId);
        if (ImdbId != null)
        {
          webMovieBasic.ExternalId.Add(new WebExternalId
          {
            Site = "IMDB",
            Id = ImdbId
          });
        }

        webMovieBasic.Runtime = (int)movieAspect[MovieAspect.ATTR_RUNTIME_M];
        webMovieBasic.IsProtected = false; //??
        var rating = movieAspect.GetAttributeValue(MovieAspect.ATTR_TOTAL_RATING);
        if (rating != null)
          webMovieBasic.Rating = Convert.ToSingle(rating);
        webMovieBasic.Type = WebMediaType.Movie;
        webMovieBasic.Watched = ((int)(mediaAspect[MediaAspect.ATTR_PLAYCOUNT] ?? 0) > 0);
        //webTvEpisodeBasic.Path = ;
        //webTvEpisodeBasic.Artwork = ;
        //webMovieBasic.Year =;
        webMovieBasic.DateAdded = (DateTime)importerAspect[ImporterAspect.ATTR_DATEADDED];
        webMovieBasic.Id = item.MediaItemId.ToString();
        webMovieBasic.PID = 0;
        webMovieBasic.Title = (string)movieAspect[MovieAspect.ATTR_MOVIE_NAME];
        var movieActors = (HashSet<object>)videoAspect[VideoAspect.ATTR_ACTORS];
        if (movieActors != null)
        {
          webMovieBasic.Actors = new List<WebActor>();
          foreach (var actor in movieActors)
          {
            webMovieBasic.Actors.Add(new WebActor
            {
              Title = actor.ToString(),
              PID = 0
            });
          }
        }
        var movieGenres = (HashSet<object>)videoAspect[VideoAspect.ATTR_GENRES];
        if (movieGenres != null)
          webMovieBasic.Genres = movieGenres.Cast<string>().ToList();

        output.Add(webMovieBasic);
      }

      // sort and filter
      HttpParam httpParam = request.Param;
      string sort = httpParam["sort"].Value;
      string order = httpParam["order"].Value;
      string filter = httpParam["filter"].Value;
      if (sort != null && order != null)
      {
        WebSortField webSortField = (WebSortField)JsonConvert.DeserializeObject(sort, typeof(WebSortField));
        WebSortOrder webSortOrder = (WebSortOrder)JsonConvert.DeserializeObject(order, typeof(WebSortOrder));

        output = output.Filter(filter).SortWebMovieBasic(webSortField, webSortOrder).ToList();
      }
      else
        output = output.Filter(filter).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}