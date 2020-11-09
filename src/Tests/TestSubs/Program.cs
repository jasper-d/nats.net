using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IntegrationTests;
using NATS.Client;
using Xunit;

namespace TestSubs
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            await Task.Delay(15_000);
            Console.WriteLine("Starting...");
            await TestWatingSyncSubscription();
            //await TestWaitingAsyncSubscription();
        }

        public static async Task TestWatingSyncSubscription()
        {
            var Context = new SubscriptionsSuiteContext();
            var cts = new CancellationTokenSource();
            
            using (NATSServer.CreateFastAndVerify(Context.Server1.Port))
            {
                using (IConnection c = Context.OpenConnection(Context.Server1.Port))
                {
                    var subs = new ISyncSubscription[1000];
                    var messages = new Task<Msg>[1000];
                    for (var i = 0; i < 1000; i++)
                    {
                        var s = c.SubscribeSync($"foo-{i}");
                        subs[i] = s;
                    }
                    c.Flush();
                    for (var i = 0; i < subs.Length; i++)
                    {
                        messages[i] = subs[i].NextMessageAsync(cts.Token).AsTask();
                    }
                    cts.CancelAfter(TimeSpan.FromMinutes(2));
                    await Task.Delay(15_000, cts.Token);
                    for (var i = 0; i < subs.Length; i++)
                    {
                        c.Publish(subs[i].Subject, BitConverter.GetBytes(i));
                    }

                    c.Flush();
                    await Task.WhenAll(messages);
                    
                    // Sanity check
                    foreach (var message in messages)
                    {
                        var msg = await message;
                        Assert.Equal(msg.ArrivalSubscription.Subject, $"foo-{BitConverter.ToInt32(msg.Data, 0)}");
                    }
                }
            }
        }
        
        public static async Task TestWaitingAsyncSubscription()
        {
            var Context = new SubscriptionsSuiteContext();
            var messages = new Task<Msg>[1000];
            var cts = new CancellationTokenSource();
            
            using (NATSServer.CreateFastAndVerify(Context.Server1.Port))
            {
                using (IConnection c = Context.OpenConnection(Context.Server1.Port))
                {
                    var subs = new IAsyncSubscription[1000];
                    for (var i = 0; i < 1000; i++)
                    {
                        var s = c.SubscribeAsync($"foo-{i}");
                        var tcs = new TaskCompletionSource<Msg>(TaskCreationOptions.RunContinuationsAsynchronously);
                        s.MessageHandler += (sender, arg) =>
                        {
                            tcs.TrySetResult(arg.Message);
                        };
                        subs[i] = s;
                        messages[i] = tcs.Task;
                    }

                    c.Flush();
                    for (var i = 0; i < subs.Length; i++)
                    {
                        subs[i].Start();
                    }
                    cts.CancelAfter(TimeSpan.FromMinutes(2));
                    await Task.Delay(15_000, cts.Token);
                    for (var i = 0; i < subs.Length; i++)
                    {
                        c.Publish(subs[i].Subject, BitConverter.GetBytes(i));
                    }
                    c.Flush();
                    await Task.WhenAll(messages);
                    
                    // Sanity check
                    foreach (var message in messages)
                    {
                        var msg = await message;
                        Assert.Equal(msg.ArrivalSubscription.Subject, $"foo-{BitConverter.ToInt32(msg.Data, 0)}");
                    }
                }
            }
        }
    }
}