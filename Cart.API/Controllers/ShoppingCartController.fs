namespace Cart.API.Controllers

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging
open Propulsion.Kafka
open Serilog
open ShoppingCart.Domain
open ShoppingCart.CommonTypes

[<ApiController>]
[<Route("[controller]")>]
type ShoppingCartController (logger : ILogger<ShoppingCartController>) =
    inherit ControllerBase()


    let produce (message : CartEvents) =
        let producer = Producer(Log.Logger, "Jet.com", Uri "localhost:9092", "shopping-carts")
        let res = producer.ProduceAsync("CART", FsCodec.NewtonsoftJson.Serdes.Serialize(message)) |> Async.RunSynchronously
                
        printfn "Producing cart event: %A" res.Value
        
    [<HttpGet>]
    member __.Get() : string =
        "get"
        
    
    [<Produces("application/json")>]    
    [<HttpPost; Route("/api/cartCreated")>]
    member __.CreateCart([<FromBody>] x : ShoppingCart.CommonTypes.CartCreated) = async {
        x |> CartEvents.CartCreated |> produce 
        return __.Ok()   
    }
    
    [<Produces("application/json")>]    
    [<HttpPost; Route("/api/productAdded")>]
    member __.AddProduct([<FromBody>] x : ProductAdded) = async {
        x |> CartEvents.ProductAdded |> produce 
        return __.Ok()   
    }
    
    [<Produces("application/json")>]    
    [<HttpPost; Route("/api/productRemoved")>]
    member __.RemoveProduct([<FromBody>] x : ProductRemoved) = async {
        x |> CartEvents.ProductRemoved |> produce 
        return __.Ok()   
    }
    
    [<Produces("application/json")>]    
    [<HttpPost; Route("/api/productQtyIncreased")>]
    member __.IncreaseProductQty([<FromBody>] x : ProductQtyIncreased) = async {
        x |> CartEvents.ProductQtyIncreased |> produce 
        return __.Ok()   
    }
    
    [<Produces("application/json")>]    
    [<HttpPost; Route("/api/productQtyDecreased")>]
    member __.ProductQtyDecreased([<FromBody>] x : ProductQtyDecreased) = async {
        x |> CartEvents.ProductQtyDecreased |> produce 
        return __.Ok()   
    }
    
    [<Produces("application/json")>]    
    [<HttpPost; Route("/api/couponApplied")>]
    member __.ApplyCoupon([<FromBody>] x : CouponApplied) = async {
        x |> CartEvents.CouponApplied |> produce 
        return __.Ok()  
    }
    
    [<Produces("application/json")>]    
    [<HttpPut; Route("/api/cartCleared")>]
    member __.ClearCart([<FromBody>] x : CartCleared) = async {
        x |> CartEvents.CartCleared |> produce 
        return __.Ok() 
    }
    
    [<Produces("application/json")>]    
    [<HttpPost; Route("/api/checkedOut")>]
    member __.Checkout([<FromBody>] x : CheckedOut) = async {
        x |> CartEvents.CheckedOut |> produce 
        return __.Ok()  
    }

    
