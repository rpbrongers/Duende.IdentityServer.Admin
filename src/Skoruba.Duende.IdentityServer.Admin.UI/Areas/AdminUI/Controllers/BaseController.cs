// Copyright (c) Jan Škoruba. All Rights Reserved.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Skoruba.Duende.IdentityServer.Admin.UI.Configuration.Constants;
using Skoruba.Duende.IdentityServer.Admin.UI.Helpers;

namespace Skoruba.Duende.IdentityServer.Admin.UI.Areas.AdminUI.Controllers
{
    [Area(CommonConsts.AdminUIArea)]
    public class BaseController : Controller
    {
        private readonly ILogger<BaseController> _logger;

        public BaseController(ILogger<BaseController> logger)
        {
            _logger = logger;
        }

        protected void SuccessNotification(string message, string title = "")
        {
            CreateNotification(NotificationHelpers.AlertType.Success, message, title);
        }

        protected void CreateNotification(NotificationHelpers.AlertType type, string message, string title = "")
        {
            var toast = new NotificationHelpers.Alert
            {
                Type = type,
                Message = message,
                Title = title
            };

            var alerts = new List<NotificationHelpers.Alert>();

            if (TempData.ContainsKey(NotificationHelpers.NotificationKey))
            {
                alerts = JsonSerializer.Deserialize<List<NotificationHelpers.Alert>>(TempData[NotificationHelpers.NotificationKey].ToString());
                TempData.Remove(NotificationHelpers.NotificationKey);
            }

            alerts.Add(toast);

            var alertJson = JsonSerializer.Serialize(alerts);

            TempData.Add(NotificationHelpers.NotificationKey, alertJson);
        }

        protected void GenerateNotifications()
        {
            if (!TempData.ContainsKey(NotificationHelpers.NotificationKey)) return;
            ViewBag.Notifications = TempData[NotificationHelpers.NotificationKey];
            TempData.Remove(NotificationHelpers.NotificationKey);
        }
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            GenerateNotifications();

            base.OnActionExecuting(context);
        }

        public override ViewResult View(object model)
        {
            GenerateNotifications();

            return base.View(model);
        }
    }
}