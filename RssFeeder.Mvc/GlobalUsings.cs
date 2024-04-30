﻿global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Text;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;
global using System.Xml;
global using MediatR;
global using Microsoft.ApplicationInsights;
global using Microsoft.AspNetCore.Authorization;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.AspNetCore.Mvc;
global using Microsoft.AspNetCore.Mvc.Authorization;
global using Microsoft.AspNetCore.Rewrite;
global using Microsoft.Azure.Cosmos;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Identity.Web;
global using Microsoft.Identity.Web.UI;
global using Microsoft.Net.Http.Headers;
global using Microsoft.SyndicationFeed;
global using Microsoft.SyndicationFeed.Atom;
global using Newtonsoft.Json;
global using RssFeeder.Mvc.Models;
global using RssFeeder.Mvc.Queries;
global using RssFeeder.Mvc.Services;
global using Serilog;
global using ZiggyCreatures.Caching.Fusion;
