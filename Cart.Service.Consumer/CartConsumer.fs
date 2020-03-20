// Learn more about F# at http://fsharp.org

open Equinox.Cosmos

open Cart.Consumer
open Propulsion.Streams
open System
open Serilog
open ShoppingCart.Domain.ShoppingCart
open ShoppingCart.CommonTypes
open Cart.Consumer.CartIngester

type Args =
    {
        connectionString: string
        database : string
        container: string
        cache: string
        broker: string
        topic: string
        consumerGroup: string
        cartID : string 
    }

    
    
let connect (args : Args) =
   async {
       let connectionString = args.connectionString
       let (Discovery.UriAndKey (endpointUri,_) as discovery) = Discovery.FromConnectionString connectionString

       let! connection = (Connector((TimeSpan.FromSeconds 5.), 1, (TimeSpan.FromSeconds 5.), Log.Logger, mode=Equinox.Cosmos.ConnectionMode.Direct)).Connect("jet", discovery)
       return Context(connection, args.database, args.container)         
   }
   
  
      
let start args =  
    let context = connect args |> Async.RunSynchronously
    let cache = Equinox.Cache(args.cache, sizeMb=10)
    let manager = Cosmos.create(context,cache)

    let decode (data : Byte[]) : string = System.Text.Encoding.UTF8.GetString data
    
    // depending on event type we map to a different function in the ingester
    let consume (event : string) =
        let des : CartEvents = event |> FsCodec.NewtonsoftJson.Serdes.Deserialize<_> 
        match des with
        | CartEvents.CartCreated x -> Ingester.ingestCartCreated manager.CreateCart x
        | CartEvents.ProductAdded x -> Ingester.ingestProductAdded manager.AddProduct x 
        | CartEvents.ProductRemoved x -> Ingester.ingestProductRemoved manager.RemoveProduct x
        | CartEvents.ProductQtyIncreased x -> Ingester.ingestProductQtyIncreased manager.IncreaseProductQty  x
        | CartEvents.ProductQtyDecreased x -> Ingester.ingestProductQtyDecreased manager.DecreaseProductQty  x
        | CartEvents.CheckedOut x -> Ingester.ingestCheckedOut manager.CheckOut  x
        | CartEvents.CouponApplied x -> Ingester.ingestCouponApplied manager.ApplyCoupon  x
        | CartEvents.CartCleared x -> Ingester.ingestCartCleared manager.ClearCart  x
        
    let handle(_, span) : Async<unit> =
        span.events
        |> Array.map (fun x -> decode x.Data)
        |> Seq.map consume
        |> Async.Sequential
        |> Async.Ignore
    
    let config =
        FsKafka.KafkaConsumerConfig.Create("jet", Uri args.broker, [args.topic], args.consumerGroup)
        
    let stats = Scheduling.StreamSchedulerStats(Log.Logger, (TimeSpan.FromMinutes 1.), (TimeSpan.FromMinutes 5.))      
    let sequencer = Propulsion.Kafka.Core.StreamKeyEventSequencer()
    Propulsion.Kafka.StreamsConsumer.Start(Log.Logger, config, sequencer.ToStreamEvent, handle, 5, stats, idleDelay = TimeSpan.FromSeconds 1.)

    
[<EntryPoint>]
let main argv =
    
    let args = {
        connectionString = "AccountEndpoint=https://dev-thor-equinox.documents.azure.com:443/;AccountKey=HigZsQI7rwi1df5qD13e4sOsIdJFyfVnKy8BbnnKXPscVVkwzFgGEnys9kBj2ESDPgHWftEdArbtXYzLSEYySQ==;"
        database = "onboarding-test"
        container = "shopping-carts"
        cache = "test"
        broker = "localhost:9092"
        topic = "shopping-carts"
        consumerGroup = "cart-consumers"
        cartID = "C-X003"
    }
    
    Serilog.Log.Logger <- LoggerConfiguration()
                              .Destructure.FSharpTypes()
                              .WriteTo.Console()
                              .CreateLogger()
                              
    
    use consumer = start args
    consumer.AwaitCompletion() |> Async.RunSynchronously
    
    0