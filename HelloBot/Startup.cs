﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio EchoBot v4.15.0

using HelloBot.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure.Blobs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HelloBot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            //services.AddTransient<IBot, Bots.EchoBot>();
            services.AddTransient<IBot, Bots.GreetingBot>();

            ConfigureState(services);
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
            app.UseWebSockets();
            //app.UseHttpsRedirection();
            app.UseMvc();
        }

        private void ConfigureState(IServiceCollection services)
        {
            // Create the storage we'll be using for User and Conversation state.(Memory is great for testing purposes)
            services.AddSingleton<IStorage, MemoryStorage>();

            // For production and permanent storage we can use Azure blob storage as IStorageProvider
            var connectionString = Configuration.GetValue<string>("BlobStorageConnectionString");
            const string container = "mystatedata";
            var blobsStorage = new BlobsStorage(connectionString, container);
            services.AddSingleton<IStorage>(blobsStorage);

            // User state 
            services.AddSingleton<UserState>();

            // Conversation state 
            services.AddSingleton<ConversationState>();

            // Create an instance of the state service 
            services.AddSingleton<StateService>();
        }
    }
}