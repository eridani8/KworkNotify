﻿using KworkNotify.Core.Interfaces;
using KworkNotify.Core.Kwork;
using KworkNotify.Core.Service.Cache;
using KworkNotify.Core.Service.Database;
using KworkNotify.Core.Service.Types;
using KworkNotify.Core.Telegram.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotBase;
using TelegramBotBase.Builder;
using TelegramBotBase.Commands;

namespace KworkNotify.Core.Telegram;

public class TelegramService : IHostedService
{
    private readonly IMongoContext _context;
    private readonly IAppCache _redis;
    private readonly IOptions<AppSettings> _settings;
    private readonly BotBase _bot;

    public TelegramService(ITelegramData data, IMongoContext context, KworkService kworkService, IAppCache redis, IOptions<AppSettings> settings)
    {
        _context = context;
        _redis = redis;
        _settings = settings;
        var serviceCollection = new ServiceCollection()
            .AddSingleton<IMongoContext, MongoContext>(_ => (context as MongoContext)!)
            .AddSingleton<IAppCache, AppCache>(_ => (redis as AppCache)!)
            .AddSingleton<IAppSettings, AppSettings>(_ => settings.Value);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        _bot = BotBaseBuilder.Create()
            .WithAPIKey(data.Token)
            .DefaultMessageLoop()
            .WithServiceProvider<StartForm>(serviceProvider)
            .NoProxy()
            .CustomCommands(c =>
            {
                c.Add("start", "Запуск");
            })
            .NoSerialization()
            .UseRussian()
            .UseThreadPool()
            .Build();
        
        data.Bot = _bot;
        
        kworkService.AddedNewProject += KworkServiceOnAddedNewProject;
    }
    private async Task KworkServiceOnAddedNewProject(object? sender, KworkProjectArgs e)
    {
        var project = await _context.Projects
            .Find(p => p.Id == e.KworkProject.Id)
            .FirstOrDefaultAsync();

        if (project == null)
        {
            List<TelegramUser> usersToNotify;

            if (_settings.Value.IsDebug)
            {
                var mainId = _settings.Value.AdminIds.First();
                usersToNotify = await _context.Users
                    .Find(u => u.Id == mainId)
                    .ToListAsync();
            }
            else
            {
                usersToNotify = await _context.Users
                    .Find(u => u.SendUpdates)
                    .ToListAsync();
            }
            
            var projectText = e.KworkProject.ToString()
                .Replace("|SET_URL_HERE|", $"{_settings.Value.SiteUrl}/projects/{e.KworkProject.Id}/view");
            
            foreach (var user in usersToNotify)
            {
                try
                {
                    // Log.ForContext<TelegramService>().Information("[{Device}] send project '{ProjectName}'", user.Id, e.KworkProject.Name);
                    // // await _bot.Client.TelegramClient.SendTextMessageAsync(new ChatId(user.Id), projectText, disableWebPagePreview: true);
                    // var projectForm = new ProjectForm(_context, _redis, _settings.Value, e.KworkProject);
                    // var r = _bot;
                    // await _context.Users.UpdateOneAsync(u => u.Id == user.Id,
                    //     Builders<TelegramUser>.Update.Inc(u => u.ReceivedMessages, 1));
                }
                catch (Exception exception)
                {
                    Log.ForContext<TelegramService>().Error(exception, "The message was not sent to [{Device}]", user.Id);
                }
                finally
                {
                    await Task.Delay(500);
                }
            }

            if (!_settings.Value.IsDebug)
            {
                await _context.Projects.InsertOneAsync(e.KworkProject);
            }
        }
        else
        {
            await _context.Projects.UpdateOneAsync(
                p => p.Id == e.KworkProject.Id,
                Builders<KworkProject>.Update
                    .Set(p => p.GetWantsActiveCount, e.KworkProject.GetWantsActiveCount)
            );
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bot.UploadBotCommands();
        await _bot.Start(); 
        Log.ForContext<TelegramService>().Information("Telegram bot started");
        await _bot.Client.TelegramClient.SendTextMessageAsync(new ChatId(_settings.Value.AdminIds.First()), "Бот запущен", cancellationToken: cancellationToken);
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _bot.Stop();
        Log.ForContext<TelegramService>().Information("Telegram bot stopped");
        await _bot.Client.TelegramClient.SendTextMessageAsync(new ChatId(_settings.Value.AdminIds.First()), "Бот остановлен", cancellationToken: cancellationToken);
    }
}