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
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  internal class GetPicturesDetailed : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Images found");

      var output = new List<WebPictureDetailed>();

      foreach (var item in items)
      {
        SingleMediaItemAspect mediaAspect = MediaItemAspect.GetAspect(item.Aspects, MediaAspect.Metadata);
        SingleMediaItemAspect imageAspect = MediaItemAspect.GetAspect(item.Aspects, ImageAspect.Metadata);
        SingleMediaItemAspect importerAspect = MediaItemAspect.GetAspect(item.Aspects, ImporterAspect.Metadata);

        WebPictureDetailed webPictureDetailed = new WebPictureDetailed();

        //webPictureBasic.Categories = imageAspects.GetAttributeValue(ImageAspect);
        //webPictureBasic.DateTaken = imageAspects.GetAttributeValue(ImageAspect.);
        webPictureDetailed.Type = WebMediaType.Picture;
        //webPictureBasic.Artwork;
        webPictureDetailed.DateAdded = (DateTime)imageAspect.GetAttributeValue(ImporterAspect.ATTR_DATEADDED);
        webPictureDetailed.Id = item.MediaItemId.ToString();
        webPictureDetailed.PID = 0;
        //webPictureBasic.Path;
        webPictureDetailed.Title = (string)mediaAspect.GetAttributeValue(MediaAspect.ATTR_TITLE);
        //webPictureDetailed.Rating = imageAspects.GetAttributeValue(ImageAspect.);
        //webPictureDetailed.Author = imageAspects.GetAttributeValue(ImageAspect.);
        //webPictureDetailed.Dpi = imageAspects.GetAttributeValue(ImageAspect.);
        webPictureDetailed.Width = (string)(imageAspect.GetAttributeValue(ImageAspect.ATTR_WIDTH) ?? string.Empty);
        webPictureDetailed.Height = (string)(imageAspect.GetAttributeValue(ImageAspect.ATTR_HEIGHT) ?? string.Empty);
        //webPictureDetailed.Mpixel = imageAspects.GetAttributeValue(ImageAspect.);
        //webPictureDetailed.Copyright;
        webPictureDetailed.CameraModel = (string)(imageAspect.GetAttributeValue(ImageAspect.ATTR_MODEL) ?? string.Empty);
        webPictureDetailed.CameraManufacturer = (string)(imageAspect.GetAttributeValue(ImageAspect.ATTR_MAKE) ?? string.Empty);
        //webPictureDetailed.Comment;
        //webPictureDetailed.Subject;

        output.Add(webPictureDetailed);
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

        output = output.AsQueryable().Filter(filter).SortMediaItemList(webSortField, webSortOrder).ToList();
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