module Cart.Consumer.CartIngester

open ShoppingCart
open ShoppingCart.Domain
open ShoppingCart.CommonTypes

module Ingester =
    let toCartCreated (message : CommonTypes.CartCreated) : ShoppingCart.Events.CartCreated  =
        {
            cartID = message.id
            user = {
                userID = message.userID
                name = message.userName
                shippingAddress = message.shippingAddress
            }
            items = [||]
            totalPrice = 0.0
        }
    
        
    let toProductRemoved (message : CommonTypes.ProductRemoved) : ShoppingCart.Events.ProductRemoved  =
        {
            item = {
                productId = message.product.id
                name = message.product.productName
                quantity = message.product.qty
                price = message.product.price
            }
        }
     
        
    let toProductAdded (message : CommonTypes.ProductAdded) : ShoppingCart.Events.ProductAdded  =
        {
            item = {
                productId = message.product.id
                name = message.product.productName
                quantity = message.product.qty
                price = message.product.price
            }
        }
     
        
    let toProductQtyIncreased (message : CommonTypes.ProductQtyIncreased) : ShoppingCart.Events.ProductQtyIncreased  =
        {
            item = {
                productId = message.product.id
                name = message.product.productName
                quantity = message.product.qty
                price = message.product.price
            }
        }
      
        
    let toProductQtyDecreased (message : CommonTypes.ProductQtyDecreased) : ShoppingCart.Events.ProductQtyDecreased  =
        {
            item = {
                productId = message.product.id
                name = message.product.productName
                quantity = message.product.qty
                price = message.product.price
            }
        }
      
        
    let toCartCleared (message : CommonTypes.CartCleared) : ShoppingCart.Events.CartCleared  =
        {
            items = [||]
            totalPrice = 0.0
        }
     
        
    let toCheckedOut (message : CommonTypes.CheckedOut) : ShoppingCart.Events.CheckedOut  =
        {
            checkedOut = true
        }
      
        
    let toCouponApplied (message : CommonTypes.CouponApplied) : ShoppingCart.Events.CouponApplied =
        {
            coupon = {
                applied = message.promoCoupon.isApplied
                discount = message.promoCoupon.percentageDiscount
            }
        }
        

    let ingestCartCreated (persist: ShoppingCart.Events.CartCreated -> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.CartCreated) : Async<ShoppingCart.IngestionResult> =
        message |> toCartCreated |> persist
     
        
    let ingestProductAdded (persist: string -> ShoppingCart.Events.ProductAdded-> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.ProductAdded) : Async<ShoppingCart.IngestionResult> =
        message |> toProductAdded |> persist message.cartID
      
        
    let ingestProductRemoved (persist: string -> ShoppingCart.Events.ProductRemoved-> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.ProductRemoved) : Async<ShoppingCart.IngestionResult> =
        message |> toProductRemoved |> persist message.cartID
     
        
    let ingestProductQtyIncreased (persist: string -> ShoppingCart.Events.ProductQtyIncreased-> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.ProductQtyIncreased) : Async<ShoppingCart.IngestionResult> =
        message |> toProductQtyIncreased |> persist message.cartID
       
        
    let ingestProductQtyDecreased (persist: string -> ShoppingCart.Events.ProductQtyDecreased-> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.ProductQtyDecreased) : Async<ShoppingCart.IngestionResult> =
        message |> toProductQtyDecreased |> persist message.cartID
       
        
    let ingestCartCleared (persist: string -> ShoppingCart.Events.CartCleared-> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.CartCleared) : Async<ShoppingCart.IngestionResult> =
        message |> toCartCleared |> persist message.cartID
       
        
    let ingestCheckedOut (persist: string -> ShoppingCart.Events.CheckedOut-> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.CheckedOut) : Async<ShoppingCart.IngestionResult> =
        message |> toCheckedOut |> persist message.cartID
       
        
    let ingestCouponApplied (persist: string -> ShoppingCart.Events.CouponApplied-> Async<ShoppingCart.IngestionResult>) (message : CommonTypes.CouponApplied) : Async<ShoppingCart.IngestionResult> =
        message |> toCouponApplied |> persist message.cartID
        
