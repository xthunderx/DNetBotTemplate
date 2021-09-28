// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
// URL For Receiving a Event:  https://<NGROK_ID>.ngrok.io/api/NewMessageEvent
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using DNetUtils.Entities;
using Microsoft.Azure.EventGrid;
using System.Collections.Generic;
using Newtonsoft.Json;
using DNetBotFunctions.Clients;
using StackExchange.Redis;

namespace DNetBotFunctions.Events.Messaging
{
    public class NewMessage
    {
        private IConnectionMultiplexer _redis;

        public NewMessage(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        private EventGridClient eventGridClient = new EventGridClient(new TopicCredentials(System.Environment.GetEnvironmentVariable("EventGridKey")));

        [FunctionName("NewMessage")]
        public void Run([EventGridTrigger]EventGridEvent eventGridEvent, ILogger log)
        {
            log.LogInformation("New Message Event Triggered On: {Topic} with the Subject: {Subject}", eventGridEvent.Topic.ToString(), eventGridEvent.Subject.ToString());
            //var message = JsonConvert.DeserializeObject<DiscordMessage>(eventGridEvent.Data.ToString());

            var status = _redis.GetStatus();
            IDatabase cache = _redis.GetDatabase();
            var cachedMessage = cache.StringGet(eventGridEvent.Data.ToString());

            cache.StringSet("TestKey", eventGridEvent.Data.ToString());

            var test = cache.StringGet("guild:765590399066439700");

            if(!cachedMessage.HasValue)
            {
                log.LogError("Could not retrieve cached value");
                return;
            }

            var message = new DiscordMessage(cachedMessage);

            if (message.Content.StartsWith("!ping"))
            {
                var returnMessage = new DiscordMessage();
                returnMessage.ChannelId = message.ChannelId;
                returnMessage.Content = "pong!";        

                var myEvent = new EventGridEvent(eventGridEvent.Id, "ReturnMessage", returnMessage, "DNetBot.Message.ReturnMessage", DateTime.Now, "1.0", "returnmessage");
                string eventGridHostname = new Uri(System.Environment.GetEnvironmentVariable("EventGridDomain")).Host;
                try
                {
                    eventGridClient.PublishEventsAsync(eventGridHostname, new List<EventGridEvent>() { myEvent }).Wait();
                }
                catch(Exception ex)
                {
                    log.LogError(ex.ToString());
                }
            }
        }
    }
}
