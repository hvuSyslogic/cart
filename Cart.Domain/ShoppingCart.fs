module ShoppingCart.Domain.ShoppingCart


open Serilog

let [<Literal>] Category = "ShoppingCart"
let streamName (shoppingCartID : string) =
    printfn "@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@"
    FsCodec.StreamName.compose Category [shoppingCartID]

module Types =
    type User = {
        userID : string
        name : string
        shippingAddress : string
    }

    type Product = {
        productId : string
        name : string
        quantity : int
        price : float 
    }
    
    type Coupon = {
        applied : bool
        discount : float // in %
    }

    type ShoppingCart = {
        cartID : string 
        user : User
        items : Product[]
        totalPrice : float
        coupon : Coupon option
        checkedOut : bool 
    }
    
module Events =
    open Types
    // create cart | add product | remove product | increase qty | decrease qty | clear cart | apply coupon - BONUS | checkout
    type CartCreated = {
        cartID : string 
        user : User
        items : Product[]
        totalPrice : float
    }
    
    // cons with existing product array 
    type ProductAdded = {
        item : Product 
    }
    
    // filter out this product 
    type ProductRemoved = {
        item : Product 
    }
    
    type ProductQtyIncreased = {
        item : Product 
    }
    
    
    type ProductQtyDecreased = {
        item : Product
    }
    
    
    // total price becomes 0, items is []
    type CartCleared = {
        items : Product[]
        totalPrice : float
    }
    
    // add coupon, if its added when we checkout we will decrease price whatever % 
    type CouponApplied = {
        coupon : Coupon 
    }
    
    // cant do anything after this, finished!
    type CheckedOut = {
        checkedOut : bool
    }
    
    
    type Snapshotted = {snapshot: ShoppingCart}

    
    type Event =
        | CartCreated of CartCreated
        | ProductAdded of ProductAdded
        | ProductRemoved of ProductRemoved
        | ProductQtyIncreased of ProductQtyIncreased
        | ProductQtyDecreased of ProductQtyDecreased
        | CartCleared of CartCleared
        | CouponApplied of CouponApplied
        | CheckedOut of CheckedOut
        | CartSnapshotted of Snapshotted
        interface TypeShape.UnionContract.IUnionContract
    let codec = FsCodec.NewtonsoftJson.Codec.Create<Event>()
    
    
module Fold =
    open Events
    type State = Types.ShoppingCart option
    let initial : State = None
    
    let handleCartCreation (state : State) (event : CartCreated) : State =
        Log.Information("Trying to create cart {@CartCreated}", event)
        match state with 
        | Some _ ->
            Log.Information("This cart has already been created")
            state
        | None ->
            Some(
                {
                    cartID = event.cartID
                    user = event.user
                    items = [||]
                    totalPrice = 0.0
                    coupon = None
                    checkedOut = false
                })       

    let handleProductAdd (state : State) (event : ProductAdded) : State =
        match state with
        | None -> None
        | Some m -> Some ({ m with items = Array.concat [m.items; [|event.item|]]; totalPrice = m.totalPrice + event.item.price })
    
    let handleProductRemove (state : State) (event : ProductRemoved) : State =
        match state with
        | None -> None
        | Some m ->
            let filteredProducts = m.items |> Array.filter (fun item -> item.productId <> event.item.productId)
            let productToRemove = m.items |> Array.find (fun item -> item.productId = event.item.productId)
            Some({m with items = filteredProducts; totalPrice = m.totalPrice - (event.item.price * float(productToRemove.quantity))})
            
    let handleProductQtyIncrease (state : State) (event : ProductQtyIncreased) : State =
        match state with
        | None -> None
        | Some m ->
            let i = m.items |> Array.findIndex (fun item -> item.productId = event.item.productId)
            m.items.[i] <- { m.items.[i] with quantity = m.items.[i].quantity + 1}
            Some ({m with items = m.items; totalPrice = m.totalPrice + m.items.[i].price})
            
    let handleProductQtyDecrease (state : State) (event : ProductQtyDecreased) : State =
        match state with
        | None -> None
        | Some m ->
            let i = m.items |> Array.findIndex (fun item -> item.productId = event.item.productId)
            m.items.[i] <- { m.items.[i] with quantity = m.items.[i].quantity - 1}
            Some ({m with items = m.items; totalPrice = m.totalPrice - m.items.[i].price})
            
    let handleCartClear (state : State) (event : CartCleared) : State =
        match state with
        | Some m -> Some ({ m with items = [||] ; totalPrice = 0.0})
        | None -> None 
        
    let handleCouponApply (state : State) (event : CouponApplied) : State =
        match state with
        | Some m -> Some ({ m with coupon = Some({applied = true; discount = event.coupon.discount}) })
        | None -> None
        
    let handleCheckOut (state : State) (event : CheckedOut) : State =
        match state with
        | Some m ->
            match m.coupon with
            | Some x -> Some ({m with totalPrice = m.totalPrice * (x.discount / 100.00); checkedOut = true})
            | None -> Some ({ m with checkedOut = true })
        | None -> None 
        
    let handleSnapshot (state : State) (event : Snapshotted) : State =
        Log.Information("Snapshot is {@MessageSnapshotted}", event)
        Log.Information("State is {@State}", state)
        event.snapshot |> Some 
        
    let private evolve (state : State) (event) : State =
        Log.Information("State is {@State}", state)
        match event with
        | Events.CartCreated x -> handleCartCreation state x
        | Events.ProductAdded x -> handleProductAdd state x 
        | Events.ProductRemoved x -> handleProductRemove state x
        | Events.ProductQtyIncreased x -> handleProductQtyIncrease state x 
        | Events.ProductQtyDecreased x -> handleProductQtyDecrease state x
        | Events.CartCleared x -> handleCartClear state x 
        | Events.CouponApplied x -> handleCouponApply state x 
        | Events.CheckedOut x -> handleCheckOut state x 
        | Events.CartSnapshotted x -> handleSnapshot state x
    
    let fold : State -> Events.Event seq -> State = Seq.fold evolve
    
    // eqx dump tool - good for looking at snapshots 
    let snapshot state =
        match state with
        | None -> failwithf "Invalid state in snapshot %A" state
        | Some x -> { Events.Snapshotted.snapshot = x } |> Events.CartSnapshotted
        
    let isOrigin = function
        | Events.CartCreated _ -> false
        | Events.CartSnapshotted _ -> true
        | Events.ProductRemoved _ -> false
        | Events.ProductAdded _ -> false
        | Events.ProductQtyIncreased _ -> false
        | Events.ProductQtyDecreased _ -> false
        | Events.CartCleared _ -> false
        | Events.CheckedOut _ -> false // should this be true ? 
        | Events.CouponApplied _ -> false 

  
[<RequireQualifiedAccess>]
type IngestionResult =
    | CartCreated
    | CartAlreadyCreated of Fold.State
    | ProductAdded
    | ProductAlreadyAdded of Fold.State
    | ProductRemoved
    | ProductAlreadyRemoved of Fold.State
    | ProductQtyIncreased
    | ProductQtyDecreased
    | CartCleared
    | CouponApplied
    | CheckedOut
    | CartNotFound 
    | ProductNotFound 
    | ProductQuantityAlreadyAtMinimum of Fold.State
    

// helper function used in several functions below to determine if the array has the product we are trying to add/remove/change qty in it
let findProductIndex (items : Types.Product[]) (product : Types.Product) = items |> Array.tryFindIndex (fun item -> item.productId = product.productId)


// ------------- Spoke to Eoin, he said these function would be a good place to put in my business logic, as i previously had them in the evolve fn
let createCart (event: Events.CartCreated) (state: Fold.State) =
    match state with
    | Some _ ->
        Log.Information("Cart Already Created")
        (IngestionResult.CartAlreadyCreated state, [])
    | None ->
        Log.Information("IngestionResult.CartCreated")
        (IngestionResult.CartCreated, [event |> Events.CartCreated])
        

let addProduct (event: Events.ProductAdded) (state: Fold.State) =
    match state with
    | Some m ->
        match (findProductIndex m.items event.item) with
        | Some _ ->
            Log.Information("Product Already Added")
            (IngestionResult.ProductAlreadyAdded state, [])
        | None ->
            Log.Information("Adding product to cart")
            (IngestionResult.ProductAdded, [event |> Events.ProductAdded])
    | None ->
        Log.Information("IngestionResult.CartNotFound")
        (IngestionResult.CartNotFound, [])
    
let removeProduct (event: Events.ProductRemoved) (state: Fold.State) =
    match state with
    | Some m ->
        match (findProductIndex m.items event.item) with
        | Some _ ->
            Log.Information("Product Removed")
            (IngestionResult.ProductRemoved, [event |> Events.ProductRemoved])
        | None ->
            Log.Information("Product Already Removed")
            (IngestionResult.ProductAlreadyRemoved state, [])
    | None ->
        Log.Information("IngestionResult.CartNotFound")
        (IngestionResult.CartNotFound, [])
        
let increaseQtyForProduct (event: Events.ProductQtyIncreased) (state : Fold.State) =
    match state with
    | Some m ->
        match (findProductIndex m.items event.item) with
        | Some _ ->
            Log.Information("Increased Quantity for product")
            (IngestionResult.ProductQtyIncreased, [event |> Events.ProductQtyIncreased])
        | None ->
            Log.Information("IngestionResult.ProductNotFound")
            (IngestionResult.ProductNotFound, [])
    | None ->
        Log.Information("IngestionResult.CartNotFound")
        (IngestionResult.CartNotFound, [])
        
let decreaseQtyForProduct (event: Events.ProductQtyDecreased) (state : Fold.State) =
    match state with
    | Some m ->
        match (findProductIndex m.items event.item) with
        | Some i ->
            match (m.items.[i].quantity) < 1 with
            | true ->
                Log.Information("Quantity is at mimimum")
                (IngestionResult.ProductQuantityAlreadyAtMinimum state, [])
            | false ->  
                Log.Information("Decreased Quantity for product")
                (IngestionResult.ProductQtyDecreased, [event |> Events.ProductQtyDecreased])
        | None ->
            Log.Information("IngestionResult.ProductNotFound")
            (IngestionResult.ProductNotFound, [])
    | None ->
        Log.Information("IngestionResult.CartNotFound")
        (IngestionResult.CartNotFound, [])
        
let clearCart (event: Events.CartCleared) (state : Fold.State) =
    match state with
    | Some _ ->
        Log.Information("Cart was cleared.")
        (IngestionResult.CartCleared, [event |> Events.CartCleared])
    | None ->
        Log.Information("IngestionResult.CartNotFound")
        (IngestionResult.CartNotFound, [])
        
        
let applyCoupon (event: Events.CouponApplied) (state : Fold.State) =
    match state with
    | Some _ ->
        Log.Information("Coupon was applied.")
        (IngestionResult.CouponApplied, [event |> Events.CouponApplied])
    | None ->
        Log.Information("IngestionResult.CartNotFound")
        (IngestionResult.CartNotFound, [])
        
        
let checkout (event: Events.CheckedOut) (state : Fold.State) =
    match state with
    | Some _ ->
        Log.Information("Cart was checked out.")
        (IngestionResult.CheckedOut, [event |> Events.CheckedOut])
    | None ->
        Log.Information("IngestionResult.CartNotFound")
        (IngestionResult.CartNotFound, [])
        
        
        
type Manager internal (resolve : string -> Equinox.Stream<Events.Event, Fold.State>) =
    member __.Read cartId : Async<Fold.State> =
        let stream = resolve cartId
        stream.Query(id)
        
    member __.CreateCart (info : Events.CartCreated) =
        let stream = resolve info.cartID
        stream.Transact(createCart info)
 
            
    member __.AddProduct cartId (event : Events.ProductAdded) =
        let stream = resolve cartId
        stream.Transact(addProduct event)
        
    member __.RemoveProduct cartId (event : Events.ProductRemoved) =
        let stream = resolve cartId
        stream.Transact(removeProduct event)
        
    member __.IncreaseProductQty cartId (event : Events.ProductQtyIncreased) =
        let stream = resolve cartId
        stream.Transact(increaseQtyForProduct event)
        
    member __.DecreaseProductQty cartId (event : Events.ProductQtyDecreased) =
        let stream = resolve cartId
        stream.Transact(decreaseQtyForProduct event)

    member __.ClearCart cartId (event : Events.CartCleared) =
        let stream = resolve cartId
        stream.Transact(clearCart event)
        
    member __.ApplyCoupon cartId (event : Events.CouponApplied) =
        let stream = resolve cartId
        stream.Transact(applyCoupon event)
        
    member __.CheckOut cartId (event : Events.CheckedOut) =
        let stream = resolve cartId
        stream.Transact(checkout event) 
    

        
let create resolver =
    printfn "@@@@@@@@@"
    let resolve messageId =
        let stream = resolver (streamName messageId)
        Equinox.Stream(Serilog.Log.ForContext<Manager>(), stream, maxAttempts = 3)
    
    Manager(resolve)
    

module Cosmos =
    let accessStrategy = Equinox.Cosmos.AccessStrategy.Snapshot (Fold.isOrigin, Fold.snapshot)

    let private resolver (context, cache) =
        let cacheStrategy = Equinox.Cosmos.CachingStrategy.SlidingWindow (cache, System.TimeSpan.FromMinutes 20.)
        Equinox.Cosmos.Resolver(context, Events.codec, Fold.fold, Fold.initial, cacheStrategy, accessStrategy).Resolve
    let create (context, cache) = create (resolver (context, cache))