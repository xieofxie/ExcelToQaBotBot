﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QnABot.Models;
using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Add the HttpClientFactory to be used for the QnAMaker calls.
            services.AddHttpClient();

            // Create the credential provider to be used with the Bot Framework Adapter.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();

            // Create the Bot Framework Adapter with error handling enabled. 
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();


            services.AddSingleton<IStorage>(sp =>
            {
                var config = sp.GetService<IConfiguration>();
                var options = config.GetSection("CosmosDb").Get<CosmosDbStorageOptions>();
                return new CosmosDbStorage(options);
            });

            services.AddSingleton<BotStateSet>(sp =>
            {
                return new BotStateSet();
            });

            services.AddTransient<QnAMakerClient>(sp =>
            {
                var config = sp.GetService<IConfiguration>();
                var key = config.GetSection("SubscriptionKey").Value;
                return new QnAMakerClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = "https://westus.api.cognitive.microsoft.com" };
            });
            services.AddTransient<Knowledgebase>();
            services.AddTransient<Operations>();

            // For creating
            services.AddSingleton<QnAMakerEndpoint>(sp =>
            {
                var config = sp.GetService<IConfiguration>();
                var key = config.GetSection("EndpointKey").Value;
                var host = config.GetSection("Host").Value;
                return new QnAMakerEndpoint
                {
                    EndpointKey = key,
                    Host = host
                };
            });

            services.AddSingleton<QnAModel>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, QnABot>();

            services.AddSingleton<FileHost>();

            services.AddCors(options =>
            {
                options.AddPolicy("Any",
                    builder =>
                    {
                        builder.WithOrigins("*")
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                    });
                options.AddPolicy("Localhost",
                    builder =>
                    {
                        builder.WithOrigins("*")
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                    });
                options.AddPolicy("QnA",
                    builder =>
                    {
                        builder.WithOrigins("*")
                                            .AllowAnyHeader()
                                            .AllowAnyMethod();
                    });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            //app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
