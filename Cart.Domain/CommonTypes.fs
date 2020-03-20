module ShoppingCart.CommonTypes

type Product = {
    id : string
    qty : int
    productName : string
    price : float
    countryOfOrigin : string    
}

type CartCreated = {
    id : string
    userID : string
    userName : string
    shippingAddress : string
    products : Product[]
    finalPrice : float
}

type ProductAdded = {
    cartID : string
    product : Product
}

type ProductRemoved = {
    cartID : string
    product : Product
}

type ProductQtyIncreased = {
    cartID : string
    product : Product
}

type ProductQtyDecreased = {
    cartID : string
    product : Product
}

type Coupon = {
    isApplied : bool
    percentageDiscount : float
}
type CouponApplied = {
    cartID : string
    promoCoupon : Coupon 
}

type CartCleared = {
    cartID : string
    products : Product[]
    finalPrice : float
}

type CheckedOut = {
    cartID : string
    isCheckedOut : bool 
}

type CartEvents =
    | CartCreated of CartCreated
    | ProductAdded of ProductAdded
    | ProductRemoved of ProductRemoved
    | ProductQtyIncreased of ProductQtyIncreased
    | ProductQtyDecreased of ProductQtyDecreased
    | CouponApplied of CouponApplied
    | CartCleared of CartCleared
    | CheckedOut of CheckedOut
